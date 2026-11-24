#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Compares the committed public API surface with a previous release tag.

.DESCRIPTION
    Reads PublicAPI.Shipped.txt, PublicAPI.Unshipped.txt, and per-TFM baseline files for every
    packable project under src/. The current surface is read from the working tree (or a git ref),
    and the baseline surface is read from -BaselineRef.

    Changes are classified as added, removed, or changed. Removed entries and changed signatures
    are breaking. Every change must have an explicit approval entry in the approvals file.

.PARAMETER BaselineRef
    Git tag or ref to compare against. Defaults to v1.0.0-preview.2.

.PARAMETER CurrentRef
    WORKTREE (default), HEAD, or any git ref.

.PARAMETER OutputRoot
    Report root. Defaults to artifacts/api-diff.

.PARAMETER Version
    Report label. Defaults to the product version evaluated from MSBuild.

.PARAMETER ApprovalPath
    JSON approval file. Defaults to api-diff-approvals.json next to this script.

.PARAMETER RepositoryRoot
    Repository root. Defaults to two levels above this script.

.EXAMPLE
    pwsh scripts/api/compare-public-api.ps1

.EXAMPLE
    pwsh scripts/api/compare-public-api.ps1 -BaselineRef v1.0.0-preview.2 -CurrentRef HEAD
#>

[CmdletBinding()]
param(
    [string]$BaselineRef = 'v1.0.0-preview.2',
    [string]$CurrentRef = 'WORKTREE',
    [string]$OutputRoot = 'artifacts/api-diff',
    [string]$Version,
    [string]$ApprovalPath = (Join-Path $PSScriptRoot 'api-diff-approvals.json'),
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$approvalPath = if ([System.IO.Path]::IsPathRooted($ApprovalPath)) {
    $ApprovalPath
}
else {
    Join-Path $repositoryRoot $ApprovalPath
}

$errors = [System.Collections.Generic.List[string]]::new()
$changes = [System.Collections.Generic.List[object]]::new()
$packageRows = [System.Collections.Generic.List[object]]::new()
$appliedApprovals = [System.Collections.Generic.HashSet[int]]::new()

function Get-ToolPath {
    param([Parameter(Mandatory)][string]$Name)

    $command = Get-Command $Name -ErrorAction SilentlyContinue

    if (-not $command) {
        throw "Required tool '$Name' was not found on PATH."
    }

    return $command.Source
}

function Get-ProductVersion {
    param([Parameter(Mandatory)][string]$DotnetPath)

    $project = Join-Path $repositoryRoot 'src/Jeninnet.Validation/Jeninnet.Validation.csproj'
    $value = & $DotnetPath msbuild $project -getProperty:Version -nologo

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet msbuild failed with exit code $LASTEXITCODE."
    }

    $text = ($value | Out-String).Trim()

    if ([string]::IsNullOrWhiteSpace($text)) {
        throw 'The evaluated MSBuild Version is empty.'
    }

    return $text
}

function Get-SafePathSegment {
    param([Parameter(Mandatory)][string]$Value)

    $safe = ($Value -replace '[^A-Za-z0-9._-]', '-').Trim('-', '.')

    if ([string]::IsNullOrWhiteSpace($safe)) {
        return 'unknown'
    }

    return $safe
}

function Get-RelativePath {
    param([Parameter(Mandatory)][string]$Path)

    return ([System.IO.Path]::GetRelativePath($repositoryRoot, $Path) -replace '\\', '/')
}

function Get-GitValue {
    param(
        [Parameter(Mandatory)][string]$GitPath,
        [Parameter(Mandatory)][string[]]$Arguments,
        [string]$Fallback = ''
    )

    $value = & $GitPath @Arguments 2>$null

    if ($LASTEXITCODE -ne 0) {
        return $Fallback
    }

    $text = ($value | Out-String).Trim()

    if ([string]::IsNullOrWhiteSpace($text)) {
        return $Fallback
    }

    return $text
}

