# Jeninnet.Validation.DataAnnotations

Runs `Jeninnet.Validation` validators inside DataAnnotations-based pipelines:
MVC model binding, Blazor's `DataAnnotationsValidator`, and custom
`IValidatableObject` implementations.

## Requirements

- .NET 11.0 or later (the package ships `lib/net11.0` assets).
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.DataAnnotations --version 1.0.0-preview.1
```

## Class-level attribute

Annotate a model class with the validator type:

```csharp
using Jeninnet.Validation.DataAnnotations;

[JeninnetValidatable(typeof(CreateOrderValidator))]
public sealed record CreateOrderRequest(string CustomerName, string Email);
```

The validator is resolved from the validation context's service provider first
(`IValidator<T>` or the concrete type), then created with its parameterless
constructor. Register it when it has dependencies:

```csharp
builder.Services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderValidator>();
```

## Property-level attribute

Run only one member's rules with `[JeninnetValidateProperty]`:

```csharp
public sealed class CreateOrderRequest
{
    [JeninnetValidateProperty(typeof(CreateOrderValidator), nameof(CustomerName))]
    public string CustomerName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}

// Generic syntax (C# 11+):
public sealed class CreateOrderRequest
{
    [JeninnetValidateProperty<CreateOrderValidator>(nameof(CustomerName))]
    public string CustomerName { get; set; } = string.Empty;
}
```

The attribute uses `ValidationCallOptions.IncludeProperties`; it does not copy
or reimplement rule logic. Member paths can be nested or indexed when they match
the validator's rule-group name:

```csharp
[JeninnetValidateProperty(typeof(OrderValidator), "Address.Street")]
public Address? Address { get; set; }
```

When the model also has `[JeninnetValidatable]`, the class-level attribute wins
and the property-level attribute is skipped, so the model is not validated twice.
Unknown member names follow `IncludeProperties` semantics: they produce no errors.
The validator must implement `IValidationOptionsValidator<T>`; otherwise the
adapter throws a clear `InvalidOperationException`.

## Options

| Property | Default | Meaning |
|---|---|---|
| `RuleSets` | empty | Rule sets passed to the validator; empty means all rules. |
| `IncludeSeverity` | `true` | Prefix `Warning`/`Info` messages with `[Warning]`/`[Info]`. |
| `IncludeErrorCodes` | `false` | Append `(code)` to each message. |
| `MessageSeparator` | `"; "` | Joins multiple errors into one DataAnnotations message. |
| `ErrorMessage` | inherited | When set, overrides the generated message. |

The same options are available on the class-level and property-level attributes.

## IValidatableObject

Use the adapter directly when the model implements `IValidatableObject`:

```csharp
public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    var result = JeninnetDataAnnotationsAdapter.Validate(
        this,
        typeof(CreateOrderRequest),
        typeof(CreateOrderValidator),
        validationContext.GetService);

    return JeninnetDataAnnotationsAdapter.ToValidationResults(result);
}
```

For a single member, call `JeninnetDataAnnotationsAdapter.ValidateProperty(...)`.

## Sync-only

DataAnnotations validation is synchronous. Validators with asynchronous rules
throw `AsyncValidatorInvokedSynchronouslyException`; keep async rules on the
MediatR/ASP.NET Core async paths. Reflection invocation unwraps the original
exception instead of leaking `TargetInvocationException`.

## Dependency direction

Only this optional bridge references `System.ComponentModel.DataAnnotations`.
`Jeninnet.Validation` and `Jeninnet.Validation.Abstractions` remain free of it;
the architecture suite guards that direction.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
