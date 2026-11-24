#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
  Verifies that the project names and the claims in README.md are backed by reality: every
  backticked "Jeninnet.Validation.*" name must exist as a project, the "N packable projects"
  claim must match src/, the test count must match the release notes and ROADMAP, the AOT counts
  must match the support matrix, the preview wording must distinguish branch from publication,
  and the generator registration wording must match the emitter source.

.PARAMETER ReadmePath
  Repo README.md. Defaults to <repo>/README.md.

.PARAMETER SourceDirectory
  Shipping source directory whose projects declare IsPackable=true. Defaults to <repo>/src.

.OUTPUTS
  Prints the checked claims, then a summary. Exits 1 if any claim is not backed.

.EXAMPLE
  pwsh scripts/docs/check-readme-claims.ps1
#>
param(
    [string]$ReadmePath = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path 'README.md'),
    [string]$SourceDirectory = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path 'src')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ReadmePath)) {
    Write-Error "README not found: $ReadmePath"
    exit 2
}

$repositoryRoot = Split-Path -Parent $ReadmePath

# ---------------------------------------------------------------- project-name claims
$projectDirectories = @('src', 'integrations', 'samples', 'tests', 'consumers')
$knownProjects = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($directory in $projectDirectories) {
    $path = Join-Path $repositoryRoot $directory
    if (-not (Test-Path -LiteralPath $path)) { continue }
    foreach ($project in Get-ChildItem $path -Recurse -Filter '*.csproj' -File) {
        [void]$knownProjects.Add($project.BaseName)
    }
}

$readme = [System.IO.File]::ReadAllText($ReadmePath)
$tokens = [regex]::Matches($readme, '`(Jeninnet\.Validation\.[A-Za-z0-9._*]+)`') |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique

$unresolved = [System.Collections.Generic.List[string]]::new()
foreach ($token in $tokens) {
    if ($token.EndsWith('.*')) {
        $prefix = $token.Substring(0, $token.Length - 2)
        $match = $knownProjects | Where-Object { $_.StartsWith($prefix, [System.StringComparison]::Ordinal) } | Select-Object -First 1
        if ($null -eq $match) {
            [void]$unresolved.Add("$token (no project with prefix '$prefix')")
        }
        continue
    }

    if (-not $knownProjects.Contains($token)) {
        [void]$unresolved.Add("$token (no such project)")
    }
}

# ---------------------------------------------------------------- packable-count claim
$countFailures = [System.Collections.Generic.List[string]]::new()
$packableCount = 0
if (Test-Path -LiteralPath $SourceDirectory) {
    $packableCount = @(
        Get-ChildItem $SourceDirectory -Recurse -Filter '*.csproj' -File |
            Where-Object { (Get-Content -LiteralPath $_.FullName -Raw) -match '<IsPackable>true</IsPackable>' }
    ).Count
}

$countMatch = [regex]::Match($readme, '(?<count>\d+)\s+packable\s+projects')
if ($countMatch.Success) {
    $claimed = [int]$countMatch.Groups['count'].Value
    if ($claimed -ne $packableCount) {
        [void]$countFailures.Add("README claims $claimed packable projects, but $packableCount src project(s) declare IsPackable=true")
    }
}
else {
    [void]$countFailures.Add("README does not state the 'N packable projects' count that this gate verifies")
}

# ---------------------------------------------------------------- test-count consistency
$claimFailures = [System.Collections.Generic.List[string]]::new()

$propsPath = Join-Path $repositoryRoot 'Directory.Build.props'
$version = $null
if (Test-Path -LiteralPath $propsPath) {
    $props = [System.IO.File]::ReadAllText($propsPath)
    $versionMatch = [regex]::Match($props, '<Version>(?<version>[^<]+)</Version>')
    if ($versionMatch.Success) {
        $version = $versionMatch.Groups['version'].Value.Trim()
    }
}