function Get-GitFileLines {
    param(
        [Parameter(Mandatory)][string]$GitPath,
        [Parameter(Mandatory)][string]$Ref,
        [Parameter(Mandatory)][string]$RelativePath
    )

    $value = & $GitPath show "$Ref`:$RelativePath" 2>$null

    if ($LASTEXITCODE -ne 0) {
        return @()
    }

    return @($value)
}

function Get-WorkingFileLines {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return @()
    }

    return @(Get-Content -LiteralPath $Path)
}

function Get-PackageDefinitions {
    $definitions = [System.Collections.Generic.List[object]]::new()

    foreach ($project in Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'src') -Recurse -Filter '*.csproj' -File) {
        $xml = Get-Content -LiteralPath $project.FullName -Raw

        if ($xml -notmatch '<IsPackable>\s*true\s*</IsPackable>') {
            continue
        }

        $targetFrameworks = [regex]::Match($xml, '<TargetFrameworks>([^<]+)</TargetFrameworks>').Groups[1].Value
        $targetFramework = [regex]::Match($xml, '<TargetFramework>([^<]+)</TargetFramework>').Groups[1].Value
        $tfms = if (-not [string]::IsNullOrWhiteSpace($targetFrameworks)) {
            @($targetFrameworks -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        }
        elseif (-not [string]::IsNullOrWhiteSpace($targetFramework)) {
            @($targetFramework.Trim())
        }
        else {
            @('net10.0')
        }

        foreach ($tfm in $tfms) {
            [void]$definitions.Add([pscustomobject]@{
                Package = $project.BaseName
                Tfm     = $tfm
                Directory = $project.DirectoryName
            })
        }
    }

    return @($definitions | Sort-Object Package, Tfm)
}

function Get-MemberKey {
    param([Parameter(Mandatory)][string]$Signature)

    $value = $Signature.Trim()
    $value = $value -replace '^(?:static|abstract|virtual|override|sealed|readonly|const|new)\s+', ''
    $left = if ($value -match '\s+->\s+') { ($value -split '\s+->\s+', 2)[0] } else { $value }
    $parameterStart = $left.IndexOf('(')

    if ($parameterStart -ge 0) {
        return $left.Substring(0, $parameterStart)
    }

    return $left
}

function Read-ApiSurface {
    param(
        [Parameter(Mandatory)]$Definition,
        [Parameter(Mandatory)][string]$Ref,
        [Parameter(Mandatory)][string]$GitPath
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $sources = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::Ordinal)
    $files = @(
        'PublicAPI.Shipped.txt',
        'PublicAPI.Unshipped.txt',
        "PublicAPI.Shipped.$($Definition.Tfm).txt",
        "PublicAPI.Unshipped.$($Definition.Tfm).txt"
    )

    foreach ($file in $files) {
        $path = Join-Path $Definition.Directory $file
        $relative = Get-RelativePath $path
        $fileLines = if ($Ref -eq 'WORKTREE') {
            Get-WorkingFileLines -Path $path
        }
        else {
            Get-GitFileLines -GitPath $GitPath -Ref $Ref -RelativePath $relative
        }

        foreach ($line in $fileLines) {
            $signature = $line.Trim()

            if ([string]::IsNullOrWhiteSpace($signature) -or $signature.StartsWith('#')) {
                continue
            }

            if (-not $sources.ContainsKey($signature)) {
                [void]$lines.Add($signature)
                $sources[$signature] = $relative
            }
        }
    }

    return [pscustomobject]@{
        Lines   = @($lines)
        Sources = $sources
    }
}

