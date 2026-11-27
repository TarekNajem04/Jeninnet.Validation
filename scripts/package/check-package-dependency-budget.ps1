#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Verifies the produced NuGet packages against the committed dependency/asset/size budget.

.DESCRIPTION
    Reads every .nupkg in -PackagesPath, parses its .nuspec and archive entries, and compares them
    with scripts/package/package-dependency-budget.json. The baseline covers dependencies by TFM,
    lib/analyzer/tool assets, metadata, and a reviewed package-size budget.

    Use -Update to regenerate the baseline from the current package set; review the diff before
    committing it.

.PARAMETER PackagesPath
    Directory containing the produced .nupkg files. Defaults to artifacts/packages.

.PARAMETER BaselinePath
    Baseline JSON path. Defaults to package-dependency-budget.json next to this script.

.PARAMETER PackageFilter
    Optional package ID to check. Intended for targeted tests; the default checks all packages.

.PARAMETER Update
    Regenerate the baseline from the current packages instead of checking.

.EXAMPLE
    pwsh scripts/package/check-package-dependency-budget.ps1 -PackagesPath artifacts/packages

.EXAMPLE
    pwsh scripts/package/check-package-dependency-budget.ps1 -PackagesPath artifacts/packages -Update
#>

[CmdletBinding()]
param(
    [string]$PackagesPath = 'artifacts/packages',
    [string]$BaselinePath = (Join-Path $PSScriptRoot 'package-dependency-budget.json'),
    [string]$PackageFilter,
    [switch]$Update
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$packagesRoot = if ([System.IO.Path]::IsPathRooted($PackagesPath)) {
    $PackagesPath
}
else {
    Join-Path $repositoryRoot $PackagesPath
}

if (-not (Test-Path -LiteralPath $packagesRoot)) {
    throw "Packages directory not found: $packagesRoot"
}

$packageFiles = @(Get-ChildItem -LiteralPath $packagesRoot -Filter '*.nupkg' -File | Sort-Object Name)

if (-not [string]::IsNullOrWhiteSpace($PackageFilter)) {
    $packageFiles = @($packageFiles | Where-Object {
        $_.BaseName -like "$PackageFilter.*"
    })
}

if ($packageFiles.Count -eq 0) {
    throw "No .nupkg files matched in '$packagesRoot'."
}

function Normalize-Tfm {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return 'any'
    }

    $value = $Value.Trim()
    $match = [regex]::Match($value, '^\.NETStandard([0-9.]+)$', 'IgnoreCase')
    if ($match.Success) { return "netstandard$($match.Groups[1].Value)" }

    $match = [regex]::Match($value, '^\.NETCoreApp([0-9.]+)$', 'IgnoreCase')
    if ($match.Success) { return "net$($match.Groups[1].Value)" }

    return $value.ToLowerInvariant()
}

