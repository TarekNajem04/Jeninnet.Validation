#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Adds unrecorded public API to PublicAPI.Unshipped.txt and promotes unshipped API at release.

.DESCRIPTION
    Ensures every packable project has baseline files, runs the PublicApiAnalyzers code fix for
    RS0016, normalizes the files, and verifies the solution builds.

    Default mode is the development workflow: new public API is added to PublicAPI.Unshipped.txt
    for review. The analyzer's RS0017 (deleted API) is not auto-fixed; remove stale entries
    intentionally and explain the change in the pull request.

    -Promote moves every Unshipped entry into Shipped and leaves Unshipped empty. Run it at release
    time after reviewing the additions. Razor-generated Blazor members are not discoverable by the
    Roslyn code fix; maintain those entries manually as described in scripts/api/README.md.

.PARAMETER Promote
    Moves all Unshipped entries to Shipped. Use only when preparing a release.

.PARAMETER NoRestore
    Skips the implicit restore before running dotnet format.

.PARAMETER RepositoryRoot
    The repository root. Defaults to two levels above this script.

.EXAMPLE
    pwsh scripts/api/sync-public-api.ps1

.EXAMPLE
    pwsh scripts/api/sync-public-api.ps1 -Promote
#>
param(
    [switch]$Promote,
    [switch]$NoRestore,
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'

$solutionPath = Join-Path $RepositoryRoot 'Jeninnet.Validation.slnx'
$srcDirectory = Join-Path $RepositoryRoot 'src'

function Write-BaselineFile {
    param(
        [string]$Path,
        [string[]]$Entries
    )

    $set = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)

    foreach ($entry in $Entries) {
        if ($entry -and -not $entry.StartsWith('#', [System.StringComparison]::Ordinal)) {
            [void]$set.Add($entry)
        }
    }

    $lines = @('#nullable enable') + @($set)
    $text = ($lines -join "`n") + "`n"
    [System.IO.File]::WriteAllText($Path, $text, [System.Text.UTF8Encoding]::new($false))
}

$projects = @(
    Get-ChildItem -LiteralPath $srcDirectory -Recurse -Filter '*.csproj' -File |
        Where-Object {
            (Get-Content -LiteralPath $_.FullName -Raw) -match '<IsPackable>\s*true\s*</IsPackable>'
        } |
        Sort-Object FullName
)

if ($projects.Count -eq 0) {
    Write-Error "No packable projects found under '$srcDirectory'."
    exit 2
}

foreach ($project in $projects) {
    foreach ($fileName in @('PublicAPI.Shipped.txt', 'PublicAPI.Unshipped.txt')) {
        $path = Join-Path $project.DirectoryName $fileName

        if (-not (Test-Path -LiteralPath $path)) {
            Write-BaselineFile -Path $path -Entries @()
        }
    }
}

$formatArguments = @(
    'format', 'analyzers', $solutionPath,
    '--diagnostics', 'RS0016',
    '--severity', 'warn',
    '--verbosity', 'minimal'
)

if ($NoRestore) {
    $formatArguments += '--no-restore'
}

& dotnet @formatArguments

if ($LASTEXITCODE -ne 0) {
    Write-Error 'dotnet format failed. Fix the reported diagnostics before continuing.'
    exit 1
}

if ($Promote) {
    foreach ($project in $projects) {
        $shippedPath = Join-Path $project.DirectoryName 'PublicAPI.Shipped.txt'
        $unshippedPath = Join-Path $project.DirectoryName 'PublicAPI.Unshipped.txt'
        $entries = @(
            (Get-Content -LiteralPath $shippedPath) +
            (Get-Content -LiteralPath $unshippedPath)
        )

        Write-BaselineFile -Path $shippedPath -Entries $entries
        Write-BaselineFile -Path $unshippedPath -Entries @()
    }
}

foreach ($project in $projects) {
    foreach ($fileName in @('PublicAPI.Shipped.txt', 'PublicAPI.Unshipped.txt')) {
        $path = Join-Path $project.DirectoryName $fileName
        $entries = @(Get-Content -LiteralPath $path)
        Write-BaselineFile -Path $path -Entries $entries
    }
}

& dotnet build $solutionPath --configuration Debug --nologo --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Error 'The solution does not build after the baseline update. Inspect the analyzer output.'
    exit 1
}

$mode = if ($Promote) { 'promoted Unshipped to Shipped' } else { 'updated Unshipped' }
Write-Host "Public API baselines synced ($mode) for $($projects.Count) packable project(s)." -ForegroundColor Green
