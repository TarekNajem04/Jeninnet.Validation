#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
  Verifies the deterministic validation contract fingerprint on net10 and net11 and compares it
  with the reviewed baseline.

.DESCRIPTION
  Runs the internal contract-fingerprint tool for both target frameworks, proves the outputs are
  byte-identical, compares them with scripts/quality/validation-contract-fingerprints.json, and runs
  the tool's sensitivity self-test. The check also rejects reflection/dynamic-code tokens in the
  fingerprint source.

.PARAMETER BaselinePath
  Reviewed baseline. Defaults to scripts/quality/validation-contract-fingerprints.json.

.PARAMETER OutputDirectory
  Artifact directory. Defaults to artifacts/contract-fingerprints.

.PARAMETER SkipSelfTest
  Skip the sensitivity self-test.

.OUTPUTS
  Prints the fingerprint summary. Exits 1 on mismatch or sensitivity failure.

.EXAMPLE
  pwsh scripts/quality/check-contract-fingerprints.ps1
#>

[CmdletBinding()]
param(
    [string]$BaselinePath,
    [string]$OutputDirectory,
    [switch]$SkipSelfTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$toolDirectory = Join-Path $repositoryRoot 'tools/Jeninnet.Validation.ContractFingerprint'
$toolProject = Join-Path $toolDirectory 'Jeninnet.Validation.ContractFingerprint.csproj'
$dotnetPath = (Get-Command dotnet -ErrorAction Stop).Source

if ([string]::IsNullOrWhiteSpace($BaselinePath)) {
    $BaselinePath = Join-Path $repositoryRoot 'scripts/quality/validation-contract-fingerprints.json'
}
elseif (-not [System.IO.Path]::IsPathRooted($BaselinePath)) {
    $BaselinePath = Join-Path $repositoryRoot $BaselinePath
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot 'artifacts/contract-fingerprints'
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot $OutputDirectory
}

if (-not (Test-Path -LiteralPath $BaselinePath)) {
    Write-Error "Contract fingerprint baseline not found: $BaselinePath"
    exit 2
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$fingerprintSources = Get-ChildItem -LiteralPath $toolDirectory -Filter '*.cs' -File |
    ForEach-Object { [System.IO.File]::ReadAllText($_.FullName) }

foreach ($source in $fingerprintSources) {
    if ($source -match 'System\.Reflection|\bActivator\s*\.|\bdynamic\b') {
        Write-Error 'The fingerprint source must not use reflection, Activator, or dynamic code.'
        exit 1
    }
}

function Invoke-FingerprintTool {
    param(
        [string]$TargetFramework,
        [string]$OutputPath
    )

    & $dotnetPath run --configuration Release --framework $TargetFramework --project $toolProject -- --output $OutputPath
    if ($LASTEXITCODE -ne 0) {
        throw "Contract fingerprint tool failed for $TargetFramework with exit code $LASTEXITCODE."
    }
}

$net10Path = Join-Path $OutputDirectory 'net10.json'
$net11Path = Join-Path $OutputDirectory 'net11.json'

Invoke-FingerprintTool -TargetFramework 'net10.0' -OutputPath $net10Path
Invoke-FingerprintTool -TargetFramework 'net11.0' -OutputPath $net11Path

$net10Hash = (Get-FileHash -LiteralPath $net10Path -Algorithm SHA256).Hash
$net11Hash = (Get-FileHash -LiteralPath $net11Path -Algorithm SHA256).Hash
$baselineHash = (Get-FileHash -LiteralPath $BaselinePath -Algorithm SHA256).Hash

if ($net10Hash -ne $net11Hash) {
    Write-Error 'Contract fingerprints differ between net10.0 and net11.0.'
    exit 1
}

$baselineBytes = [System.IO.File]::ReadAllBytes($BaselinePath)
if ($baselineBytes.Length -ge 3 -and
    $baselineBytes[0] -eq 239 -and $baselineBytes[1] -eq 187 -and $baselineBytes[2] -eq 191) {
    Write-Error 'Contract fingerprint baseline must not have a UTF-8 BOM.'
    exit 1
}

if ($net10Hash -ne $baselineHash) {
    Write-Host "Baseline mismatch: $BaselinePath"
    Write-Host "Current fingerprint: $net10Path"
    Write-Host 'Update the baseline only after reviewing the descriptor change.'
    exit 1
}

if (-not $SkipSelfTest) {
    & $dotnetPath run --configuration Release --framework net10.0 --project $toolProject -- --self-test
    if ($LASTEXITCODE -ne 0) {
        throw "Contract fingerprint self-test failed with exit code $LASTEXITCODE."
    }
}

Write-Host "Contract fingerprints OK: net10.0 and net11.0 are identical and match the baseline."
Write-Host "Baseline: $BaselinePath"
exit 0