function New-Change {
    param(
        [string]$Package,
        [string]$Tfm,
        [string]$Kind,
        [string]$Signature,
        [string]$PreviousSignature,
        [string]$SourceFile,
        [string]$BaselineFile
    )

    return [pscustomobject]@{
        Package           = $Package
        Tfm               = $Tfm
        Kind              = $Kind
        Breaking          = ($Kind -in @('removed', 'changed'))
        Signature         = $Signature
        PreviousSignature = $PreviousSignature
        SourceFile        = $SourceFile
        BaselineFile      = $BaselineFile
        Approved          = $false
        ApprovalReason    = $null
        ApprovedBy        = $null
        ApprovedOn        = $null
        ApprovalIndex     = -1
    }
}

function Find-Approval {
    param(
        [Parameter(Mandatory)]$Change,
        [object[]]$Approvals
    )

    for ($index = 0; $index -lt $Approvals.Count; $index++) {
        $approval = $Approvals[$index]

        if ($approval.package -ne $Change.Package) { continue }
        if ($approval.tfm -ne $Change.Tfm) { continue }
        if ($approval.kind -ne $Change.Kind) { continue }
        if ($approval.signature -ne $Change.Signature) { continue }

        if ($Change.Kind -eq 'changed' -and
            -not [string]::IsNullOrWhiteSpace($approval.previousSignature) -and
            $approval.previousSignature -ne $Change.PreviousSignature) {
            continue
        }

        return [pscustomobject]@{
            Index    = $index
            Approval = $approval
        }
    }

    return $null
}

function Format-MarkdownCell {
    param([string]$Value)

    if ($null -eq $Value) {
        return ''
    }

    return (($Value -replace '\|', '\|') -replace '\r?\n', ' ').Trim()
}

