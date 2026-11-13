# Production Checklist

Use this list before shipping an application that depends on Jeninnet.Validation.

## Packages and versions

- [ ] Reference the current preview explicitly (`1.0.0-preview.1`) or pin a central version in
      `Directory.Packages.props`.
- [ ] Add only the integration packages you use; Core and Abstractions stay framework-free.
- [ ] Keep all Jeninnet.Validation packages on the same version.
- [ ] Read [Compatibility and versions](../03-reference/06-compatibility-and-versions.md) before
      upgrading across previews.

## Registration and lifetimes

- [ ] Register every validator as `IValidator<T>`; hosts resolve the interface, not the concrete
      type.
- [ ] Validators are stateless after configuration and safe to reuse. `AddGeneratedValidators`
      registers them as scoped; explicit registration can be scoped or singleton.
- [ ] Do not capture a scoped service in a singleton validator. Constructor-inject only services
      with a compatible lifetime.
- [ ] For trimmed or Native AOT deployments, register validators explicitly:

```csharp
services.AddScoped<CustomerValidator>();
services.AddScoped<IValidator<Customer>, CustomerValidator>();
```

- [ ] Do not rely on `AddGeneratedValidators` in AOT; it is annotated
      `[RequiresUnreferencedCode]`.

## Execution path

- [ ] Use `ValidateAsync` when the validator contains `MustAsync`, `CustomAsync`, async conditions,
      or async filters.
- [ ] Never call sync `Validate` on an async-only validator; it throws
      `AsyncValidatorInvokedSynchronouslyException` by design.
- [ ] Pass the request cancellation token through the async path.
- [ ] Choose `Validate` vs `ValidateAndThrow` deliberately. `ValidateAndThrow` collects every error
      before throwing.
- [ ] Use `ValidationCallOptions` or `ValidationStrategy` for per-call rule sets, property filters,
      cascade, and fail-fast.

## Error responses

- [ ] Map validation failures to your API contract once, at the boundary. ASP.NET Core users can
      use `AddValidationEndpoints` / `AddMvcValidation` and `ToProblemErrors`.
- [ ] Keep `IncludeErrorCodes` and `IncludeSeverities` off unless the client needs them.
- [ ] Do not put secrets, SQL, stack traces, or raw user input into custom messages.
- [ ] Confirm `ValidationException` is handled and does not escape as a 500.
- [ ] Keep ProblemDetails enrichment (`IncludeCorrelationId`, `IncludeModelType`,
      `IncludeDuration`) off unless the client contract needs it; correlation ids are request
      metadata, not user data.
- [ ] See [Security and data exposure](09-security.md) for the threat model.

## Observability

- [ ] Add `Jeninnet.Validation.Diagnostics` only when an exporter or listener is configured;
      without `AddValidationDiagnostics` the sink is null and the core adds no allocation.
- [ ] Register the `Jeninnet.Validation` ActivitySource/Meter with the OpenTelemetry SDK.
- [ ] Keep sampling intentional: metrics are never sampled, tracing can use
      `SamplingProbability`.
- [ ] Do not add messages, property values, or custom state to telemetry tags; the package emits
      only type, validator, rule-set, outcome, and error-count metadata.

## Localization

- [ ] Register exactly one `IInnerMessageProvider` through `AddDefaultMessagePacks`.
- [ ] Add custom packs and overrides through the options callback, not a second provider.
- [ ] Set a `DefaultCulture` and verify the fallback chain for unsupported cultures.
- [ ] Test at least one right-to-left culture (Arabic) end to end.
- [ ] Keep message keys as constants (`ValidationMessageKeys.*`), not literals.

## Schema, OpenAPI, and client rules

- [ ] Generate JSON Schema/OpenAPI/client rules from the same descriptors the runtime uses.
- [ ] Treat client rules as advisory data; the server remains authoritative.
- [ ] Verify that conditions export as `hasCondition`, not as executable predicates.
- [ ] Check custom JSON naming policies (`snake_case`, `kebab-case`) against the OpenAPI
      transformer’s case-insensitive matching.
- [ ] Version the client-rules payload (`schemaVersion`) and ship it with the client.

## AOT and trimming

- [ ] Publish with `PublishTrimmed` / Native AOT in CI, not only locally.
- [ ] Run the published binary; a successful publish is not enough.
- [ ] Keep reflection-based integrations (DataAnnotations, Blazor) out of the AOT claim unless you
      have tested them.
- [ ] Use `samples/Jeninnet.Validation.AotSmoke` as the reference harness.

## Testing

- [ ] Cover a valid model and every member’s invalid case.
- [ ] Cover rule sets, conditions, dependent rules, and cascade behavior.
- [ ] Cover async rules on the async path.
- [ ] Cover generated/fluent parity when both authoring styles exist.
- [ ] Use `Jeninnet.Validation.Assertions` for readable failures; it is test-framework agnostic.
- [ ] Add an integration test for each host adapter you ship (ASP.NET Core, MediatR, Blazor, MVC).

## Performance

- [ ] Measure in Release, after warmup.
- [ ] Keep the happy path allocation-free; use the allocation audit as the contract.
- [ ] Avoid allocating closures or formatting strings inside hot custom predicates.
- [ ] Benchmark your own validators if they are on a critical path; the built-in benchmarks are a
      baseline, not a guarantee for custom rules.

## Operations

- [ ] Log validation failures at the boundary with a correlation ID, not the full payload.
- [ ] Add `Jeninnet.Validation.Diagnostics` when you want built-in `ActivitySource`/`Meter`
      instrumentation; otherwise add your own counters/histograms at the boundary.
- [ ] Monitor 400 rates separately from 500 rates.
- [ ] Alert on `AsyncValidatorInvokedSynchronouslyException`; it indicates a wrong execution path.
- [ ] Keep the package version and the documentation version in sync.
- [ ] Run the repository gates if you contribute changes: build, tests, format, docs checks.

## Release gate

| Gate | Command |
|---|---|
| Build | `dotnet build Jeninnet.Validation.slnx` |
| Tests | `dotnet test Jeninnet.Validation.slnx` |
| Format | `dotnet format Jeninnet.Validation.slnx --verify-no-changes --severity info` |
| Docs links | `pwsh scripts/docs/check-doc-links.ps1` |
| README claims | `pwsh scripts/docs/check-readme-claims.ps1` |
| Arabic text | `pwsh scripts/docs/check-arabic-text.ps1` |
| AOT smoke | `dotnet publish samples/Jeninnet.Validation.AotSmoke/... -c Release -r linux-x64` |

## Related

- [Performance](01-performance.md)
- [AOT and trimming](02-aot-and-trimming.md)
- [Troubleshooting](04-troubleshooting.md)
- [Security and data exposure](09-security.md)
