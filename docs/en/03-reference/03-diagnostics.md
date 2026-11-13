# Diagnostics

The analyzer and source generator report rule misuse at compile time. All diagnostics use the
`VAL` prefix and the `ValidationGenerator` category.

## Diagnostic reference

| ID | Severity | Title | Code fix |
|---|---|---|---|
| `VAL0001` | Error | Invalid rule parameter | Converts the literal to the expected type (Boolean, DateOnly, DateTime, Decimal, Enum, Guid, TimeSpan). |
| `VAL0002` | Error | Rule applied to incompatible property type | — |
| `VAL0003` | Error | Rule parameter type mismatch | — |
| `VAL0004` | Warning | Nested validator not found | — |
| `VAL0005` | Warning | Collection element validator not found | — |
| `VAL0006` | Warning | Rule cannot be used on nullable type | Adds `[NotNull]` before the numeric rule. |
| `VAL0007` | Error | Conflicting rules on property | Removes the conflicting rule. |
| `VAL0008` | Error | Unsupported attribute argument type | — |
| `VAL0009` | Error | Invalid rule combination | Removes `[NotEmpty]` or changes `[Length(0, …)]` to `[Length(1, …)]`. |
| `VAL0010` | Error | Rule parameter out of range | Swaps reversed `InclusiveBetween` bounds. |
| `VAL0011` | Info | Consider adding `[NotEmpty]` for string property | Adds `[NotEmpty]`. |
| `VAL0012` | Info | Consider adding `[NotNull]` for nullable property | Adds `[NotNull]`. |
| `VAL0013` | Info | Rule can be simplified | — |
| `VAL0014` | Warning | Redundant `[NotNull]` attribute | — |
| `VAL0015` | Error | Target property not found | — |
| `VAL0016` | Warning | Synchronous `Validate` on async-only validator | Rewrites the call to `await ValidateAsync(...)`. |
| `VAL0017` | Info | Tautological validation rule | — |
| `VAL0018` | Error | Invalid condition method | — |
| `VAL0019` | Error | Invalid lifecycle hook | — |
| `VAL0020` | Error | Invalid custom validator | — |
| `VAL0021` | Error | Invalid date/time rule value | — |
| `VAL0022` | Error | Generic model is not supported | — |
| `VAL0023` | Warning | `RuleFor` member name mismatch | Rewrites the argument to `nameof(Model.Member)`. |
| `VAL0024` | Warning | Collection rule requires a collection | — |
| `VAL0025` | Warning | Async-only validator in synchronous adapter | — |
| `VAL0027` | Error | `[Sanitize]` cannot be applied to the member | — |
| `VAL0028` | Warning | `[SkipValidation]` conflicts with validation attributes | — |
| `VAL0029` | Error | Conditional attribute has an invalid target | — |
| `VAL0030` | Error | Conditional attribute value is not supported | — |
| `VAL0031` | Error | Conditional attribute is duplicated | — |
| `VAL0032` | Error | Relative date rule requires a date/time member | — |
| `VAL0033` | Error | Exclusive range has equal bounds | — |
| `VAL0034` | Error | Invalid multiple-of divisor | — |

The catalog has 33 diagnostics; `VAL0026` is intentionally unused.

The generator emits `[AsyncOnlyValidator]` when every attribute-driven rule is asynchronous. The
runtime guard throws for async-only validators even when the marker is absent, so `VAL0016` is a
compile-time aid rather than the only protection.

## Common fixes

### `VAL0001` — invalid rule parameter

```csharp
[Before("2026-01-01")]          // string is converted to the member type
public DateOnly ExpiresOn { get; init; }
```

If the value cannot be parsed, the code fix rewrites it to the expected literal type. Prefer
`DateOnly`/`DateTime` members when the rule is date-based.

### `VAL0006` — rule on a nullable type

```csharp
[GreaterThan(0)]                // VAL0006 when the property is int?
public int? Quantity { get; init; }
```

Either make the property non-nullable or add `[NotNull]` before the numeric rule. The code fix
adds the attribute for you.

### `VAL0009` — conflicting rules

```csharp
[NotEmpty, Length(0, 100)]      // NotEmpty implies a minimum length of 1
public string Name { get; set; } = string.Empty;
```

Use `[Length(1, 100)]`, or remove `[NotEmpty]` when the length rule is sufficient.

### `VAL0010` — reversed bounds

```csharp
[InclusiveBetween(100, 0)]      // min > max
public int Score { get; init; }
```

Swap the bounds; the code fix does it automatically.

### `VAL0015` — target property not found