function Write-Report {
    param(
        [Parameter(Mandatory)][string]$ReportDirectory,
        [Parameter(Mandatory)][string]$ReportVersion,
        [Parameter(Mandatory)][string]$CommitSha,
        [Parameter(Mandatory)][string]$BaselineCommit,
        [Parameter(Mandatory)][string]$OverallStatus,
        [object[]]$Approvals
    )

    $summary = [ordered]@{
        projects      = @($packageRows | Select-Object -ExpandProperty Package -Unique).Count
        packageTfmRows = $packageRows.Count
        changes       = $changes.Count
        added         = @($changes | Where-Object Kind -eq 'added').Count
        removed       = @($changes | Where-Object Kind -eq 'removed').Count
        changed       = @($changes | Where-Object Kind -eq 'changed').Count
        breaking      = @($changes | Where-Object Breaking).Count
        approved      = @($changes | Where-Object Approved).Count
        unapproved    = @($changes | Where-Object { -not $_.Approved }).Count
    }

    $packageTable = for ($index = 0; $index -lt $packageRows.Count; $index++) {
        $row = $packageRows[$index]
        $rowChanges = @($changes | Where-Object { $_.Package -eq $row.Package -and $_.Tfm -eq $row.Tfm })
        $rowStatus = if (@($rowChanges | Where-Object { -not $_.Approved }).Count -gt 0) {
            'UNAPPROVED'
        }
        elseif ($rowChanges.Count -gt 0) {
            'APPROVED'
        }
        else {
            'CLEAN'
        }

        "| $($index + 1) | $(Format-MarkdownCell $row.Package) | $(Format-MarkdownCell $row.Tfm) | $($row.BaselineEntries) | $($row.CurrentEntries) | $($rowChanges.Count) | $rowStatus |"
    }

    $changeTable = if ($changes.Count -eq 0) {
        '| — | — | — | — | — | No public API changes. |'
    }
    else {
        for ($index = 0; $index -lt $changes.Count; $index++) {
            $change = $changes[$index]
            $approval = if ($change.Approved) { 'APPROVED' } else { 'UNAPPROVED' }
            $previous = if ([string]::IsNullOrWhiteSpace($change.PreviousSignature)) { '' } else { $change.PreviousSignature }
            "| $($index + 1) | $(Format-MarkdownCell $change.Package) | $(Format-MarkdownCell $change.Tfm) | $($change.Kind) | $($change.Breaking) | $approval | $(Format-MarkdownCell $change.Signature) | $(Format-MarkdownCell $previous) |"
        }
    }

    $approvalTable = if ($null -eq $Approvals -or $Approvals.Count -eq 0) {
        'No approvals recorded.'
    }
    else {
        for ($index = 0; $index -lt $Approvals.Count; $index++) {
            $approval = $Approvals[$index]
            $status = if ($appliedApprovals.Contains($index)) { 'APPLIED' } else { 'UNUSED' }
            "| $($index + 1) | $(Format-MarkdownCell $approval.package) | $(Format-MarkdownCell $approval.tfm) | $($approval.kind) | $status | $(Format-MarkdownCell $approval.reason) |"
        }
    }

    $failureLines = [System.Collections.Generic.List[string]]::new()

    if ($errors.Count -gt 0) {
        [void]$failureLines.Add('### Setup/approval errors')
        [void]$failureLines.Add('')
        foreach ($error in $errors) {
            [void]$failureLines.Add("- $error")
        }
        [void]$failureLines.Add('')
    }

    $unapproved = @($changes | Where-Object { -not $_.Approved })

    if ($unapproved.Count -gt 0) {
        [void]$failureLines.Add('### Unapproved API changes')
        [void]$failureLines.Add('')
        foreach ($change in $unapproved) {
            [void]$failureLines.Add("- $($change.Package) [$($change.Tfm)] $($change.Kind): $($change.Signature)")
        }
        [void]$failureLines.Add('')
    }

    if ($failureLines.Count -eq 0) {
        [void]$failureLines.Add('No unapproved API changes.')
    }

    $jsonPath = Join-Path $ReportDirectory 'api-diff.json'
    $markdownPath = Join-Path $ReportDirectory 'api-diff.md'
    $reportFiles = [ordered]@{
        schemaVersion      = 1
        version            = $ReportVersion
        baselineRef        = $BaselineRef
        baselineCommit     = $BaselineCommit
        currentRef         = $CurrentRef
        currentCommit      = $CommitSha
        generatedAtUtc     = [DateTime]::UtcNow.ToString('o')
        overallStatus      = $OverallStatus
        summary            = $summary
        packageTfm         = @($packageRows)
        changes            = @($changes)
        approvals          = @($Approvals)
        appliedApprovalIds = @($appliedApprovals | Sort-Object)
        errors             = @($errors)
    }

    $markdown = [System.Text.StringBuilder]::new()
    [void]$markdown.AppendLine("# Public API diff — $ReportVersion")
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine("- **Baseline:** ``$BaselineRef`` (``$BaselineCommit``)")
    [void]$markdown.AppendLine("- **Current:** ``$CurrentRef`` (``$CommitSha``)")
    [void]$markdown.AppendLine("- **Generated:** $([DateTime]::UtcNow.ToString('u'))")
    [void]$markdown.AppendLine("- **Overall:** **$($OverallStatus.ToUpperInvariant())**")
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('## Summary')
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('| Packages | Package/TFM rows | Added | Removed | Changed | Breaking | Approved | Unapproved |')
    [void]$markdown.AppendLine('|---:|---:|---:|---:|---:|---:|---:|---:|')
    [void]$markdown.AppendLine("| $($summary.projects) | $($summary.packageTfmRows) | $($summary.added) | $($summary.removed) | $($summary.changed) | $($summary.breaking) | $($summary.approved) | $($summary.unapproved) |")
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('## Package/TFM coverage')
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('| # | Package | TFM | Baseline entries | Current entries | Changes | Status |')
    [void]$markdown.AppendLine('|---:|---|---|---:|---:|---:|---|')
    [void]$markdown.AppendLine(($packageTable -join [Environment]::NewLine))
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('## Changes')
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('| # | Package | TFM | Kind | Breaking | Approval | Signature | Previous signature |')
    [void]$markdown.AppendLine('|---:|---|---|---|---|---|---|---|')
    [void]$markdown.AppendLine(($changeTable -join [Environment]::NewLine))
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('## Approvals')
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('| # | Package | TFM | Kind | Status | Reason |')
    [void]$markdown.AppendLine('|---:|---|---|---|---|---|')
    [void]$markdown.AppendLine(($approvalTable -join [Environment]::NewLine))
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('## Failures')
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine(($failureLines -join [Environment]::NewLine))
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('## Report files')
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine("- Markdown: ``$(Get-RelativePath $markdownPath)``")
    [void]$markdown.AppendLine("- JSON: ``$(Get-RelativePath $jsonPath)``")

    $reportFiles | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $jsonPath -Encoding utf8
    Set-Content -LiteralPath $markdownPath -Value $markdown.ToString() -Encoding utf8

    return [pscustomobject]@{
        MarkdownPath = $markdownPath
        JsonPath     = $jsonPath
        Overall      = $OverallStatus
        Summary      = $summary
    }
}

