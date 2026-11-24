#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Proves the package dependency budget gate rejects a removed asset or an injected dependency.

.DESCRIPTION
    Copies one produced package to a temporary directory, mutates the copy, runs the budget check
    against that package, and expects exit code 1. The temporary copy is always removed.

    RemovedAsset deletes a lib assembly. InjectedDependency inserts Roslynator.Analyzers into the
    package's dependency group, which must fail both the dependency comparison and the analyzer
    rule.

.PARAMETER PackagesPath
    Directory containing the authoritative packages to copy. Defaults to artifacts/packages.

.PARAMETER Mode
    Mutation to apply: RemovedAsset or InjectedDependency.

.PARAMETER PackageId
    Package to mutate. Defaults to Jeninnet.Validation.Abstractions for RemovedAsset and
    Jeninnet.Validation for InjectedDependency.

.PARAMETER KeepArtifacts
    Keeps the temporary package directory and prints its path.

.EXAMPLE
    pwsh scripts/package/test-package-dependency-budget-gate.ps1 -PackagesPath artifacts/packages

.EXAMPLE
    pwsh scripts/package/test-package-dependency-budget-gate.ps1 -PackagesPath artifacts/packages -Mode InjectedDependency
#>

[CmdletBinding()]
param(
    [string]$PackagesPath = 'artifacts/packages',
    [ValidateSet('RemovedAsset', 'InjectedDependency')]
    [string]$Mode = 'RemovedAsset',
    [string]$PackageId,
    [switch]$KeepArtifacts
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

if ([string]::IsNullOrWhiteSpace($PackageId)) {
    $PackageId = if ($Mode -eq 'RemovedAsset') {
        'Jeninnet.Validation.Abstractions'
    }
    else {
        'Jeninnet.Validation'
    }
}

$sourcePackage = Get-ChildItem -LiteralPath $packagesRoot -Filter "$PackageId.*.nupkg" -File |
    Select-Object -First 1

if ($null -eq $sourcePackage) {
    throw "Package '$PackageId' was not found in '$packagesRoot'."
}

$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('jeninnet-package-budget-' + [guid]::NewGuid().ToString('N'))
$copyPath = Join-Path $temporaryRoot $sourcePackage.Name
$checker = Join-Path $PSScriptRoot 'check-package-dependency-budget.ps1'
$pwshPath = (Get-Command pwsh -ErrorAction Stop).Source

New-Item -ItemType Directory -Force -Path $temporaryRoot | Out-Null
Copy-Item -LiteralPath $sourcePackage.FullName -Destination $copyPath

try {
    $archive = [System.IO.Compression.ZipFile]::Open($copyPath, [System.IO.Compression.ZipArchiveMode]::Update)

    try {
        if ($Mode -eq 'RemovedAsset') {
            $asset = $archive.Entries |
                Where-Object { $_.FullName -match '^lib/[^/]+/.*\.dll$' } |
                Select-Object -First 1

            if ($null -eq $asset) {
                throw "No lib assembly was found in '$($sourcePackage.Name)'."
            }

            $mutation = "removed asset $($asset.FullName)"
            $asset.Delete()
        }
        else {
            $nuspecEntry = $archive.Entries |
                Where-Object { $_.FullName -like '*.nuspec' } |
                Select-Object -First 1

            if ($null -eq $nuspecEntry) {
                throw "Package '$($sourcePackage.Name)' has no .nuspec entry."
            }

            $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())

            try {
                $nuspec = $reader.ReadToEnd()
            }
            finally {
                $reader.Dispose()
            }

            $group = [regex]::Match($nuspec, '<group(?<attributes>\s[^>]*)?>')

            if (-not $group.Success) {
                throw "Package '$($sourcePackage.Name)' has no dependency group to mutate."
            }

            $injection = '<dependency id="Roslynator.Analyzers" version="4.12.0" />'
            $updated = $nuspec.Insert($group.Index + $group.Length, $injection)
            $entryName = $nuspecEntry.FullName
            $nuspecEntry.Delete()
            $newEntry = $archive.CreateEntry($entryName)
            $writer = [System.IO.StreamWriter]::new($newEntry.Open(), [System.Text.UTF8Encoding]::new($false))

            try {
                $writer.Write($updated)
            }
            finally {
                $writer.Dispose()
            }

            $mutation = "injected dependency Roslynator.Analyzers into $entryName"
        }
    }
    finally {
        $archive.Dispose()
    }

    $output = & $pwshPath -NoProfile -File $checker -PackagesPath $temporaryRoot -PackageFilter $PackageId 2>&1 | Out-String
    $exitCode = $LASTEXITCODE
    Write-Host $output.TrimEnd()

    if ($exitCode -eq 0) {
        throw "The package budget gate did not fail after it $mutation."
    }

    if ($Mode -eq 'InjectedDependency' -and $output -notmatch 'analyzer dependency') {
        throw 'The package budget gate failed, but the analyzer-dependency rule did not fire.'
    }

    Write-Host "Package budget negative test OK ($Mode): gate rejected mutation." -ForegroundColor Green
    Write-Host "Mutation: $mutation"
    exit 0
}
finally {
    if (-not $KeepArtifacts) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
    else {
        Write-Host "Kept temporary package at: $temporaryRoot"
    }
}
