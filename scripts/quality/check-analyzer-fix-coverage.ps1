#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
  Verifies that every analyzer/generator diagnostic has a documented fix status, that every code-fix
  provider is reflected in that status, and that the release-tracking files cover the catalog.

.DESCRIPTION
  The documentation table in docs/en/03-reference/03-diagnostics.md is the public matrix. A row is
  either "Available" (at least one code-fix provider advertises the ID) or "Intentional - <reason>"
  (there is no safe automatic fix). The check fails when a diagnostic is undocumented, a provider is
  not marked Available, an intentional omission has no reason, or a release-tracking row is missing
  or duplicated.

.PARAMETER Root
  Repository root. Defaults to two directories above this script.

.PARAMETER DiagnosticsPath
  Diagnostic descriptor source. Defaults to the analyzers project file.

.PARAMETER CodeFixesDirectory
  Directory containing the code-fix providers. Defaults to the analyzers project folder.

.PARAMETER DocumentationPath
  Public diagnostics reference. Defaults to docs/en/03-reference/03-diagnostics.md.

.PARAMETER ShippedPath
  AnalyzerReleases.Shipped.md. Defaults to the analyzers project file.

.PARAMETER UnshippedPath
  AnalyzerReleases.Unshipped.md. Defaults to the analyzers project file.

.OUTPUTS
  Prints the coverage summary. Exits 1 if any invariant is broken.

.EXAMPLE
  pwsh scripts/quality/check-analyzer-fix-coverage.ps1
#>

[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$DiagnosticsPath,
    [string]$CodeFixesDirectory,
    [string]$DocumentationPath,
    [string]$ShippedPath,
    [string]$UnshippedPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($DiagnosticsPath)) {
    $DiagnosticsPath = Join-Path $Root 'src/Jeninnet.Validation.Analyzers/Diagnostics/DiagnosticDescriptors.cs'
}
if ([string]::IsNullOrWhiteSpace($CodeFixesDirectory)) {
    $CodeFixesDirectory = Join-Path $Root 'src/Jeninnet.Validation.Analyzers/CodeFixes'
}
if ([string]::IsNullOrWhiteSpace($DocumentationPath)) {
    $DocumentationPath = Join-Path $Root 'docs/en/03-reference/03-diagnostics.md'
}
if ([string]::IsNullOrWhiteSpace($ShippedPath)) {
    $ShippedPath = Join-Path $Root 'src/Jeninnet.Validation.Analyzers/AnalyzerReleases.Shipped.md'
}
if ([string]::IsNullOrWhiteSpace($UnshippedPath)) {
    $UnshippedPath = Join-Path $Root 'src/Jeninnet.Validation.Analyzers/AnalyzerReleases.Unshipped.md'
}

foreach ($path in @($DiagnosticsPath, $CodeFixesDirectory, $DocumentationPath, $ShippedPath, $UnshippedPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Error "Analyzer fix coverage input not found: $path"
        exit 2
    }
}

$failures = [System.Collections.Generic.List[string]]::new()

# ---------------------------------------------------------------- descriptors
$descriptorMap = [ordered]@{}
$idToName = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::Ordinal)

$descriptorText = [System.IO.File]::ReadAllText($DiagnosticsPath)
$descriptorPattern = 'public static readonly DiagnosticDescriptor (?<name>\w+)\s*=\s*new\(\s*id:\s*"(?<id>VAL\d{4})"'

foreach ($match in [regex]::Matches($descriptorText, $descriptorPattern)) {
    $name = $match.Groups['name'].Value
    $id = $match.Groups['id'].Value

    if ($idToName.ContainsKey($id)) {
        $failures.Add("Diagnostic ID $id is declared more than once.")
        continue
    }

    $idToName[$id] = $name
    $descriptorMap[$name] = $id
}

if ($descriptorMap.Count -eq 0) {
    Write-Error "No diagnostic descriptors were found in $DiagnosticsPath"
    exit 2
}

# ---------------------------------------------------------------- code fixes
$providerIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$providerPattern = 'DiagnosticDescriptors\.(?<name>\w+)\.Id'