```csharp
[PropertyEqual("Password")]     // no Password member on the model
public string ConfirmPassword { get; init; }
```

The comparison attribute takes a property name, not an expression. Use `nameof(...)` in fluent code
and verify the member exists on the same type.

### `VAL0016` — sync call on an async-only validator

```csharp
var result = validator.Validate(request);          // VAL0016
var result = await validator.ValidateAsync(request); // correct
```

Async-only validators throw `AsyncValidatorInvokedSynchronouslyException` at runtime. Use the async
path or add a synchronous rule.

### `VAL0018` — invalid condition method

```csharp
[WhenCondition("IsActive")]     // method is missing or has the wrong signature
public string? Reason { get; init; }
```

The method must be accessible and return `bool` with `(T instance)` or
`(T instance, ValidationContext<T> context)`. Both instance and static methods are supported. Rename
the method or change its signature; unsupported overloads are not selected implicitly.

### `VAL0019` — invalid lifecycle hook

```csharp
[PreValidate("BeforeValidate")] // must be static and return bool
public sealed class Order { /* ... */ }
```

`PreValidate` accepts `(T, ValidationContext<T>)` or
`(T, ValidationContext<T>, ValidationErrorCollector)` and must return `bool`. `OnFailure` accepts
`(ValidationResult)` and must return `void`. Both hooks must be static.

### `VAL0020` — invalid custom validator

```csharp
[CustomValidator(typeof(Rules), "Validate")] // method is missing or has the wrong signature
public string? Name { get; init; }
```

The method must be static and match one of the supported signatures. Synchronous rules return
`bool` or `ValidationResult` and accept `(TValue value)` or `(TValue value, IServiceProvider
services)`. Async rules set `IsAsync = true`, return `ValueTask<bool>` or
`ValueTask<ValidationResult>`, and end with a `CancellationToken` parameter. Rename the method or
change its signature; unsupported overloads are not selected implicitly.

### `VAL0021` — invalid date/time rule value

```csharp
[After("not-a-date")]
public DateOnly StartDate { get; init; } // VAL0021
```

The value must parse under `CultureInfo.InvariantCulture` as the property type (`DateTime`,
`DateTimeOffset`, `DateOnly`, or `TimeOnly`). The generator never substitutes `DateTime.Now` for an
invalid value. Fix the literal or the property type.

### `VAL0022` — generic model is not supported

```csharp
[Validate]
public sealed class Box<T> { /* ... */ } // VAL0022
```

The source generator emits a closed, non-generic validator class. Use a non-generic model or a
fluent validator for generic types.

### `VAL0023` — RuleFor member name mismatch

```csharp
builder.RuleFor(x => x.Name, "Email"); // VAL0023 when Email is another member
```

Pass the member name selected by the expression. The analyzer only reports provable mismatches: the
name must be a compile-time constant and must match a real property or field on the model. Custom
paths such as `"Contact.Email"` and free-form names are left alone. The code fix rewrites the
argument to `nameof(Model.Name)`.

### `VAL0024` — collection rule on a non-collection

```csharp
builder.RuleFor(x => x.Name, nameof(Model.Name)).MinCount(1); // VAL0024
[Count(1, 5)] public int CountValue { get; set; }              // VAL0024
```

`Count`, `MinCount`, and `MaxCount` are cardinality rules; apply them to arrays or
`IEnumerable<T>` members, not strings or scalar values. The analyzer stays silent for `object`,
`dynamic`, and unconstrained type parameters because their runtime value can be a collection.

### `VAL0025` — async-only validator in a synchronous adapter

```csharp
[AsyncOnlyValidator]
public sealed class AsyncValidator : Validator<Order> { /* async-only rules */ }

[JeninnetValidatable(typeof(AsyncValidator))] // VAL0025
public sealed class Order { /* ... */ }
```

The DataAnnotations adapter calls the synchronous `Validate` API, which throws
`AsyncValidatorInvokedSynchronouslyException` for an async-only validator. Use an async integration
path or add a synchronous rule. The analyzer proves this only where the validator type is part of
the attribute (`[JeninnetValidatable]` and `[JeninnetValidateProperty]`); DI/Blazor/Observable
paths remain protected by `VAL0016` and the runtime guard.

### `VAL0027` — `[Sanitize]` cannot be applied to the member

```csharp
public sealed class Order
{
    [Sanitize(Trim = true)]
    public string Code { get; init; } // VAL0027: init-only setter
}
```

