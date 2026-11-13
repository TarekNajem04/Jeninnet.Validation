# Attribute Authoring

Attribute authoring declares rules on the model and lets the Roslyn source generator emit the
validator.

## Enable the generator

```xml
<PackageReference Include="Jeninnet.Validation.Analyzers" Version="1.0.0-preview.1"
                  PrivateAssets="all" />
```

## Annotate the model

```csharp
using Jeninnet.Validation.Attributes;

[Validate]
public sealed class Customer
{
    [NotEmpty, Length(1, 100)]
    public string Name { get; init; } = string.Empty;

    [NotEmpty, Email]
    public string Email { get; init; } = string.Empty;

    [InclusiveBetween(18, 120)]
    public int Age { get; init; }
}
```

The generator emits `CustomerValidator` in the model’s `Validators` namespace. `[Validate]` accepts an
optional target namespace; without it, the namespace is `<model namespace>.Validators`.

The generated class:

- implements `IValidator<Customer>`;
- implements `IValidationDescriptorProvider` with a per-instance descriptor built from the runtime
  rules;
- has a parameterless constructor when no nested validators are injected;
- invokes `AddCustomValidators()` before building the composite.

`[Validate]` also works on `struct` and `record struct` models:

```csharp
[Validate]
public struct Money
{
    [GreaterThanOrEqual(0)]
    public decimal Amount { get; set; }
}
```

Generic model declarations are rejected with `VAL0022`; use a closed, non-generic model or a fluent
validator instead. This keeps generated type names and DI registrations deterministic.

## Inheritance

When a derived class is annotated with `[Validate]`, the generator includes the rules declared on
its base classes before the derived rules:

```csharp
[Validate]
public class Contact
{
    [NotEmpty] public string? Name { get; set; }
}

[Validate]
public sealed class Person : Contact
{
    [NotEmpty] public string? Email { get; set; }
}
```

`PersonValidator` validates both `Name` and `Email`. A base type annotated with `[Validate]` does
not automatically generate validators for its derived types; annotate each type that needs a
validator. Annotated interfaces contribute their matching property rules to annotated implementing
classes.

For runtime-type dispatch and polymorphic collections, see
[Inheritance and polymorphism](20-inheritance-and-polymorphism.md).

## Rule metadata

Validation attributes expose optional named properties that flow to runtime errors and descriptors:

```csharp
using System.ComponentModel.DataAnnotations;
using Jeninnet.Validation;

[Validate]
public sealed class Customer
{
    [Email(
        ErrorCode = "email_invalid",
        Severity = ValidationErrorSeverity.Warning,
        DisplayName = "Email address",
        CustomState = "signup")]
    public string? Email { get; init; }

    [Display(Name = "Customer name")]
    [NotEmpty]
    public string? Name { get; init; }

    [NotEmpty(OverridePropertyName = "renamed_name")]
    public string? RenamedName { get; init; }
}
```

- `ErrorCode`, `Severity`, `DisplayName`, `OverridePropertyName`, and `CustomState` are available on
  every validation attribute because they live on `ValidationAttribute`.
- `CustomState` is a string in attributes because attribute arguments must be compile-time
  constants. Use the fluent `WithState` for arbitrary objects.
- `[Display(Name = ...)]` from `System.ComponentModel.DataAnnotations` is bridged to
  `{PropertyName}`; a rule-level `WithName`/`OverridePropertyName` or a configured
  `DisplayNameResolver` wins over `[Display]`.
- `OverridePropertyName` changes `ValidationError.MemberName`; `DisplayName` changes only the
  `{PropertyName}` parameter.
- The same values appear in `IValidationRuleDescriptor`, client rules, and `${"x-validation-rules"}`
  schema advisory entries (`x-validation-rules`).

## Use the generated validator

```csharp
var validator = new CustomerValidator();
var result = validator.Validate(customer);
```

Register it explicitly (the AOT-safe pattern) or discover it through assembly scanning:

```csharp
services.AddScoped<IValidator<Customer>, CustomerValidator>();

// Reflection-based discovery; not AOT-safe:
services.AddGeneratedValidators(typeof(CustomerValidator).Assembly);
```

## Model-level validation shortcuts

By default the generator also emits a `{Type}ValidationExtensions` class in the model namespace (or
the `TargetNamespace` when one is set). It creates one static validator instance and exposes the
same contract through extension methods:

```csharp
var result = customer.Validate();
var filtered = customer.Validate(new ValidationCallOptions { IncludeProperties = ["Name"] });
var asyncResult = await customer.ValidateAsync(cancellationToken);
var asyncFiltered = await customer.ValidateAsync(
    new ValidationCallOptions { FailFast = true },
    cancellationToken);
```

