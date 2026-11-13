#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

#requires -Version 7
<#
.SYNOPSIS
    Installs the local jeninnet-validator template, generates a project, and verifies its shape.
.DESCRIPTION
    This is a fast template smoke test. It does not restore the generated project, so it works
    before the preview package is published. Run it after changing template.json or the content.
#>
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'

$templateDirectory = Join-Path $RepositoryRoot 'templates\jeninnet-validator'
$outputDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('jeninnet-template-' + [guid]::NewGuid().ToString('N'))
$installed = $false

try {
    if (-not (Test-Path -LiteralPath $templateDirectory)) {
        throw "Template directory not found: $templateDirectory"
    }

    dotnet new install $templateDirectory --force | Out-Null
    $installed = $true

    dotnet new jeninnet-validator -n JeninnetTemplateSmoke -o $outputDirectory | Out-Null

    $expected = @(
        'JeninnetTemplateSmoke.csproj',
        'Customer.cs',
        'CustomerValidator.cs'
    )

    foreach ($file in $expected) {
        $path = Join-Path $outputDirectory $file
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Generated template is missing '$file'."
        }
    }

    $project = Get-Content -LiteralPath (Join-Path $outputDirectory 'JeninnetTemplateSmoke.csproj') -Raw
    if ($project -notmatch '<PackageReference Include="Jeninnet\.Validation"') {
        throw 'Generated project does not reference Jeninnet.Validation.'
    }

    $validator = Get-Content -LiteralPath (Join-Path $outputDirectory 'CustomerValidator.cs') -Raw
    if ($validator -notmatch 'Validator<Customer>') {
        throw 'Generated validator does not use Validator<Customer>.'
    }

    Write-Host "Template OK: jeninnet-validator generated 3 file(s) in a temporary directory." -ForegroundColor Green
}
finally {
    if ($installed) {
        dotnet new uninstall $templateDirectory | Out-Null
    }

    if (Test-Path -LiteralPath $outputDirectory) {
        Remove-Item -LiteralPath $outputDirectory -Recurse -Force
    }
}