try {
    $dotnet = Get-ToolPath 'dotnet'
    $git = Get-ToolPath 'git'
    $productVersion = if ([string]::IsNullOrWhiteSpace($Version)) { Get-ProductVersion -DotnetPath $dotnet } else { $Version }
    $reportVersion = $productVersion
    $outputRootPath = if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
        $OutputRoot
    }
    else {
        Join-Path $repositoryRoot $OutputRoot
    }

    $reportDirectory = Join-Path $outputRootPath (Get-SafePathSegment $reportVersion)

    if (Test-Path -LiteralPath $reportDirectory) {
        Remove-Item -LiteralPath $reportDirectory -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null

    $commitSha = Get-GitValue -GitPath $git -Arguments @('rev-parse', 'HEAD') -Fallback 'unknown'
    $baselineCommit = Get-GitValue -GitPath $git -Arguments @('rev-parse', "$BaselineRef^{commit}") -Fallback 'unknown'

    if ($baselineCommit -eq 'unknown') {
        [void]$errors.Add("Baseline ref '$BaselineRef' does not resolve to a commit.")
    }

    $approvalDocument = $null
    $approvals = @()

    if (Test-Path -LiteralPath $approvalPath) {
        try {
            $approvalDocument = Get-Content -LiteralPath $approvalPath -Raw | ConvertFrom-Json

            if ($approvalDocument.schemaVersion -ne 1) {
                [void]$errors.Add("Approval file schemaVersion must be 1; found '$($approvalDocument.schemaVersion)'.")
            }

            if ($approvalDocument.baselineRef -and $approvalDocument.baselineRef -ne $BaselineRef) {
                [void]$errors.Add("Approval file baselineRef '$($approvalDocument.baselineRef)' does not match '$BaselineRef'.")
            }

            if ($approvalDocument.targetVersion -and $approvalDocument.targetVersion -ne $reportVersion) {
                [void]$errors.Add("Approval file targetVersion '$($approvalDocument.targetVersion)' does not match '$reportVersion'.")
            }

            $approvals = @($approvalDocument.approvals)
        }
        catch {
            [void]$errors.Add("Approval file '$approvalPath' could not be read: $($_.Exception.Message)")
        }
    }
    else {
        [void]$errors.Add("Approval file not found: $approvalPath")
    }

    $definitions = Get-PackageDefinitions

    if ($definitions.Count -eq 0) {
        [void]$errors.Add('No packable projects were found under src/.')
    }

    foreach ($definition in $definitions) {
        $baseline = Read-ApiSurface -Definition $definition -Ref $BaselineRef -GitPath $git
        $current = Read-ApiSurface -Definition $definition -Ref $CurrentRef -GitPath $git
        $baselineSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        $currentSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

        foreach ($line in $baseline.Lines) { [void]$baselineSet.Add($line) }
        foreach ($line in $current.Lines) { [void]$currentSet.Add($line) }

        $added = @($current.Lines | Where-Object { -not $baselineSet.Contains($_) })
        $removed = @($baseline.Lines | Where-Object { -not $currentSet.Contains($_) })
        $addedBuckets = @{}

        foreach ($line in $added) {
            $key = Get-MemberKey -Signature $line
            if (-not $addedBuckets.ContainsKey($key)) {
                $addedBuckets[$key] = [System.Collections.Generic.List[string]]::new()
            }
            [void]$addedBuckets[$key].Add($line)
        }

        $consumedAdded = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        $finalRemoved = [System.Collections.Generic.List[string]]::new()
        $changedCount = 0

        foreach ($line in $removed) {
            $key = Get-MemberKey -Signature $line
            $match = $null

            if ($addedBuckets.ContainsKey($key)) {
                foreach ($candidate in $addedBuckets[$key]) {
                    if (-not $consumedAdded.Contains($candidate)) {
                        $match = $candidate
                        break
                    }
                }
            }

            if ($null -ne $match) {
                [void]$consumedAdded.Add($match)
                [void]$changes.Add((New-Change -Package $definition.Package -Tfm $definition.Tfm -Kind 'changed' -Signature $match -PreviousSignature $line -SourceFile $current.Sources[$match] -BaselineFile $baseline.Sources[$line]))
                $changedCount++
            }
            else {
                [void]$finalRemoved.Add($line)
            }
        }

        foreach ($line in $finalRemoved) {
            [void]$changes.Add((New-Change -Package $definition.Package -Tfm $definition.Tfm -Kind 'removed' -Signature $line -PreviousSignature $null -SourceFile $null -BaselineFile $baseline.Sources[$line]))
        }

        foreach ($line in $added) {
            if (-not $consumedAdded.Contains($line)) {
                [void]$changes.Add((New-Change -Package $definition.Package -Tfm $definition.Tfm -Kind 'added' -Signature $line -PreviousSignature $null -SourceFile $current.Sources[$line] -BaselineFile $null))
            }
        }

        [void]$packageRows.Add([pscustomobject]@{
            Package         = $definition.Package
            Tfm             = $definition.Tfm
            BaselineEntries = $baseline.Lines.Count
            CurrentEntries  = $current.Lines.Count
            Added           = $added.Count
            Removed         = $finalRemoved.Count + $changedCount
            Changed         = $changedCount
        })
    }

    for ($index = 0; $index -lt $changes.Count; $index++) {
        $match = Find-Approval -Change $changes[$index] -Approvals $approvals

        if ($null -ne $match) {
            $changes[$index].Approved = $true
            $changes[$index].ApprovalReason = $match.Approval.reason
            $changes[$index].ApprovedBy = $match.Approval.approvedBy
            $changes[$index].ApprovedOn = $match.Approval.approvedOn
            $changes[$index].ApprovalIndex = $match.Index
            [void]$appliedApprovals.Add($match.Index)
        }
    }

    $unapproved = @($changes | Where-Object { -not $_.Approved })
    $overall = if ($errors.Count -gt 0 -or $unapproved.Count -gt 0) { 'failed' } else { 'passed' }
    $report = Write-Report -ReportDirectory $reportDirectory -ReportVersion $reportVersion -CommitSha $commitSha -BaselineCommit $baselineCommit -OverallStatus $overall -Approvals $approvals

    Write-Host ''
    Write-Host "API diff: $($report.Overall.ToUpperInvariant())" -ForegroundColor $(if ($report.Overall -eq 'passed') { 'Green' } else { 'Red' })
    Write-Host "Packages: $($report.Summary.projects); package/TFM rows: $($report.Summary.packageTfmRows)"
    Write-Host "Changes: $($report.Summary.changes) (added $($report.Summary.added), removed $($report.Summary.removed), changed $($report.Summary.changed))"
    Write-Host "Report: $($report.MarkdownPath)"
    Write-Host "JSON  : $($report.JsonPath)"

    if ($report.Overall -eq 'passed') {
        exit 0
    }

    exit 1
}
catch {
    Write-Host ''
    Write-Host "API diff failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
