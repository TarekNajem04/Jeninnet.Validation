# Rule Reference

Every rule has a fluent method, a stable message key, and — for most rules — an attribute
equivalent. The tables below are the complete public rule surface.

## Attribute namespaces

Most attributes live in `Jeninnet.Validation.Attributes`. A few are grouped by concern:

| Namespace | Attributes |
|---|---|
| `Jeninnet.Validation.Attributes` | `[Validate]`, `[NotEmpty]`, `[Length]`, `[Email]`, `[Regex]`, `[InclusiveBetween]`, `[PropertyEqual]`, `[ForEach]`, `[Nested]`, `[RuleSet]`, and most others |
| `Jeninnet.Validation.Attributes.Common` | `[NotNull]`, `[Enum]`, `[In]`, `[NotIn]`, `[ScalePrecision]` |
| `Jeninnet.Validation.Attributes.String` | `[Phone]`, `[Url]`, `[IPAddress]` |

Add the matching `using` directive when you use a grouped attribute.

## Presence

| Fluent | Attribute | Message key | Notes |
|---|---|---|---|
| `NotNull()` | `[NotNull]` | `NotNull` | Fails only when the value is `null`. |
| `Null()` | — | `Null` | Fails when the value is not `null`. |
| `NotEmpty()` | `[NotEmpty]` | `NotEmpty` | Fails for `null`, a whitespace-only string, an empty collection, or `Guid.Empty`. Value-type defaults such as `0` and `false` are **not** empty. |
| `NotDefault()` | — | `NotDefault` | Fails for the default value of the member type: `0`, `false`, `Guid.Empty`, `default(DateTime)`, or `null`. |
| `Empty()` | — | `Empty` | Inverse of `NotEmpty()`. |

## Comparison

| Fluent | Attribute | Message key | Notes |
|---|---|---|---|
| `Equal(value[, comparer])` | `[Equal(value)]` | `Equal` | Null is skipped; the fluent overload accepts `IEqualityComparer<T>`/`StringComparer`. |
| `NotEqual(value[, comparer])` | `[NotEqual(value)]` | `NotEqual` | Null is skipped; the fluent overload accepts `IEqualityComparer<T>`/`StringComparer`. |
| `GreaterThan(value)` | `[GreaterThan(value)]` | `GreaterThan` | Null is skipped. |
| `GreaterThanOrEqual(value)` | `[GreaterThanOrEqual(value)]` | `GreaterThanOrEqual` | Null is skipped. |
| `LessThan(value)` | `[LessThan(value)]` | `LessThan` | Null is skipped. |
| `LessThanOrEqual(value)` | `[LessThanOrEqual(value)]` | `LessThanOrEqual` | Null is skipped. |
| `InclusiveBetween(min, max)` | `[InclusiveBetween(min, max)]` | `InclusiveBetween` | Both bounds are inclusive. Reversed bounds throw `ArgumentException` at configuration. |
| `ExclusiveBetween(min, max)` | `[ExclusiveBetween(min, max)]` | `ExclusiveBetween` | Both bounds are exclusive. Reversed or equal bounds throw `ArgumentException`. |
| `Range(min, max)` | — | `Range` | Inclusive range for comparable value types. Reversed bounds throw `ArgumentException`. |
| `ScalePrecision(precision, scale[, ignoreTrailingZeros])` | `[ScalePrecision(precision, scale, IgnoreTrailingZeros = true)]` | `PrecisionScale` | Decimal digits; null and non-decimal values are skipped. Trailing fractional zeros can be ignored. |
| `MultipleOf(divisor)` | `[MultipleOf(divisor)]` | `MultipleOf` | Integral, decimal, floating-point, and numeric-string values; the divisor must be greater than zero. Null is skipped. |