foreach ($file in Get-ChildItem -LiteralPath $CodeFixesDirectory -Filter '*.cs' -File) {
    $text = [System.IO.File]::ReadAllText($file.FullName)

    foreach ($match in [regex]::Matches($text, $providerPattern)) {
        $name = $match.Groups['name'].Value

        if (-not $descriptorMap.Contains($name)) {
            $failures.Add("Code fix $($file.Name) references unknown descriptor $name.")
            continue
        }

        [void]$providerIds.Add($descriptorMap[$name])
    }
}

# ---------------------------------------------------------------- documentation matrix
$rows = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
$rowPattern = '^\| `(?<id>VAL\d{4})` \| (?<severity>[^|]+) \| (?<title>[^|]+) \| (?<fix>[^|]*) \| (?<status>[^|]+) \|$'

foreach ($line in ([System.IO.File]::ReadAllText($DocumentationPath) -split "`r?`n")) {
    $match = [regex]::Match($line, $rowPattern)

    if (-not $match.Success) {
        continue
    }

    $id = $match.Groups['id'].Value

    if ($rows.ContainsKey($id)) {
        $failures.Add("Diagnostic $id has more than one documentation row.")
        continue
    }

    $rows[$id] = [pscustomobject]@{
        Status = $match.Groups['status'].Value.Trim()
        Fix = $match.Groups['fix'].Value.Trim()
    }
}

foreach ($id in $idToName.Keys) {
    if (-not $rows.ContainsKey($id)) {
        $failures.Add("Diagnostic $id is not documented in $DocumentationPath.")
    }
}

foreach ($id in $rows.Keys) {
    if (-not $idToName.ContainsKey($id)) {
        $failures.Add("Documentation row $id has no matching diagnostic descriptor.")
    }
}

foreach ($id in $providerIds) {
    if ($rows.ContainsKey($id) -and $rows[$id].Status -ne 'Available') {
        $failures.Add("Diagnostic $id has a code-fix provider but is not marked Available.")
    }
}

foreach ($id in $idToName.Keys) {
    if ($providerIds.Contains($id)) {
        continue
    }

    if (-not $rows.ContainsKey($id)) {
        continue
    }

    if ($rows[$id].Status -notmatch '^Intentional\s+[\u2014-]\s+(?<reason>.{8,})$') {
        $failures.Add("Diagnostic $id has no code fix and no intentional-omission reason.")
    }
}

# ---------------------------------------------------------------- release tracking
$releaseIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$releasePattern = '^(?<id>VAL\d{4})\s*\|'

foreach ($path in @($ShippedPath, $UnshippedPath)) {
    foreach ($line in ([System.IO.File]::ReadAllText($path) -split "`r?`n")) {
        $match = [regex]::Match($line, $releasePattern)

        if (-not $match.Success) {
            continue
        }

        $id = $match.Groups['id'].Value

        if (-not $releaseIds.Add($id)) {
            $failures.Add("Release tracking lists $id more than once.")
        }
    }
}

foreach ($id in $idToName.Keys) {
    if (-not $releaseIds.Contains($id)) {
        $failures.Add("Release tracking does not list $id.")
    }
}

foreach ($id in $releaseIds) {
    if (-not $idToName.ContainsKey($id)) {
        $failures.Add("Release tracking lists unknown diagnostic $id.")
    }
}

# ---------------------------------------------------------------- summary
$available = 0
$intentional = 0

foreach ($id in $idToName.Keys) {
    if (-not $rows.ContainsKey($id)) {
        continue
    }

    if ($rows[$id].Status -eq 'Available') {
        $available++
    } elseif ($rows[$id].Status.StartsWith('Intentional', [System.StringComparison]::Ordinal)) {
        $intentional++
    }
}

$missing = $idToName.Count - $available - $intentional

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Host "FAIL: $failure"
    }

    Write-Host "Analyzer fix coverage FAILED: $($failures.Count) invariant(s) broken."
    exit 1
}

Write-Host "Analyzer fix coverage OK: $($idToName.Count) diagnostic(s); $available available; $intentional intentional; $missing missing."
Write-Host "Release tracking: $($releaseIds.Count) row(s) across shipped and unshipped."
exit 0
