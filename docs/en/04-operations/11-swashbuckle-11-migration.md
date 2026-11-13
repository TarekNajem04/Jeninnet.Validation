# Swashbuckle 11 and .NET 11 migration

## Status

Completed for the first release. The solution targets `net11.0` only and uses:

| Component | Version / target |
|---|---|
| .NET SDK | 11.0.100 |
| Target framework | `net11.0` |
| `Microsoft.AspNetCore.OpenApi` | 11.0.0 |
| `Microsoft.AspNetCore.TestHost` | 11.0.0 |
| `Microsoft.OpenApi` | 3.11.0 |
| `Swashbuckle.AspNetCore.SwaggerGen` | 11.0.1 |
| `Microsoft.CodeAnalysis.*` | 5.9.0 |
| `System.Collections.Immutable` | 11.0.0 |

## Why the upgrade could not happen on net10.0

| Package | Version | Dependency |
|---|---|---|
| `Microsoft.AspNetCore.OpenApi` | 10.0.15 | `Microsoft.OpenApi [2.13.0, 3.0.0)` |
| `Swashbuckle.AspNetCore.Swagger` | 10.3.1 | `Microsoft.OpenApi 2.7.5` |
| `Swashbuckle.AspNetCore.Swagger` | 11.0.1 | `Microsoft.OpenApi 3.11.0` |
| `Microsoft.AspNetCore.OpenApi` | 11.0.0 | `Microsoft.OpenApi [3.11.0, 4.0.0)` and `net11.0` only |

The 2.x and 3.x constraints are mutually exclusive, so a standalone Swashbuckle bump failed restore
with `NU1107`. ASP.NET Core OpenAPI 11 is also `net11.0`-only, so the OpenAPI surface had to move to
`net11.0` before Swashbuckle could move.

## What changed

- `Directory.Build.props` targets `net11.0`; the temporary dual-targeting `src/Directory.Build.props`
  was removed.
- All shipping libraries now produce `net11.0` assets, including the OpenAPI packages.
- `Microsoft.AspNetCore.OpenApi`, `Microsoft.AspNetCore.TestHost`, `Microsoft.OpenApi`, and
  `Swashbuckle.AspNetCore.SwaggerGen` moved to the 11.0 / 3.11.0 line.
- Roslyn 5.9, `System.Collections.Immutable` 11.0.0, and `PublicApiAnalyzers` 5.6.0 are enabled;
  the Public API baselines were regenerated for the new analyzer and record-generated members.
- `ValidationSchemaMapper` computes `ScalePrecision` with exact decimal arithmetic.
- `NumericValueConversion` parses floating-point values from their shortest round-trip text so
  `MultipleOf` stays stable on .NET 11.
- Blazor tests use `EditContext.ValidateAsync`; OpenAPI tests use `GetTypeInfo<T>()`.
- The consumer smoke test targets `net11.0` only.

## Verification

- `dotnet build --configuration Release`: 0 warnings / 0 errors.
- Full suite: 997 tests pass on `net11.0`.
- DocFX metadata and build: 0 warnings / 0 errors.
- Package validation, consumer smoke test, and Public API baselines pass.
- CI, SonarCloud, CodeQL, Documentation, and AOT Smoke pass on the migration commit.

## Compatibility note

The first release is `net11.0`-only (STS). If LTS demand appears later, adding `net10.0` is an
additive change once the public API stabilizes.
