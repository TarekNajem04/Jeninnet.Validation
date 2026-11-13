# Mental Model

Understanding four concepts is enough to predict how the library behaves: **rules**, **validators**,
**execution**, and **metadata**.

## Rules

A rule is a small object that:

1. reads a value through an `IPropertyAccessor<T, TProperty>`;
2. decides whether the value is valid;
3. adds a `ValidationError` to a collector when it is not.

Rules are stateless after configuration and can be reused across validation calls. Built-in rules
cover presence, comparison, strings, dates, collections, and property comparison.

## Validators

A fluent validator is a `Validator<T>` that builds an ordered set of rules in `Configure`. A
generated validator is a partial class emitted from attributes; it builds the same rule set.

Both implement `IValidator<T>`, which combines `ISyncValidator<T>` and `IAsyncValidator<T>`.
The validator is the unit you register in DI and resolve in a host.

## Execution

`Validate` and `ValidateAsync` create a `ValidationContext<T>` and a pooled
`ValidationErrorCollector`, run the selected rule groups in order, and return a
`ValidationResult`.

- **Sync rules** run on both paths.
- **Async rules** run only on the async path.
- **Async-only validators** throw on the sync path.
- **Rule sets** select named groups; when a set is selected, ungrouped rules run only if `default` is included.
- **Cascade** controls whether a member continues after a failure.
- **Fail-fast** stops the whole validation at the first error.
- **Conditions** gate individual rules or groups.
- **Dependent rules** run only when the preceding rule passed.

The happy path is allocation-free after warmup: the collector and root context data are pooled per
thread, and rule selection returns the original rule array when no filters apply.

## Metadata

Every fluent and generated validator implements `IValidationDescriptorProvider`. `Describe()`
returns a frozen `IValidationDescriptor` containing members, rule kinds, message keys, severities,
conditions, and parameters.

Descriptors are the only source for JSON Schema, OpenAPI, and client-rule emission. They contain no
delegates and no reflection objects.

## Two authoring styles, one engine

| Concern | Fluent | Generated |
|---|---|---|
| Declaration | `Validator<T>.Configure` | Attributes on the model |
| Rule order | Source order | Attribute order |
| Custom rules | `AddCustomValidators()` or `Custom` | `AddCustomValidators()` partial hook |
| Descriptor | Built once per validator | Static frozen field |
| AOT | Explicit registration | Generated code |

Use fluent authoring for dynamic or context-heavy rules; use attributes for declarative models and
compile-time validation of the rule configuration.

## Integration packages

Integration packages never change engine semantics. They adapt the host:

- ASP.NET Core maps errors to ProblemDetails.
- MediatR runs validators in a pipeline behavior.
- DataAnnotations runs validators inside `ValidationAttribute` pipelines.
- Blazor maps errors to `FieldIdentifier` values.
- Schema/OpenAPI/client packages consume descriptors.

## Next

- [Fluent authoring](../02-guides/01-fluent-authoring.md)
- [Architecture](../03-reference/00-architecture.md)
