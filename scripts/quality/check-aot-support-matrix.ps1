#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Verifies the explicit AOT/trim position of every packable project.

.DESCRIPTION
    Reads scripts/quality/aot-support-matrix.json, compares it with the packable projects under
    src/, and verifies that only packages marked aot-compatible declare IsAotCompatible=true.
    Also verifies that the AOT page contains a row for every package.

.PARAMETER RepositoryRoot
    Repository root. Defaults to two levels above this script.

.EXAMPLE
    pwsh scripts/quality/check-aot-support-matrix.ps1
#>

[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$manifestPath = Join-Path $PSScriptRoot 'aot-support-matrix.json'
$docsPath = Join-Path $repositoryRoot 'docs/en/04-operations/02-aot-and-trimming.md'
$errors = [System.Collections.Generic.List[string]]::new()

if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "AOT matrix manifest not found: $manifestPath"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$entries = @($manifest.packages)

if ($manifest.schemaVersion -ne 1) {
    [void]$errors.Add("Unsupported AOT matrix schemaVersion: $($manifest.schemaVersion)")
}

$duplicates = @($entries | Group-Object package | Where-Object Count -gt 1 | ForEach-Object Name)
if ($duplicates.Count -gt 0) {
    [void]$errors.Add("Duplicate package entries: $($duplicates -join ', ')")
}

$projects = @(
    Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'src') -Recurse -Filter '*.csproj' -File |
        Where-Object {
            (Get-Content -LiteralPath $_.FullName -Raw) -match '<IsPackable>\s*true\s*</IsPackable>'
        } |
        Sort-Object BaseName
)

$entriesByName = @{}
foreach ($entry in $entries) {
    if ([string]::IsNullOrWhiteSpace($entry.package)) {
        [void]$errors.Add('Every AOT matrix entry must declare a package name.')
        continue
    }

    $entriesByName[[string]$entry.package] = $entry
}

foreach ($project in $projects) {
    $package = $project.BaseName
    $entry = $entriesByName[$package]

    if ($null -eq $entry) {
        [void]$errors.Add("Packable project '$package' has no AOT matrix entry.")
        continue
    }

    $xml = Get-Content -LiteralPath $project.FullName -Raw
    $hasAotProperty = $xml -match '<IsAotCompatible>\s*true\s*</IsAotCompatible>'
    $shouldHaveAotProperty = [string]$entry.position -eq 'aot-compatible'

    if ($shouldHaveAotProperty -and -not $hasAotProperty) {
        [void]$errors.Add("'$package' is marked aot-compatible in the matrix but does not declare IsAotCompatible=true.")
    }

    if (-not $shouldHaveAotProperty -and $hasAotProperty) {
        [void]$errors.Add("'$package' declares IsAotCompatible=true but its matrix position is '$($entry.position)'.")
    }
}

$projectNames = @($projects | ForEach-Object BaseName)
foreach ($entry in $entries) {
    if ($projectNames -notcontains [string]$entry.package) {
        [void]$errors.Add("AOT matrix package '$($entry.package)' is not a packable project under src/.")
    }
}

if (Test-Path -LiteralPath $docsPath) {
    $docs = Get-Content -LiteralPath $docsPath -Raw

    foreach ($entry in $entries) {
        if (-not $docs.Contains([string]$entry.package, [StringComparison]::Ordinal)) {
            [void]$errors.Add("AOT page does not mention matrix package '$($entry.package)'.")
        }
    }
}
else {
    [void]$errors.Add("AOT page not found: $docsPath")
}

$counts = $entries | Group-Object position | Sort-Object Name

if ($errors.Count -gt 0) {
    Write-Host "AOT support matrix check FAILED ($($errors.Count) problem(s)):" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}

Write-Host "AOT support matrix OK: $($entries.Count) packages." -ForegroundColor Green
$counts | ForEach-Object { Write-Host ("  {0,-22} {1}" -f $_.Name, $_.Count) }
