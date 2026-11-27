#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
  Generates the public diagnostic gallery from the gallery data file.

.DESCRIPTION
  The gallery data file is the single source for the learning content (trigger, compiler output,
  fix, and principle). Diagnostic IDs, titles, severities, and fix availability are cross-checked
  against DiagnosticDescriptors.cs and the code-fix providers before any page is written.

  -Check regenerates everything in memory and compares the bytes with the checked-in pages. It
  fails on a missing page, an extra page, or any content drift. This is the deterministic CI gate.

.PARAMETER Check
  Compare generated output with the checked-in pages instead of writing files.

.PARAMETER Root
  Repository root. Defaults to two directories above this script.

.PARAMETER SourcePath
  Gallery data file. Defaults to docs/en/03-reference/09-diagnostic-gallery/diagnostics.json.

.PARAMETER OutputDirectory
  Gallery output directory. Defaults to the source file's directory.

.PARAMETER DiagnosticsPath
  Diagnostic descriptor source. Defaults to the analyzers project file.

.PARAMETER CodeFixesDirectory
  Directory containing code-fix providers. Defaults to the analyzers project folder.

.EXAMPLE
  pwsh scripts/docs/generate-diagnostic-gallery.ps1

.EXAMPLE
  pwsh scripts/docs/generate-diagnostic-gallery.ps1 -Check
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$SourcePath,
    [string]$OutputDirectory,
    [string]$DiagnosticsPath,
    [string]$CodeFixesDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Join-Path $Root 'docs/en/03-reference/09-diagnostic-gallery/diagnostics.json'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Split-Path -Parent $SourcePath
}
if ([string]::IsNullOrWhiteSpace($DiagnosticsPath)) {
    $DiagnosticsPath = Join-Path $Root 'src/Jeninnet.Validation.Analyzers/Diagnostics/DiagnosticDescriptors.cs'
}
if ([string]::IsNullOrWhiteSpace($CodeFixesDirectory)) {
    $CodeFixesDirectory = Join-Path $Root 'src/Jeninnet.Validation.Analyzers/CodeFixes'
}

foreach ($path in @($SourcePath, $DiagnosticsPath, $CodeFixesDirectory)) {
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Error "Diagnostic gallery input not found: $path"
        exit 2
    }
}

$failures = [System.Collections.Generic.List[string]]::new()

function Add-Failure([string]$Message) {
    [void]$failures.Add($Message)
}

function Read-DescriptorRows {
    $text = [System.IO.File]::ReadAllText($DiagnosticsPath)
    $pattern = '(?s)public static readonly DiagnosticDescriptor (?<name>\w+)\s*=\s*new\(\s*id:\s*"(?<id>VAL\d{4})",\s*title:\s*"(?<title>(?:[^"\\]|\\.)*)",\s*messageFormat:\s*"(?<message>(?:[^"\\]|\\.)*)",\s*category:\s*[^,]+,\s*defaultSeverity:\s*DiagnosticSeverity\.(?<severity>\w+)'
    $rows = [ordered]@{}

    foreach ($match in [regex]::Matches($text, $pattern)) {
        $id = $match.Groups['id'].Value

        if ($rows.Contains($id)) {
            Add-Failure "Descriptor $id is declared more than once."
            continue
        }

        $rows[$id] = [pscustomobject]@{
            Name = $match.Groups['name'].Value
            Id = $id
            Title = $match.Groups['title'].Value.Replace('\"', '"')
            Message = $match.Groups['message'].Value.Replace('\"', '"')
            Severity = $match.Groups['severity'].Value
        }
    }

    if ($rows.Count -eq 0) {
        Add-Failure "No diagnostic descriptors were found in '$DiagnosticsPath'."
    }

    return $rows
}

function Read-AvailableFixIds($descriptorRows) {
    $nameToId = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::Ordinal)

    foreach ($row in $descriptorRows.Values) {
        $nameToId[$row.Name] = $row.Id
    }

    $available = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

    foreach ($file in Get-ChildItem -LiteralPath $CodeFixesDirectory -Filter '*.cs' -File) {
        $text = [System.IO.File]::ReadAllText($file.FullName)

        foreach ($match in [regex]::Matches($text, 'DiagnosticDescriptors\.(?<name>\w+)\.Id')) {
            $name = $match.Groups['name'].Value

            if (-not $nameToId.ContainsKey($name)) {
                Add-Failure "Code fix $($file.Name) references unknown descriptor $name."
                continue
            }

            [void]$available.Add($nameToId[$name])
        }
    }

    return $available
}

function Assert-Text($value, [string]$field, [string]$id) {
    if ($null -eq $value -or [string]::IsNullOrWhiteSpace([string]$value)) {
        Add-Failure "$id is missing '$field'."
        return $false
    }

    return $true
}