```csharp
builder.RuleFor(x => x.Age, nameof(Customer.Age))
       .InclusiveBetween(18, 120);

builder.RuleFor(x => x.Price, nameof(OrderLine.Price))
       .ScalePrecision(precision: 10, scale: 2);

builder.RuleFor(x => x.Quantity, nameof(OrderLine.Quantity))
       .MultipleOf(5);

builder.RuleFor(x => x.Rate, nameof(OrderLine.Rate))
       .ScalePrecision(precision: 5, scale: 2, ignoreTrailingZeros: true);
```

## Strings

| Fluent | Attribute | Message key | Notes |
|---|---|---|---|
| `Length(min, max)` | `[Length(min, max)]` | `Length` | Empty strings fail when `min > 0`. |
| `MinimumLength(min)` | `[MinimumLength(min)]` | `Length` | Implemented as `Length(min, int.MaxValue)`. |
| `MaximumLength(max)` | `[MaximumLength(max)]` | `Length` | Implemented as `Length(0, max)`. |
| `ExactLength(length)` | `[ExactLength(length)]` | `ExactLength` | Exact character count. |
| `Matches(pattern)` | `[Regex(pattern)]` | `Regex` | Null and empty strings are skipped. |
| `Contains(value)` | `[Contains(value)]` | `Contains` | Ordinal substring check. |
| `StartsWith(value)` | `[StartsWith(value)]` | `StartsWith` | Ordinal prefix check. |
| `EndsWith(value)` | `[EndsWith(value)]` | `EndsWith` | Ordinal suffix check. |
| `EmailAddress()` | `[Email]` | `Email` | Null and empty strings are skipped; combine with `NotEmpty()`. Uses one built-in regex mode; there is no separate simple/regex switch. |
| `CreditCard()` | `[CreditCard]` | `CreditCard` | Luhn-style card shape. |
| `Phone()` | `[Phone]` | `Phone` | Phone shape; import the `String` attribute namespace. |
| `Url()` | `[Url]` | `Url` | Absolute URL shape. |
| `IPAddress()` | `[IPAddress]` | `IPAddress` | IPv4 or IPv6. JSON Schema emits `anyOf` with `ipv4` and `ipv6`. |
| `Base64String()` | `[Base64String]` | `Base64String` | Valid Base64 payload. |
| `FileExtensions(extensions)` | `[FileExtensions(...)]` | `FileExtensions` | `params string[]`; the overload also accepts `ignoreCase` and `allowNonStandard`. |
| `IsEnumName(enumType[, caseSensitive])` | — | `IsEnumName` | String must be a defined name of the enum type. |

```csharp
builder.RuleFor(x => x.Email, nameof(Customer.Email))
       .NotEmpty()
       .EmailAddress()
       .MaximumLength(254);

builder.RuleFor(x => x.Avatar, nameof(User.Avatar))
       .FileExtensions([".png", ".jpg"], ignoreCase: true, allowNonStandard: false);
```

## Domain rules

The optional `Jeninnet.Validation.Rules.Domain` package adds structural, offline domain rules. A
country argument is explicit; an unsupported country throws `NotSupportedException` when the rule
is constructed. Checksums stay on the server: client rules mark these rules `serverOnly`, while
JSON Schema/OpenAPI emit patterns, formats, and length constraints where they are expressible.

| Fluent | Attribute | Message key | Notes |
|---|---|---|---|
| `Iban(country?)` | `[Iban("NL")]` | `Iban` | Mod-97 checksum plus the country-specific IBAN length. Spaces and hyphens are ignored; null/empty values are skipped. |
| `VatNumber(country)` | `[VatNumber("NL")]` | `VatNumber` | Supports `NL`, `DE`, `FR`, `GB`, and `BE`; includes the Dutch btw-id eleven-test where applicable. |
| `PostalCode(country)` | `[PostalCode("NL")]` | `PostalCode` | Supports `NL`, `US`, `DE`, `FR`, `GB`, `BE`, and `EG`. Dutch codes reject `SA`, `SD`, and `SS`. |
| `NationalId(country)` | `[NationalId("NL")]` | `NationalId` | Supports Dutch BSN (elfproef), Egyptian national ID (date/governorate structure), and US SSN shape. |
| `PasswordStrength(policy?)` | `[PasswordStrength(PasswordStrengthPreset.Strict)]` | `PasswordStrength` | `Basic`, `Strong` (default), and `Strict` presets; fluent code can pass a custom `PasswordPolicy`. |
| `Uuid()` | `[Uuid]` | `Uuid` | Parses any `Guid` format; null/empty values are skipped. |
| `Ulid()` | `[Ulid]` | `Ulid` | 26 Crockford Base32 characters; the first character must be `0`–`7`. |