Sanitization writes the cleaned value back in place, so the member needs an accessible non-init
setter and must be a `string` or `IList<string>` member. An unknown `Culture` name fails the same
way. Give the member a setter, sanitize a writable copy, or remove the attribute. The fluent
equivalent (`.Sanitize()` without a setter) throws `InvalidOperationException` at configuration
time instead.

### `VAL0028` — `[SkipValidation]` combined with validation attributes

```csharp
[Validate]
public sealed class Order
{
    [SkipValidation, NotEmpty]     // VAL0028: intent is ambiguous
    public string? Code { get; set; }
}
```

`[SkipValidation]` drops every rule for the property, so the accompanying attributes never run.
Remove the validation attributes to opt out deliberately, or remove `[SkipValidation]` to keep
them. The warning does not change the outcome: the property is still excluded from the generated
validator.

### `VAL0029` — invalid conditional target

```csharp
[Validate]
public sealed class Order
{
    [RequiredIf("Missing", 1)]          // VAL0029: member not found
    public string? Code { get; init; }

    [RequiredIf(nameof(Code), 1)]       // VAL0029: self-reference
    public string? Code2 { get; init; }
}
```

`[RequiredIf]`, `[RequiredUnless]`, and `[ConditionalRange]` take a simple member name on the same
model. The member must exist and have a getter the generated validator can read (public, internal,
or protected-internal; not static). Use `nameof(...)` and a readable sibling member.

### `VAL0030` — unsupported conditional value

```csharp
public bool IsActive { get; set; }

[RequiredIf(nameof(IsActive), "yes")]   // VAL0030: Boolean member, string value
public string? Reason { get; set; }
```

The constant must match the sibling member's type: string for strings, Boolean for Booleans,
numeric for numeric types, enum or numeric for enums, and a parseable string for date/time,
`Guid`, and `TimeSpan`. Integral members reject fractional constants. Attribute arguments are
compile-time constants by C# rules, so a non-constant expression fails at the language level.

### `VAL0031` — duplicate conditional attribute

```csharp
[RequiredIf(nameof(IsActive), true)]
[RequiredIf(nameof(IsActive), false)]   // VAL0031: same attribute, same target
public string? Reason { get; set; }
```

The same conditional attribute type may target a given member only once. Combine the conditions
into one attribute with the intended value, or use different targets. Different conditional
attribute types may target the same member; their conditions are combined with AND.

### `VAL0032` — relative date on an unsupported member

```csharp
[FutureDate]
public string? When { get; init; } // VAL0032: string is not a date/time type
```

`[FutureDate]` and `[PastDate]` support `DateTime`, `DateTimeOffset`, and `DateOnly` (including
nullable variants). Change the member type or use a custom/fluent rule for other types. `TimeOnly`
is not supported because it has no date component to compare with the current instant.

### `VAL0033` — exclusive range with equal bounds

```csharp
[ExclusiveBetween(5, 5)]        // VAL0033: an exclusive range can never match
public int Score { get; init; }
```

An exclusive range requires `min < max`. Widen one of the bounds, or use `[InclusiveBetween]` when
a single value is the intended range. Reversed bounds are reported as `VAL0010` instead; the
generator rejects both before the generated validator can run. The fluent
`ExclusiveBetween(5, 5)` call throws `ArgumentException` at configuration time for the same reason.

## Suppression

Suppress at the narrowest scope and explain why:

```csharp
#pragma warning disable VAL0016 // Sync path is guarded by an integration adapter.
var result = validator.Validate(request);
#pragma warning restore VAL0016
```

Prefer fixing the call site. Suppressing `VAL0002`, `VAL0007`, or `VAL0009` usually hides a real
configuration error.

## Treat warnings as errors

The repository builds with warnings as errors. If your application does the same, `VAL0004`,
`VAL0005`, `VAL0006`, `VAL0014`, `VAL0016`, `VAL0023`, `VAL0024`, `VAL0025`, `VAL0027`, and
`VAL0028` become build blockers. Fix them rather than lowering the severity; each one points at a
rule that would behave differently at runtime. ### `VAL0034` — invalid multiple-of divisor

```csharp
[MultipleOf(0)]               // VAL0034: divisor must be positive and finite
public decimal Amount { get; init; }
```

`MultipleOf` accepts a positive, finite decimal divisor. Zero, negative, `NaN`, and infinite
literals are rejected at build time; the same rule applies to the fluent overload.

`VAL0029`–`VAL0034` are errors and always block the build.

## Related

- [Attribute authoring](../02-guides/02-attribute-authoring.md)
- [Rule reference](02-rule-reference.md)
- [Known limitations](07-known-limitations.md)
