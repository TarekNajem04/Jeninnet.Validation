# AOT and Trimming

## Guarantee

The AOT-compatible libraries (`Jeninnet.Validation`, `Abstractions`, `Assertions`, `ClientRules`,
`Diagnostics`, `Extensions.Validation`, `JsonSchema`, `Localization`, `OpenApi`, and `Rules.Domain`)
declare `IsAotCompatible` and build with zero trim/AOT analyzer warnings in Release. The engine uses no runtime reflection on the hot path; rules, accessors, and
descriptors are constructed explicitly.

## Package matrix

Every packable package has an explicit position. The machine-readable source of truth is
[`scripts/quality/aot-support-matrix.json`](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/scripts/quality/aot-support-matrix.json);
the release gate checks that this table and the project properties stay aligned.

| Package | TFM(s) | Position | Evidence / fallback |
|---|---|---|---|
| `Jeninnet.Validation` | net10.0 | AOT-compatible | Engine and generated registry are reflection-free; NativeAOT smoke runs it. |
| `Jeninnet.Validation.Abstractions` | net10.0 | AOT-compatible | Contracts and value types only. |
| `Jeninnet.Validation.Analyzers` | netstandard2.0 | Unsupported (build-time only) | Roslyn analyzer/source generator; reference as an analyzer. |
| `Jeninnet.Validation.AspNetCore` | net10.0 | Reflection-dependent | Runtime generic validator resolution; follow the ASP.NET Core host posture. |
| `Jeninnet.Validation.Assertions` | net10.0 | AOT-compatible (test-only) | Explicit assertions; do not ship in production. |
| `Jeninnet.Validation.Blazor` | net10.0 | Reflection-dependent | Cached reflection resolves nested fields; not an AOT target. |
| `Jeninnet.Validation.ClientRules` | net10.0 | AOT-compatible | Reflection-free descriptor projection; parity fixtures cover the client subset. |
| `Jeninnet.Validation.DataAnnotations` | net10.0;net11.0 | Reflection-dependent | Generic validator invocation uses reflection; not an AOT target. |
| `Jeninnet.Validation.Diagnostics` | net10.0 | AOT-compatible | Explicit ActivitySource/Meter integration. |
| `Jeninnet.Validation.Extensions.Validation` | net10.0;net11.0 | AOT-compatible | Generic overloads; non-generic overloads are annotated for trimming/AOT. |
| `Jeninnet.Validation.GraphQL` | net10.0 | Trim-compatible only | No reflection added by the interceptor; follows the HotChocolate host. |
| `Jeninnet.Validation.Grpc` | net10.0 | Trim-compatible only | Explicit request types; follows the gRPC host. |
| `Jeninnet.Validation.JsonSchema` | net10.0 | AOT-compatible | Frozen descriptors and JsonNode output. |
| `Jeninnet.Validation.Localization` | net10.0 | AOT-compatible | Explicit message packs and catalog. |
| `Jeninnet.Validation.Localization.AspNetCore` | net10.0 | Trim-compatible only | IStringLocalizer bridge; follows ASP.NET Core localization. |
| `Jeninnet.Validation.MediatR` | net10.0 | Trim-compatible only | Generic pipeline; follows the MediatR host. |
| `Jeninnet.Validation.MediatR.CSharpFunctionalExtensions` | net10.0 | Trim-compatible only | Explicit result mapping; follows the host. |
| `Jeninnet.Validation.MediatR.ErrorOr` | net10.0 | Trim-compatible only | Explicit result mapping; follows the host. |
| `Jeninnet.Validation.MediatR.FluentResults` | net10.0 | Trim-compatible only | Explicit result mapping; follows the host. |
| `Jeninnet.Validation.Migration` | net10.0 | Unsupported (build-time only) | Roslyn migration tool; not an application runtime component. |
| `Jeninnet.Validation.Observable` | net10.0 | Reflection-dependent | INotifyDataErrorInfo desktop/UI adapter; not an AOT target. |
| `Jeninnet.Validation.OpenApi` | net10.0;net11.0 | AOT-compatible | Applier is smoke-tested with source-generated metadata; package builds clean with AOT/trim analyzers. Host transformer requires an AOT-capable ASP.NET Core pipeline. |
| `Jeninnet.Validation.OpenApi.NSwag` | net10.0;net11.0 | Reflection-dependent | NJsonSchema/NSwag generation uses reflection. |
| `Jeninnet.Validation.OpenApi.Swashbuckle` | net10.0;net11.0 | Reflection-dependent | Swashbuckle generation uses reflection. |
| `Jeninnet.Validation.Rules.Domain` | net10.0 | AOT-compatible | Explicit checks and reference data. |