```csharp
using Jeninnet.Validation.Rules;

builder.RuleFor(x => x.Iban, nameof(Account.Iban)).Iban("NL");
builder.RuleFor(x => x.VatNumber, nameof(Company.VatNumber)).VatNumber("NL");
builder.RuleFor(x => x.PostalCode, nameof(Address.PostalCode)).PostalCode("NL");
builder.RuleFor(x => x.Bsn, nameof(Person.Bsn)).NationalId("NL");
builder.RuleFor(x => x.Password, nameof(Account.Password)).PasswordStrength(PasswordPolicy.Strict);
```

`PasswordStrength` skips null and empty values; pair it with `NotEmpty()` when the password is
required. The error parameters include `Policy`, `MinLength`, and `MissingRequirements` (stable codes
such as `length`, `uppercase`, `digit`, `symbol`, and `common`).

## Collections

| Fluent | Attribute | Message key | Notes |
|---|---|---|---|
| `Count(min, max)` | `[Count(min, max)]` | `Count` | Null collections are skipped. |
| `MinCount(min)` | `[MinCount(min)]` | `MinCount` | Null collections are skipped. |
| `MaxCount(max)` | `[MaxCount(max)]` | `MaxCount` | Null collections are skipped. |
| `Unique([comparer])` | `[UniqueItems]` | `UniqueItems` | Null collections are skipped; the default comparer is `EqualityComparer<T>.Default`, or pass `IEqualityComparer<TElement>`. |
| `ForEach(validator)` | `[ForEach]` | Child validator | Null collections and null elements are skipped. |
| `ForEachElement(configure)` | — | Element rules | Applies value rules directly to elements; null elements are validated. |
| `Where(predicate)` | — | — | Filters elements for `ForEach` or `ForEachElement`; original indexes are preserved. |
| `WhereAsync(predicate)` | — | — | Async-path-only filter. |
| `OverrideIndexer(configure)` | — | — | Changes the displayed indexer; `CollectionIndex` keeps the original integer index. |
| `ChildRules(configure)` | `[Nested]` | Child rules | Inline child rules; null children are skipped. |
| `SetValidator(validator)` | `[Nested]` | Child validator | Reusable child validator; null children are skipped. |

```csharp
builder.RuleFor(x => x.Lines, nameof(Order.Lines))
       .MinCount(1)
       .Unique()
       .ForEach(new OrderLineValidator())
       .Where(line => line.IsActive);
```

Collection error paths use zero-based indexes: `Lines[0].Quantity`. Element-rule errors also carry
`CollectionIndex` in `MessageParameters`; message packs can use `{CollectionIndex}`. `IncludeProperties`
and `ExcludeProperties` accept `Lines[].Quantity` or `Lines[*].Quantity`; numeric indexes are
normalized to the same wildcard segment.

Conditions applied to `ForEach` or `ForEachElement` are **collection-level**: `.When(...)` is
evaluated against the parent instance, not per element, and it gates both the synchronous and
asynchronous paths. Element errors still carry the item path (`Lines[0].Name`) or the custom
`OverrideIndexer` text. Put the condition inside the child validator when it should be evaluated
per element.

## Dates and times