function Assert-Lines($value, [string]$field, [string]$id) {
    $lines = @($value)

    if ($lines.Count -eq 0 -or ($lines -join '').Trim().Length -eq 0) {
        Add-Failure "$id is missing '$field'."
        return $false
    }

    return $true
}

function Join-Lines($value) {
    return (@($value) -join "`n")
}

function Render-CodeFence([string]$language, $lines) {
    return "``````$language`n$(Join-Lines $lines)`n``````"
}

function Render-Page($entry, $descriptor, [string]$status) {
    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.AppendLine("# $($entry.id) — $($descriptor.Title)")
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('| Field | Value |')
    [void]$builder.AppendLine('|---|---|')
    [void]$builder.AppendLine("| Severity | ``$($descriptor.Severity)`` |")
    $fixLabel = if ($status -eq 'Available') { "$status — $($entry.fix)" } else { 'Intentional — see below' }
    [void]$builder.AppendLine("| Code fix | $fixLabel |")
    [void]$builder.AppendLine("| Constitution principle | $($entry.principle) |")
    [void]$builder.AppendLine()
    [void]$builder.AppendLine($entry.summary)
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('## Triggering code')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine((Render-CodeFence -language 'csharp' -lines $entry.trigger))
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('## Compiler output')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine((Render-CodeFence -language 'text' -lines @($entry.message)))
    [void]$builder.AppendLine()

    if ($status -eq 'Available') {
        [void]$builder.AppendLine('## Fix')
        [void]$builder.AppendLine()
        [void]$builder.AppendLine($entry.fix)
        [void]$builder.AppendLine()
        [void]$builder.AppendLine('## Fixed code')
        [void]$builder.AppendLine()
        [void]$builder.AppendLine((Render-CodeFence -language 'csharp' -lines $entry.after))
        [void]$builder.AppendLine()
        [void]$builder.AppendLine('## Why this is the fix')
        [void]$builder.AppendLine()
        [void]$builder.AppendLine($entry.guidance)
    }
    else {
        [void]$builder.AppendLine('## Why there is no automatic fix')
        [void]$builder.AppendLine()
        [void]$builder.AppendLine($entry.reason)
        [void]$builder.AppendLine()
        [void]$builder.AppendLine('## How to fix it')
        [void]$builder.AppendLine()
        [void]$builder.AppendLine($entry.guidance)
    }

    [void]$builder.AppendLine()
    [void]$builder.AppendLine('---')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('[← Diagnostic gallery](index.md) · [Diagnostics reference](../03-diagnostics.md)')

    return $builder.ToString()
}

function Render-Index($entries, $descriptorRows, $availableIds) {
    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.AppendLine('# Diagnostic gallery')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('Every `VAL` diagnostic with a minimal trigger, the compiler output, and either a tested')
    [void]$builder.AppendLine('fix or the reason no automatic fix is safe. The pages are generated from')
    [void]$builder.AppendLine('`diagnostics.json` and checked byte-for-byte in CI.')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('| ID | Severity | Title | Fix status |')
    [void]$builder.AppendLine('|---|---|---|---|')

    foreach ($entry in $entries) {
        $descriptor = $descriptorRows[$entry.id]
        $status = if ($availableIds.Contains($entry.id)) { 'Available' } else { 'Intentional' }
        [void]$builder.AppendLine("| [$($entry.id)]($($entry.id).md) | ``$($descriptor.Severity)`` | $($descriptor.Title) | $status |")
    }

    $availableCount = @($entries | Where-Object { $availableIds.Contains($_.id) }).Count
    $intentionalCount = $entries.Count - $availableCount

    [void]$builder.AppendLine()
    [void]$builder.AppendLine("**$($entries.Count) diagnostics:** $availableCount with a tested code fix and $intentionalCount intentional omissions.")
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('The matrix view lives in the [diagnostics reference](../03-diagnostics.md). The analyzer')
    [void]$builder.AppendLine('package README links here for the before/after learning path.')

    return $builder.ToString()
}

function Convert-ToCrlf([string]$text) {
    return $text.Replace("`r`n", "`n").Replace("`n", "`r`n")
}

function Get-ContentHash([byte[]]$bytes) {
    return [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($bytes))
}

$descriptorRows = Read-DescriptorRows
$availableIds = Read-AvailableFixIds $descriptorRows

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "ERROR: $_" -ForegroundColor Red }
    exit 1
}

$document = [System.IO.File]::ReadAllText($SourcePath) | ConvertFrom-Json
$entries = @($document.diagnostics)

if ($document.version -ne 1) {
    Add-Failure "Unsupported gallery schema version '$($document.version)'."
}

if ($entries.Count -ne $descriptorRows.Count) {
    Add-Failure "Gallery has $($entries.Count) entries but the descriptor catalog has $($descriptorRows.Count)."
}

$seenIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

foreach ($entry in $entries) {
    if (-not $seenIds.Add([string]$entry.id)) {
        Add-Failure "Gallery repeats $($entry.id)."
        continue
    }

    if (-not $descriptorRows.Contains($entry.id)) {
        Add-Failure "Gallery entry $($entry.id) has no descriptor."
        continue
    }

    $descriptor = $descriptorRows[$entry.id]

    if ($entry.severity -ne $descriptor.Severity) {
        Add-Failure "$($entry.id) severity '$($entry.severity)' does not match descriptor '$($descriptor.Severity)'."
    }

    if ($entry.title -ne $descriptor.Title) {
        Add-Failure "$($entry.id) title '$($entry.title)' does not match descriptor '$($descriptor.Title)'."
    }

    [void](Assert-Text $entry.principle 'principle' $entry.id)
    [void](Assert-Text $entry.summary 'summary' $entry.id)
    [void](Assert-Lines $entry.trigger 'trigger' $entry.id)
    [void](Assert-Text $entry.message 'message' $entry.id)
    [void](Assert-Text $entry.guidance 'guidance' $entry.id)

    $expectedStatus = if ($availableIds.Contains($entry.id)) { 'Available' } else { 'Intentional' }

    if ($entry.status -ne $expectedStatus) {
        Add-Failure "$($entry.id) status '$($entry.status)' does not match the code-fix providers ('$expectedStatus')."
    }

    if ($expectedStatus -eq 'Available') {
        [void](Assert-Text $entry.fix 'fix' $entry.id)
        [void](Assert-Lines $entry.after 'after' $entry.id)
    }
    else {
        [void](Assert-Text $entry.reason 'reason' $entry.id)
    }

    $serialized = $entry | ConvertTo-Json -Depth 10 -Compress

    # "comparison" alone is a legitimate validation term ("comparison rule"), so this guard
    # targets library-comparison language, private paths, and machine-specific paths instead.
    if ($serialized -match '(?i)(private[\\/]|[A-Za-z]:\\|/Users/|/home/|\bpeer\b|\brival\b|\bcompetitor\b|\bbenchmark\b|\bversus\b|compared\s+to|comparison\s+(?:with|against)|better\s+than|worse\s+than)') {
        Add-Failure "$($entry.id) contains a private path or comparison language."
    }
}

$missingEntries = @($descriptorRows.Keys | Where-Object { -not $seenIds.Contains($_) })

foreach ($id in $missingEntries) {
    Add-Failure "Descriptor $id has no gallery entry."
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "ERROR: $_" -ForegroundColor Red }
    exit 1
}

$entries = @($entries | Sort-Object { $_.id })
$rendered = [ordered]@{}
$rendered['index.md'] = Convert-ToCrlf (Render-Index $entries $descriptorRows $availableIds)

foreach ($entry in $entries) {
    $descriptor = $descriptorRows[$entry.id]
    $status = if ($availableIds.Contains($entry.id)) { 'Available' } else { 'Intentional' }
    $rendered["$($entry.id).md"] = Convert-ToCrlf (Render-Page $entry $descriptor $status)
}

$utf8 = [System.Text.UTF8Encoding]::new($false)

if ($Check) {
    $differences = [System.Collections.Generic.List[string]]::new()

    foreach ($name in $rendered.Keys) {
        $path = Join-Path $OutputDirectory $name

        if (-not (Test-Path -LiteralPath $path)) {
            [void]$differences.Add("missing: $name")
            continue
        }

        $expectedHash = Get-ContentHash ([System.Text.Encoding]::UTF8.GetBytes($rendered[$name]))
        $actualHash = Get-ContentHash ([System.IO.File]::ReadAllBytes($path))

        if ($expectedHash -ne $actualHash) {
            [void]$differences.Add("changed: $name")
        }
    }

    foreach ($file in Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.md' -File) {
        if (-not $rendered.Contains($file.Name)) {
            [void]$differences.Add("unexpected: $($file.Name)")
        }
    }

    if ($differences.Count -gt 0) {
        $differences | ForEach-Object { Write-Host "ERROR: $_" -ForegroundColor Red }
        Write-Host 'Run scripts/docs/generate-diagnostic-gallery.ps1 to regenerate the gallery.' -ForegroundColor Yellow
        exit 1
    }

    Write-Host "Diagnostic gallery OK: $($entries.Count) pages are byte-stable." -ForegroundColor Green
    exit 0
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

foreach ($name in $rendered.Keys) {
    [System.IO.File]::WriteAllText((Join-Path $OutputDirectory $name), $rendered[$name], $utf8)
}

Write-Host "Diagnostic gallery generated: $($entries.Count) pages in '$OutputDirectory'." -ForegroundColor Green
