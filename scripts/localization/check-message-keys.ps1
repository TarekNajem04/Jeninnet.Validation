#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Verifies that every ValidationMessageKeys constant has a pack entry and vice versa.

.DESCRIPTION
    Reads the message-key constants from ValidationMessageKeys.cs and compares them with every
    Resources/*.json pack of the Localization package, per culture:

      - A constant without a pack entry (missing key) is an error.
      - A pack entry without a constant (extra key) is an error unless it is allow-listed.
      - An allow-list entry that matches nothing is an error (no stale entries).
      - A non-string or empty template is an error.

    Custom consumer keys (rule-template keys, caller-supplied codes) are intentionally outside
    this gate: they are neither constants nor pack entries. The allow-list covers only the
    opposite case: a pack entry that is deliberate despite having no constant.

    Writes a deterministic report (ordinal-sorted) to the report directory and exits 1 on any
    violation. The report directory defaults to artifacts/message-keys and is git-ignored.

.PARAMETER RepositoryRoot
    The repository root. Defaults to two levels above this script.

.PARAMETER ReportDirectory
    Where report.json and report.md are written. Created when missing.

.PARAMETER NoReport
    Skips writing the report files; only prints the result.

.OUTPUTS
    Prints a summary and exits 1 on any violation.

.EXAMPLE
    pwsh scripts/localization/check-message-keys.ps1

.EXAMPLE
    pwsh scripts/localization/check-message-keys.ps1 -NoReport
#>
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$ReportDirectory = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path 'artifacts\message-keys'),
    [string]$KeysPath = '',
    [string]$ResourcesPath = '',
    [string]$AllowListPath = '',
    [switch]$NoReport
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($KeysPath)) {
    $KeysPath = Join-Path $RepositoryRoot 'src\Jeninnet.Validation.Abstractions\Configuration\ValidationMessageKeys.cs'
}

if ([string]::IsNullOrWhiteSpace($ResourcesPath)) {
    $ResourcesPath = Join-Path $RepositoryRoot 'src\Jeninnet.Validation.Localization\Resources'
}

if ([string]::IsNullOrWhiteSpace($AllowListPath)) {
    $AllowListPath = Join-Path $RepositoryRoot 'src\Jeninnet.Validation.Localization\MessageKeyAllowList.json'
}

$keysPath = $KeysPath
$resourcesPath = $ResourcesPath
$allowListPath = $AllowListPath
$errors = [System.Collections.Generic.List[string]]::new()

function Add-Error {
    param([string]$Message)

    $errors.Add($Message)
}

if (-not (Test-Path -LiteralPath $keysPath)) {
    Write-Error "Message-key source not found at '$keysPath'."
    exit 2
}

if (-not (Test-Path -LiteralPath $resourcesPath)) {
    Write-Error "Message-pack directory not found at '$resourcesPath'."
    exit 2
}

if (-not (Test-Path -LiteralPath $allowListPath)) {
    Write-Error "Allow-list not found at '$allowListPath'."
    exit 2
}

# Source of truth: public const string NAME = "Value";
# Note: the value-keyed lookup lives in a HashSet, not a Dictionary. A value-keyed
# Dictionary would contain the key "Count" (the COUNT constant), and PowerShell resolves
# $dict.Count to that entry instead of the Count property.
$constantPattern = 'public\s+const\s+string\s+(?<name>[A-Za-z0-9_]+)\s*=\s*"(?<value>(?:[^"\\]|\\.)*)"\s*;'
$constantsByName = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::Ordinal)
$constantValues = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$valueToName = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::Ordinal)
[int]$constantCount = 0

foreach ($match in [regex]::Matches([System.IO.File]::ReadAllText($keysPath), $constantPattern)) {
    $name = $match.Groups['name'].Value
    $value = $match.Groups['value'].Value
    $constantCount++

    if (-not $constantsByName.TryAdd($name, $value)) {
        Add-Error "ValidationMessageKeys declares '$name' more than once."
        continue
    }

    if (-not $valueToName.TryAdd($value, $name)) {
        Add-Error "ValidationMessageKeys maps '$name' to duplicate value '$value' (already used by '$($valueToName[$value])')."
        continue
    }

    [void]$constantValues.Add($value)
}

if ($constantCount -eq 0) {
    Add-Error 'No message-key constants were found; the source-of-truth file may have moved.'
}

# Allow-list: JSON array of pack keys that may exist without a constant.
# Note: ConvertFrom-Json unwraps a single-element array to a bare string, so wrap with @().
# A top-level JSON object is rejected by checking the raw text first.
try {
    $allowListRaw = [System.IO.File]::ReadAllText($allowListPath)
    $allowListItems = @($allowListRaw | ConvertFrom-Json)
    $allowListIsArray = $allowListRaw.TrimStart().StartsWith('[')
}
catch {
    Add-Error "Allow-list at '$allowListPath' is not valid JSON: $($_.Exception.Message)."
    $allowListItems = @()
    $allowListIsArray = $true
}

$allowedExtras = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

if (-not $allowListIsArray) {
    Add-Error 'Allow-list must be a JSON array of strings.'
}
else {
    foreach ($entry in $allowListItems) {
        if ($entry -isnot [string] -or [string]::IsNullOrWhiteSpace($entry)) {
            Add-Error 'Allow-list entries must be non-empty strings.'
            continue
        }

        if (-not $allowedExtras.Add($entry)) {
            Add-Error "Allow-list contains '$entry' more than once."
        }
    }
}

$packFiles = @(Get-ChildItem -LiteralPath $resourcesPath -Filter '*.json' -File | Sort-Object Name)

if ($packFiles.Count -eq 0) {
    Add-Error "No message packs found in '$resourcesPath'."
}

$cultures = [System.Collections.Generic.SortedDictionary[string, string[]]]::new([System.StringComparer]::Ordinal)
$missing = [System.Collections.Generic.SortedDictionary[string, string[]]]::new([System.StringComparer]::Ordinal)
$extra = [System.Collections.Generic.SortedDictionary[string, string[]]]::new([System.StringComparer]::Ordinal)
$usedAllowList = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

foreach ($packFile in $packFiles) {
    $culture = [System.IO.Path]::GetFileNameWithoutExtension($packFile.Name)

    try {
        $pack = Get-Content -LiteralPath $packFile.FullName -Raw | ConvertFrom-Json -AsHashtable
    }
    catch {
        Add-Error "Pack '$culture' is not valid JSON: $($_.Exception.Message)."
        continue
    }

    $keys = @($pack.Keys | Sort-Object -CaseSensitive)

    foreach ($key in $keys) {
        $template = $pack[$key]

        if ($template -isnot [string]) {
            Add-Error "Pack '$culture' key '$key' must be a string template."
        }
        elseif ([string]::IsNullOrWhiteSpace($template)) {
            Add-Error "Pack '$culture' key '$key' must not be empty."
        }
    }

    $cultures[$culture] = $keys

    $missingKeys = @($constantValues | Where-Object { -not $pack.ContainsKey($_) } | Sort-Object -CaseSensitive)
    $extraKeys = @($keys | Where-Object { -not $constantValues.Contains($_) } | Sort-Object -CaseSensitive)

    if ($missingKeys.Count -gt 0) {
        $missing[$culture] = $missingKeys

        foreach ($key in $missingKeys) {
            Add-Error "Pack '$culture' is missing key '$key' (constant '$($valueToName[$key])')."
        }
    }

    $unlisted = @($extraKeys | Where-Object { -not $allowedExtras.Contains($_) })

    if ($unlisted.Count -gt 0) {
        $extra[$culture] = $unlisted

        foreach ($key in $unlisted) {
            Add-Error "Pack '$culture' has extra key '$key' with no matching constant."
        }
    }

    foreach ($key in $extraKeys) {
        if ($allowedExtras.Contains($key)) {
            [void]$usedAllowList.Add($key)
        }
    }
}

foreach ($entry in ($allowedExtras | Sort-Object -CaseSensitive)) {
    if (-not $usedAllowList.Contains($entry)) {
        Add-Error "Allow-list entry '$entry' matches no pack key; remove the stale entry."
    }
}

if (-not $NoReport) {
    New-Item -ItemType Directory -Path $ReportDirectory -Force | Out-Null

    $report = [ordered]@{
        constants = @($constantValues | Sort-Object -CaseSensitive)
        cultures = [ordered]@{}
        missing = [ordered]@{}
        extra = [ordered]@{}
        allowList = @($allowedExtras | Sort-Object -CaseSensitive)
        staleAllowList = @($allowedExtras | Where-Object { -not $usedAllowList.Contains($_) } | Sort-Object -CaseSensitive)
    }

    foreach ($culture in $cultures.Keys) {
        $report.cultures[$culture] = $cultures[$culture]
    }

    foreach ($culture in $missing.Keys) {
        $report.missing[$culture] = $missing[$culture]
    }

    foreach ($culture in $extra.Keys) {
        $report.extra[$culture] = $extra[$culture]
    }

    $json = $report | ConvertTo-Json -Depth 5
    [System.IO.File]::WriteAllText(
        (Join-Path $ReportDirectory 'report.json'),
        $json.Replace("`r`n", "`n") + "`n",
        [System.Text.UTF8Encoding]::new($false))

    $markdown = [System.Text.StringBuilder]::new()
    [void]$markdown.AppendLine('# Message-key catalog report')
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine("- Constants: $constantCount")
    [void]$markdown.AppendLine("- Cultures: $($cultures.Count)")
    [void]$markdown.AppendLine("- Missing: $(($missing.Values | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum)")
    [void]$markdown.AppendLine("- Extra: $(($extra.Values | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum)")
    [void]$markdown.AppendLine("- Allow-listed: $($usedAllowList.Count)")

    if ($missing.Count -gt 0) {
        [void]$markdown.AppendLine()
        [void]$markdown.AppendLine('## Missing keys')
        [void]$markdown.AppendLine()
        [void]$markdown.AppendLine('| Culture | Key | Constant |')
        [void]$markdown.AppendLine('|---|---|---|')

        foreach ($culture in $missing.Keys) {
            foreach ($key in $missing[$culture]) {
                [void]$markdown.AppendLine("| $culture | $key | $($valueToName[$key]) |")
            }
        }
    }

    if ($extra.Count -gt 0) {
        [void]$markdown.AppendLine()
        [void]$markdown.AppendLine('## Extra keys')
        [void]$markdown.AppendLine()
        [void]$markdown.AppendLine('| Culture | Key |')
        [void]$markdown.AppendLine('|---|---|')

        foreach ($culture in $extra.Keys) {
            foreach ($key in $extra[$culture]) {
                [void]$markdown.AppendLine("| $culture | $key |")
            }
        }
    }

    [System.IO.File]::WriteAllText(
        (Join-Path $ReportDirectory 'report.md'),
        $markdown.ToString().Replace("`r`n", "`n"),
        [System.Text.UTF8Encoding]::new($false))
}

if ($errors.Count -gt 0) {
    Write-Host "MESSAGE KEY CHECK FAILED ($($errors.Count) issue(s)):" -ForegroundColor Red

    foreach ($message in ($errors | Sort-Object -CaseSensitive)) {
        Write-Host "  $message" -ForegroundColor Red
    }

    exit 1
}

[int]$cultureCount = $cultures.Count
[int]$allowListedCount = $usedAllowList.Count
Write-Host "Message keys OK: $constantCount constant(s) match $cultureCount pack(s); allow-list covers $allowListedCount extra key(s)." -ForegroundColor Green
