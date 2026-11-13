# Custom Rules

Use custom rules when the built-in surface cannot express the predicate.

> **Design note.** Built-in rule classes are sealed on purpose. You extend the model through
> `Must`/`Custom`, `RuleTemplate`, the abstract rule bases, or the rule interfaces — never by
> inheriting a concrete built-in. See
> [Why built-in rules are sealed](#why-built-in-rules-are-sealed).

## `Must` / `MustAsync`

```csharp
builder.RuleFor(x => x.Username, nameof(User.Username))
       .Must(name => !ReservedNames.Contains(name))
       .WithMessage("This username is reserved.");

builder.RuleFor(x => x.Email, nameof(User.Email))
       .MustAsync(async (email, cancellationToken) =>
           await IsEmailAvailableAsync(email, cancellationToken));
```

`MustAsync` makes the rule async-only. Use `ValidateAsync`.

## `Custom` / `CustomAsync`

`Custom` receives the value, the validation context, and the error collector. It can emit multiple
errors with distinct messages, keys, codes, and severities.

```csharp
builder.RuleFor(x => x.Password, nameof(User.Password))
       .Custom((password, context, errors) =>
       {
           if (password?.Any(char.IsDigit) is not true)
           {
               errors.Add(
                   context.GetFullPath(nameof(User.Password)),
                   new MessageId("Password"),
                   "Password",
                   null,
                   "Password needs a digit.",
                   "PWD_DIGIT",
                   ValidationErrorSeverity.Error);
           }

           if (password?.Any(char.IsUpper) is not true)
           {
               errors.Add(
                   context.GetFullPath(nameof(User.Password)),
                   new MessageId("Password"),
                   "Password",
                   null,
                   "Password needs an uppercase letter.",
                   "PWD_UPPER",
                   ValidationErrorSeverity.Error);
           }
       });
```

- `context.Root` is the root instance of the walk.
- `context.RootData` is a pooled per-walk bag for memoized values.
- `context.ServiceProvider` is supplied by the caller through `ValidationCallOptions`.
- `context.GetFullPath(...)` builds the correct nested path.
- An exception thrown by the delegate becomes a `MessageId.Invalid` failure with code
  `CustomValidationException`.

## Property path chains

`ValidationPath` and `PropertyChain` help advanced rules reason about nested member paths without
changing the textual `MemberPath` carried by errors.

```csharp
var chain = PropertyChain.FromPath(context.MemberPath);
var parents = chain.GetParentChain();   // e.g. ["Customer.Address", "Customer"]
var nested = chain.IsChildChainOf(PropertyChain.FromPath("Customer"));
var child = chain.Add("Street");        // e.g. "Customer.Address.Street"
```

- `ValidationPath.GetParentChain("A.B[0].C")` returns `A.B[0]`, `A.B`, and `A` in that order.
- `PropertyChain.FromPath` trims the input and treats null or whitespace as an empty chain.
- Comparisons are ordinal and only `.` and `[` count as boundaries, so `A` is not a parent of
  `AB.C`.
- `PropertyChain` is a readonly record struct; `Add` appends a member with a `.` separator.

## Async custom rules

```csharp
builder.RuleFor(x => x.Code, nameof(Order.Code))
       .CustomAsync(async (code, context, errors, cancellationToken) =>
       {
           if (!await IsUniqueAsync(code, cancellationToken))
           {
               errors.Add(/* ... */);
           }
       });
```

`CustomAsync` runs only on the async path and honors cancellation.

## Why built-in rules are sealed

The concrete built-in rules that consumers use directly — `NotEmptyRule<T, TValue>`, `EmailRule<T>`,
`InclusiveBetweenRule<T, TValue>`, `RangeRule<T, TProperty>`, and the rest — are `sealed`. The
abstract bases, the interfaces, and the two-arity string forms described below are the deliberate
exceptions. This is a deliberate contract:

- **Performance.** The engine executes rules through `ISyncValidationRule<T>` and
  `IAsyncValidationRule<T>` arrays. A sealed leaf lets the JIT devirtualize and inline the call; if
  every rule were open, each execution would pay a virtual dispatch that the nanosecond-scale hot
  path is designed to avoid.
- **Invariants.** Rule constructors validate their inputs (`RangeBounds.EnsureOrdered`,
  scale/precision checks) or precompute immutable state (compiled regexes, comparison delegates). A
  subclass of a concrete rule could bypass those guarantees while still presenting itself to the
  engine and to metadata as the built-in rule.
- **Truthful metadata.** Descriptors, JSON Schema, OpenAPI, and client-rule emission recognize
  built-in rule kinds. A subclass of `EmailRule<T>` would still be reported as `email` even if its
  predicate changed. Custom rules are exported as advisory entries instead, and the server remains
  authoritative.
- **API evolution.** Removing `sealed` later is source-compatible; adding it after consumers inherit
  is breaking. Starting sealed keeps the option open.

Sealing a leaf does not seal the model. The library opens the *abstractions* and keeps the *concrete
rules* stable — the same template-method split used by extensible frameworks: an open abstract base
defines the contract, and the concrete implementations stay closed.

### Choose the right extension point

| You need | Use | Inherit? |
| --- | --- | --- |
| Change the message, code, severity, name, or state of one rule | `WithMessage`, `WithCode`, `WithSeverity`, `OverridePropertyName`, `WithState`, `OnRuleAdded` | No |
| A one-off predicate with one error | `Must` / `MustAsync` | No |
| A one-off predicate that emits several errors | `Custom` / `CustomAsync` | No |
| A reusable predicate with message and parameters | `RuleTemplate<TValue>` / `RuleTemplate<T, TValue>` | No |
| A reusable synchronous rule with accessor and standard metadata | `SyncValidateRule<T, TProperty>` | Yes — abstract base |
| A reusable asynchronous rule with accessor and standard metadata | `AsyncValidateRule<T, TProperty>` | Yes — abstract base |
| Full control over the member path and error construction | `ISyncValidationRule<T>` / `IAsyncValidationRule<T>` | Yes — interface |
| A predicate for a generated attribute validator | `[CustomValidator]` | No |
| A variation of a comparison or string rule | the open base (`ComparisonRule`, `BetweenRule`, `EmailRule<T, TProperty>`, …) or composition | Yes — open base only |

The public open bases are `ValidateRuleBase<T, TProperty>`, `SyncValidateRule<T, TProperty>`,
`AsyncValidateRule<T, TProperty>`, `ComparisonRuleBase<T, TValue>`, `ComparisonRule<T, TValue>`,
`BetweenRule<T, TValue>`, and `PropertyComparisonRuleBase<T, TProperty>`. The two-arity string rule
forms (`EmailRule<T, TProperty>`, `LengthRule<T, TProperty>`, `RegexMatchRule<T, TProperty>`, …) are
also open; their one-arity forms (`EmailRule<T>`, `LengthRule<T>`, …) are sealed convenience wrappers.

> Do not copy the source of a sealed built-in into your project to change one detail. Use the per-rule
> options, compose a custom rule, or derive from the corresponding open base.

## Reusable rule classes

### Synchronous rule

Derive from `SyncValidateRule<T, TProperty>` and implement `ValidateValue`. The base class resolves the
accessor, member name, message, localization key, severity, error code, and custom state.

```csharp
using Jeninnet.Validation;
using Jeninnet.Validation.Rules;

public sealed class StartsWithRule<T>(
    IPropertyAccessor<T, string?> accessor,
    string prefix,
    string? message = null,
    string? messageKey = null)
    : SyncValidateRule<T, string?>(accessor, message, messageKey) {

    protected override void ValidateValue(
        string? value,
        in ValidationContext<T> context,
        ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if (value is not null && !value.StartsWith(prefix, StringComparison.Ordinal)) {
            AddError(errors, context);
        }
    }
}
```

`AddError` fills `MemberName`, `PropertyName`, `PropertyPath`, `PropertyValue`, the localized message,
the error code, and the severity from the rule metadata. Pass an authored `message` or a `messageKey`
(with a localized pack entry) to the constructor; a per-rule `WithMessage(...)` still overrides it.
Override `GetMessageParameters` when the message needs named placeholders:

```csharp
protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context)
    => new Dictionary<string, object> { ["Prefix"] = prefix };
```

Expose the rule through an extension method so consumers use it exactly like a built-in:

```csharp
public static class StartsWithRuleExtensions {
    public static IRuleBuilder<T, string?> StartsWith<T>(
        this IRuleBuilder<T, string?> builder,
        string prefix,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddRule(new StartsWithRule<T>(builder.Accessor, prefix, message));
        return builder;
    }
}
```

```csharp
builder.RuleFor(x => x.Code, nameof(Order.Code))
       .NotEmpty()
       .StartsWith("INT-");
```

`IRuleBuilder<T, TProperty>.AddRule` and `AddAsyncRule` are public for exactly this purpose.

### Asynchronous rule

Derive from `AsyncValidateRule<T, TProperty>` when the rule needs the same accessor and metadata
plumbing on the async path:

```csharp
public sealed class UsernameAvailableRule<T>(
    IPropertyAccessor<T, string?> accessor,
    string? message = null,
    string? messageKey = null)
    : AsyncValidateRule<T, string?>(accessor, message, messageKey) {

    protected override async ValueTask ValidateValueAsync(
        string? value,
        ValidationContext<T> context,
        ValidationErrorCollector errors,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(errors);

        if (string.IsNullOrWhiteSpace(value)) {
            return;
        }

        if (!await IsAvailableAsync(value, cancellationToken).ConfigureAwait(false)) {
            AddError(errors, context);
        }
    }
}
```

`IsAvailableAsync` is application code: return `true` when the value is free. `AsyncValidateRule`
implements only `IAsyncValidationRule<T>`, so a validator that contains it is async-only and must run
through `ValidateAsync`. Implement `IAsyncValidationRule<T>` directly when you need to build the member
path or the `ValidationError` yourself; `samples/Jeninnet.Validation.Sample.Customization/CustomRules.cs`
shows both forms.

### Metadata and client rules

- Rules derived from `SyncValidateRule`/`AsyncValidateRule` describe themselves: the descriptor sees
  their kind and `ErrorCode`, and `GetMessageParameters`/`GetMetadataParameters` feed the parameters.
- A custom rule that cannot describe itself is recorded as a `Custom` advisory entry rather than
  dropped. In generated validators, add explicit metadata with the `ConfigureDescriptor` partial hook.
- Custom rules are not executable on the client. JSON Schema and OpenAPI keep their constraints
  advisory, and client-rule output marks unsupported or conditional rules as `serverOnly`; the server
  remains the source of truth.
- For attribute-driven validators, prefer `[CustomValidator]` for a static predicate, or add the rule
  in the generated `AddCustomValidators()` hook. The fluent `Custom`/`CustomAsync` delegates remain the
  better choice when the predicate needs the full validation context.

## Customizing a built-in rule without inheritance

Most "I want to change the built-in" requests are options, not subclasses:

| Goal | API |
| --- | --- |
| Replace the failure message | `WithMessage("...")` or a dynamic `WithMessage` overload |
| Set a programmatic error code | `WithCode("...")` |
| Change severity | `WithSeverity(ValidationErrorSeverity.Warning)` |
| Report a different property name | `OverridePropertyName("...")` |
| Attach computed state to errors | `WithState(...)` |
| Run only under a condition | `When(...)` / `Unless(...)` |
| Put the rule in a named set | `RuleSet("...")` |
| Adjust every rule added by one validator | override `OnRuleAdded(IRuleDefinition)` |
| Use a different regular expression | construct `RegexMatchRule` with your `Regex` |
| Different comparison or bounds semantics | derive from `ComparisonRule` / `BetweenRule` |

Only when the behavior itself differs should you write a rule class. If the difference belongs to a
built-in family, derive from that family's open base; otherwise use `SyncValidateRule` /
`AsyncValidateRule` or the interfaces.

## Rule templates

When the same predicate is reused across validators, capture it in a `RuleTemplate` instead of
copying a `Must` delegate. A template bundles the predicate with its message, message key,
severity, error code, and named parameters. Apply it with `RuleFor(...).Rule(template)`:

```csharp
using Jeninnet.Validation.Rules;

private static readonly RuleTemplate<string?> ExactLinesCount = new RuleTemplate<string?>(
        "ExactLinesCount",
        static value => value is not null && CountLines(value) == 3,
        message: "Must contain exactly {Expected} lines; found {Actual}.",
        messageKey: "ExactLinesCount",
        severity: ValidationErrorSeverity.Warning,
        errorCode: "LINES_COUNT")
    .WithParameter("Expected", 3)
    .WithParameter("Actual", static value => CountLines(value));

builder.RuleFor(static x => x.Bio, nameof(User.Bio))
       .Rule(ExactLinesCount);
```

Static parameters (`.WithParameter("Expected", 3)`) are fixed at definition time.
Value-dependent parameters (`.WithParameter("Actual", static value => CountLines(value))`) run only
when the rule fails, so the success path allocates nothing. Values keep their runtime types, so a
`decimal` parameter formats with the requested culture (`{Amount:C}` renders `€` under `nl-NL`).

For predicates that need the whole model, use the instance-aware template:

```csharp
private static readonly RuleTemplate<Customer, int> AdultByCompany = new RuleTemplate<Customer, int>(
        "AdultByCompany",
        static (customer, age) => customer.IsCompany ? age >= 18 : age >= 21,
        message: "{PropertyName} must be at least {MinAge} for a {CustomerKind}.",
        messageKey: "AdultByCompany",
        errorCode: "AGE_LIMIT")
    .WithParameter("MinAge", static (customer, _) => customer.IsCompany ? 18 : 21)
    .WithParameter("CustomerKind", static (customer, _) => customer.IsCompany ? "business" : "consumer");

builder.RuleFor(static x => x.Age, nameof(Customer.Age))
       .Rule(AdultByCompany);
```

Templates are immutable: every `WithMessage`, `WithMessageKey`, `WithSeverity`, `WithErrorCode`,
`WithParameter`, and `WithParameters` call returns a new template. The returned `IRuleBuilder`
supports the usual per-rule options (`WithMessage`, `WithCode`, `WithSeverity`), conditions
(`When`/`Unless`), rule sets (`RuleSet`), and cascade modes.

Descriptors expose the template name as the rule kind with its static parameters; dynamic parameter
names appear with a `null` value because they need a model instance. Client rules export templates
as `serverOnly: true` advisory entries with the static parameters preserved. Templates are a
fluent-authoring feature: the predicate is a runtime delegate, so the source generator cannot
discover them. Use attributes for generated validators.

## Attribute custom validators

`[CustomValidator]` binds a property or model to a static method on a rules type. Use it when the
model is validated through the generated attribute path but the predicate belongs in a reusable
rules class:

```csharp
[Validate]
public sealed class Order
{
    [CustomValidator(typeof(OrderRules), nameof(OrderRules.IsReserved))]
    public string? CustomerName { get; init; }
}
```

Supported synchronous signatures return `bool` or `ValidationResult` and accept
`(TValue value)` or `(TValue value, IServiceProvider services)`. Async rules set
`IsAsync = true` and return `ValueTask<bool>` or `ValueTask<ValidationResult>` with a final
`CancellationToken` parameter. A class-level `[CustomValidator]` receives the whole model.

- A `bool` false result adds one error using the attribute message.
- `ValidationResult` errors with an empty member name are attributed to the property; explicit paths
  are preserved.
- The service provider is the one supplied through `ValidationCallOptions.ServiceProvider`.
- Async rules run only on `ValidateAsync`; all-async validators are marked `[AsyncOnlyValidator]`.

The fluent `Custom`/`CustomAsync` delegates remain the better choice when the predicate captures
instance state or needs the full validation context.

## Shared context

Use `RootData` for one lookup reused by several rules:

```csharp
var customer = context.RootData!.GetOrAdd(
    "customer",
    () => LoadCustomer(context.Root!));
```

The bag is per-walk, sequential, and not thread-safe. The engine never resolves services itself; it
only exposes the provider supplied by the caller.

## Advanced rule hooks

`WithState` accepts a context-aware factory so an error can carry computed state:

```csharp
builder.RuleFor(x => x.Email, nameof(Customer.Email))
       .NotEmpty()
       .WithState((customer, email, context) => new { customer.Id, email });
```

`Validator<T>.OnRuleAdded(IRuleDefinition)` runs once per leaf rule after configuration and before
the validator is built. Cast the rule to `IRuleDefinitionOptions` to adjust its default message,
error code, or severity. `ValidationPath.GetParent`/`IsChildOf` help with member and indexer chains;
generated validators do not use the hook.

## When not to use custom rules

- If a built-in rule exists, use it; descriptors and schema/client emission understand built-ins.
- If the logic is reusable across models, write a dedicated rule class instead of a large delegate.
- If the rule needs a condition, prefer `.When(...)` over branching inside the predicate.
- If you only need a different message, code, severity, or state, use the per-rule options first.
- If you need a variation of a built-in, derive from its open base, not from the sealed leaf.

## Related

- [Rules and messages](03-rules-and-messages.md)
- [Rule reference](../03-reference/02-rule-reference.md)
- [Metadata](../03-reference/04-metadata.md)
