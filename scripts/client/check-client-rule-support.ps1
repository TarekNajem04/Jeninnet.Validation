#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Verifies that the generated C# and JavaScript client-rule sets match the manifest.

.DESCRIPTION
    Calls generate-client-rule-support.ps1 -Check. The manifest is the source of truth; a manual
    edit to either generated file fails this gate.

.EXAMPLE
    pwsh scripts/client/check-client-rule-support.ps1
#>

[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$generator = Join-Path $PSScriptRoot 'generate-client-rule-support.ps1'
$pwshPath = (Get-Command pwsh -ErrorAction Stop).Source

& $pwshPath -NoProfile -File $generator -Check -RepositoryRoot $RepositoryRoot

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
