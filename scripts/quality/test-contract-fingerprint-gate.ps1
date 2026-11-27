#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
  Proves the contract fingerprint gate fails when the reviewed baseline is changed without a rule
  change and passes when the real baseline is restored.

.EXAMPLE
  pwsh scripts/quality/test-contract-fingerprint-gate.ps1
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$checker = Join-Path $PSScriptRoot 'check-contract-fingerprints.ps1'
$baseline = Join-Path $repositoryRoot 'scripts/quality/validation-contract-fingerprints.json'
$pwshPath = (Get-Command pwsh -ErrorAction Stop).Source
$temporaryBaseline = Join-Path $repositoryRoot ('validation-contract-negative-' + [guid]::NewGuid().ToString('N') + '.json')

$text = [System.IO.File]::ReadAllText($baseline)
$mutated = $text -replace '("MinimalValidator": "v1:sha256:)[0-9a-f]+(")', '${1}0000000000000000000000000000000000000000000000000000000000000000${2}'

if ($mutated -eq $text) {
    throw 'The negative test could not mutate the baseline fingerprint.'
}

[System.IO.File]::WriteAllText($temporaryBaseline, $mutated, [System.Text.UTF8Encoding]::new($false))

try {
    $output = & $pwshPath -NoProfile -File $checker -BaselinePath $temporaryBaseline -SkipSelfTest 2>&1 | Out-String
    $exitCode = $LASTEXITCODE
    Write-Host $output.TrimEnd()

    if ($exitCode -eq 0) {
        throw 'The contract fingerprint gate did not fail after the baseline was changed.'
    }

    if ($output -notmatch 'Baseline mismatch') {
        throw 'The contract fingerprint gate failed, but it did not report a baseline mismatch.'
    }

    Write-Host 'PASS: a changed baseline fails the gate.'
}
finally {
    if (Test-Path -LiteralPath $temporaryBaseline) {
        Remove-Item -LiteralPath $temporaryBaseline -Force
    }
}

$output = & $pwshPath -NoProfile -File $checker 2>&1 | Out-String
$exitCode = $LASTEXITCODE
Write-Host $output.TrimEnd()

if ($exitCode -ne 0) {
    throw 'The contract fingerprint gate failed with the reviewed baseline.'
}

Write-Host 'Contract fingerprint gate self-test OK.'
exit 0