**Counts:** 10 AOT-compatible (one test-only), 6 trim-compatible only, 7 reflection-dependent, and
2 unsupported build-time tools. No package gets a NativeAOT claim without the matrix and the
smoke/analyzer evidence.

## Generated registry (AOT-safe)

The generator emits a registry for every assembly that contains `[Validate]` types. The registry is
plain `typeof` registration with no assembly scanning:

```csharp
using MyApp.GeneratedValidation;

services.AddGeneratedValidatorsFromThisAssembly();
```

The generated namespace is `<assembly name>.GeneratedValidation`. The extension registers every
generated validator as its concrete type plus `IValidator<T>`, `ISyncValidator<T>`, and
`IAsyncValidator<T>`. The default lifetime is `Scoped`; pass another `ServiceLifetime` if needed.
Registrations use `TryAdd`, so calling the registry twice does not duplicate services.

`AddGeneratedValidatorsFromThisAssembly()` calls `AddValidationCore()`, which registers the
validator factory, service-provider forwarding, and localization without reflection. Use
`AddValidationCore()` directly when a project has no generated validators but still needs the
infrastructure.

`[Flags]` enum validation builds a bit mask from `Enum.GetValuesAsUnderlyingType` once when the
rule is constructed; validation itself performs only bitwise checks. The NativeAOT smoke harness
exercises a generated `[Flags]` enum (declared combinations pass, undefined bits fail).

The registry is generated only when the compilation references
`Microsoft.Extensions.DependencyInjection`, which is a transitive dependency of
`Jeninnet.Validation`.

## Explicit registration

Explicit registration remains supported and is the fallback when generated validators live in
different assemblies from the registration code:

```csharp
services.AddScoped<CustomerValidator>();
services.AddScoped<IValidator<Customer>, CustomerValidator>();
```

`AddGeneratedValidators(...)` reflection overloads are still available for compatibility but remain
annotated `[RequiresUnreferencedCode]`; the `Type[]` overload is not AOT-safe either. Prefer the
generated registry in trimmed and Native AOT applications.

## Smoke harness

`samples/Jeninnet.Validation.AotSmoke` publishes with NativeAOT and runs:

- generated registry registration (no assembly scanning);
- generated validator, generated email/range rules, custom rule;
- descriptor generation, JSON Schema output, and client-rules export;
- Microsoft OpenAPI applier with source-generated JSON metadata;
- rule sets;
- fluent async validation;
- strict-async guard;
- async-only rule;
- localization;
- source-generated JSON round-trip.

```bash
dotnet publish samples/Jeninnet.Validation.AotSmoke/Jeninnet.Validation.AotSmoke.csproj \
  -c Release -r linux-x64 --self-contained -o out/aot-smoke
./out/aot-smoke/Jeninnet.Validation.AotSmoke
```

CI runs the same flow on `linux-x64`.

## Exclusions

Benchmarks and samples are outside the runtime AOT claim. The assertions package is AOT-compatible
but test-only. Integration packages (ASP.NET Core, MediatR, OpenApi, DataAnnotations, Blazor) follow
their host’s AOT posture; DataAnnotations and Blazor use reflection and are not AOT targets.

## Related

- [Performance](01-performance.md)
- [Known limitations](../03-reference/07-known-limitations.md)
