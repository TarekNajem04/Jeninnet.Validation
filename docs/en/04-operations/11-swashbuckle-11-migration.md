# Swashbuckle 11 and .NET 11 migration

## Status

Completed for the .NET 11 path. The .NET 10 baseline stays on `Swashbuckle.AspNetCore.SwaggerGen`
10.3.1, `Microsoft.AspNetCore.OpenApi` 10.0.14, and `Microsoft.OpenApi` 2.x. The .NET 11 path uses:

| Component | Version / target |
|---|---|
| .NET SDK | 11.0.100 |
| Target framework | `net11.0` (additive; net10.0 is the baseline) |
| `Microsoft.AspNetCore.OpenApi` | 11.0.0 |
| `Microsoft.AspNetCore.TestHost` | 11.0.0 |
| `Microsoft.OpenApi` | 3.11.0 |
| `Swashbuckle.AspNetCore.SwaggerGen` | 11.0.1 |
| `Microsoft.CodeAnalysis.*` | 5.9.0 |
| `System.Collections.Immutable` | 11.0.0 |

## Preview.2 update

Preview.2 restores `net10.0` as the baseline and keeps the .NET 11 path described here as additive.
The OpenAPI packages now dual-target:

- `net10.0`: `Microsoft.AspNetCore.OpenApi` 10.0.14, `Microsoft.OpenApi` 2.12.2, and
  `Swashbuckle.AspNetCore.SwaggerGen` 10.3.1.
- `net11.0`: `Microsoft.AspNetCore.OpenApi` 11.0.0, `Microsoft.OpenApi` 3.11.0, and
  `Swashbuckle.AspNetCore.SwaggerGen` 11.0.1.

The historical migration notes below explain why the .NET 11 path uses the 11.x / 3.x line.

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

- `Directory.Build.props` targeted `net11.0` during the migration; preview.2 restores the `net10.0`
  baseline and keeps the net11 path as additive assets.
- All shipping libraries produce `net10.0` assets; the bridge and OpenAPI packages also produce
  `net11.0` assets.
- `Microsoft.AspNetCore.OpenApi`, `Microsoft.AspNetCore.TestHost`, `Microsoft.OpenApi`, and
  `Swashbuckle.AspNetCore.SwaggerGen` moved to the 11.0 / 3.11.0 line.
- Roslyn 5.9, `System.Collections.Immutable` 11.0.0, and `PublicApiAnalyzers` 5.6.0 are enabled;
  the Public API baselines were regenerated for the new analyzer and record-generated members.
- `ValidationSchemaMapper` computes `ScalePrecision` with exact decimal arithmetic.
- `NumericValueConversion` parses floating-point values from their shortest round-trip text so
  `MultipleOf` stays stable on .NET 11.
- Blazor tests use `EditContext.ValidateAsync`; OpenAPI tests use `GetTypeInfo<T>()`.
- The consumer smoke test targeted `net11.0` during the migration; preview.2 runs it on `net10.0`
  and `net11.0`.

## Verification

- `dotnet build --configuration Release`: 0 warnings / 0 errors.
- Full suite: 997 tests passed on `net11.0` during the migration; preview.2 runs 1,038 tests across
  `net10.0` and `net11.0`.
- DocFX metadata and build: 0 warnings / 0 errors.
- Package validation, consumer smoke test, and Public API baselines pass.
- CI, SonarCloud, CodeQL, Documentation, and AOT Smoke pass on the migration commit.

## Compatibility note

`net10.0` is the baseline, and the `net11.0` path described here is additive. The OpenAPI package
selects the matching dependency line per target framework, so a .NET 10 consumer is never forced onto
`Microsoft.AspNetCore.OpenApi` 11 or `Microsoft.OpenApi` 3.x.