function Read-Nuspec {
    param([string]$PackagePath)

    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)

    try {
        $nuspecEntry = $archive.Entries |
            Where-Object { $_.FullName -like '*.nuspec' } |
            Select-Object -First 1

        if ($null -eq $nuspecEntry) {
            throw "Package has no .nuspec: $PackagePath"
        }

        $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())

        try {
            $text = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $xml = [xml]$text
        $metadata = $xml.SelectSingleNode("//*[local-name()='metadata']")

        if ($null -eq $metadata) {
            throw "Package nuspec has no metadata element: $PackagePath"
        }

        $id = [string]$metadata.SelectSingleNode("*[local-name()='id']").InnerText
        $version = [string]$metadata.SelectSingleNode("*[local-name()='version']").InnerText
        $readmeNode = $metadata.SelectSingleNode("*[local-name()='readme']")
        $iconNode = $metadata.SelectSingleNode("*[local-name()='icon']")
        $licenseNode = $metadata.SelectSingleNode("*[local-name()='license']")
        $repositoryNode = $metadata.SelectSingleNode("*[local-name()='repository']")
        $developmentNode = $metadata.SelectSingleNode("*[local-name()='developmentDependency']")
        $readme = if ($null -ne $readmeNode) { [string]$readmeNode.InnerText } else { '' }
        $icon = if ($null -ne $iconNode) { [string]$iconNode.InnerText } else { '' }

        $dependencies = @{}

        $dependenciesNode = $metadata.SelectSingleNode("*[local-name()='dependencies']")
        if ($null -ne $dependenciesNode) {
            foreach ($group in $dependenciesNode.SelectNodes("*[local-name()='group']")) {
                $tfm = Normalize-Tfm $group.GetAttribute('targetFramework')
                if (-not $dependencies.ContainsKey($tfm)) {
                    $dependencies[$tfm] = [System.Collections.Generic.List[object]]::new()
                }

                foreach ($dependency in $group.SelectNodes("*[local-name()='dependency']")) {
                    [void]$dependencies[$tfm].Add([ordered]@{
                        id      = $dependency.GetAttribute('id')
                        version = $dependency.GetAttribute('version')
                        exclude = $dependency.GetAttribute('exclude')
                    })
                }
            }
        }

        $assets = @{}
        $kind = 'library'

        foreach ($entry in $archive.Entries) {
            $path = $entry.FullName -replace '\\', '/'

            $libMatch = [regex]::Match($path, '^lib/(?<tfm>[^/]+)/(?<asset>.+)$')
            if ($libMatch.Success) {
                $tfm = Normalize-Tfm $libMatch.Groups['tfm'].Value
                if (-not $assets.ContainsKey($tfm)) { $assets[$tfm] = [System.Collections.Generic.List[string]]::new() }
                [void]$assets[$tfm].Add($path)
                continue
            }

            if ($path -match '^analyzers/dotnet/cs/') {
                $kind = 'analyzer'
                if (-not $assets.ContainsKey('netstandard2.0')) { $assets['netstandard2.0'] = [System.Collections.Generic.List[string]]::new() }
                [void]$assets['netstandard2.0'].Add($path)
                continue
            }

            if ($path -match '^build/') {
                if (-not $assets.ContainsKey('netstandard2.0')) { $assets['netstandard2.0'] = [System.Collections.Generic.List[string]]::new() }
                [void]$assets['netstandard2.0'].Add($path)
                continue
            }

            $toolMatch = [regex]::Match($path, '^tools/(?<tfm>[^/]+)/any/(?<asset>.+)$')
            if ($toolMatch.Success) {
                $kind = 'tool'
                $tfm = Normalize-Tfm $toolMatch.Groups['tfm'].Value
                if (-not $assets.ContainsKey($tfm)) { $assets[$tfm] = [System.Collections.Generic.List[string]]::new() }
                [void]$assets[$tfm].Add($path)
            }
        }

        foreach ($tfm in @($dependencies.Keys)) {
            if (-not $assets.ContainsKey($tfm)) {
                $assets[$tfm] = [System.Collections.Generic.List[string]]::new()
            }
        }

        foreach ($tfm in @($assets.Keys)) {
            if (-not $dependencies.ContainsKey($tfm)) {
                $dependencies[$tfm] = [System.Collections.Generic.List[object]]::new()
            }
        }

        $targets = [ordered]@{}
        foreach ($tfm in @($assets.Keys | Sort-Object)) {
            $targetAssets = @($assets[$tfm] | Sort-Object -Unique)
            $targetDependencies = @(
                $dependencies[$tfm] |
                    Sort-Object id, version, exclude |
                    ForEach-Object {
                        [ordered]@{
                            id      = $_.id
                            version = $_.version
                            exclude = $_.exclude
                        }
                    }
            )

            if ($kind -eq 'library' -and -not ($targetAssets | Where-Object { $_ -like "lib/$tfm/*.dll" })) {
                throw "Package '$id' has no lib/$tfm assembly in the expected asset set."
            }

            if ($kind -eq 'analyzer' -and -not ($targetAssets | Where-Object { $_ -like 'analyzers/dotnet/cs/*.dll' })) {
                throw "Analyzer package '$id' has no analyzer assembly."
            }

            if ($kind -eq 'tool' -and -not ($targetAssets | Where-Object { $_ -like "tools/$tfm/any/*.dll" })) {
                throw "Tool package '$id' has no tool assembly for $tfm."
            }

            $targets[$tfm] = [ordered]@{
                assets       = $targetAssets
                dependencies = $targetDependencies
            }
        }

        $size = (Get-Item -LiteralPath $PackagePath).Length
        $headroom = [Math]::Max(8192, [Math]::Ceiling($size * 0.10))
        $sizeBudget = [Math]::Ceiling(($size + $headroom) / 1024) * 1024

        return [ordered]@{
            id                    = $id
            kind                  = $kind
            version               = $version
            sizeBytes             = $size
            sizeBudgetBytes       = [int]$sizeBudget
            developmentDependency = if ($null -ne $developmentNode) { [string]$developmentNode.InnerText -eq 'true' } else { $false }
            metadata              = [ordered]@{
                readme        = $readme
                icon          = $icon
                license       = if ($null -ne $licenseNode) { [string]$licenseNode.InnerText } else { '' }
                repositoryUrl = if ($null -ne $repositoryNode) { [string]$repositoryNode.url } else { '' }
            }
            targets               = $targets
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Test-Package {
    param(
        [string]$PackageId,
        [object]$Actual,
        [object]$Expected
    )

    $errors = [System.Collections.Generic.List[string]]::new()

    if ($Actual.kind -ne $Expected.kind) {
        [void]$errors.Add("package kind '$($Actual.kind)' != '$($Expected.kind)'")
    }

    if ($Actual.sizeBytes -gt $Expected.sizeBudgetBytes) {
        [void]$errors.Add("size $($Actual.sizeBytes) exceeds budget $($Expected.sizeBudgetBytes)")
    }

    foreach ($metadataKey in @('readme', 'icon', 'license', 'repositoryUrl')) {
        $expectedValue = [string]$Expected.metadata.PSObject.Properties[$metadataKey].Value

        if ([string]::IsNullOrWhiteSpace([string]$Actual.metadata[$metadataKey])) {
            [void]$errors.Add("metadata '$metadataKey' is missing")
        }
        elseif ([string]$Actual.metadata[$metadataKey] -ne $expectedValue) {
            [void]$errors.Add("metadata '$metadataKey' changed from '$expectedValue' to '$($Actual.metadata[$metadataKey])'")
        }
    }

    $actualTargets = @($Actual.targets.Keys | Sort-Object)
    $expectedTargets = @($Expected.targets.PSObject.Properties.Name | Sort-Object)

    if (($actualTargets -join ',') -ne ($expectedTargets -join ',')) {
        [void]$errors.Add("targets changed from [$($expectedTargets -join ', ')] to [$($actualTargets -join ', ')]")
    }
    else {
        foreach ($tfm in $actualTargets) {
            $actualTarget = $Actual.targets[$tfm]
            $expectedTarget = $Expected.targets.PSObject.Properties[$tfm].Value
            $actualAssets = @($actualTarget.assets)
            $expectedAssets = @($expectedTarget.assets)
            $assetDiff = @(Compare-Object -ReferenceObject $expectedAssets -DifferenceObject $actualAssets -CaseSensitive)

            if ($assetDiff.Count -gt 0) {
                [void]$errors.Add("$tfm assets changed")
            }

            $actualDeps = @(
                $actualTarget.dependencies |
                    ForEach-Object { "$($_.id)|$($_.version)|$($_.exclude)" }
            )
            $expectedDeps = @(
                $expectedTarget.dependencies |
                    ForEach-Object { "$($_.id)|$($_.version)|$($_.exclude)" }
            )
            $dependencyDiff = @(Compare-Object -ReferenceObject $expectedDeps -DifferenceObject $actualDeps -CaseSensitive)

            if ($dependencyDiff.Count -gt 0) {
                [void]$errors.Add("$tfm dependencies changed from [$($expectedDeps -join ', ')] to [$($actualDeps -join ', ')]")
            }

            foreach ($dependency in $actualTarget.dependencies) {
                if ($dependency.id -match '^(Roslynator\.|SonarAnalyzer\.|Microsoft\.CodeAnalysis)|\.Analyzers?$') {
                    [void]$errors.Add("$tfm has analyzer dependency '$($dependency.id)' in a runtime package")
                }
            }
        }
    }

    return $errors
}

try {
    $actual = [ordered]@{}
    foreach ($packageFile in $packageFiles) {
        $package = Read-Nuspec -PackagePath $packageFile.FullName
        if ($actual.Contains($package.id)) {
            throw "Duplicate package id in output: $($package.id)"
        }
        $actual[$package.id] = $package
    }

    if ($Update) {
        if ($actual.Count -ne 25 -and [string]::IsNullOrWhiteSpace($PackageFilter)) {
            throw "Expected 25 packages, found $($actual.Count)."
        }

        $baseline = [ordered]@{
            schemaVersion = 1
            packageCount  = $actual.Count
            packages      = $actual
        }

        $json = $baseline | ConvertTo-Json -Depth 20
        [System.IO.File]::WriteAllText($BaselinePath, $json, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Package dependency budget written for $($actual.Count) packages." -ForegroundColor Green
        exit 0
    }

    if (-not (Test-Path -LiteralPath $BaselinePath)) {
        throw "Baseline not found: $BaselinePath"
    }

    $expected = Get-Content -LiteralPath $BaselinePath -Raw | ConvertFrom-Json
    $errors = [System.Collections.Generic.List[string]]::new()

    if ($expected.schemaVersion -ne 1) {
        [void]$errors.Add("Unsupported baseline schemaVersion: $($expected.schemaVersion)")
    }

    if ([string]::IsNullOrWhiteSpace($PackageFilter) -and $expected.packageCount -ne 25) {
        [void]$errors.Add("Baseline packageCount must be 25, found $($expected.packageCount).")
    }

    foreach ($packageName in @($actual.Keys | Sort-Object)) {
        $expectedPackage = $expected.packages.PSObject.Properties[$packageName].Value

        if ($null -eq $expectedPackage) {
            [void]$errors.Add("$packageName has no baseline entry")
            continue
        }

        foreach ($message in (Test-Package -PackageId $packageName -Actual $actual[$packageName] -Expected $expectedPackage)) {
            [void]$errors.Add("${packageName}: $message")
        }
    }

    if ([string]::IsNullOrWhiteSpace($PackageFilter)) {
        foreach ($expectedPackage in @($expected.packages.PSObject.Properties.Name)) {
            if (-not $actual.Contains($expectedPackage)) {
                [void]$errors.Add("$expectedPackage is missing from the package output")
            }
        }
    }

    if ($errors.Count -gt 0) {
        Write-Host "Package dependency budget FAILED ($($errors.Count) problem(s)):" -ForegroundColor Red
        $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        exit 1
    }

    $totalSize = ($actual.Values | ForEach-Object { [int64]$_.sizeBytes } | Measure-Object -Sum).Sum
    $totalBudget = ($actual.Values | ForEach-Object { [int64]$_.sizeBudgetBytes } | Measure-Object -Sum).Sum
    Write-Host "Package dependency budget OK: $($actual.Count) package(s), $($totalSize) bytes actual / $($totalBudget) bytes budget." -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "Package dependency budget error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
