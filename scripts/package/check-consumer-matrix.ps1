#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
  Builds the clean-feed consumer matrix for every package and TFM.

.DESCRIPTION
  Copies the packed packages into an empty temporary feed and creates package-only consumer
  projects (no ProjectReference). It verifies package asset selection from project.assets.json,
  builds representative host scenarios on net10.0/net11.0, installs the tool package, rejects
  analyzer runtime leakage, and runs a negative missing-package test.

.PARAMETER PackagesPath
  Directory containing the 25 .nupkg files.

.PARAMETER Version
  Package version to reference.

.PARAMETER TargetFrameworks
  Consumer target frameworks. Defaults to net10.0 and net11.0.

.PARAMETER OutputDirectory
  Matrix report directory. Defaults to artifacts/consumer-matrix/<version>.

.PARAMETER Keep
  Keep the temporary workspace for inspection.

.EXAMPLE
  pwsh scripts/package/check-consumer-matrix.ps1 -PackagesPath artifacts/release/1.0.0-preview.3/packages
#>
param(
    [string]$PackagesPath = "artifacts/release/1.0.0-preview.3/packages",
    [string]$Version = "1.0.0-preview.3",
    [string[]]$TargetFrameworks = @("net10.0", "net11.0"),
    [string]$OutputDirectory = "",
    [switch]$Keep
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$resolvedPackages = (Resolve-Path -LiteralPath $PackagesPath).Path
$frameworks = @($TargetFrameworks | ForEach-Object { $_ -split '[,;]' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot "artifacts/consumer-matrix/$Version"
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$packages = @(Get-ChildItem -LiteralPath $resolvedPackages -Filter '*.nupkg' -File | Sort-Object Name)
if ($packages.Count -ne 25) {
    throw "Expected 25 packages in '$resolvedPackages', found $($packages.Count)."
}

$budget = Get-Content (Join-Path $repositoryRoot 'scripts/package/package-dependency-budget.json') -Raw | ConvertFrom-Json
$allPackageIds = @($budget.packages.PSObject.Properties.Name)
$toolPackageId = 'Jeninnet.Validation.Migration'
$analyzerPackageId = 'Jeninnet.Validation.Analyzers'
$libraryPackageIds = @($allPackageIds | Where-Object { $_ -ne $toolPackageId })

$root = Join-Path ([System.IO.Path]::GetTempPath()) ("jeninnet-consumer-matrix-" + [Guid]::NewGuid().ToString("N"))
$feed = Join-Path $root 'feed'
$toolPath = Join-Path $root 'tool'
New-Item -ItemType Directory -Force -Path $feed | Out-Null
New-Item -ItemType Directory -Force -Path $toolPath | Out-Null
$packages | Copy-Item -Destination $feed

$nugetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$($feed.Replace('\', '/'))" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@
Set-Content -LiteralPath (Join-Path $root 'NuGet.config') -Value $nugetConfig -Encoding utf8

$matrix = [System.Collections.Generic.List[object]]::new()
$scenarioRows = [System.Collections.Generic.List[object]]::new()
$failures = [System.Collections.Generic.List[string]]::new()

function Invoke-Dotnet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function New-Project {
    param(
        [string]$Directory,
        [string]$ProjectName,
        [string[]]$PackageIds,
        [switch]$AspNetCoreFramework
    )

    New-Item -ItemType Directory -Force -Path $Directory | Out-Null

    $references = ($PackageIds | ForEach-Object {
        $private = if ($_ -eq $analyzerPackageId) { ' PrivateAssets="all"' } else { '' }
        '    <PackageReference Include="{0}" Version="{1}"{2} />' -f $_, $Version, $private
    }) -join "`r`n"

    $framework = if ($AspNetCoreFramework) {
        '  <ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App" /></ItemGroup>'
    } else { '' }

    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>$($frameworks -join ';')</TargetFrameworks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
$framework
  <ItemGroup>
$references
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $Directory "$ProjectName.csproj") -Encoding utf8

    Set-Content -LiteralPath (Join-Path $Directory 'Consumer.cs') -Value @"
namespace ConsumerMatrix;

public sealed class Consumer
{
}
"@ -Encoding utf8
}

function Get-SelectedAsset {
    param(
        [string]$AssetsPath,
        [string]$Framework,
        [string]$PackageId
    )

    $assets = Get-Content -LiteralPath $AssetsPath -Raw | ConvertFrom-Json
    $target = $assets.targets.$Framework

    if ($null -eq $target) {
        return $null
    }

    $entry = $target.PSObject.Properties |
        Where-Object { $_.Name.StartsWith("$PackageId/", [StringComparison]::Ordinal) } |
        Select-Object -First 1

    if ($null -eq $entry) {
        return $null
    }

    foreach ($kind in @('runtime', 'compile', 'analyzers', 'build')) {
        $assetsProperty = $entry.Value.PSObject.Properties[$kind]

        if ($null -ne $assetsProperty) {
            $selected = $assetsProperty.Value.PSObject.Properties.Name | Select-Object -First 1

            if ($selected) {
                return "$kind/$selected"
            }
        }
    }

    return 'declared'
}

try {
    $allPackagesDirectory = Join-Path $root 'AllPackages'
    New-Project -Directory $allPackagesDirectory -ProjectName 'AllPackages' -PackageIds $libraryPackageIds -AspNetCoreFramework

    $allProject = Join-Path $allPackagesDirectory 'AllPackages.csproj'

    foreach ($framework in $frameworks) {
        Invoke-Dotnet @('restore', $allProject, '--configfile', (Join-Path $root 'NuGet.config'), '--force')
        Invoke-Dotnet @('build', $allProject, '-c', 'Release', '-f', $framework, '--no-restore')

        $assetsPath = Join-Path $allPackagesDirectory "obj/project.assets.json"

        foreach ($packageId in $libraryPackageIds) {
            $asset = Get-SelectedAsset -AssetsPath $assetsPath -Framework $framework -PackageId $packageId
            $status = if ($asset) { 'Passed' } else { 'Failed' }
            $matrix.Add([pscustomobject]@{
                Package = $packageId
                Framework = $framework
                Asset = $asset
                Status = $status
            })

            if (-not $asset) {
                $failures.Add("$packageId $framework has no selected asset.")
            }
        }

        $assetsJson = Get-Content -LiteralPath $assetsPath -Raw
        if ($assetsJson -match '(?i)Roslynator|Microsoft\.CodeAnalysis' ) {
            $failures.Add("Analyzer dependency leaked into the $framework consumer graph.")
        }
    }

    $scenarios = @(
        @{ Name = 'CoreFluentAttribute'; Packages = @('Jeninnet.Validation', 'Jeninnet.Validation.Abstractions', $analyzerPackageId); Framework = $false },
        @{ Name = 'AspNetCoreMinimalApi'; Packages = @('Jeninnet.Validation', 'Jeninnet.Validation.AspNetCore', $analyzerPackageId); Framework = $true },
        @{ Name = 'MvcProblemDetails'; Packages = @('Jeninnet.Validation', 'Jeninnet.Validation.AspNetCore'); Framework = $true },
        @{ Name = 'BlazorEditContext'; Packages = @('Jeninnet.Validation', 'Jeninnet.Validation.Blazor'); Framework = $true },
        @{ Name = 'MediatR'; Packages = @('Jeninnet.Validation', 'Jeninnet.Validation.MediatR'); Framework = $false },
        @{ Name = 'OpenApiSwashbuckle'; Packages = @('Jeninnet.Validation', 'Jeninnet.Validation.OpenApi', 'Jeninnet.Validation.OpenApi.Swashbuckle'); Framework = $false },
        @{ Name = 'OpenApiNSwag'; Packages = @('Jeninnet.Validation', 'Jeninnet.Validation.OpenApi', 'Jeninnet.Validation.OpenApi.NSwag'); Framework = $false },
        @{ Name = 'JsonSchema'; Packages = @('Jeninnet.Validation', 'Jeninnet.Validation.JsonSchema'); Framework = $false },
        @{ Name = 'ClientRules'; Packages = @('Jeninnet.Validation', 'Jeninnet.Validation.ClientRules'); Framework = $false }
    )

    foreach ($scenario in $scenarios) {
        $projectName = "$($scenario.Name)Scenario"
        $directory = Join-Path $root "scenarios/$projectName"
        New-Project -Directory $directory -ProjectName $projectName -PackageIds $scenario.Packages -AspNetCoreFramework:$scenario.Framework
        $project = Join-Path $directory "$projectName.csproj"
        Invoke-Dotnet @('restore', $project, '--configfile', (Join-Path $root 'NuGet.config'), '--force')
        Invoke-Dotnet @('build', $project, '-c', 'Release', '--no-restore')

        foreach ($framework in $frameworks) {
            $scenarioRows.Add([pscustomobject]@{
                Scenario = $scenario.Name
                Framework = $framework
                Status = 'Passed'
            })
        }
    }

    Invoke-Dotnet @('tool', 'install', '--tool-path', $toolPath, '--add-source', $feed, '--version', $Version, $toolPackageId)
    $toolExecutable = Join-Path $toolPath $(if ($IsWindows) { 'jeninnet.exe' } else { 'jeninnet' })
    $toolProbe = Join-Path $root 'tool-probe'
    New-Item -ItemType Directory -Force -Path $toolProbe | Out-Null
    & $toolExecutable migrate analyze $toolProbe --format json *> $null
    if ($LASTEXITCODE -ne 0) {
        throw 'The installed migration tool did not run.'
    }

    foreach ($framework in $frameworks) {
        $matrix.Add([pscustomobject]@{
            Package = $toolPackageId
            Framework = $framework
            Asset = 'tool-install'
            Status = 'Passed'
        })
    }

    $badDirectory = Join-Path $root 'BadConsumer'
    New-Project -Directory $badDirectory -ProjectName 'BadConsumer' -PackageIds @('Jeninnet.Validation')
    $badProject = Join-Path $badDirectory 'BadConsumer.csproj'
    (Get-Content -LiteralPath $badProject -Raw).Replace("Version=`"$Version`"", 'Version="[0.0.0-missing]"') |
        Set-Content -LiteralPath $badProject -Encoding utf8
    & dotnet restore $badProject --configfile (Join-Path $root 'NuGet.config') *> $null
    $negativePassed = $LASTEXITCODE -ne 0

    if (-not $negativePassed) {
        $failures.Add('The negative missing-package consumer unexpectedly restored.')
    }

    $samples = @(
        [pscustomobject]@{ Project = 'Jeninnet.Validation.Sample.Authentication'; Classification = 'Excluded — out-of-solution demo uses ProjectReference, not package consumption.' },
        [pscustomobject]@{ Project = 'Jeninnet.Validation.Sample.DtoVsDomain'; Classification = 'Excluded — out-of-solution demo uses ProjectReference, not package consumption.' },
        [pscustomobject]@{ Project = 'Jeninnet.Validation.Sample.ECommerce'; Classification = 'Excluded — out-of-solution demo uses ProjectReference, not package consumption.' },
        [pscustomobject]@{ Project = 'Jeninnet.Validation.Sample.ErrorHandling'; Classification = 'Excluded — out-of-solution demo uses ProjectReference, not package consumption.' },
        [pscustomobject]@{ Project = 'Jeninnet.Validation.Sample.FullDemo'; Classification = 'Excluded — out-of-solution demo uses ProjectReference, not package consumption.' },
        [pscustomobject]@{ Project = 'templates/jeninnet-validator'; Classification = 'Deferred to P3-003 template publication; not a package consumer.' }
    )

    if ($failures.Count -gt 0) {
        throw ($failures -join [Environment]::NewLine)
    }

    $matrix | Sort-Object Framework, Package | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath (Join-Path $OutputDirectory 'matrix.json') -Encoding utf8
    $scenarioRows | Sort-Object Framework, Scenario | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath (Join-Path $OutputDirectory 'scenarios.json') -Encoding utf8
    $samples | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath (Join-Path $OutputDirectory 'samples.json') -Encoding utf8

    $report = [System.Text.StringBuilder]::new()
    [void]$report.AppendLine("# Consumer matrix — $Version")
    [void]$report.AppendLine()
    [void]$report.AppendLine("Packages: $($allPackageIds.Count); frameworks: $($frameworks -join ', '); feed: isolated temporary directory.")
    [void]$report.AppendLine()
    [void]$report.AppendLine('| Package | Framework | Selected asset | Status |')
    [void]$report.AppendLine('|---|---|---|---|')
    foreach ($row in ($matrix | Sort-Object Framework, Package)) {
        [void]$report.AppendLine("| ``$($row.Package)`` | $($row.Framework) | ``$($row.Asset)`` | $($row.Status) |")
    }
    [void]$report.AppendLine()
    [void]$report.AppendLine('## Scenarios')
    [void]$report.AppendLine()
    [void]$report.AppendLine('| Scenario | Framework | Status |')
    [void]$report.AppendLine('|---|---|---|')
    foreach ($row in ($scenarioRows | Sort-Object Framework, Scenario)) {
        [void]$report.AppendLine("| $($row.Scenario) | $($row.Framework) | $($row.Status) |")
    }
    [void]$report.AppendLine()
    [void]$report.AppendLine("Negative missing-package test: **$(if ($negativePassed) { 'Passed' } else { 'Failed' })**.")
    [void]$report.AppendLine()
    [void]$report.AppendLine('## Out-of-solution samples and template')
    [void]$report.AppendLine()
    [void]$report.AppendLine('| Project | Classification |')
    [void]$report.AppendLine('|---|---|')
    foreach ($sample in $samples) {
        [void]$report.AppendLine("| ``$($sample.Project)`` | $($sample.Classification) |")
    }
    [void]$report.AppendLine()
    [void]$report.AppendLine('No consumer uses ProjectReference; every package comes from the temporary feed.')
    $report.ToString() | Set-Content -LiteralPath (Join-Path $OutputDirectory 'matrix.md') -Encoding utf8

    Write-Host "Consumer matrix passed: $($allPackageIds.Count) packages × $($frameworks.Count) frameworks; $($scenarios.Count) scenarios."
}
finally {
    if (-not $Keep) {
        Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
    }
    else {
        Write-Host "Kept consumer matrix workspace at $root"
    }
}
