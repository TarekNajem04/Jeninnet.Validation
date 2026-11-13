#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

param(
    [string]$PackagesPath = "artifacts/packages",
    [switch]$Keep
)

$ErrorActionPreference = "Stop"

$resolvedPath = (Resolve-Path -LiteralPath $PackagesPath).Path
$root = Join-Path ([System.IO.Path]::GetTempPath()) ("jeninnet-consumer-" + [Guid]::NewGuid().ToString("N"))

New-Item -ItemType Directory -Path $root | Out-Null
New-Item -ItemType Directory -Path (Join-Path $root "feed") | Out-Null

try {
    Get-ChildItem -LiteralPath $resolvedPath -Filter *.nupkg | Copy-Item -Destination (Join-Path $root "feed")

    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net11.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Jeninnet.Validation" Version="1.0.0-preview.1" />
    <PackageReference Include="Jeninnet.Validation.Analyzers" Version="1.0.0-preview.1" PrivateAssets="all" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="11.0.0" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $root "ConsumerSmoke.csproj") -Encoding utf8

    @"
using Jeninnet.Validation.Abstractions;
using Jeninnet.Validation.Attributes;
using Jeninnet.Validation.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace ConsumerSmoke;

[Validate]
public partial class User
{
    [NotEmpty]
    public string? Name { get; set; }
}

public static class Program
{
    public static void Main()
    {
        var services = new ServiceCollection();
        services.AddValidation();
        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<User>>();
        var result = validator.Validate(new User { Name = "" });

        if (result.IsValid || result.Errors.Count != 1 || result.Errors[0].MessageKey != "NotEmpty")
        {
            throw new InvalidOperationException("The packed consumer smoke test did not observe the expected validation failure.");
        }

        Console.WriteLine("Consumer smoke passed.");
    }
}
"@ | Set-Content -LiteralPath (Join-Path $root "Program.cs") -Encoding utf8

    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -LiteralPath (Join-Path $root "NuGet.config") -Encoding utf8

    foreach ($framework in @("net11.0")) {
        dotnet run --project (Join-Path $root "ConsumerSmoke.csproj") -f $framework -v:q
        if ($LASTEXITCODE -ne 0) {
            throw "Consumer smoke test for $framework failed with exit code $LASTEXITCODE."
        }
    }

    Write-Host "Consumer smoke test passed for net11.0."
}
finally {
    if (-not $Keep) {
        Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
    }
    else {
        Write-Host "Kept consumer project at $root"
    }
}
