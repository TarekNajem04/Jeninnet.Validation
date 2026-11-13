# Rules and Messages

## Rule families

| Family | Examples |
|---|---|
| Presence | `NotNull`, `Null`, `NotEmpty`, `NotDefault`, `Empty` |
| Comparison | `Equal`, `NotEqual`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `InclusiveBetween`, `ExclusiveBetween`, `MultipleOf` |
| Strings | `Length`, `MinimumLength`, `MaximumLength`, `ExactLength`, `Matches`, `Contains`, `StartsWith`, `EndsWith`, `EmailAddress`, `CreditCard`, `Phone`, `Url`, `IPAddress`, `Base64String`, `FileExtensions` |
| Collections | `Count`, `MinCount`, `MaxCount`, `Unique`, `ForEach`, `Where`, `WhereAsync` |
| Dates | `Before`, `After` |
| Property comparison | `Equal`, `NotEqual`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual` against another property |
| Domain (optional) | `Iban`, `VatNumber`, `PostalCode`, `NationalId`, `PasswordStrength`, `Uuid`, `Ulid` |
| Custom | `Must`, `MustAsync`, `Custom`, `CustomAsync` |

`NotEmpty` does not treat value-type defaults (`0`, `false`, `default(DateTime)`) as empty. Use
`NotDefault()` when those values must be rejected explicitly.

The full list with parameters and message keys is in the [rule reference](../03-reference/02-rule-reference.md).

## Messages

Rules carry a default message key. `WithMessage` sets `ValidationError.Message` and marks it as
authored, so the localization pipeline keeps the authored text and does not replace it with a pack
template. Rules without an authored message resolve `MessageKey` through the packs. To change
localized default text, override the key in the message pack; see
[Localization](13-localization.md).

```csharp
builder.RuleFor(x => x.Email, nameof(Customer.Email))
       .NotEmpty()
       .WithMessage("Email is required.")
       .WithCode("EMAIL_REQUIRED")
       .WithSeverity(ValidationErrorSeverity.Warning)
       .WithName("Email address")
       .WithState(new { source = "signup" });
```

Precedence for the reported member name:

1. the string overload of `WithName` / `OverridePropertyName` replaces
   `ValidationError.MemberName` (and therefore the default `{PropertyName}`);
2. `WithName(Func<T, string>)` changes the `{PropertyName}` parameter only;
3. otherwise `MemberName` is the code-facing path (`Address.Street`, `Lines[0].Quantity`).

`ValidationOptions.DisplayNameResolver` changes only the `{PropertyName}` message parameter; it
does not change `MemberName`. A rule-level override wins over the resolver.

## Dynamic messages and names

Messages and display names can be computed from the instance that failed validation:

```csharp
builder.RuleFor(x => x.Email, nameof(Customer.Email))
       .NotEmpty()
       .WithMessage(x => $"Email for {x.Name} is required.");

builder.RuleFor(x => x.Email, nameof(Customer.Email))
       .NotEmpty()
       .WithMessage((x, context) => $"{context.GetFullPath(nameof(Customer.Email))} is required.");

builder.RuleFor(x => x.Email, nameof(Customer.Email))
       .NotEmpty()
       .WithName(x => $"Email for {x.Name}");

builder.RuleFor(x => x.Email, nameof(Customer.Email))
       .NotEmpty()
       .OverridePropertyName(x => $"email-{x.Name}");
```

- `WithMessage(Func<T, string>)` and `WithMessage(Func<T, ValidationContext<T>, string>)` run only
  when the rule fails. A `null` or blank result falls back to the static message/default message.
- `WithName(Func<T, string>)` changes only the `{PropertyName}` parameter and leaves
  `MemberName` unchanged. The string overload keeps its historical alias behavior for
  `OverridePropertyName`.
- `OverridePropertyName(Func<T, string>)` changes `MemberName` as well.
- A blank dynamic name falls back to the static override, the global resolver, or the code-facing
  member name.
- Options apply to composite rules too; the factory receives the parent instance and context.

`ValidationOptions.DisplayNameResolverForInstance` is the instance-aware resolver. It receives
`(Type, memberName, instance)` and takes precedence over the legacy `DisplayNameResolver` when it
returns a non-empty name.

Localization precedence: an authored `ValidationError.Message` (from `WithMessage`, attribute
messages, or explicit custom messages) wins over a matching message-pack template. A rule without
an authored message resolves its `MessageKey` through the packs; the pack template is the normal
path for generated rules.

## Nested, collection, and child-rule options

`WithMessage`, `WithCode`, `WithSeverity`, `WithState`, and `WithName`/`OverridePropertyName` also
apply to composite rules:

- `SetValidator(...)` and `ForEach(...)` options apply to every error emitted by the child.
- `ChildRules(...)` options apply to every inline child error.
- The string overload of `WithName` replaces `ValidationError.MemberName` for every emitted error;
  use it deliberately when collapsing a child path to one field. The delegate overload changes the
  display name only.

Options must directly follow the rule they configure. Configure them before `When`/`Unless` or
`DependentRules`; applying them after such a modifier throws `InvalidOperationException` instead of
silently doing nothing.

## Input sanitization

Sanitization is opt-in preprocessing that runs before validation rules, not a rule itself.
Declare it with `[Sanitize]` on a property (or on the class for every writable string member
without its own `[Sanitize]`), or register it in fluent code with a setter:

```csharp
[Sanitize(Trim = true, NormalizeWhitespace = true)]
public string? Bio { get; set; }

