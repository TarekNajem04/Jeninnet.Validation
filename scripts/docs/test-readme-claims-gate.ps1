#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Proves the README claim gate fails when the verified test count is changed in one place.

.DESCRIPTION
    Copies README.md to a temporary file at the repository root, replaces the current test count
    with a deliberately wrong value, runs check-readme-claims.ps1 against the copy, and expects
    exit code 1 with a test-count mismatch. The temporary file is always removed.

.PARAMETER ReadmePath
    Authoritative README. Defaults to <repo>/README.md.

.EXAMPLE
    pwsh scripts/docs/test-readme-claims-gate.ps1
#>

[CmdletBinding()]
param(
    [string]$ReadmePath = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path 'README.md')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$checker = Join-Path $PSScriptRoot 'check-readme-claims.ps1'
$pwshPath = (Get-Command pwsh -ErrorAction Stop).Source
$temporaryPath = Join-Path $repositoryRoot ('README.claim-negative-' + [guid]::NewGuid().ToString('N') + '.md')

$text = [System.IO.File]::ReadAllText($ReadmePath)

if ($text -notmatch 'current suite runs \*\*(?<count>[\d,]+) tests\*\*') {
    throw "README does not contain the current test-count claim expected by the gate: $ReadmePath"
}

$originalCount = [regex]::Match($text, 'current suite runs \*\*(?<count>[\d,]+) tests\*\*').Groups['count'].Value
$mutated = $text -replace 'current suite runs \*\*[\d,]+ tests\*\*', 'current suite runs **999 tests**'
[System.IO.File]::WriteAllText($temporaryPath, $mutated, [System.Text.UTF8Encoding]::new($false))

try {
    $output = & $pwshPath -NoProfile -File $checker -ReadmePath $temporaryPath 2>&1 | Out-String
    $exitCode = $LASTEXITCODE
    Write-Host $output.TrimEnd()

    if ($exitCode -eq 0) {
        throw 'The README claim gate did not fail after the test count was changed.'
    }

    if ($output -notmatch 'Test count mismatch') {
        throw 'The README claim gate failed, but it did not report a test-count mismatch.'
    }

    Write-Host "README claim negative test OK: test count $originalCount -> 999 failed the gate." -ForegroundColor Green
    exit 0
}
finally {
    Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
}
