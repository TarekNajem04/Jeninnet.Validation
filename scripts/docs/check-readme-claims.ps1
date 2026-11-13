#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
  Verifies that the project names and the packable-project count claimed by README.md
  are backed by reality: every backticked "Jeninnet.Validation.*" name must exist as a
  project in the solution tree, and the "N packable projects" claim must match the number
  of src/ projects that declare <IsPackable>true</IsPackable>.

.PARAMETER ReadmePath
  Repo README.md. Defaults to <repo>/README.md.

.PARAMETER SourceDirectory
  Shipping source directory whose projects declare IsPackable=true. Defaults to <repo>/src.

.OUTPUTS
  Prints the checked claims, then a summary. Exits 1 if any claim is not backed.

.EXAMPLE
  pwsh scripts/docs/check-readme-claims.ps1
#>
param(
    [string]$ReadmePath = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path 'README.md'),
    [string]$SourceDirectory = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path 'src')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ReadmePath)) {
    Write-Error "README not found: $ReadmePath"
    exit 2
}

$repositoryRoot = Split-Path -Parent $ReadmePath

# ---------------------------------------------------------------- project-name claims
$projectDirectories = @('src', 'integrations', 'samples', 'tests', 'consumers')
$knownProjects = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($directory in $projectDirectories) {
    $path = Join-Path $repositoryRoot $directory
    if (-not (Test-Path -LiteralPath $path)) { continue }
    foreach ($project in Get-ChildItem $path -Recurse -Filter '*.csproj' -File) {
        [void]$knownProjects.Add($project.BaseName)
    }
}

$readme = [System.IO.File]::ReadAllText($ReadmePath)
$tokens = [regex]::Matches($readme, '`(Jeninnet\.Validation\.[A-Za-z0-9._*]+)`') |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique

$unresolved = [System.Collections.Generic.List[string]]::new()
foreach ($token in $tokens) {
    if ($token.EndsWith('.*')) {
        $prefix = $token.Substring(0, $token.Length - 2)
        $match = $knownProjects | Where-Object { $_.StartsWith($prefix, [System.StringComparison]::Ordinal) } | Select-Object -First 1
        if ($null -eq $match) {
            [void]$unresolved.Add("$token (no project with prefix '$prefix')")
        }
        continue
    }

    if (-not $knownProjects.Contains($token)) {
        [void]$unresolved.Add("$token (no such project)")
    }
}

# ---------------------------------------------------------------- packable-count claim
$countFailures = [System.Collections.Generic.List[string]]::new()
$packableCount = 0
if (Test-Path -LiteralPath $SourceDirectory) {
    $packableCount = @(
        Get-ChildItem $SourceDirectory -Recurse -Filter '*.csproj' -File |
            Where-Object { (Get-Content -LiteralPath $_.FullName -Raw) -match '<IsPackable>true</IsPackable>' }
    ).Count
}

$countMatch = [regex]::Match($readme, '(?<count>\d+)\s+packable\s+projects')
if ($countMatch.Success) {
    $claimed = [int]$countMatch.Groups['count'].Value
    if ($claimed -ne $packableCount) {
        [void]$countFailures.Add("README claims $claimed packable projects, but $packableCount src project(s) declare IsPackable=true")
    }
}
else {
    [void]$countFailures.Add("README does not state the 'N packable projects' count that this gate verifies")
}

Write-Host "Checked $($tokens.Count) project-name claim(s) against $($knownProjects.Count) solution project(s)."
Write-Host "Packable-project claim: $(if ($countMatch.Success) { $countMatch.Groups['count'].Value } else { '(absent)' }) vs $packableCount src project(s) with IsPackable=true."

if ($unresolved.Count -gt 0 -or $countFailures.Count -gt 0) {
    Write-Host ''
    Write-Host "README CLAIMS FAILED ($($unresolved.Count + $countFailures.Count) problem(s)):" -ForegroundColor Red
    $unresolved | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $countFailures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}

Write-Host 'README claims OK: every project name and the packable-project count are backed by the repository.' -ForegroundColor Green