builder.RuleFor(static x => x.Name, nameof(User.Name), static (m, v) => m.Name = v!)
       .Sanitize(Sanitize.Trim, Sanitize.ToUpperInvariant)
       .NotEmpty();
```

Available operations: `Trim`, `NormalizeWhitespace`, `RemoveControlChars`, `ToUpper`/`ToLower`
(invariant by default, explicit `CultureInfo` opt-in), and `Sanitize.EachElement(...)` for
`IList<string?>` members. `SanitizeOptions` bundles the flags; `[Sanitize]` mirrors them with an
optional culture name. Whole-model cleanup uses `builder.Sanitize(...)` with an `Action<T>` or
`ISanitizer<T>`.

Sanitization mutates the model in place before rules run; `Validate` on a model with
`[Sanitize]` is an explicit preprocessing contract, never silent. Members without an accessible
non-init setter are rejected at build time with `VAL0027`; the same happens for non-string
members and unknown cultures. Sanitizers run even when rule-set filtering selects no rules, and
never appear in descriptors or client payloads. This is whitespace/casing/control-character
normalization only, not HTML or markup sanitization.

## Message parameters

Every error carries a stable parameter dictionary for message templates and clients. The common
keys are `PropertyName` (display name or member path), `PropertyValue` (the validated value), and
`PropertyPath` (the full member path, including indexes). Rule-specific names include
`ComparisonValue`, `ComparisonProperty`, `Min`, `Max`, `From`, `To`, `TotalLength`, `Length`,
`ExactLength`, `Pattern`, `Extensions`, `Values`, `EnumType`, `Count`, `Precision`, `Scale`,
`Divisor`, `Value`, `Country`, `Policy`, `MinLength`, `MissingRequirements`, and `CollectionIndex`.

```csharp
builder.RuleFor(x => x.Age, nameof(Customer.Age))
       .InclusiveBetween(18, 120)
       .WithMessage("{PropertyName} must be between {From} and {To}.");

builder.RuleFor(x => x.Tags, nameof(Order.Tags))
       .ForEachElement(static tag => tag.NotEmpty())
       .WithMessage("{PropertyPath}[{CollectionIndex}] is required.");
```

Use the `MessageParameterNames` constants instead of string literals. The old positional keys
(`"0"`, `"1"`) remain available for compatibility. See the
[rule reference](../03-reference/02-rule-reference.md#message-parameters) for the complete table.

## Error model

| Member | Meaning |
|---|---|
| `MemberName` | Code-facing path (`Address.Street`, `Lines[0].Quantity`), unless a rule-level `WithName` overrides it. |
| `Message` | Resolved or custom message. |
| `MessageKey` | Localization key. |
| `MessageParameters` | Stable named parameters (`PropertyName`, `PropertyValue`, `PropertyPath`, `ComparisonValue`, `CollectionIndex`, ...). |
| `ErrorCode` | Machine-readable code. |
| `Severity` | `Info`, `Warning`, or `Error`. |
| `CustomState` | Arbitrary state attached by the rule. |

## Localization

Message keys resolve through the sibling `Jeninnet.Localization` pipeline. The
`Jeninnet.Validation.Localization` package supplies packs for 40 cultures. See
[Localization](13-localization.md).

## Related

- [Rule sets and cascade](04-rule-sets-and-cascade.md)
- [Rule reference](../03-reference/02-rule-reference.md)
- [Localization](13-localization.md)
