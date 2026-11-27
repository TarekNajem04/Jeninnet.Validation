#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
  Verifies that every shipped localization culture has an explicit native-review status.

.DESCRIPTION
  The status manifest is a declaration, not a claim of executed review. Every resource pack must
  have exactly one entry. A reviewed entry requires a reviewer and a yyyy-MM-dd date; machine-assisted
  and needs-review entries must not carry reviewer/date fields that imply review.

  -SelfTest proves the gate fails when a resource has no status entry.

.PARAMETER SelfTest
  Run a synthetic missing-entry case and exit 0 only when the gate reports it.

.PARAMETER RepositoryRoot
  Repository root. Defaults to two directories above this script.

.PARAMETER ManifestPath
  Review-status manifest. Defaults to scripts/quality/localization-review-status.json.

.PARAMETER ResourcesPath
  Localization resource directory. Defaults to
  src/Jeninnet.Validation.Localization/Resources.

.EXAMPLE
  pwsh scripts/quality/check-localization-review-status.ps1

.EXAMPLE
  pwsh scripts/quality/check-localization-review-status.ps1 -SelfTest
#>
[CmdletBinding()]
param(
    [switch]$SelfTest,
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$ManifestPath,
    [string]$ResourcesPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $RepositoryRoot 'scripts/quality/localization-review-status.json'
}
if ([string]::IsNullOrWhiteSpace($ResourcesPath)) {
    $ResourcesPath = Join-Path $RepositoryRoot 'src/Jeninnet.Validation.Localization/Resources'
}

$allowedStatuses = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
[void]$allowedStatuses.Add('machine-assisted')
[void]$allowedStatuses.Add('needs-review')
[void]$allowedStatuses.Add('reviewed')

function Test-ReviewStatus($manifest, [string[]]$cultures) {
    $errors = [System.Collections.Generic.List[string]]::new()

    if ($manifest.schemaVersion -ne 1) {
        [void]$errors.Add("Unsupported review-status schema version '$($manifest.schemaVersion)'.")
    }

    $entries = @($manifest.statuses)
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $resourceSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($culture in $cultures) {
        [void]$resourceSet.Add($culture)
    }

    foreach ($entry in $entries) {
        $culture = [string]$entry.culture
        $status = [string]$entry.status

        if ([string]::IsNullOrWhiteSpace($culture)) {
            [void]$errors.Add('A review-status entry has no culture.')
            continue
        }

        if (-not $seen.Add($culture)) {
            [void]$errors.Add("Culture '$culture' has more than one review-status entry.")
        }

        if (-not $resourceSet.Contains($culture)) {
            [void]$errors.Add("Culture '$culture' has a review status but no resource pack.")
        }

        if (-not $allowedStatuses.Contains($status)) {
            [void]$errors.Add("Culture '$culture' has unsupported status '$status'.")
        }

        $hasReviewer = -not [string]::IsNullOrWhiteSpace([string]$entry.reviewer)
        $hasDate = -not [string]::IsNullOrWhiteSpace([string]$entry.reviewedOn)

        if ($status -eq 'reviewed') {
            if (-not $hasReviewer) {
                [void]$errors.Add("Culture '$culture' is reviewed but has no reviewer.")
            }

            if (-not $hasDate) {
                [void]$errors.Add("Culture '$culture' is reviewed but has no reviewedOn date.")
            }
            else {
                $parsed = [datetime]::MinValue

                if (-not [datetime]::TryParseExact(
                    [string]$entry.reviewedOn,
                    'yyyy-MM-dd',
                    [System.Globalization.CultureInfo]::InvariantCulture,
                    [System.Globalization.DateTimeStyles]::None,
                    [ref]$parsed)) {
                    [void]$errors.Add("Culture '$culture' has an invalid reviewedOn date '$($entry.reviewedOn)'.")
                }
            }
        }
        elseif ($hasReviewer -or $hasDate) {
            [void]$errors.Add("Culture '$culture' is '$status' but carries reviewer/date fields that imply review.")
        }

        if ([string]::IsNullOrWhiteSpace([string]$entry.note)) {
            [void]$errors.Add("Culture '$culture' has no review-status note.")
        }
    }

    foreach ($culture in $resourceSet) {
        if (-not $seen.Contains($culture)) {
            [void]$errors.Add("Culture '$culture' has a resource pack but no review-status entry.")
        }
    }

    return $errors
}

if ($SelfTest) {
    $syntheticManifest = [pscustomobject]@{
        schemaVersion = 1
        statuses = @(
            [pscustomobject]@{
                culture = 'en'
                status = 'machine-assisted'
                reviewer = $null
                reviewedOn = $null
                note = 'synthetic'
            }
        )
    }
    $errors = @(Test-ReviewStatus -manifest $syntheticManifest -cultures @('en', 'ar'))

    if (-not ($errors | Where-Object { $_ -like "*'ar'*" })) {
        throw 'The self-test did not report the missing culture status.'
    }

    Write-Host 'Localization review-status gate self-test OK: a culture without a status is rejected.'
    exit 0
}

foreach ($path in @($ManifestPath, $ResourcesPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Error "Localization review-status input not found: $path"
        exit 2
    }
}

$manifest = [System.IO.File]::ReadAllText($ManifestPath) | ConvertFrom-Json
$cultures = @(
    Get-ChildItem -LiteralPath $ResourcesPath -Filter '*.json' -File |
        ForEach-Object { $_.BaseName } |
        Sort-Object
)
$errors = @(Test-ReviewStatus -manifest $manifest -cultures $cultures)

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Host "ERROR: $_" -ForegroundColor Red }
    exit 1
}

$counts = @{}
foreach ($entry in @($manifest.statuses)) {
    $status = [string]$entry.status
    $counts[$status] = 1 + ($counts[$status] ?? 0)
}

$summary = ($counts.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join ', '
Write-Host "Localization review status OK: $($cultures.Count) cultures ($summary)."