The extensions work for classes, structs, and record structs. They do not replace the DI path:
`IValidator<T>` remains the official integration contract, and the extensions hold no service
provider. Validators that require constructor injection (for example nested validators) keep the DI
path and do not get the shortcut. If the model already declares an instance `Validate` member,
normal C# overload resolution prefers the instance member. Disable the feature per model with
`[Validate(GenerateModelExtensions = false)]`.

## Custom rules in a generated validator

Attributes cannot express every predicate. The generator emits a `partial void AddCustomValidators()`
hook that runs before the composite validator is created. Add rules directly to the generated
`_syncRules` or `_asyncRules` lists:

```csharp
using Jeninnet.Validation.Core;
using Jeninnet.Validation.Rules;

namespace MyApp.Models.Validators;   // <model namespace>.Validators

public sealed partial class CustomerValidator
{
    partial void AddCustomValidators()
    {
        _syncRules.Add(
            new MustRule<Customer, string?>(
                ValidatorFactory.Accessor<Customer, string?>(
                    static x => x.Name,
                    nameof(Customer.Name)),
                static name => name != "admin",
                "This name is reserved."));
    }
}
```

The hook has no builder parameter; it is a partial method on the generated class, so it can access
the generated rule lists. Rules added this way execute normally and are described by `Describe()`:
built-in rules and rules derived from `SyncValidateRule`/`AsyncValidateRule` expose their own
metadata. A rule that cannot do so appears as a `Custom` advisory entry with a reason and type.

To add explicit metadata for a custom rule, implement the generated `ConfigureDescriptor` hook:

```csharp
using Jeninnet.Validation.Metadata;

public sealed partial class CustomerValidator
{
    partial void ConfigureDescriptor(ValidationDescriptorBuilder builder)
    {
        builder.Member(nameof(Customer.Name)).Rule(
            kind: "ReservedName",
            messageKey: "ReservedName",
            severity: ValidationErrorSeverity.Warning,
            parameters: new Dictionary<string, object?> { ["Value"] = "admin" },
            errorCode: "reserved_name",
            displayName: "Customer name");
    }
}
```

The descriptor hook adds data only. It does not add executable rules, and it cannot expose
delegates. The build-time client-rules catalog still reflects attributes only; use
`ClientRulesWriter` at runtime when custom descriptor entries must reach the client. For
context-heavy predicates, fluent `Custom`/`CustomAsync` rules remain the better choice.

## Rule sets in attributes

Apply `[RuleSet("Admin")]` to a property or the model to place generated rules in a named set:

```csharp
[Validate]
[RuleSet("Admin")]
public sealed class Customer
{
    [NotEmpty]
    public string Name { get; init; } = string.Empty;

    [InclusiveBetween(0, 100)]
    public int InternalScore { get; init; }
}
```

A property-level `[RuleSet]` wins over the class-level attribute. A rule-level named property wins
over both and applies to one rule only:

```csharp
[NotEmpty(RuleSet = "Admin")]
public string? Code { get; init; }
```

Selecting `["Admin"]` runs only that set; add `"default"` to include ungrouped rules. When no rule
sets are supplied, all rules run, including rules in named sets.

## Conditions in attributes

`[WhenCondition]` and `[UnlessCondition]` gate generated rules on a synchronous method that must exist
on the containing type:

```csharp
[Validate]
[WhenCondition(nameof(IsEditable))]
public sealed class Order
{
    [WhenCondition(nameof(IsReasonRequired))]
    public string? Reason { get; init; }

    public bool IsEditable(Order instance) => instance.Reason != "cancelled";

    public bool IsReasonRequired(Order instance) => instance.Reason != "draft";
}
```

The method must return `bool` and accept either `(T instance)` or
`(T instance, ValidationContext<T> context)`. It may be an instance or static method. A property-level
condition wins over a class-level condition. Synchronous attribute conditions run on both `Validate`
and `ValidateAsync`.

`[WhenAsyncCondition]` and `[UnlessAsyncCondition]` gate rules on a method with the signature
`Task<bool> MethodName(T instance, CancellationToken ct)`. The property-level condition wins over a
class-level condition. These rules run only on the async path; use `ValidateAsync`.

### Value conditions

Three shortcuts cover the common "required or in range depending on another field" scenarios
without writing a condition method:

