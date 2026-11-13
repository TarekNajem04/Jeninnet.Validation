#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

<#
.SYNOPSIS
    Verifies that every source file keeps the Jeninnet attribution header.

.DESCRIPTION
    Scans C#, JavaScript, and PowerShell source files in the repository and fails when the
    standard copyright/license header is missing. Generated and tooling folders (bin, obj,
    node_modules, TestResults, artifacts, out, .vs, .git) are ignored.

.EXAMPLE
    pwsh ./scripts/quality/check-source-headers.ps1
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".." ".."))
$sourceRoots = @("src", "tests", "samples", "templates", "client", "scripts")
$extensions = @(".cs", ".js", ".mjs", ".ps1", ".psd1", ".psm1")
$excludedSegments = @("bin", "obj", "node_modules", "TestResults", "artifacts", "out", ".vs", ".git", "private")
$requiredLines = @(
    "Copyright (c) Jeninnet.",
    "Part of the Jeninnet Platform.",
    "Repository: https://github.com/TarekNajem04/Jeninnet.Validation",
    "Licensed under the MIT License."
)

$files = foreach($root in $sourceRoots) {
    $rootPath = Join-Path $repositoryRoot $root

    if(-not (Test-Path -LiteralPath $rootPath)) {
        continue
    }

    Get-ChildItem -LiteralPath $rootPath -Recurse -File |
        Where-Object {
            $extensions -contains $_.Extension -and
            -not ($_.FullName -split '[\\/]' | Where-Object { $excludedSegments -contains $_ })
        }
}

$missing = [System.Collections.Generic.List[string]]::new()

foreach($file in $files) {
    $header = (Get-Content -LiteralPath $file.FullName -TotalCount 10) -join "`n"
    $hasEveryLine = $true

    foreach($requiredLine in $requiredLines) {
        if(-not $header.Contains($requiredLine, [System.StringComparison]::Ordinal)) {
            $hasEveryLine = $false
            break
        }
    }

    if(-not $hasEveryLine) {
        $missing.Add([System.IO.Path]::GetRelativePath($repositoryRoot, $file.FullName))
    }
}

if($missing.Count -gt 0) {
    Write-Host "Missing the standard source header in $($missing.Count) file(s):" -ForegroundColor Red

    foreach($file in ($missing | Sort-Object)) {
        Write-Host "  $file" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Source headers OK: $($files.Count) file(s) carry the Jeninnet attribution header." -ForegroundColor Green