| Fluent | Attribute | Message key | Notes |
|---|---|---|---|
| `Before(threshold)` | `[Before("2026-01-01")]` | `Before` | Strictly earlier than the threshold. |
| `After(threshold)` | `[After("2026-01-01")]` | `After` | Strictly later than the threshold. |
| — | `[FutureDate]` | `FutureDate` | Later than the current UTC time; `IncludeToday = true` allows equality. |
| — | `[PastDate]` | `PastDate` | Earlier than the current UTC time; `IncludeToday = true` allows equality. |

The attribute constructor takes a string and converts it to the member type (`DateTime`,
`DateTimeOffset`, `DateOnly`, or `TimeOnly`). Parsing uses `CultureInfo.InvariantCulture`; a value
that cannot be parsed is a build error (`VAL0021`) and never falls back to the current time. Null
values are skipped.

`[FutureDate]` and `[PastDate]` have no threshold. They support `DateTime`, `DateTimeOffset`, and
`DateOnly` (including nullable members) and compare against the current **UTC** time. The time comes
from a `TimeProvider` resolved through `ValidationCallOptions.ServiceProvider`, falling back to
`TimeProvider.System`; no local-time conversion is performed. Null passes, and `IncludeToday`
controls whether equality with the current instant is valid. Unsupported member types are a build
error (`VAL0032`).

```csharp
[After("2026-01-01")]   public DateOnly StartDate { get; init; }
[Before("17:00")]      public TimeOnly ClosingTime { get; init; }
[After("2026-01-01T00:00:00+00:00")] public DateTimeOffset CreatedAt { get; init; }
```

For relative dates, use the dedicated attributes instead of a fixed threshold:

```csharp
public sealed class Subscription
{
    [FutureDate]                            public DateTime StartsAt { get; init; }
    [FutureDate(IncludeToday = true)]       public DateTimeOffset TrialEndsAt { get; init; }
    [PastDate]                              public DateOnly? CancelledOn { get; init; }
}
```

## Enums and sets

| Fluent | Attribute | Message key | Notes |
|---|---|---|---|
| `Enum()` | `[Enum]` | `Enum` | Fails when the numeric value is not defined on the enum. `[Flags]` enums accept any combination of declared flags and `0`; undefined bits fail. Null is skipped. |
| `In(allowedValues)` | `[In(...)]` | `In` | Null is skipped. |
| `NotIn(disallowedValues)` | `[NotIn(...)]` | `NotIn` | Null is skipped. |
| `IsEnumName(enumType[, caseSensitive])` | — | `IsEnumName` | String-only enum-name check. |

For `[Flags]` enums, `Enum` treats every declared member as a bit and accepts combinations such as
`Read | Write`; `0` is always accepted and any undefined bit fails. The rule builds its bit mask
from `Enum.GetValuesAsUnderlyingType` once at construction, then performs bitwise checks only, so
the validation path stays trim/AOT-safe.

## Property comparison

Fluent property comparisons take an `IPropertyAccessor<T, TProperty>` for the other member. Build it
with `ValidatorFactory.Accessor`:

```csharp
using Jeninnet.Validation.Core;

builder.RuleFor(x => x.ConfirmPassword, nameof(Register.ConfirmPassword))
       .Equal(ValidatorFactory.Accessor<Register, string?>(
           static x => x.Password,
           nameof(Register.Password)));
```

| Fluent | Attribute | Notes |
|---|---|---|
| `Equal(accessor)` | `[PropertyEqual(nameof(Other))]` | Both null counts as equal. |
| `NotEqual(accessor)` | `[PropertyNotEqual(nameof(Other))]` | |
| `GreaterThan(accessor)` | `[PropertyGreaterThan(nameof(Other))]` | Null sorts before a non-null value. |
| `GreaterThanOrEqual(accessor)` | `[PropertyGreaterThanOrEqual(nameof(Other))]` | |
| `LessThan(accessor)` | `[PropertyLessThan(nameof(Other))]` | |
| `LessThanOrEqual(accessor)` | `[PropertyLessThanOrEqual(nameof(Other))]` | |