if ([string]::IsNullOrWhiteSpace($version)) {
    [void]$claimFailures.Add('Directory.Build.props has no <Version> value for claim verification')
}
else {
    $releaseNotesPath = Join-Path $repositoryRoot ".github/release-notes/v$version.md"
    $roadmapPath = Join-Path $repositoryRoot 'ROADMAP.md'
    $readmeCountMatch = [regex]::Match($readme, 'current suite runs \*\*(?<count>[\d,]+) tests\*\*')

    if (-not $readmeCountMatch.Success) {
        [void]$claimFailures.Add('README does not state the current test count in the expected form')
    }
    elseif (-not (Test-Path -LiteralPath $releaseNotesPath)) {
        [void]$claimFailures.Add("Release notes not found for version $version`: $releaseNotesPath")
    }
    elseif (-not (Test-Path -LiteralPath $roadmapPath)) {
        [void]$claimFailures.Add("ROADMAP.md not found: $roadmapPath")
    }
    else {
        $releaseText = [System.IO.File]::ReadAllText($releaseNotesPath)
        $roadmapText = [System.IO.File]::ReadAllText($roadmapPath)
        $releaseCountMatch = [regex]::Match($releaseText, 'Tests:\s*(?<count>[\d,]+)/')
        $roadmapCountMatch = [regex]::Match($roadmapText, '\|\s*Tests\s*\|\s*\*\*Verified:\*\*\s*(?<count>[\d,]+)/')

        if (-not $releaseCountMatch.Success) {
            [void]$claimFailures.Add("Release notes v$version do not state 'Tests: N/N'")
        }

        if (-not $roadmapCountMatch.Success) {
            [void]$claimFailures.Add('ROADMAP.md does not state its verified test count')
        }

        if ($releaseCountMatch.Success -and $roadmapCountMatch.Success) {
            $readmeCount = [int]($readmeCountMatch.Groups['count'].Value -replace ',', '')
            $releaseCount = [int]($releaseCountMatch.Groups['count'].Value -replace ',', '')
            $roadmapCount = [int]($roadmapCountMatch.Groups['count'].Value -replace ',', '')

            if ($readmeCount -ne $releaseCount -or $readmeCount -ne $roadmapCount) {
                [void]$claimFailures.Add("Test count mismatch: README $readmeCount, release notes $releaseCount, ROADMAP $roadmapCount")
            }
        }
    }
}

# ---------------------------------------------------------------- AOT-count claim
$aotMatrixPath = Join-Path $repositoryRoot 'scripts/quality/aot-support-matrix.json'
$aotDocPath = Join-Path $repositoryRoot 'docs/en/04-operations/02-aot-and-trimming.md'

if (-not (Test-Path -LiteralPath $aotMatrixPath)) {
    [void]$claimFailures.Add("AOT matrix not found: $aotMatrixPath")
}
elseif (-not (Test-Path -LiteralPath $aotDocPath)) {
    [void]$claimFailures.Add("AOT documentation not found: $aotDocPath")
}
else {
    try {
        $matrix = Get-Content -LiteralPath $aotMatrixPath -Raw | ConvertFrom-Json
        $expectedAot = @{}
        foreach ($position in @('aot-compatible', 'trim-compatible-only', 'reflection-dependent', 'unsupported')) {
            $expectedAot[$position] = @($matrix.packages | Where-Object { $_.position -eq $position }).Count
        }

        $aotDoc = [System.IO.File]::ReadAllText($aotDocPath)
        $aotCountsMatch = [regex]::Match($aotDoc, '(?s)\*\*Counts:\*\*\s*(?<aot>\d+)\s+AOT-compatible.*?(?<trim>\d+)\s+trim-compatible only.*?(?<refl>\d+)\s+reflection-dependent.*?(?<unsupported>\d+)\s+unsupported')

        if (-not $aotCountsMatch.Success) {
            [void]$claimFailures.Add('AOT page does not state the four package-position counts in the expected form')
        }
        else {
            $documentedAot = @{
                'aot-compatible'         = [int]$aotCountsMatch.Groups['aot'].Value
                'trim-compatible-only'   = [int]$aotCountsMatch.Groups['trim'].Value
                'reflection-dependent'   = [int]$aotCountsMatch.Groups['refl'].Value
                'unsupported'            = [int]$aotCountsMatch.Groups['unsupported'].Value
            }

            foreach ($position in $expectedAot.Keys) {
                if ($documentedAot[$position] -ne $expectedAot[$position]) {
                    [void]$claimFailures.Add("AOT count mismatch for '$position': docs $($documentedAot[$position]), matrix $($expectedAot[$position])")
                }
            }
        }
    }
    catch {
        [void]$claimFailures.Add("AOT claim check failed: $($_.Exception.Message)")
    }
}

