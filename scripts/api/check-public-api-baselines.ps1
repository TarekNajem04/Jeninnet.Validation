#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Verifies that every packable project has committed PublicAPI baseline files.

.DESCRIPTION
    Checks the analyzer integration and every src project that declares IsPackable=true:

      - PublicAPI.Shipped.txt and PublicAPI.Unshipped.txt exist.
      - Both start with #nullable enable.
      - Both are UTF-8 without BOM, LF-only, and end with a newline.
      - No API line is duplicated within or across the two files.
      - Shipped contains at least one API line.
      - Directory.Build.targets references Microsoft.CodeAnalysis.PublicApiAnalyzers and
        Directory.Packages.props pins its version.

    PublicApiAnalyzers (RS0016/RS0017) remains the authoritative gate. This script catches the
    structural mistakes that would make the analyzer silently ineffective.

.PARAMETER RepositoryRoot
    The repository root. Defaults to two levels above this script.

.OUTPUTS
    Prints a summary and exits 1 on any violation.

.EXAMPLE
    pwsh scripts/api/check-public-api-baselines.ps1
#>
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'

$srcDirectory = Join-Path $RepositoryRoot 'src'
$targetsPath = Join-Path $RepositoryRoot 'Directory.Build.targets'
$packagesPath = Join-Path $RepositoryRoot 'Directory.Packages.props'
$errors = [System.Collections.Generic.List[string]]::new()

function Add-Error {
    param([string]$Message)

    $errors.Add($Message)
}

if (-not (Test-Path -LiteralPath $srcDirectory)) {
    Write-Error "src directory not found at '$srcDirectory'."
    exit 2
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

if (-not (Test-Path -LiteralPath $targetsPath)) {
    Add-Error 'Directory.Build.targets is missing.'
}
else {
    $targetsText = Get-Content -LiteralPath $targetsPath -Raw

    if ($targetsText -notmatch 'Microsoft\.CodeAnalysis\.PublicApiAnalyzers') {
        Add-Error 'Directory.Build.targets does not reference Microsoft.CodeAnalysis.PublicApiAnalyzers.'
    }

    if ($targetsText -notmatch 'IsPackable') {
        Add-Error 'Directory.Build.targets does not scope the analyzer to IsPackable projects.'
    }
}

if (-not (Test-Path -LiteralPath $packagesPath)) {
    Add-Error 'Directory.Packages.props is missing.'
}
else {
    $packagesText = Get-Content -LiteralPath $packagesPath -Raw

    if ($packagesText -notmatch 'PackageVersion\s+Include="Microsoft\.CodeAnalysis\.PublicApiAnalyzers"\s+Version="[^"]+"') {
        Add-Error 'Directory.Packages.props does not pin Microsoft.CodeAnalysis.PublicApiAnalyzers.'
    }
}

$shippedTotal = 0
$unshippedTotal = 0

foreach ($project in $projects) {
    $relativeDirectory = [System.IO.Path]::GetRelativePath($RepositoryRoot, $project.DirectoryName)
    $seen = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::Ordinal)

    foreach ($kind in @('Shipped', 'Unshipped')) {
        $fileName = "PublicAPI.$kind.txt"
        $path = Join-Path $project.DirectoryName $fileName

        if (-not (Test-Path -LiteralPath $path)) {
            Add-Error "$relativeDirectory is missing $fileName."
            continue
        }

        $bytes = [System.IO.File]::ReadAllBytes($path)

        if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
            Add-Error "$relativeDirectory/$fileName has a UTF-8 BOM; use UTF-8 without BOM."
        }

        try {
            $text = [System.Text.UTF8Encoding]::new($false, $true).GetString($bytes)
        }
        catch {
            Add-Error "$relativeDirectory/$fileName is not valid UTF-8."
            continue
        }

        if ($text.Contains("`r", [System.StringComparison]::Ordinal)) {
            Add-Error "$relativeDirectory/$fileName must use LF line endings."
        }

        if (-not $text.EndsWith("`n", [System.StringComparison]::Ordinal)) {
            Add-Error "$relativeDirectory/$fileName must end with a newline."
        }

        $lines = $text -split "`n"
        $firstLine = if ($lines.Count -gt 0) { $lines[0] } else { '' }

        if ($firstLine -ne '#nullable enable') {
            Add-Error "$relativeDirectory/$fileName must start with '#nullable enable'."
        }

        $apiLines = @($lines | Where-Object { $_ -and -not $_.StartsWith('#', [System.StringComparison]::Ordinal) })

        if ($kind -eq 'Shipped') {
            $shippedTotal += $apiLines.Count

            if ($apiLines.Count -eq 0) {
                Add-Error "$relativeDirectory/PublicAPI.Shipped.txt contains no API lines."
            }
        }
        else {
            $unshippedTotal += $apiLines.Count
        }

        foreach ($line in $apiLines) {
            if ($seen.ContainsKey($line)) {
                Add-Error "$relativeDirectory contains the API line '$line' in both $($seen[$line]) and $fileName."
            }
            else {
                $seen[$line] = $fileName
            }
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host "PUBLIC API BASELINE CHECK FAILED ($($errors.Count) issue(s)):" -ForegroundColor Red

    foreach ($message in $errors) {
        Write-Host "  $message" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Public API baselines OK: $($projects.Count) packable project(s), $shippedTotal shipped line(s), $unshippedTotal unshipped line(s)." -ForegroundColor Green