The property-comparison attributes live in `Jeninnet.Validation.Attributes`.

## Conditional value shortcuts

These attributes compare another member to a compile-time constant and gate a presence or range rule
on the result. They are attribute-only; the fluent API expresses the same intent with `.When(...)`
and a normal rule.

| Attribute | Rule when active | Message key | Notes |
|---|---|---|---|
| `[RequiredIf(nameof(Other), value)]` | `NotEmpty` (string/collection) or `NotNull` | `NotEmpty` / `NotNull` | Value must be a constant. |
| `[RequiredUnless(nameof(Other), value)]` | `NotEmpty` (string/collection) or `NotNull` | `NotEmpty` / `NotNull` | Inverse of `RequiredIf`. |
| `[ConditionalRange(nameof(Other), value, min, max)]` | `InclusiveBetween(min, max)` | `InclusiveBetween` | Both bounds are inclusive; only runs when the comparison matches. Reversed bounds fail generation with `VAL0010`. |

```csharp
[Validate]
public sealed class Order
{
    public string? Channel { get; set; }

    [RequiredIf(nameof(Channel), "Partner")]
    public string? PartnerCode { get; set; }

    [ConditionalRange(nameof(Channel), "Partner", 1, 20)]
    public int PartnerPriority { get; set; }
}
```

The comparison supports strings, Booleans, characters, numeric types, enums, date/time types,
`Guid`, and `TimeSpan`; numeric constants are compared numerically across widths. The target is a
simple member name on the same model and must be readable from the generated validator. Invalid
targets, unsupported values, and duplicate attributes produce `VAL0029`–`VAL0031`. Conditional rules
are marked `serverOnly` in client-rules output and remain advisory in JSON Schema.

## Custom rules

