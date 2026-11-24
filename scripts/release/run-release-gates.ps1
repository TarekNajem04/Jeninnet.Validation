#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Runs the full repository release gate and writes a Markdown/JSON report.

.DESCRIPTION
    Executes the release gates in dependency order, captures a log per gate, and writes:

        artifacts/release/<version>/release-gates.md
        artifacts/release/<version>/release-gates.json
        artifacts/release/<version>/logs/*.log

    The command exits 0 only when every required gate passes. External coverage and Sonar
    gates are reported-only by default until their credentials/baseline are available.

.PARAMETER Version
    Optional report label. Defaults to the product version evaluated from MSBuild.

.PARAMETER OutputRoot
    Report root. Defaults to artifacts/release.

.PARAMETER ExpectedVersion
    Optional expected product version, for example the version derived from a release tag.

.PARAMETER Solution
    Solution file. Defaults to Jeninnet.Validation.slnx.

.PARAMETER RuntimeIdentifier
    Runtime identifier used by the NativeAOT smoke gate. Defaults to the current OS/architecture.

.PARAMETER SkipRestore
    Skip dotnet restore.

.PARAMETER SkipBuild
    Skip the Release build and every gate that depends on it.

.PARAMETER SkipTests
    Skip the full test suite.

.PARAMETER SkipFormat
    Skip dotnet format verification.

.PARAMETER SkipSourceHeaders
    Skip the source-header check.

.PARAMETER SkipApiBaselines
    Skip both public-API baseline checks.

.PARAMETER SkipApiGateSelfTest
    Skip only the throwaway RS0016/RS0017 gate self-test.

.PARAMETER SkipApiDiff
    Skip the public API diff against v1.0.0-preview.2.

.PARAMETER SkipMessageKeys
    Skip the message-key catalog check.

.PARAMETER SkipDocs
    Skip documentation links, README claims, and Arabic-text checks.

.PARAMETER SkipClientSupport
    Skip the client rule-support drift check.

.PARAMETER SkipClient
    Skip the JavaScript client runtime tests.

.PARAMETER SkipVulnerability
    Skip the NuGet vulnerability audit.

.PARAMETER SkipPackages
    Skip package packing and every package-dependent gate.

.PARAMETER SkipConsumer
    Skip only the packed-package consumer smoke test.

.PARAMETER SkipAotSupport
    Skip the AOT/trim support-matrix check.

.PARAMETER SkipAot
    Skip the NativeAOT smoke publish/run.

.PARAMETER SkipCoverage
    Mark coverage as skipped instead of reported-only.

.PARAMETER SkipSonar
    Mark Sonar as skipped instead of reported-only.

.PARAMETER IncludeCoverage
    Run the coverage workflow instead of marking it reported-only.

.PARAMETER IncludeSonar
    Run the Sonar preview instead of marking it reported-only.

.EXAMPLE
    pwsh scripts/release/run-release-gates.ps1 -ExpectedVersion 1.0.0-preview.3

.EXAMPLE
    pwsh scripts/release/run-release-gates.ps1 -SkipPackages -SkipAot

.EXAMPLE
    pwsh scripts/release/run-release-gates.ps1 -SkipRestore -SkipBuild -SkipTests -SkipFormat -SkipSourceHeaders -SkipApiBaselines -SkipApiGateSelfTest -SkipApiDiff -SkipMessageKeys -SkipClientSupport -SkipClient -SkipVulnerability -SkipPackages -SkipConsumer -SkipAotSupport -SkipAot -SkipCoverage -SkipSonar
#>

[CmdletBinding()]
param(
    [string]$Version,
    [string]$OutputRoot = 'artifacts/release',
    [string]$ExpectedVersion,
    [string]$Solution = 'Jeninnet.Validation.slnx',
    [string]$RuntimeIdentifier,
    [switch]$SkipRestore,
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$SkipFormat,
    [switch]$SkipSourceHeaders,
    [switch]$SkipApiBaselines,
    [switch]$SkipApiGateSelfTest,
    [switch]$SkipApiDiff,
    [switch]$SkipMessageKeys,
    [switch]$SkipDocs,
    [switch]$SkipClientSupport,
    [switch]$SkipClient,
    [switch]$SkipVulnerability,
    [switch]$SkipPackages,
    [switch]$SkipConsumer,
    [switch]$SkipAotSupport,
    [switch]$SkipAot,
    [switch]$SkipCoverage,
    [switch]$SkipSonar,
    [switch]$IncludeCoverage,
    [switch]$IncludeSonar
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..' '..'))
$startedAtUtc = [DateTime]::UtcNow
$totalTimer = [System.Diagnostics.Stopwatch]::StartNew()
$gates = [System.Collections.Generic.List[object]]::new()
$failureMessages = [System.Collections.Generic.List[string]]::new()
$script:setupError = $null
$script:dotnetPath = $null
$script:pwshPath = $null
$script:scriptParameters = $PSBoundParameters

function Write-Banner {
    Write-Host ''
    Write-Host '=============================================' -ForegroundColor Cyan
    Write-Host ' Jeninnet Release Gates' -ForegroundColor Cyan
    Write-Host '=============================================' -ForegroundColor Cyan
    Write-Host ''
}

function Get-ToolPath {
    param(
        [Parameter(Mandatory)]
        [string]$Name
    )

    $command = Get-Command $Name -ErrorAction SilentlyContinue

    if (-not $command) {
        throw "Required tool '$Name' was not found on PATH."
    }

    return $command.Source
}

function Get-ProductVersion {
    $project = Join-Path $repositoryRoot 'src/Jeninnet.Validation/Jeninnet.Validation.csproj'
    $value = & $script:dotnetPath msbuild $project -getProperty:Version -nologo

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet msbuild failed with exit code $LASTEXITCODE while resolving the product version."
    }

    $version = ($value | Out-String).Trim()

    if ([string]::IsNullOrWhiteSpace($version)) {
        throw 'The evaluated MSBuild Version is empty.'
    }

    return $version
}

function Get-GitValue {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,
        [string]$Fallback = 'unknown'
    )

    try {
        $git = Get-ToolPath 'git'
        $value = & $git @Arguments

        if ($LASTEXITCODE -ne 0) {
            return $Fallback
        }

        $text = ($value | Out-String).Trim()

        if ([string]::IsNullOrWhiteSpace($text)) {
            return $Fallback
        }

        return $text
    }
    catch {
        return $Fallback
    }
}

function Get-CommandLine {
    $parts = [System.Collections.Generic.List[string]]::new()
    [void]$parts.Add('pwsh scripts/release/run-release-gates.ps1')

    foreach ($entry in $script:scriptParameters.GetEnumerator()) {
        $value = $entry.Value

        if ($value -is [System.Management.Automation.SwitchParameter]) {
            if ($value.IsPresent) {
                [void]$parts.Add("-$($entry.Key)")
            }

            continue
        }

        [void]$parts.Add("-$($entry.Key) `"$value`"")
    }

    return ($parts -join ' ')
}

function Get-SafePathSegment {
    param(
        [Parameter(Mandatory)]
        [string]$Value
    )

    $safe = $Value -replace '[^A-Za-z0-9._-]', '-'
    $safe = $safe.Trim('-', '.')

    if ([string]::IsNullOrWhiteSpace($safe)) {
        return 'unknown'
    }

    return $safe
}

function Get-ReportPath {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    return ([System.IO.Path]::GetRelativePath($repositoryRoot, $Path) -replace '\\', '/')
}

function Format-Duration {
    param([double]$Seconds)

    if ($null -eq $Seconds) {
        return '-'
    }

    $span = [TimeSpan]::FromSeconds($Seconds)

    if ($span.TotalMinutes -ge 1) {
        return '{0:mm\:ss}' -f $span
    }

    return '{0:ss\.f}s' -f $span
}

function Format-MarkdownCell {
    param([string]$Value)

    if ($null -eq $Value) {
        return ''
    }

    return (($Value -replace '\|', '\|') -replace '\r?\n', ' ').Trim()
}

function Write-GateLog {
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        [Parameter(Mandatory)]
        [string]$Message
    )

    $directory = Split-Path -Parent $Path

    if ($directory) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    Set-Content -LiteralPath $Path -Value $Message -Encoding utf8
}

function Invoke-External {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,
        [string[]]$Arguments = @()
    )

    & $FilePath @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath exited with code $LASTEXITCODE."
    }
}

function Invoke-PwshScript {
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        [string[]]$Arguments = @()
    )

    Invoke-External -FilePath $script:pwshPath -Arguments (@('-NoProfile', '-File', $Path) + $Arguments)
}

function New-Gate {
    param(
        [Parameter(Mandatory)]
        [string]$Name,
        [Parameter(Mandatory)]
        [string]$Category,
        [Parameter(Mandatory)]
        [string]$Command,
        [Parameter(Mandatory)]
        [string]$Evidence,
        [Parameter(Mandatory)]
        [scriptblock]$Action,
        [string[]]$DependsOn = @(),
        [switch]$Skip,
        [switch]$ReportedOnly,
        [string]$ReportedOnlyReason = ''
    )

    return [pscustomobject]@{
        Name               = $Name
        Category           = $Category
        Command            = $Command
        Evidence           = $Evidence
        Action             = $Action
        DependsOn          = $DependsOn
        SkipRequested      = $Skip.IsPresent
        ReportedOnly       = $ReportedOnly.IsPresent
        ReportedOnlyReason = $ReportedOnlyReason
        Status             = 'Pending'
        SkipReason         = $null
        Failure            = $null
        ExitCode           = $null
        DurationSeconds    = $null
        StartedAtUtc       = $null
        LogPath            = $null
    }
}

function Get-DependencySkipReason {
    param($Gate)

    foreach ($dependencyName in $Gate.DependsOn) {
        $dependency = $gates | Where-Object Name -eq $dependencyName | Select-Object -First 1

        if ($null -eq $dependency) {
            return "dependency '$dependencyName' is not registered"
        }

        if ($dependency.Status -eq 'Failed') {
            return "dependency '$dependencyName' failed"
        }

        if ($dependency.Status -eq 'Skipped') {
            return "dependency '$dependencyName' was skipped"
        }
    }

    return $null
}

function Invoke-Gate {
    param($Gate)

    if ($Gate.SkipRequested) {
        $Gate.Status = 'Skipped'
        $Gate.SkipReason = 'skipped by command-line switch'
        Write-GateLog -Path $Gate.LogPath -Message $Gate.SkipReason
        return
    }

    $dependencySkip = Get-DependencySkipReason -Gate $Gate

    if ($dependencySkip) {
        $Gate.Status = 'Skipped'
        $Gate.SkipReason = $dependencySkip
        Write-GateLog -Path $Gate.LogPath -Message $dependencySkip
        return
    }

    if ($Gate.ReportedOnly) {
        $Gate.Status = 'ReportedOnly'
        $Gate.SkipReason = $Gate.ReportedOnlyReason
        Write-GateLog -Path $Gate.LogPath -Message $Gate.ReportedOnlyReason
        return
    }

    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    $Gate.StartedAtUtc = [DateTime]::UtcNow.ToString('o')
    $previousLocation = Get-Location

    try {
        Push-Location $repositoryRoot
        $global:LASTEXITCODE = 0
        & $Gate.Action *>&1 | Tee-Object -FilePath $Gate.LogPath

        if ($LASTEXITCODE -ne 0) {
            throw "Gate command exited with code $LASTEXITCODE."
        }

        $Gate.Status = 'Passed'
        $Gate.ExitCode = 0

        if (-not (Test-Path -LiteralPath $Gate.LogPath)) {
            Write-GateLog -Path $Gate.LogPath -Message 'Gate completed with no output.'
        }
    }
    catch {
        $Gate.Status = 'Failed'
        $Gate.ExitCode = if ($LASTEXITCODE) { $LASTEXITCODE } else { 1 }
        $Gate.Failure = $_.Exception.Message
        Add-Content -LiteralPath $Gate.LogPath -Value "`nERROR: $($Gate.Failure)" -Encoding utf8
        [void]$failureMessages.Add("$($Gate.Name): $($Gate.Failure)")
    }
    finally {
        Pop-Location
        $timer.Stop()
        $Gate.DurationSeconds = [math]::Round($timer.Elapsed.TotalSeconds, 1)
    }
}

function Write-ReleaseReport {
    param(
        [Parameter(Mandatory)]
        [string]$ReportDirectory,
        [Parameter(Mandatory)]
        [string]$ProductVersion,
        [Parameter(Mandatory)]
        [string]$ReportVersion,
        [Parameter(Mandatory)]
        [string]$CommitSha,
        [Parameter(Mandatory)]
        [string]$Branch,
        [Parameter(Mandatory)]
        [string]$CommandLine
    )

    $summary = [ordered]@{
        total        = $gates.Count
        passed       = @($gates | Where-Object Status -eq 'Passed').Count
        failed       = @($gates | Where-Object Status -eq 'Failed').Count
        skipped      = @($gates | Where-Object Status -eq 'Skipped').Count
        reportedOnly = @($gates | Where-Object Status -eq 'ReportedOnly').Count
    }

    $overallStatus = if ($summary.failed -gt 0 -or $script:setupError) { 'failed' } else { 'passed' }
    $totalTimer.Stop()

    $gateRows = for ($index = 0; $index -lt $gates.Count; $index++) {
        $gate = $gates[$index]
        $statusLabel = switch ($gate.Status) {
            'Passed' { 'PASS' }
            'Failed' { 'FAIL' }
            'Skipped' { 'SKIP' }
            'ReportedOnly' { 'REPORTED' }
            default { 'PENDING' }
        }

        $evidence = switch ($gate.Status) {
            'Skipped' { "Skipped: $($gate.SkipReason)" }
            'ReportedOnly' { "Reported-only: $($gate.ReportedOnlyReason)" }
            default { $gate.Evidence }
        }

        "| $($index + 1) | $(Format-MarkdownCell $gate.Name) | $(Format-MarkdownCell $gate.Category) | $statusLabel | $(Format-Duration $gate.DurationSeconds) | $(Format-MarkdownCell ([string]$gate.ExitCode)) | $(Format-MarkdownCell $evidence) |"
    }

    $failureSection = if ($failureMessages.Count -eq 0) {
        'No required gate failed.'
    }
    else {
        $builder = [System.Text.StringBuilder]::new()

        foreach ($failure in $failureMessages) {
            [void]$builder.AppendLine("### $failure")
            [void]$builder.AppendLine()

            $gate = $gates | Where-Object { $failure.StartsWith("$($_.Name):") } | Select-Object -First 1

            if ($gate -and $gate.LogPath -and (Test-Path -LiteralPath $gate.LogPath)) {
                [void]$builder.AppendLine('Last 20 log lines:')
                [void]$builder.AppendLine()
                [void]$builder.AppendLine('```text')
                Get-Content -LiteralPath $gate.LogPath -Tail 20 | ForEach-Object { [void]$builder.AppendLine($_) }
                [void]$builder.AppendLine('```')
                [void]$builder.AppendLine()
            }
        }

        $builder.ToString()
    }

    $report = [ordered]@{
        schemaVersion    = 1
        productVersion   = $ProductVersion
        reportVersion    = $ReportVersion
        commitSha        = $CommitSha
        branch           = $Branch
        solution         = $Solution
        startedAtUtc     = $startedAtUtc.ToString('o')
        generatedAtUtc   = [DateTime]::UtcNow.ToString('o')
        durationSeconds  = [math]::Round($totalTimer.Elapsed.TotalSeconds, 1)
        overallStatus    = $overallStatus
        commandLine      = $CommandLine
        summary          = $summary
        gates            = @($gates | Select-Object Name, Category, Command, Evidence, DependsOn, Status, SkipReason, Failure, ExitCode, DurationSeconds, StartedAtUtc, LogPath)
    }

    $jsonPath = Join-Path $ReportDirectory 'release-gates.json'
    $markdownPath = Join-Path $ReportDirectory 'release-gates.md'
    $relativeReport = Get-ReportPath $markdownPath

    $markdown = [System.Text.StringBuilder]::new()
    [void]$markdown.AppendLine("# Release gate report — $ReportVersion")
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine("- **Product version:** ``$ProductVersion``")
    [void]$markdown.AppendLine("- **Commit:** ``$CommitSha``")
    [void]$markdown.AppendLine("- **Branch:** ``$Branch``")
    [void]$markdown.AppendLine("- **Solution:** ``$Solution``")
    [void]$markdown.AppendLine("- **Generated:** $([DateTime]::UtcNow.ToString('u'))")
    [void]$markdown.AppendLine("- **Duration:** $([math]::Round($totalTimer.Elapsed.TotalSeconds, 1)) seconds")
    [void]$markdown.AppendLine("- **Overall:** **$($overallStatus.ToUpperInvariant())**")
    [void]$markdown.AppendLine("- **Command:** ``$CommandLine``")
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('## Summary')
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('| Total | Passed | Failed | Skipped | Reported-only |')
    [void]$markdown.AppendLine('|---:|---:|---:|---:|---:|')
    [void]$markdown.AppendLine("| $($summary.total) | $($summary.passed) | $($summary.failed) | $($summary.skipped) | $($summary.reportedOnly) |")
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('## Gates')
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('| # | Gate | Category | Status | Duration | Exit | Evidence |')
    [void]$markdown.AppendLine('|---:|---|---|---|---:|---:|---|')
    [void]$markdown.AppendLine(($gateRows -join [Environment]::NewLine))
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('## Failures')
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine($failureSection)
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('## Report files')
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine("- Markdown: ``$relativeReport``")
    [void]$markdown.AppendLine("- JSON: ``$(Get-ReportPath $jsonPath)``")
    [void]$markdown.AppendLine("- Logs: ``$(Get-ReportPath (Join-Path $ReportDirectory 'logs'))``")

    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8
    Set-Content -LiteralPath $markdownPath -Value $markdown.ToString() -Encoding utf8

    return [pscustomobject]@{
        MarkdownPath = $markdownPath
        JsonPath     = $jsonPath
        Overall      = $overallStatus
        Summary      = $summary
    }
}

try {
    Write-Banner

    try {
        $script:pwshPath = Get-ToolPath 'pwsh'
        $script:dotnetPath = Get-ToolPath 'dotnet'
        $productVersion = Get-ProductVersion
    }
    catch {
        $script:setupError = $_.Exception.Message
        $productVersion = 'unknown'
    }

    $reportVersion = if ([string]::IsNullOrWhiteSpace($Version)) { $productVersion } else { $Version }
    $outputRootPath = if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
        $OutputRoot
    }
    else {
        Join-Path $repositoryRoot $OutputRoot
    }

    $reportDirectory = Join-Path $outputRootPath (Get-SafePathSegment $reportVersion)
    $logsDirectory = Join-Path $reportDirectory 'logs'
    $testResultsDirectory = Join-Path $reportDirectory 'test-results'
    $packagesDirectory = Join-Path $reportDirectory 'packages'
    $messageKeysDirectory = Join-Path $reportDirectory 'message-keys'

    if (Test-Path -LiteralPath $reportDirectory) {
        Remove-Item -LiteralPath $reportDirectory -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $logsDirectory | Out-Null
    New-Item -ItemType Directory -Force -Path $testResultsDirectory | Out-Null
    New-Item -ItemType Directory -Force -Path $packagesDirectory | Out-Null

    $commitSha = Get-GitValue -Arguments @('rev-parse', 'HEAD')
    $branch = Get-GitValue -Arguments @('rev-parse', '--abbrev-ref', 'HEAD')
    $commandLine = Get-CommandLine

    Write-Host "Product version : $productVersion"
    Write-Host "Report version  : $reportVersion"
    Write-Host "Commit          : $commitSha"
    Write-Host "Branch          : $branch"
    Write-Host "Report directory: $reportDirectory"
    Write-Host ''

    if ($script:setupError) {
        $gate = New-Gate -Name 'version' -Category 'Metadata' -Command 'dotnet msbuild -getProperty:Version' -Evidence 'Product version resolution failed.' -Action {}
        $gate.Status = 'Failed'
        $gate.Failure = $script:setupError
        $gate.ExitCode = 1
        $gate.LogPath = Join-Path $logsDirectory '00-version.log'
        Write-GateLog -Path $gate.LogPath -Message $script:setupError
        [void]$gates.Add($gate)
        [void]$failureMessages.Add("version: $($script:setupError)")
    }
    else {
        $gate = New-Gate -Name 'version' -Category 'Metadata' -Command 'dotnet msbuild -getProperty:Version' -Evidence "Evaluated product version is '$productVersion'." -Action {
            if ($ExpectedVersion -and $productVersion -ne $ExpectedVersion) {
                throw "Product version '$productVersion' does not match expected '$ExpectedVersion'."
            }

            Write-Host "Product version verified: $productVersion"
        }
        $gate.LogPath = Join-Path $logsDirectory '00-version.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'restore' -Category 'Build' -Command "dotnet restore $Solution" -Evidence 'NuGet restore completed.' -Skip:$SkipRestore -Action {
            Invoke-External -FilePath $script:dotnetPath -Arguments @('restore', $Solution)
        }
        $gate.LogPath = Join-Path $logsDirectory '01-restore.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'build' -Category 'Build' -Command "dotnet build $Solution -c Release" -Evidence 'Release build completed with 0 warnings / 0 errors.' -DependsOn @('restore') -Skip:$SkipBuild -Action {
            Invoke-External -FilePath $script:dotnetPath -Arguments @('build', $Solution, '--configuration', 'Release', '--no-restore')
        }
        $gate.LogPath = Join-Path $logsDirectory '02-build.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'tests' -Category 'Test' -Command "dotnet test $Solution -c Release --no-build" -Evidence 'Full test suite passed.' -DependsOn @('build') -Skip:$SkipTests -Action {
            Invoke-External -FilePath $script:dotnetPath -Arguments @(
                'test', $Solution,
                '--configuration', 'Release',
                '--no-build',
                '--no-restore',
                '--blame-hang-timeout', '3m',
                '--logger', 'trx;LogFileName=release-gates.trx',
                '--results-directory', $testResultsDirectory
            )
        }
        $gate.LogPath = Join-Path $logsDirectory '03-tests.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'host-conformance' -Category 'Test' -Command 'dotnet test --filter TestCategory=HostConformance' -Evidence 'Every covered host path rejects invalid input without reaching the handler.' -DependsOn @('build') -Skip:$SkipTests -Action {
            Invoke-External -FilePath $script:dotnetPath -Arguments @(
                'test', $Solution,
                '--configuration', 'Release',
                '--no-build',
                '--no-restore',
                '--filter', 'TestCategory=HostConformance',
                '--logger', 'trx;LogFileName=host-conformance.trx',
                '--results-directory', $testResultsDirectory
            )
        }
        $gate.LogPath = Join-Path $logsDirectory '03a-host-conformance.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'format' -Category 'Quality' -Command 'dotnet format --verify-no-changes' -Evidence 'Formatting is clean.' -DependsOn @('build') -Skip:$SkipFormat -Action {
            Invoke-External -FilePath $script:dotnetPath -Arguments @(
                'format', $Solution,
                '--verify-no-changes',
                '--no-restore',
                '--exclude',
                'samples/Jeninnet.Validation.Analyzers.Sample.Console',
                'samples/Jeninnet.Validation.AotSmoke'
            )
        }
        $gate.LogPath = Join-Path $logsDirectory '04-format.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'source-headers' -Category 'Quality' -Command 'check-source-headers.ps1' -Evidence 'All source files carry the standard header.' -Skip:$SkipSourceHeaders -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/quality/check-source-headers.ps1')
        }
        $gate.LogPath = Join-Path $logsDirectory '05-source-headers.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'api-baselines' -Category 'Quality' -Command 'check-public-api-baselines.ps1' -Evidence 'Public API baselines are complete.' -DependsOn @('build') -Skip:$SkipApiBaselines -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/api/check-public-api-baselines.ps1')
        }
        $gate.LogPath = Join-Path $logsDirectory '06-api-baselines.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'api-gate-self-test' -Category 'Quality' -Command 'test-public-api-gate.ps1' -Evidence 'RS0016/RS0017 fail-closed behavior proved.' -Skip:($SkipApiBaselines -or $SkipApiGateSelfTest) -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/api/test-public-api-gate.ps1')
        }
        $gate.LogPath = Join-Path $logsDirectory '07-api-gate-self-test.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'api-diff' -Category 'Quality' -Command 'compare-public-api.ps1' -Evidence 'Public API diff against v1.0.0-preview.2 has no unapproved changes.' -Skip:$SkipApiDiff -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/api/compare-public-api.ps1') -Arguments @(
                '-BaselineRef', 'v1.0.0-preview.2',
                '-CurrentRef', 'WORKTREE',
                '-OutputRoot', (Join-Path $repositoryRoot 'artifacts/api-diff')
            )
        }
        $gate.LogPath = Join-Path $logsDirectory '07a-api-diff.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'message-keys' -Category 'Quality' -Command 'check-message-keys.ps1' -Evidence 'Message-key catalog matches the resource packs.' -Skip:$SkipMessageKeys -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/localization/check-message-keys.ps1') -Arguments @('-ReportDirectory', $messageKeysDirectory)
        }
        $gate.LogPath = Join-Path $logsDirectory '08-message-keys.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'docs-links' -Category 'Docs' -Command 'check-doc-links.ps1' -Evidence 'All local documentation links resolve.' -Skip:$SkipDocs -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/docs/check-doc-links.ps1')
        }
        $gate.LogPath = Join-Path $logsDirectory '09-docs-links.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'readme-claims' -Category 'Docs' -Command 'check-readme-claims.ps1' -Evidence 'README project, packable-count, test-count, AOT, preview, and generator claims match the tree.' -Skip:$SkipDocs -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/docs/check-readme-claims.ps1')
        }
        $gate.LogPath = Join-Path $logsDirectory '10-readme-claims.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'arabic-text' -Category 'Docs' -Command 'check-arabic-text.ps1' -Evidence 'Arabic text passes the repository check.' -Skip:$SkipDocs -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/docs/check-arabic-text.ps1')
        }
        $gate.LogPath = Join-Path $logsDirectory '11-arabic-text.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'client-support' -Category 'Client' -Command 'check-client-rule-support.ps1' -Evidence 'C# and JavaScript client support lists match the manifest.' -Skip:$SkipClientSupport -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/client/check-client-rule-support.ps1')
        }
        $gate.LogPath = Join-Path $logsDirectory '11a-client-support.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'client' -Category 'Client' -Command 'npm test --prefix client' -Evidence 'JavaScript client runtime tests passed.' -Skip:$SkipClient -Action {
            $npm = if ($IsWindows) { Get-ToolPath 'npm.cmd' } else { Get-ToolPath 'npm' }
            Invoke-External -FilePath $npm -Arguments @('test', '--prefix', 'client')
        }
        $gate.LogPath = Join-Path $logsDirectory '12-client.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'vulnerability' -Category 'Security' -Command 'check-vulnerable-packages.ps1' -Evidence 'NuGet audit passed or documented allow-list entries are active.' -DependsOn @('restore') -Skip:$SkipVulnerability -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/security/check-vulnerable-packages.ps1')
        }
        $gate.LogPath = Join-Path $logsDirectory '13-vulnerability.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'package-pack' -Category 'Packages' -Command 'dotnet pack with package validation' -Evidence 'All packable projects produced packages.' -DependsOn @('build') -Skip:$SkipPackages -Action {
            New-Item -ItemType Directory -Force -Path $packagesDirectory | Out-Null
            Invoke-External -FilePath $script:dotnetPath -Arguments @(
                'pack', $Solution,
                '--configuration', 'Release',
                '--no-build',
                '--no-restore',
                '/p:EnablePackageValidation=true',
                '--output', $packagesDirectory
            )
        }
        $gate.LogPath = Join-Path $logsDirectory '14-package-pack.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'package-contents' -Category 'Packages' -Command 'check-package-contents.ps1' -Evidence 'All 25 packages contain the required assets.' -DependsOn @('package-pack') -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/package/check-package-contents.ps1') -Arguments @('-PackagesPath', $packagesDirectory)
        }
        $gate.LogPath = Join-Path $logsDirectory '15-package-contents.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'package-budget' -Category 'Packages' -Command 'check-package-dependency-budget.ps1' -Evidence 'Dependencies, assets, metadata, and sizes match the reviewed package budget.' -DependsOn @('package-pack') -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/package/check-package-dependency-budget.ps1') -Arguments @('-PackagesPath', $packagesDirectory)
        }
        $gate.LogPath = Join-Path $logsDirectory '15a-package-budget.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'package-versions' -Category 'Packages' -Command 'check package file names against product version' -Evidence "All packages carry version '$productVersion'." -DependsOn @('package-pack') -Action {
            $packages = @(Get-ChildItem -LiteralPath $packagesDirectory -Filter '*.nupkg' -File)

            if ($packages.Count -eq 0) {
                throw "No packages were produced in '$packagesDirectory'."
            }

            foreach ($package in $packages) {
                if (-not $package.BaseName.EndsWith(".$productVersion", [StringComparison]::Ordinal)) {
                    throw "Package '$($package.Name)' does not match product version '$productVersion'."
                }
            }

            Write-Host "All $($packages.Count) packages match version $productVersion."
        }
        $gate.LogPath = Join-Path $logsDirectory '16-package-versions.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'consumer-smoke' -Category 'Packages' -Command 'check-consumer-smoke.ps1' -Evidence 'Packed packages restore and validate on net10.0/net11.0.' -DependsOn @('package-pack') -Skip:$SkipConsumer -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/package/check-consumer-smoke.ps1') -Arguments @('-PackagesPath', $packagesDirectory, '-Version', $productVersion)
        }
        $gate.LogPath = Join-Path $logsDirectory '17-consumer-smoke.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'aot-support' -Category 'AOT' -Command 'check-aot-support-matrix.ps1' -Evidence 'Every packable package has an explicit AOT/trim position and property alignment.' -Skip:$SkipAotSupport -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/quality/check-aot-support-matrix.ps1')
        }
        $gate.LogPath = Join-Path $logsDirectory '17a-aot-support.log'
        [void]$gates.Add($gate)

        $gate = New-Gate -Name 'aot-smoke' -Category 'AOT' -Command 'dotnet publish AotSmoke + run' -Evidence 'NativeAOT publish and run passed.' -DependsOn @('build') -Skip:$SkipAot -Action {
            $rid = $RuntimeIdentifier

            if ([string]::IsNullOrWhiteSpace($rid)) {
                $architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()

                if ($IsWindows) { $rid = "win-$architecture" }
                elseif ($IsLinux) { $rid = "linux-$architecture" }
                elseif ($IsMacOS) { $rid = "osx-$architecture" }
                else { throw 'Unable to infer a RuntimeIdentifier for this operating system.' }
            }

            $publishDirectory = Join-Path $reportDirectory "aot/$rid"
            New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null

            Invoke-External -FilePath $script:dotnetPath -Arguments @(
                'publish',
                'samples/Jeninnet.Validation.AotSmoke/Jeninnet.Validation.AotSmoke.csproj',
                '--configuration', 'Release',
                '--runtime', $rid,
                '--self-contained',
                '--output', $publishDirectory
            )

            $executable = Join-Path $publishDirectory 'Jeninnet.Validation.AotSmoke'

            if ($IsWindows) {
                $executable += '.exe'
            }

            if (-not (Test-Path -LiteralPath $executable)) {
                throw "The NativeAOT executable was not produced at '$executable'."
            }

            Push-Location $publishDirectory

            try {
                Invoke-External -FilePath $executable
            }
            finally {
                Pop-Location
            }
        }
        $gate.LogPath = Join-Path $logsDirectory '18-aot-smoke.log'
        [void]$gates.Add($gate)

        $coverageReportedOnly = -not $IncludeCoverage.IsPresent
        $gate = New-Gate -Name 'coverage' -Category 'External' -Command 'coverage.ps1 -Execute' -Evidence 'Coverage report and quality-gate evidence.' -DependsOn @('build') -Skip:$SkipCoverage -ReportedOnly:$coverageReportedOnly -ReportedOnlyReason 'Reported-only: coverage baseline/ReportGenerator is external; pass -IncludeCoverage when available (P3-004).' -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/coverage/coverage.ps1') -Arguments @('-Execute')
        }
        $gate.LogPath = Join-Path $logsDirectory '19-coverage.log'
        [void]$gates.Add($gate)

        $sonarReportedOnly = -not $IncludeSonar.IsPresent
        $gate = New-Gate -Name 'sonar' -Category 'External' -Command 'sonar/preview.ps1 -FailOnQualityGate' -Evidence 'Sonar quality-gate evidence.' -DependsOn @('build') -Skip:$SkipSonar -ReportedOnly:$sonarReportedOnly -ReportedOnlyReason 'Reported-only: Sonar token/credentials are external; pass -IncludeSonar when configured (P3-004).' -Action {
            Invoke-PwshScript -Path (Join-Path $repositoryRoot 'scripts/sonar/preview.ps1') -Arguments @('-FailOnQualityGate')
        }
        $gate.LogPath = Join-Path $logsDirectory '20-sonar.log'
        [void]$gates.Add($gate)

        foreach ($gate in $gates) {
            Write-Host "===== $($gate.Name) =====" -ForegroundColor Cyan
            Invoke-Gate -Gate $gate

            $color = switch ($gate.Status) {
                'Passed' { 'Green' }
                'Failed' { 'Red' }
                'Skipped' { 'DarkGray' }
                'ReportedOnly' { 'Yellow' }
                default { 'White' }
            }

            Write-Host "Status: $($gate.Status)" -ForegroundColor $color
            Write-Host ''
        }
    }

    $report = Write-ReleaseReport `
        -ReportDirectory $reportDirectory `
        -ProductVersion $productVersion `
        -ReportVersion $reportVersion `
        -CommitSha $commitSha `
        -Branch $branch `
        -CommandLine $commandLine

    Write-Host '=============================================' -ForegroundColor Cyan
    Write-Host "Release gates: $($report.Overall.ToUpperInvariant())" -ForegroundColor $(if ($report.Overall -eq 'passed') { 'Green' } else { 'Red' })
    Write-Host "Report: $($report.MarkdownPath)"
    Write-Host "JSON  : $($report.JsonPath)"
    Write-Host "Total : $([math]::Round($totalTimer.Elapsed.TotalSeconds, 1)) seconds"
    Write-Host '=============================================' -ForegroundColor Cyan

    if ($report.Overall -eq 'passed') {
        exit 0
    }

    exit 1
}
catch {
    $totalTimer.Stop()
    Write-Host ''
    Write-Host "Release gate runner failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ''
    exit 1
}
