#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

param(
    [string]$PackagesPath = "artifacts/packages",
    [int]$ExpectedCount = 25
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

$resolvedPath = Resolve-Path -LiteralPath $PackagesPath
$packages = @(Get-ChildItem -LiteralPath $resolvedPath -Filter *.nupkg | Sort-Object Name)

if ($packages.Count -ne $ExpectedCount) {
    throw "Expected $ExpectedCount packages in '$resolvedPath', found $($packages.Count)."
}

$issues = [System.Collections.Generic.List[string]]::new()

foreach ($package in $packages) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)

    try {
        $entries = @($archive.Entries | ForEach-Object { $_.FullName })
        $readme = $archive.Entries | Where-Object { $_.FullName -eq "README.md" } | Select-Object -First 1

        if ($null -eq $readme) {
            $issues.Add("$($package.Name): missing README.md")
        }
        else {
            if ($readme.Length -gt 30000) {
                $issues.Add("$($package.Name): README.md is $($readme.Length) bytes; expected a package README, not a transcript.")
            }

            $reader = [System.IO.StreamReader]::new($readme.Open())
            try {
                $firstLine = $reader.ReadLine()
            }
            finally {
                $reader.Dispose()
            }

            if ($firstLine -notmatch '^#\s') {
                $issues.Add("$($package.Name): README.md does not start with a Markdown heading.")
            }
        }

        foreach ($required in @("LICENSE", "jeninnet-validation-icon-128.png")) {
            if ($entries -notcontains $required) {
                $issues.Add("$($package.Name): missing $required")
            }
        }

        if (-not ($entries | Where-Object { $_ -match '^(lib|tools|analyzers)/.+\.xml$' })) {
            $issues.Add("$($package.Name): missing packaged XML documentation.")
        }

        if ($entries | Where-Object { $_ -match '\.(cs|csproj|props|targets)$' -and $_ -notmatch '^build/' }) {
            $issues.Add("$($package.Name): contains source/build files outside the expected build folder.")
        }

        if ($package.Name -like "Jeninnet.Validation.Analyzers.*") {
            if ($entries -notcontains "analyzers/dotnet/cs/Jeninnet.Validation.Analyzers.dll") {
                $issues.Add("$($package.Name): missing analyzers/dotnet/cs/Jeninnet.Validation.Analyzers.dll")
            }

            if ($entries | Where-Object { $_ -like "lib/*" }) {
                $issues.Add("$($package.Name): analyzer package must not ship a lib folder.")
            }
        }

        if ($package.Name -like "Jeninnet.Validation.Migration.*" -and $entries -match '\.dll\.config$') {
            $issues.Add("$($package.Name): .NET tool package contains a .dll.config artifact.")
        }

        foreach ($bridge in @("Jeninnet.Validation.DataAnnotations", "Jeninnet.Validation.Extensions.Validation")) {
            if ($package.Name -like "$bridge.*") {
                foreach ($tfm in @("net11.0")) {
                    if (-not ($entries | Where-Object { $_ -like "lib/$tfm/$bridge.dll" })) {
                        $issues.Add("$($package.Name): missing lib/$tfm/$bridge.dll")
                    }
                }
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}

if ($issues.Count -gt 0) {
    throw ($issues -join [Environment]::NewLine)
}

Write-Host "Package content check passed for $($packages.Count) packages."