# ---------------------------------------------------------------- preview-state wording
$stateFiles = @(
    'docs/en/01-getting-started/01-introduction.md',
    'docs/en/01-getting-started/02-installation.md',
    'docs/en/03-reference/06-compatibility-and-versions.md',
    'docs/en/04-operations/05-faq.md',
    'docs/en/appendix/02-package-map.md',
    'CONTRIBUTING.md'
)
$previewPhrase = 'implemented on the release branch; it is not yet published'

foreach ($relativePath in $stateFiles) {
    $statePath = Join-Path $repositoryRoot $relativePath
    if (-not (Test-Path -LiteralPath $statePath)) {
        [void]$claimFailures.Add("Preview-state file not found: $relativePath")
        continue
    }

    $normalizedState = ([System.IO.File]::ReadAllText($statePath)) -replace '\s+', ' '
    if ($normalizedState -notmatch [regex]::Escape($previewPhrase)) {
        [void]$claimFailures.Add("$relativePath does not distinguish 'implemented on branch' from 'published'")
    }
}

foreach ($relativePath in @('CHANGELOG.md') + $stateFiles) {
    $statePath = Join-Path $repositoryRoot $relativePath
    if (-not (Test-Path -LiteralPath $statePath)) { continue }

    $normalizedState = ([System.IO.File]::ReadAllText($statePath)) -replace '\s+', ' '
    if ($normalizedState -match 'in preparation') {
        [void]$claimFailures.Add("$relativePath still uses the ambiguous phrase 'in preparation'")
    }
}

# ---------------------------------------------------------------- generator-registration claim
$generatorChecks = @(
    [pscustomobject]@{
        Path   = 'src/Jeninnet.Validation.Analyzers/Generators/Emitter/GeneratedValidatorRegistryEmitter.cs'
        Tokens = @('ModuleInitializer', 'AddGeneratedValidatorsFromThisAssembly')
    },
    [pscustomobject]@{
        Path   = 'src/Jeninnet.Validation.Analyzers/README.md'
        Tokens = @('module initializer', 'AddGeneratedValidatorsFromThisAssembly')
    },
    [pscustomobject]@{
        Path   = 'docs/en/04-operations/02-aot-and-trimming.md'
        Tokens = @('AddGeneratedValidatorsFromThisAssembly')
    }
)

foreach ($generatorCheck in $generatorChecks) {
    $generatorPath = Join-Path $repositoryRoot $generatorCheck.Path
    if (-not (Test-Path -LiteralPath $generatorPath)) {
        [void]$claimFailures.Add("Generator claim source not found: $($generatorCheck.Path)")
        continue
    }

    $generatorText = [System.IO.File]::ReadAllText($generatorPath)
    foreach ($token in $generatorCheck.Tokens) {
        if (-not [regex]::IsMatch($generatorText, [regex]::Escape($token), [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
            [void]$claimFailures.Add("$($generatorCheck.Path) does not mention '$token'")
        }
    }
}

Write-Host "Checked $($tokens.Count) project-name claim(s) against $($knownProjects.Count) solution project(s)."
Write-Host "Packable-project claim: $(if ($countMatch.Success) { $countMatch.Groups['count'].Value } else { '(absent)' }) vs $packableCount src project(s) with IsPackable=true."
Write-Host "Test-count claim: README $(if ($readmeCountMatch.Success) { $readmeCountMatch.Groups['count'].Value } else { '(absent)' }); release notes and ROADMAP cross-checked."
Write-Host "AOT-count claim: docs cross-checked against scripts/quality/aot-support-matrix.json."
Write-Host "Preview wording: $($stateFiles.Count) state file(s) checked for branch/publication clarity."
Write-Host "Generator registration: emitter and analyzer README checked for the module initializer and generated-registry entry point."

if ($unresolved.Count -gt 0 -or $countFailures.Count -gt 0 -or $claimFailures.Count -gt 0) {
    $totalFailures = $unresolved.Count + $countFailures.Count + $claimFailures.Count
    Write-Host ''
    Write-Host "README CLAIMS FAILED ($totalFailures problem(s)):" -ForegroundColor Red
    $unresolved | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $countFailures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $claimFailures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}

Write-Host 'README claims OK: every checked claim is backed by the repository.' -ForegroundColor Green
