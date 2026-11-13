# Custom Rules

Use custom rules when the built-in surface cannot express the predicate.

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

## Related

- [Rules and messages](03-rules-and-messages.md)
- [Metadata](../03-reference/04-metadata.md)
