# FAQ

## Authoring

**Is Jeninnet.Validation a FluentValidation replacement?**

It covers the same fluent authoring style and adds generated validators, descriptors, schema/client
emission, and integration packages. Migration is mechanical; see
[Migration from FluentValidation](../02-guides/15-migration-from-fluentvalidation.md).

**Can I use both libraries?**

Yes, but do not validate the same model twice. Prefer one engine per boundary.

**Can I use it without DI?**

Yes. Fluent and generated validators are plain objects. DI is needed for constructor-injected
nested validators and host integrations.

**Where does the generated validator live?**

The generator emits `<model namespace>.Validators.<Model>Validator`. For `MyApp.Customer`, the
validator is `MyApp.Validators.CustomerValidator`. Add `[Validate]` to the model and reference the
analyzer package.

**Why is my generated validator missing?**

- The model is not annotated with `[Validate]`.
- The analyzer package is not referenced with `OutputItemType="Analyzer"`.
- The model is in a different project and the analyzer is not referenced there.
- The build is stale; rebuild after adding the attribute.

**How do I add a rule that isn't built in?**

Use `Must` / `MustAsync` for a predicate, `Custom` / `CustomAsync` for multi-error logic, or a custom
rule class for reuse. See [Custom rules](../02-guides/07-custom-rules.md).

**How do I validate only some properties?**

Use named rule sets for scenario-based subsets, or `IncludeProperties` / `ExcludeProperties` in
`ValidationCallOptions` for fluent member groups. See
[Rule sets and cascade](../02-guides/04-rule-sets-and-cascade.md).

**How do I get every error instead of the first one?**

The default is `CascadeMode.Continue`: every rule runs and every error is collected. `FailFast` or
`CascadeMode.Stop` changes that behavior.

**How do I customize messages?**

Use `WithMessage` for text, `WithCode` for a machine-readable code, `WithSeverity`, and
`WithName` / `OverridePropertyName` to replace the reported member name. To change only the display
name used by `{PropertyName}`, set `ValidationOptions.DisplayNameResolver`.

## Execution

**Are async rules supported?**

Yes, through `MustAsync`, `CustomAsync`, async conditions, and `ValidateAsync`. DataAnnotations and
Blazor adapters are sync-only.

**Why does sync validation throw for async-only validators?**

Silently skipping async rules would produce a false “valid” result. The strict-async guard makes the
contract explicit.

**What happens if a hook throws?**

`PreValidate`, `PreValidateAsync`, `OnFailure`, and `OnFailureAsync` wrap non-cancellation exceptions
in `ValidationException`; cancellation propagates. See
[Lifecycle hooks](../02-guides/19-lifecycle-hooks.md).

**Can I reuse a validator across threads?**

Yes. Validators are stateless after configuration and use pooled per-thread state during validation.

**Does it emit metrics or tracing?**

The core engine does not. The optional `Jeninnet.Validation.Diagnostics` package adds an
`ActivitySource` and `Meter` through `AddValidationDiagnostics`; ASP.NET Core ProblemDetails can
also enrich responses with correlation, model, and duration extensions. Instrument the boundary
with your application’s telemetry stack; log member paths and message keys, not payloads.

## Hosts and formats

**Does it work with Native AOT?**

The runtime libraries do. Register validators explicitly; assembly scanning is annotated and not
AOT-safe. The smoke harness proves the end-to-end path.

**Does it support minimal APIs, MVC, Blazor, and MediatR?**

Yes, through optional packages. See [Integration packages](../03-reference/05-integration-packages.md).

**Does it generate JSON Schema and OpenAPI?**

Yes, from descriptors. See
[JSON Schema, OpenAPI, and client rules](../02-guides/12-schema-openapi-client.md).

**Does it generate client-side validation code?**

It exports rule data (JSON/TS/JS). It does not port the engine to JavaScript; the server remains
authoritative.

**Can I use it in a library that only needs contracts?**

Reference `Jeninnet.Validation.Abstractions` for `IValidator<T>`, `ValidationResult`, and
`ValidationError` without the engine or integrations.

## Versions and support

**Which .NET versions are supported?**

.NET 11, with C# 15.

**Which version should I reference?**

The current preview is `1.0.0-preview.1`. The version is centralized in `Directory.Build.props`.

**Is the public API stable?**

Not yet. The current package is a preview; see
[Compatibility and versions](../03-reference/06-compatibility-and-versions.md) before upgrading.

**Where are the samples?**

See [Samples](../appendix/03-samples.md).

**How do I contribute?**

Source contributions are limited to maintainers while the library stabilizes. Bug reports, feature
requests, and API feedback are welcome; see
[CONTRIBUTING.md](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/CONTRIBUTING.md).