```csharp
[Validate]
public sealed class Order
{
    public string? Mode { get; set; }
    public bool IsGift { get; set; }

    [RequiredIf(nameof(Mode), "Express")]
    public string? Courier { get; set; }

    [RequiredUnless(nameof(Mode), "Express")]
    public string? PickupPoint { get; set; }

    [ConditionalRange(nameof(Mode), "Business", 1, 10)]
    public int Quantity { get; set; }

    [RequiredIf(nameof(IsGift), true)]
    public string? GiftMessage { get; set; }
}
```

- `[RequiredIf(other, value)]` requires the member when `other` equals `value`.
- `[RequiredUnless(other, value)]` requires the member when `other` does not equal `value`.
- `[ConditionalRange(other, value, min, max)]` applies an inclusive range when `other` equals `value`.

A string or collection member is checked for emptiness; any other member is checked for null. The
comparison supports strings, Booleans, characters, all numeric types, enums, date/time types, `Guid`,
and `TimeSpan`. Numeric constants are compared numerically, so an `int` member matches a `long`
constant such as `1L`. Date/time members accept parseable strings. The value must be a compile-time
constant.

When the property also carries `[WhenCondition]`/`[UnlessCondition]` (or the model carries a
class-level condition), both conditions must pass. `[RequiredIf]` and `[RequiredUnless]` can be
combined with other rules, but each attribute targets a simple member name on the same model; nested
paths and multiple values in one attribute are not supported. Invalid targets, unsupported values,
and duplicate attributes are reported at compile time as `VAL0029`–`VAL0031`.

These rules cannot be expressed in JSON Schema or client-side validation; generated descriptors mark
them as conditional, JSON Schema keeps them as advisory, and the client-rules export sets
`serverOnly: true`. See [Known limitations](../03-reference/07-known-limitations.md).

### Relative dates

`[FutureDate]` and `[PastDate]` validate against the current UTC time instead of a fixed threshold:

```csharp
public sealed class Subscription
{
    [FutureDate]
    public DateTime StartsAt { get; set; }

    [FutureDate(IncludeToday = true)]
    public DateTimeOffset TrialEndsAt { get; set; }

    [PastDate]
    public DateOnly? CancelledOn { get; set; }
}
```

`IncludeToday` defaults to `false`, so the value must be strictly later (or earlier) than the
current instant; set it to `true` to allow equality. The current time comes from a `TimeProvider`
resolved through `ValidationCallOptions.ServiceProvider`, falling back to `TimeProvider.System`.
Comparisons are UTC and never convert to local time, so inject a fixed provider in tests. Supported
types are `DateTime`, `DateTimeOffset`, and `DateOnly` (and nullable variants); `TimeOnly` and other
types produce `VAL0032`. Null passes; add `[NotNull]` when presence is required. Relative-date rules
are server-only and remain advisory in JSON Schema.

## Lifecycle hooks in attributes

`[PreValidate]` and `[OnFailure]` bind static methods on the model to the generated validator:

```csharp
using Jeninnet.Validation;
using Jeninnet.Validation.Attributes;
using Jeninnet.Validation.Core;

[Validate]
[PreValidate(nameof(BeforeValidate))]
[OnFailure(nameof(AfterFailure))]
public sealed class Order
{
    [NotEmpty] public string? CustomerName { get; set; }

    public static bool BeforeValidate(
        Order instance,
        ValidationContext<Order> context,
        ValidationErrorCollector errors) {
        if (instance.CustomerName == "blocked") {
            errors.Add(nameof(CustomerName), "blocked");
            return false;
        }

        return true;
    }

    public static void AfterFailure(ValidationResult result) {
        // audit or metrics; runs once when the merged result is invalid
    }
}
```

- `PreValidate` is static and returns `bool`; it accepts `(T, ValidationContext<T>)` or
  `(T, ValidationContext<T>, ValidationErrorCollector)`.
- Errors added through the collector are merged with rule errors. Returning `false` stops rule
  evaluation.
- `OnFailure` is static `void(ValidationResult)` and runs once after a failed result is known.
- Declarative hooks have no async variants; use fluent `PreValidateAsync`/`OnFailureAsync` when async
  work is required.

## Custom validators in attributes

`[CustomValidator]` delegates to a static method on a rules type:

```csharp
using System.Threading;
using System.Threading.Tasks;
using Jeninnet.Validation;
using Jeninnet.Validation.Attributes;

public static class OrderRules
{
    public static bool IsReserved(string? value) => value != "admin";

    public static ValidationResult CheckCode(string? value)
    {
        var errors = new ValidationErrorCollector();

        if (value == "bad")
        {
            errors.Add(string.Empty, "The code is not acceptable.");
        }

        return ValidationResult.FromErrors(errors);
    }

    public static ValueTask<bool> IsAvailableAsync(
        string? value,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(value != "taken");
}

[Validate]
public sealed class Order
{
    [CustomValidator(typeof(OrderRules), nameof(OrderRules.IsReserved))]
    public string? CustomerName { get; init; }

    [CustomValidator(typeof(OrderRules), nameof(OrderRules.CheckCode))]
    public string? Code { get; init; }

    [CustomValidator(typeof(OrderRules), nameof(OrderRules.IsAvailableAsync), IsAsync = true)]
    public string? Reference { get; init; }
}
```

Supported synchronous signatures:

- `bool Method(TValue value)`
- `bool Method(TValue value, IServiceProvider services)`
- `ValidationResult Method(TValue value)`
- `ValidationResult Method(TValue value, IServiceProvider services)`

Asynchronous rules add `CancellationToken` as the final parameter and return
`ValueTask<bool>` or `ValueTask<ValidationResult>`:

- `ValueTask<bool> Method(TValue value, CancellationToken cancellationToken)`
- `ValueTask<bool> Method(TValue value, IServiceProvider services, CancellationToken cancellationToken)`
- `ValueTask<ValidationResult> Method(TValue value, CancellationToken cancellationToken)`
- `ValueTask<ValidationResult> Method(TValue value, IServiceProvider services, CancellationToken cancellationToken)`

- The method must be static; generic methods and `Task`-returning methods are rejected.
- A `bool` false result adds one error using the attribute message (or the `Must`/`MustAsync`
  message key).
- `ValidationResult` errors with an empty member name are attributed to the property; explicit paths
  are preserved.
- `IServiceProvider` comes from `ValidationContext<T>.ServiceProvider` (supply it through
  `ValidationCallOptions`); it can be `null` when no provider was configured.
- A class-level `[CustomValidator]` receives the whole model instance and reports a root error.
- Async custom rules run only on `ValidateAsync`. A validator whose rules are all async is marked
  `[AsyncOnlyValidator]` and throws on synchronous `Validate`.
- Invalid signatures produce `VAL0020` at compile time.

## Rule order

Rules run in attribute order within a member. Keep presence rules before shape/comparison rules when
using `CascadeMode.Stop` or fail-fast.

## Sanitization in attributes

`[Sanitize]` is preprocessing, not a rule. It runs before every rule on every `Validate` call and
never appears in descriptors:

```csharp
public sealed class Customer
{
    [Sanitize(Trim = true)]
    [NotEmpty]
    public string? Name { get; set; }

    [Sanitize(ToUpper = true, Culture = "tr-TR")]
    public string? Code { get; set; }
}
```

A class-level `[Sanitize]` applies to every writable `string`/`IList<string>` member without its
own `[Sanitize]`; a property-level attribute wins for that property. Members without an
accessible non-init setter, non-string members, and unknown cultures fail the build with
`VAL0027`. See [Rules and messages](03-rules-and-messages.md) for the fluent equivalent.

## Skip validation

`[SkipValidation]` removes a member or a model type from generated validation. It is a marker, not
a rule: a skipped property produces no rules even when it carries other validation attributes, and
a skipped type produces no generated validator and is not validated when reached through `[Nested]`
or `[ForEach]`.

```csharp
[Validate("Shop")]
public sealed class Product
{
    [NotEmpty]
    public string? Name { get; set; }

    [SkipValidation]
    public string? LegacyCode { get; set; }   // no rules are generated

    [Nested]
    public AuditedBy? Auditor { get; set; }   // no nested validation when AuditedBy is skipped
}

[SkipValidation]
public sealed class AuditedBy
{
    [NotEmpty]
    public string? Name { get; set; }
}
```

Combining `[SkipValidation]` with validation attributes on the same property is almost always a
mistake; the generator reports `VAL0028` and still drops every rule for that property. Use the
marker to opt out of generated validation while keeping existing annotations, for example when an
external platform attribute should be the only contract. The marker does not change fluent
validators or the `ValidationStrategy` runtime option: a manually configured validator still runs
unless the caller filters properties. On a minimal API handler parameter or an MVC action
parameter, `[SkipValidation]` skips request validation for that parameter.
`AddRequestValidation<T>()` and `AddMvcValidation()` honor the marker.

## Verify

```csharp
var descriptor = ((IValidationDescriptorProvider)new CustomerValidator()).Describe();
```

The generated descriptor is static, so repeated `Describe()` calls allocate nothing.

## Related

- [Fluent authoring](01-fluent-authoring.md)
- [Metadata](../03-reference/04-metadata.md)
- [Diagnostics](../03-reference/03-diagnostics.md)
