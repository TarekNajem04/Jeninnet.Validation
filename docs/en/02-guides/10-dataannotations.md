# DataAnnotations

The `Jeninnet.Validation.DataAnnotations` package runs Jeninnet validators inside DataAnnotations
pipelines: MVC model binding, Blazor’s `DataAnnotationsValidator`, and custom
`IValidatableObject` implementations.

## Class-level attribute

```csharp
using Jeninnet.Validation.DataAnnotations;

[JeninnetValidatable(typeof(CreateOrderValidator))]
public sealed class CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

The validator is resolved from the validation context’s service provider first (`IValidator<T>` or
the concrete type), then created with a parameterless constructor. Register validators with
dependencies:

```csharp
builder.Services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderValidator>();
```

If resolution fails, the adapter throws an actionable `InvalidOperationException` instead of
silently skipping validation.

## Property-level attribute

Use `[JeninnetValidateProperty]` when only one member should be validated, for example during
partial updates or property-level MVC/Blazor validation:

```csharp
public sealed class CreateOrderRequest
{
    [JeninnetValidateProperty(typeof(CreateOrderValidator), nameof(CustomerName))]
    public string CustomerName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
```

C# 11+ also supports the generic form:

```csharp
[JeninnetValidateProperty<CreateOrderValidator>(nameof(CustomerName))]
public string CustomerName { get; set; } = string.Empty;
```

The attribute uses `ValidationCallOptions.IncludeProperties` to run only the named rule group; it
does not copy rule logic. Nested and indexed member paths work when they match the validator's
rule-group name:

```csharp
[JeninnetValidateProperty(typeof(OrderValidator), "Address.Street")]
public Address? Address { get; set; }
```

If the model also has `[JeninnetValidatable]`, the class-level attribute wins and the property-level
attribute is skipped to avoid double validation. Unknown member names follow `IncludeProperties`
semantics and produce no errors. A custom validator that does not implement
`IValidationOptionsValidator<T>` cannot be used for property-level validation and throws a clear
`InvalidOperationException`. DataAnnotations may skip property-level attributes on null reference
values; initialize the property or use class-level validation when the member can be null.

## Skip validation

`[SkipValidation]` is honored by both bridge attributes. A skipped model type bypasses
`[JeninnetValidatable]` and `[JeninnetValidateProperty]` entirely, and a skipped member bypasses
`[JeninnetValidateProperty]` even when the attribute remains on the property:

```csharp
[JeninnetValidatable(typeof(CreateOrderValidator))]
[SkipValidation]
public sealed class CreateOrderRequest { /* ... */ }

public sealed class UpdateOrderRequest
{
    [SkipValidation]
    [JeninnetValidateProperty(typeof(UpdateOrderValidator), nameof(CustomerName))]
    public string CustomerName { get; set; } = string.Empty;
}
```

The adapter methods (`JeninnetDataAnnotationsAdapter.Validate` and `ValidateProperty`) observe the
same marker on the model type, so a direct `IValidatableObject` call also returns a successful
result. The skip is silent by design; it does not throw or add a DataAnnotations error.

## Options

| Property | Default | Meaning |
|---|---|---|
| `RuleSets` | empty | Rule sets passed to the validator. |
| `IncludeSeverity` | `true` | Prefix `Warning`/`Info` messages. |
| `IncludeErrorCodes` | `false` | Append `(code)` to messages. |
| `MessageSeparator` | `"; "` | Joins multiple errors into one message. |
| `ErrorMessage` | inherited | Overrides the generated message. |

The options are available on both `[JeninnetValidatable]` and `[JeninnetValidateProperty]`.

## `IValidatableObject`

```csharp
public sealed class Order : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var result = JeninnetDataAnnotationsAdapter.Validate(
            this,
            typeof(Order),
            typeof(OrderValidator),
            validationContext.GetService);

        return JeninnetDataAnnotationsAdapter.ToValidationResults(result);
    }
}
```

## Sync-only

DataAnnotations validation is synchronous. Validators with asynchronous rules throw
`AsyncValidatorInvokedSynchronouslyException`; keep async rules on the ASP.NET Core or MediatR
async paths. Property-level validation uses the same synchronous path and unwraps reflective
invocation failures to the original exception.

## Dependency direction

Only this optional bridge references `System.ComponentModel.DataAnnotations`. Core and Abstractions
remain free of it; the architecture suite guards that direction.

## Related

- [Blazor](11-blazor.md)
- [Integration packages](../03-reference/05-integration-packages.md)
