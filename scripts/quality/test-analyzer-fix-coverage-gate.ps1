#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Proves the analyzer fix-coverage gate fails on the four drift cases it exists to catch.

.DESCRIPTION
    Copies the descriptor source, the code-fix providers, the public diagnostics table, and the
    analyzer release files into a temporary root. The positive case must pass; then one input is
    mutated at a time and the checker must fail with the expected message.

.EXAMPLE
    pwsh scripts/quality/test-analyzer-fix-coverage-gate.ps1
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$checker = Join-Path $PSScriptRoot 'check-analyzer-fix-coverage.ps1'
$pwshPath = (Get-Command pwsh -ErrorAction Stop).Source
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('jeninnet-fix-coverage-' + [guid]::NewGuid().ToString('N'))

$sourceDiagnostics = Join-Path $repositoryRoot 'src/Jeninnet.Validation.Analyzers/Diagnostics/DiagnosticDescriptors.cs'
$sourceCodeFixes = Join-Path $repositoryRoot 'src/Jeninnet.Validation.Analyzers/CodeFixes'
$sourceDocumentation = Join-Path $repositoryRoot 'docs/en/03-reference/03-diagnostics.md'
$sourceShipped = Join-Path $repositoryRoot 'src/Jeninnet.Validation.Analyzers/AnalyzerReleases.Shipped.md'
$sourceUnshipped = Join-Path $repositoryRoot 'src/Jeninnet.Validation.Analyzers/AnalyzerReleases.Unshipped.md'

$targetDiagnostics = Join-Path $temporaryRoot 'src/Jeninnet.Validation.Analyzers/Diagnostics/DiagnosticDescriptors.cs'
$targetCodeFixes = Join-Path $temporaryRoot 'src/Jeninnet.Validation.Analyzers/CodeFixes'
$targetDocumentation = Join-Path $temporaryRoot 'docs/en/03-reference/03-diagnostics.md'
$targetShipped = Join-Path $temporaryRoot 'src/Jeninnet.Validation.Analyzers/AnalyzerReleases.Shipped.md'
$targetUnshipped = Join-Path $temporaryRoot 'src/Jeninnet.Validation.Analyzers/AnalyzerReleases.Unshipped.md'

function Invoke-Check {
    param([string]$CaseName)

    $output = & $pwshPath -NoProfile -File $checker -Root $temporaryRoot 2>&1 | Out-String
    $exitCode = $LASTEXITCODE

    return [pscustomobject]@{
        Name = $CaseName
        ExitCode = $exitCode
        Output = $output
    }
}

function Assert-Pass {
    param([object]$Result)

    if ($Result.ExitCode -ne 0) {
        throw "Expected the '$($Result.Name)' case to pass, but it exited $($Result.ExitCode):`n$($Result.Output)"
    }

    Write-Host "PASS: $($Result.Name)"
}

function Assert-Fail {
    param([object]$Result, [string]$ExpectedText)

    if ($Result.ExitCode -eq 0) {
        throw "Expected the '$($Result.Name)' case to fail, but it passed:`n$($Result.Output)"
    }

    if ($Result.Output -notmatch $ExpectedText) {
        throw "The '$($Result.Name)' case failed without the expected text '$ExpectedText':`n$($Result.Output)"
    }

    Write-Host "PASS: $($Result.Name)"
}

function Write-Utf8 {
    param([string]$Path, [string]$Text)
    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

try {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $targetDiagnostics) | Out-Null
    New-Item -ItemType Directory -Force -Path $targetCodeFixes | Out-Null
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $targetDocumentation) | Out-Null

    Copy-Item -LiteralPath $sourceDiagnostics -Destination $targetDiagnostics
    Copy-Item -Path (Join-Path $sourceCodeFixes '*.cs') -Destination $targetCodeFixes
    Copy-Item -LiteralPath $sourceDocumentation -Destination $targetDocumentation
    Copy-Item -LiteralPath $sourceShipped -Destination $targetShipped
    Copy-Item -LiteralPath $sourceUnshipped -Destination $targetUnshipped

    Assert-Pass (Invoke-Check 'complete-matrix')

    $documentation = [System.IO.File]::ReadAllText($targetDocumentation)
    $originalDocumentation = $documentation

    Write-Utf8 $targetDocumentation ($documentation -replace '(?m)^\| `VAL0001` \|.*\r?\n', '')
    Assert-Fail (Invoke-Check 'missing-documentation-row') 'not documented'
    Write-Utf8 $targetDocumentation $originalDocumentation

    Write-Utf8 $targetDocumentation ($documentation -replace '(?m)^(\| `VAL0001` \|.*\| )Available( \|\r?)$', '${1}Intentional — test reason${2}')
    Assert-Fail (Invoke-Check 'fix-not-marked-available') 'not marked Available'
    Write-Utf8 $targetDocumentation $originalDocumentation

    Write-Utf8 $targetDocumentation ($documentation -replace '(?m)^(\| `VAL0002` \|.*\| )Intentional — [^|]*( \|\r?)$', '${1}Intentional —${2}')
    Assert-Fail (Invoke-Check 'intentional-without-reason') 'no intentional-omission reason'
    Write-Utf8 $targetDocumentation $originalDocumentation

    $shipped = [System.IO.File]::ReadAllText($targetShipped)
    Write-Utf8 $targetShipped ($shipped -replace '(?m)^VAL0003\s*\|.*\r?\n', '')
    Assert-Fail (Invoke-Check 'missing-release-row') 'does not list VAL0003'
    Write-Utf8 $targetShipped $shipped

    Assert-Pass (Invoke-Check 'restored-matrix')
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}

Write-Host 'Analyzer fix-coverage gate self-test OK.'
exit 0
