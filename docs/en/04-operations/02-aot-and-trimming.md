# AOT and Trimming

## Guarantee

The AOT-compatible libraries (`Jeninnet.Validation`, `Abstractions`, `JsonSchema`, `ClientRules`,
`Localization`, and `Assertions`) declare `IsAotCompatible` and build with zero trim/AOT analyzer
warnings in Release. The engine uses no runtime reflection on the hot path; rules, accessors, and
descriptors are constructed explicitly.

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
