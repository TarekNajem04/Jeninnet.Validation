#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Proves that the API diff gate fails on an unapproved removed member.

.DESCRIPTION
    Temporarily removes one shipped public API entry from a working-tree baseline, runs
    compare-public-api.ps1 against v1.0.0-preview.2, and verifies that the report fails with a
    removed change. The original file bytes are restored in a finally block.

.PARAMETER RepositoryRoot
    Repository root. Defaults to two levels above this script.

.PARAMETER KeepArtifacts
    Keeps the temporary report directory and prints its path.

.EXAMPLE
    pwsh scripts/api/test-api-diff-gate.ps1
#>

[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [switch]$KeepArtifacts
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$compareScript = Join-Path $PSScriptRoot 'compare-public-api.ps1'
$approvalFile = Join-Path $PSScriptRoot 'api-diff-approvals.json'
$baselineFile = Join-Path $repositoryRoot 'src/Jeninnet.Validation.Abstractions/PublicAPI.Shipped.txt'
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('jeninnet-api-diff-' + [guid]::NewGuid().ToString('N'))
$originalBytes = $null
$pwshPath = (Get-Command pwsh -ErrorAction Stop).Source

try {
    if (-not (Test-Path -LiteralPath $compareScript)) {
        throw "Comparison script not found: $compareScript"
    }

    if (-not (Test-Path -LiteralPath $baselineFile)) {
        throw "Baseline fixture not found: $baselineFile"
    }

    if (-not (Test-Path -LiteralPath $approvalFile)) {
        throw "Approval file not found: $approvalFile"
    }

    $originalBytes = [System.IO.File]::ReadAllBytes($baselineFile)
    $lines = [System.IO.File]::ReadAllLines($baselineFile)
    $removed = $lines |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and -not $_.TrimStart().StartsWith('#') } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($removed)) {
        throw "No API entry was found in '$baselineFile'."
    }

    $remaining = @($lines | Where-Object { $_ -ne $removed })
    [System.IO.File]::WriteAllText(
        $baselineFile,
        (($remaining -join "`n") + "`n"),
        [System.Text.UTF8Encoding]::new($false))

    New-Item -ItemType Directory -Force -Path $temporaryRoot | Out-Null

    & $pwshPath -NoProfile -File $compareScript `
        -BaselineRef 'v1.0.0-preview.2' `
        -CurrentRef 'WORKTREE' `
        -OutputRoot $temporaryRoot `
        -ApprovalPath $approvalFile

    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        throw 'The API diff gate did not fail for an unapproved removed member.'
    }

    $report = Get-ChildItem -LiteralPath $temporaryRoot -Recurse -Filter 'api-diff.json' -File |
        Select-Object -First 1

    if ($null -eq $report) {
        throw "The API diff gate failed but produced no JSON report under '$temporaryRoot'."
    }

    $document = Get-Content -LiteralPath $report.FullName -Raw | ConvertFrom-Json
    $removedChanges = @($document.changes | Where-Object Kind -eq 'removed')

    if ($document.overallStatus -ne 'failed') {
        throw "Expected overallStatus 'failed', found '$($document.overallStatus)'."
    }

    if ($removedChanges.Count -eq 0) {
        throw 'The report did not contain a removed change.'
    }

    Write-Host 'API diff negative test OK: unapproved removal failed the gate.' -ForegroundColor Green
    Write-Host "Removed signature: $removed"
    Write-Host "Report: $($report.FullName)"
    exit 0
}
finally {
    if ($null -ne $originalBytes) {
        [System.IO.File]::WriteAllBytes($baselineFile, $originalBytes)
    }

    if (-not $KeepArtifacts -and (Test-Path -LiteralPath $temporaryRoot)) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
    elseif ($KeepArtifacts) {
        Write-Host "Kept temporary artifacts at: $temporaryRoot"
    }
}
