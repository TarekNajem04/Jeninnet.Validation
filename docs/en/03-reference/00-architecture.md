# Architecture

## Packages and dependency direction

```text
Jeninnet.Validation.Abstractions   contracts + value types (no framework references)
        ▲
Jeninnet.Validation                engine + fluent API
        ▲
        ├── Analyzers                 source generator + analyzers (netstandard2.0)
        ├── Assertions                test-time helpers
        ├── Localization              message packs
        ├── AspNetCore                minimal API + MVC
        ├── JsonSchema                descriptor → JSON Schema
        ├── OpenApi                   descriptor → OpenAPI constraints
        ├── ClientRules               descriptor → JSON/TS/JS
        ├── MediatR                   pipeline behavior
        ├── DataAnnotations           ValidationAttribute bridge
        └── Blazor                    EditContext adapter
```

Core and Abstractions never reference ASP.NET Core, MediatR, Blazor, or DataAnnotations. The
architecture suite pins the direction.

## Execution pipeline

1. `Validate`/`ValidateAsync` selects the rule sets and filters.
2. A pooled `ValidationErrorCollector` and `RootContextData` are rented.
3. Rules run in order; each rule reads the accessor, evaluates, and adds errors.
4. `ValidationResult.FromErrors` snapshots the errors before the collector returns to the pool.
5. Cascade, fail-fast, conditions, and dependent gates decide whether later rules run.

The happy path is allocation-free after warmup: the collector and root data are per-thread pooled,
and the default rule selection returns the original array.

## Metadata pipeline

Fluent and generated validators implement `IValidationDescriptorProvider`. `Describe()` returns a
frozen descriptor. JSON Schema, OpenAPI, and client-rule packages consume descriptors only; they do
not execute validation.

## AOT and trimming

The AOT-compatible libraries (`Jeninnet.Validation`, `Abstractions`, `JsonSchema`, `ClientRules`,
`Localization`, and `Assertions`) declare `IsAotCompatible`. The engine uses no runtime reflection on
the hot path; descriptors are frozen. Reflection-based assembly scanning is annotated and has
explicit registration alternatives. The NativeAOT smoke harness publishes and runs the engine,
generator, localization, and source-generated JSON.

## Error model

`ValidationError` carries `MemberName`, `MessageId`, `Message`, `MessageKey`, `MessageParameters`,
`ErrorCode`, `Severity`, and `CustomState`. `ValidationResult` is a readonly struct with `IsValid`
and `Errors`.

## Related

- [Core API](01-core-api.md)
- [Metadata](04-metadata.md)
- [AOT and trimming](../04-operations/02-aot-and-trimming.md)
