#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Exercises the PublicApiAnalyzers gate in a throwaway project.

.DESCRIPTION
    Creates a temporary class library with a committed baseline and proves the four contract
    transitions that protect consumers:

      1. A clean baseline builds.
      2. Adding a public API without a baseline fails with RS0016.
      3. Recording the new API in PublicAPI.Unshipped.txt builds.
      4. Removing an API while its baseline entry remains fails with RS0017.

    The fixture reads the analyzer version from Directory.Packages.props so the test stays aligned
    with the repository. The real solution is not modified.

.PARAMETER RepositoryRoot
    The repository root. Defaults to two levels above this script.

.PARAMETER KeepArtifacts
    Keeps the temporary fixture on disk and prints its path for inspection.

.OUTPUTS
    Prints the result and exits 1 on any failed expectation.

.EXAMPLE
    pwsh scripts/api/test-public-api-gate.ps1

.EXAMPLE
    pwsh scripts/api/test-public-api-gate.ps1 -KeepArtifacts
#>
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [switch]$KeepArtifacts
)

$ErrorActionPreference = 'Stop'

$packagesPath = Join-Path $RepositoryRoot 'Directory.Packages.props'
$versionMatch = [regex]::Match(
    [System.IO.File]::ReadAllText($packagesPath),
    'PackageVersion\s+Include="Microsoft\.CodeAnalysis\.PublicApiAnalyzers"\s+Version="([^"]+)"')

if (-not $versionMatch.Success) {
    Write-Error "Microsoft.CodeAnalysis.PublicApiAnalyzers is not pinned in '$packagesPath'."
    exit 2
}

$analyzerVersion = $versionMatch.Groups[1].Value
$fixtureDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('jeninnet-public-api-gate-' + [guid]::NewGuid().ToString('N'))

function Write-TextFile {
    param(
        [string]$Path,
        [string]$Content
    )

    $normalized = $Content.Replace("`r`n", "`n")
    [System.IO.File]::WriteAllText($Path, $normalized, [System.Text.UTF8Encoding]::new($false))
}

function Invoke-FixtureBuild {
    param([string]$ProjectPath)

    $output = & dotnet build $ProjectPath --configuration Debug --nologo --verbosity minimal 2>&1
    $exitCode = $LASTEXITCODE

    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = ($output | Out-String)
    }
}

function Assert-BuildSucceeded {
    param(
        [pscustomobject]$Result,
        [string]$Step
    )

    if ($Result.ExitCode -ne 0) {
        throw "$Step expected a successful build, but dotnet build exited $($Result.ExitCode).`n$($Result.Output)"
    }
}

function Assert-BuildFailedWith {
    param(
        [pscustomobject]$Result,
        [string]$Step,
        [string]$DiagnosticId
    )

    if ($Result.ExitCode -eq 0) {
        throw "$Step expected the build to fail with $DiagnosticId, but it succeeded."
    }

    if ($Result.Output -notmatch [regex]::Escape($DiagnosticId)) {
        throw "$Step expected diagnostic $DiagnosticId, but it was not found.`n$($Result.Output)"
    }
}

$baselineSource = @'
namespace GateFixture;

public sealed class Widget {
    public string Name { get; set; } = string.Empty;
}
'@

$sourceWithNewApi = @'
namespace GateFixture;

public sealed class Widget {
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}
'@

$sourceWithRemovedApi = @'
namespace GateFixture;

public sealed class Widget {
    public int Age { get; set; }
}
'@

$projectTemplate = @'
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net11.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
        <WarningsAsErrors>$(WarningsAsErrors);RS0016;RS0017</WarningsAsErrors>
        <GenerateDocumentationFile>false</GenerateDocumentationFile>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.CodeAnalysis.PublicApiAnalyzers" Version="__VERSION__" PrivateAssets="all" />
    </ItemGroup>

</Project>
'@

$shipped = @'
#nullable enable
GateFixture.Widget
GateFixture.Widget.Name.get -> string!
GateFixture.Widget.Name.set -> void
GateFixture.Widget.Widget() -> void
'@

$unshippedWithNewApi = @'
#nullable enable
GateFixture.Widget.Age.get -> int
GateFixture.Widget.Age.set -> void
'@

$shippedWithoutRemovedApi = @'
#nullable enable
GateFixture.Widget
GateFixture.Widget.Widget() -> void
'@

try {
    New-Item -ItemType Directory -Path $fixtureDirectory -Force | Out-Null

    $projectPath = Join-Path $fixtureDirectory 'GateFixture.csproj'
    $sourcePath = Join-Path $fixtureDirectory 'Widget.cs'
    $shippedPath = Join-Path $fixtureDirectory 'PublicAPI.Shipped.txt'
    $unshippedPath = Join-Path $fixtureDirectory 'PublicAPI.Unshipped.txt'

    Write-TextFile -Path $projectPath -Content $projectTemplate.Replace('__VERSION__', $analyzerVersion)
    Write-TextFile -Path $sourcePath -Content $baselineSource
    Write-TextFile -Path $shippedPath -Content $shipped
    Write-TextFile -Path $unshippedPath -Content "#nullable enable`n"

    $baselineBuild = Invoke-FixtureBuild -ProjectPath $projectPath
    Assert-BuildSucceeded -Result $baselineBuild -Step 'Baseline build'

    Write-TextFile -Path $sourcePath -Content $sourceWithNewApi
    $missingBaselineBuild = Invoke-FixtureBuild -ProjectPath $projectPath
    Assert-BuildFailedWith -Result $missingBaselineBuild -Step 'New API without a baseline' -DiagnosticId 'RS0016'

    Write-TextFile -Path $unshippedPath -Content $unshippedWithNewApi
    $recordedBuild = Invoke-FixtureBuild -ProjectPath $projectPath
    Assert-BuildSucceeded -Result $recordedBuild -Step 'New API recorded in Unshipped'

    Write-TextFile -Path $sourcePath -Content $sourceWithRemovedApi
    $removedApiBuild = Invoke-FixtureBuild -ProjectPath $projectPath
    Assert-BuildFailedWith -Result $removedApiBuild -Step 'Removed API with a stale baseline' -DiagnosticId 'RS0017'

    Write-TextFile -Path $shippedPath -Content $shippedWithoutRemovedApi
    $cleanedBuild = Invoke-FixtureBuild -ProjectPath $projectPath
    Assert-BuildSucceeded -Result $cleanedBuild -Step 'Removed API baseline cleanup'

    Write-Host 'Public API gate test OK: baseline, RS0016 add, Unshipped recording, RS0017 removal, and cleanup all behave as expected.' -ForegroundColor Green
}
finally {
    if ($KeepArtifacts) {
        Write-Host "Fixture kept at '$fixtureDirectory'."
    }
    elseif (Test-Path -LiteralPath $fixtureDirectory) {
        Remove-Item -LiteralPath $fixtureDirectory -Recurse -Force
    }
}
