#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Verifies that the generated DocFX API metadata covers every packable assembly.

.DESCRIPTION
    Discovers the packable projects under src, reads their committed PublicAPI.Shipped.txt
    baselines, and checks that at least one public type from each project appears in the DocFX
    metadata manifest under docs/api. The type-based check handles packages that contribute types
    to an existing namespace (for example Rules.Domain contributes to Jeninnet.Validation.Rules).

    The manifest is produced by:

        dotnet docfx metadata docs/docfx.json --warningsAsErrors

    It is git-ignored, so this check runs after metadata generation in CI. A missing baseline or a
    missing assembly fails the documentation gate instead of silently dropping API pages.

.PARAMETER ApiDirectory
    The generated DocFX API directory. Defaults to <repo>/docs/api.

.PARAMETER SourceDirectory
    The packable project directory. Defaults to <repo>/src.

.PARAMETER WindowsOnlyAssemblies
    Assembly names that are intentionally documented by package READMEs rather than DocFX.

.OUTPUTS
    Prints the covered assembly count and exits 1 when an assembly or its baseline is missing.

.EXAMPLE
    pwsh scripts/docs/check-api-reference.ps1
#>
param(
    [string]$ApiDirectory = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path 'docs\api'),
    [string]$SourceDirectory = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path 'src'),
    [string[]]$WindowsOnlyAssemblies = @()
)

$ErrorActionPreference = 'Stop'

$manifestPath = Join-Path $ApiDirectory '.manifest'
$tocPath = Join-Path $ApiDirectory 'toc.yml'

if (-not (Test-Path -LiteralPath $manifestPath)) {
    Write-Error "DocFX API metadata not found at '$ApiDirectory'. Run 'dotnet docfx metadata docs/docfx.json' first."
    exit 2
}

if (-not (Test-Path -LiteralPath $tocPath)) {
    Write-Error "DocFX API table of contents not found at '$tocPath'."
    exit 2
}

if (-not (Test-Path -LiteralPath $SourceDirectory)) {
    Write-Error "Source directory not found at '$SourceDirectory'."
    exit 2
}

function Get-PublicTypeNames {
    param([string]$ShippedPath)

    Get-Content -LiteralPath $ShippedPath |
        Where-Object {
            $_ -and
            -not $_.StartsWith('#', [System.StringComparison]::Ordinal) -and
            -not $_.Contains('(', [System.StringComparison]::Ordinal) -and
            -not $_.Contains('->', [System.StringComparison]::Ordinal) -and
            -not $_.Contains(' = ', [System.StringComparison]::Ordinal)
        } |
        ForEach-Object { $_.TrimStart('~') } |
        Select-Object -Unique
}

function ConvertTo-DocFxUid {
    param([string]$TypeName)

    $builder = [System.Text.StringBuilder]::new()
    $index = 0

    while ($index -lt $TypeName.Length) {
        if ($TypeName[$index] -ne '<') {
            [void]$builder.Append($TypeName[$index])
            $index++
            continue
        }

        $depth = 1
        $commas = 0
        $cursor = $index + 1

        while ($cursor -lt $TypeName.Length -and $depth -gt 0) {
            if ($TypeName[$cursor] -eq '<') {
                $depth++
            }
            elseif ($TypeName[$cursor] -eq '>') {
                $depth--
            }
            elseif ($TypeName[$cursor] -eq ',' -and $depth -eq 1) {
                $commas++
            }

            $cursor++
        }

        [void]$builder.Append('`').Append($commas + 1)
        $index = $cursor
    }

    return $builder.ToString()
}

$projects = @(
    Get-ChildItem -LiteralPath $SourceDirectory -Recurse -Filter '*.csproj' -File |
        Where-Object {
            (Get-Content -LiteralPath $_.FullName -Raw) -match '<IsPackable>\s*true\s*</IsPackable>'
        } |
        Sort-Object FullName
)

if ($projects.Count -eq 0) {
    Write-Error "No packable projects found under '$SourceDirectory'."
    exit 2
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$uidSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

foreach ($property in $manifest.PSObject.Properties) {
    [void]$uidSet.Add($property.Name)
}

$checkedAssemblies = [System.Collections.Generic.List[string]]::new()
$missingBaselines = [System.Collections.Generic.List[string]]::new()
$missingAssemblies = [System.Collections.Generic.List[string]]::new()

foreach ($project in $projects) {
    $projectText = Get-Content -LiteralPath $project.FullName -Raw
    $assemblyName = if ($projectText -match '<AssemblyName>\s*([^<]+?)\s*</AssemblyName>') {
        $Matches[1].Trim()
    }
    else {
        [System.IO.Path]::GetFileNameWithoutExtension($project.Name)
    }

    if ($WindowsOnlyAssemblies -contains $assemblyName) {
        continue
    }

    $shipped = Join-Path $project.DirectoryName 'PublicAPI.Shipped.txt'

    if (-not (Test-Path -LiteralPath $shipped)) {
        $missingBaselines.Add("$assemblyName (missing PublicAPI.Shipped.txt)")
        continue
    }

    $typeUids = @(
        Get-PublicTypeNames -ShippedPath $shipped |
            ForEach-Object { ConvertTo-DocFxUid -TypeName $_ }
    )

    $covered = $false

    foreach ($typeUid in $typeUids) {
        if ($uidSet.Contains($typeUid)) {
            $covered = $true
            break
        }
    }

    if (-not $covered) {
        # Fallback for projects whose baseline contains only forwarded or nested declarations.
        $prefix = $assemblyName + '.'

        foreach ($uid in $uidSet) {
            if ($uid -eq $assemblyName -or $uid.StartsWith($prefix, [System.StringComparison]::Ordinal)) {
                $covered = $true
                break
            }
        }
    }

    if (-not $covered) {
        $missingAssemblies.Add($assemblyName)
        continue
    }

    $checkedAssemblies.Add($assemblyName)
}

if ($missingBaselines.Count -gt 0) {
    Write-Host "API REFERENCE CHECK FAILED (missing baseline(s)):" -ForegroundColor Red

    foreach ($assembly in $missingBaselines) {
        Write-Host "  $assembly" -ForegroundColor Red
    }

    exit 1
}

if ($missingAssemblies.Count -gt 0) {
    Write-Host "API REFERENCE CHECK FAILED ($($missingAssemblies.Count) missing assembly/assemblies):" -ForegroundColor Red

    foreach ($assembly in $missingAssemblies) {
        Write-Host "  $assembly" -ForegroundColor Red
    }

    exit 1
}

Write-Host "API reference OK: $($checkedAssemblies.Count) cross-platform packable assemblies are represented in the generated metadata; $($WindowsOnlyAssemblies.Count) Windows-only integration package(s) are documented by their package READMEs." -ForegroundColor Green