| Fluent | Attribute | Message key | Notes |
|---|---|---|---|
| `Must(predicate)` | — | `Must` | Null is skipped. |
| `MustAsync(predicate)` | — | `MustAsync` | Async path only; null is skipped. |
| `Custom(delegate)` | — | caller-supplied | Can emit multiple errors with distinct keys, codes, and severities. |
| `CustomAsync(delegate)` | — | caller-supplied | Async path only; honors cancellation. |
| `Rule(template)` | — | template-supplied | Reusable `RuleTemplate<TValue>` / `RuleTemplate<T, TValue>` with message, key, severity, code, and named parameters. |
| `AddRule(rule)` | — | rule-supplied | Adds a reusable `ISyncValidationRule<T>`; see [Reusable rule classes](../02-guides/07-custom-rules.md#reusable-rule-classes). |
| `AddAsyncRule(rule)` | — | rule-supplied | Adds a reusable `IAsyncValidationRule<T>`; async path only. |

An exception thrown by a custom delegate becomes an `Invalid` failure with error code
`CustomValidationException`. See [Custom rules](../02-guides/07-custom-rules.md).

Built-in rule leaves are sealed by design. Extend them through `SyncValidateRule<T, TProperty>`,
`AsyncValidateRule<T, TProperty>`, the rule interfaces, `Custom`/`CustomAsync`, or `RuleTemplate`; do
not inherit a concrete built-in. The rationale and the full extension-point table are in
[Why built-in rules are sealed](../02-guides/07-custom-rules.md#why-built-in-rules-are-sealed).

Sanitization (`[Sanitize]`, `Sanitize.Trim`, `SanitizeOptions`) is preprocessing, not a rule: it
has no row in this table, no message key, and no descriptor or client-rule entry. It runs before
every rule and mutates string members in place. See the sanitization section in
[Rules and messages](../02-guides/03-rules-and-messages.md).

Rule templates bundle a predicate with its failure contract so it can be shared across models.
Value-dependent parameters are evaluated only on failure; the descriptor exposes the template
name as the rule kind (dynamic parameter names appear with a `null` value), and client rules
export templates as `serverOnly: true` advisory entries. Templates are fluent-only: the source
generator cannot discover runtime delegates, so generated validators keep using attributes.

## Message parameters

Every error carries `PropertyName`, `PropertyValue`, and `PropertyPath` in
`ValidationError.MessageParameters` (see `MessageParameterNames`). `PropertyName` is the display
name when a resolver or `WithName` supplies one, otherwise the code-facing member path.
`PropertyPath` is always the full code-facing path, including nested and indexed segments.

| Rule family | Semantic parameters | Legacy aliases |
|---|---|---|
| Comparison (`Equal`, `GreaterThan`, `After`, ...) | `ComparisonValue` | `"0"` |
| Property comparison | `PropertyValue`, `ComparisonValue`, `ComparisonProperty` | `"0"`, `"1"` |
| Inclusive/exclusive between and range | `Min`, `Max`, `From`, `To` | `"0"`, `"1"` |
| `Length` | `Min`, `Max`, `TotalLength` | `"0"`, `"1"` |
| `ExactLength` | `Length`, `ExactLength` | `"0"` |
| `Regex` | `Pattern` | `"0"` |
| `FileExtensions` | `Extensions` | `"0"` |
| `In` / `NotIn` | `Values` | `"0"` |
| `Enum` / `IsEnumName` | `EnumType` | `"0"` |
| `Count` | `Min`, `Max`, `Count` | `"0"`, `"1"` |
| `MinCount` / `MaxCount` | `Min` or `Max`, `Count` | `"0"` |
| `ScalePrecision` | `Precision`, `Scale` | `"0"`, `"1"` |
| `Contains` / `StartsWith` / `EndsWith` | `Value` | `"0"` |
| `Iban` | `Country` | — |
| `VatNumber` / `PostalCode` / `NationalId` | `Country` | — |
| `PasswordStrength` | `Policy`, `MinLength`, `MissingRequirements` | — |
| Element rules inside `ForEach` | `CollectionIndex` | — |

The positional aliases remain for backward compatibility, but new templates should use the
semantic names. The metadata descriptors expose the same static names for client rules and schema
writers; runtime-only values such as `PropertyValue`, `PropertyPath`, and `CollectionIndex` appear
on errors rather than in descriptors.

## Message keys

`ValidationMessageKeys` exposes the stable key for every built-in rule:

```text
NotNull, NotEmpty, NotDefault, Enum, In, NotIn, Must, MustAsync, Between,
InclusiveBetween, ExclusiveBetween, Comparable, Equal, NotEqual,
GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual, Range,
PrecisionScale, MultipleOf, Email, Phone, Url, IPAddress, CreditCard, Regex,
Length, Count, MinCount, MaxCount, UniqueItems, ExactLength, Base64String,
FileExtensions, Contains, StartsWith, EndsWith, After, Before,
Invalid, Null, Empty, IsEnumName, Iban, VatNumber, PostalCode, NationalId,
PasswordStrength, Uuid, Ulid
```

Use the constants rather than string literals so localization packs and client rules stay aligned.

## Null handling

The default is **skip null**, not fail:

- Presence rules (`NotNull`, `Null`, `NotEmpty`, `Empty`) are the exception.
- Shape and comparison rules skip `null`; combine with `NotNull` or `NotEmpty` when the value is
  required.
- `Length` does not skip an empty string; an empty string fails when `min > 0`.
- `ForEach`, `SetValidator`, and `ChildRules` skip null collections and null children.
- `Must` and `MustAsync` skip null; use `NotNull` for required values.
- Property comparison orders `null` before non-null; both null counts as equal.

## Related

- [Rules and messages](../02-guides/03-rules-and-messages.md)
- [Conditions and dependent rules](../02-guides/05-conditions-and-dependent-rules.md)
- [Diagnostics](03-diagnostics.md)
- [Core API](01-core-api.md)
