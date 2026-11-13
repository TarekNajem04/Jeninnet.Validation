#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

param(
    [string]$Solution = "Jeninnet.Validation.slnx",
    [string]$AllowListPath = "scripts/security/vulnerability-allow-list.json"
)

$ErrorActionPreference = "Stop"

$allowList = @()

if (Test-Path -LiteralPath $AllowListPath) {
    $allowList = @(Get-Content -LiteralPath $AllowListPath -Raw | ConvertFrom-Json)
}

$today = (Get-Date).Date
$activeAdvisories = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

foreach ($entry in $allowList) {
    if ([string]::IsNullOrWhiteSpace($entry.advisory)) {
        throw "Every allow-list entry must declare an advisory id."
    }

    if ($entry.expires) {
        $expiry = [datetime]::Parse($entry.expires, [System.Globalization.CultureInfo]::InvariantCulture)

        if ($expiry -lt $today) {
            throw "Allow-list entry '$($entry.advisory)' expired on $($entry.expires). Re-review it before continuing."
        }
    }

    [void]$activeAdvisories.Add($entry.advisory)
}

$rawOutput = (& dotnet list $Solution package --vulnerable --include-transitive --format json 2>&1) -join [Environment]::NewLine

if ($LASTEXITCODE -ne 0) {
    throw "dotnet list package failed with exit code $LASTEXITCODE.`n$rawOutput"
}

$jsonStart = $rawOutput.IndexOf('{')
$jsonEnd = $rawOutput.LastIndexOf('}')

if ($jsonStart -lt 0 -or $jsonEnd -le $jsonStart) {
    throw "dotnet list package did not return a JSON report.`n$rawOutput"
}

$report = $rawOutput.Substring($jsonStart, $jsonEnd - $jsonStart + 1) | ConvertFrom-Json
$findings = [System.Collections.Generic.List[object]]::new()

foreach ($project in @($report.projects)) {
    foreach ($framework in @($project.frameworks)) {
        $packages = @()

        if ($framework.topLevelPackages) {
            $packages += @($framework.topLevelPackages)
        }

        if ($framework.transitivePackages) {
            $packages += @($framework.transitivePackages)
        }

        foreach ($package in $packages) {
            foreach ($vulnerability in @($package.vulnerabilities)) {
                if ($null -eq $vulnerability) {
                    continue
                }

                $advisory = [string]$vulnerability.advisoryUrl
                $advisoryId = ($advisory -split '/')[-1]

                $findings.Add([pscustomobject]@{
                    Project = [string]$project.path
                    Package = [string]$package.id
                    ResolvedVersion = [string]$package.resolvedVersion
                    Severity = [string]$vulnerability.severity
                    Advisory = $advisoryId
                })
            }
        }
    }
}

$unexpected = @($findings | Where-Object { -not $activeAdvisories.Contains($_.Advisory) })

if ($unexpected.Count -gt 0) {
    $lines = $unexpected | ForEach-Object {
        "  $($_.Advisory) ($($_.Severity)) in $($_.Package) $($_.ResolvedVersion) — $($_.Project)"
    }

    throw "Unreviewed NuGet vulnerabilities were found:`n$($lines -join [Environment]::NewLine)"
}

$reviewed = @($findings | Select-Object -ExpandProperty Advisory -Unique)

if ($reviewed.Count -gt 0) {
    Write-Host "Vulnerability check passed with reviewed advisories: $($reviewed -join ', ')"
}
else {
    Write-Host "Vulnerability check passed: no vulnerable NuGet packages."
}
