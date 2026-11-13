# Jeninnet.Validation.Assertions

Stack-agnostic assertion helpers for the Jeninnet validation framework. The package has no
dependency on MSTest, xUnit, or FluentAssertions: failures throw `ValidationAssertionException`
with the failed expectation and the actual errors, so any test framework can consume it.

## Requirements

- .NET 10.0 or later (including .NET 11).
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.Assertions --version 1.0.0-preview.1
```

## Quick start

```csharp
using Jeninnet.Validation.Assertions;

var result = validator.Validate(model);

result.ShouldHaveValidationErrors()
      .WithErrorCount(2)
      .ForMember(nameof(User.Email))
      .ContainsRule(ValidationMessageKeys.EMAIL)
      .ContainingMessage("valid email");

result.ShouldNotHaveValidationErrors();
```

## Validator entry points

```csharp
var test = validator.TestValidate(model);

test.ShouldHaveValidationErrorFor(nameof(User.Email))
    .ContainsRule(ValidationMessageKeys.EMAIL);

test.ShouldNotHaveValidationErrorFor(nameof(User.Age));

var asyncTest = await validator.TestValidateAsync(model, cancellationToken);
asyncTest.ShouldNotHaveValidationErrors();
```

## Matchers

| Method | Scope | Matches |
|---|---|---|
| `WithErrorCount(int)` | result / member | exact error count |
| `ContainingMessage(string)` | result / member | message fragment (ordinal) |
| `ContainsRule(string)` | member | `ValidationError.MessageKey` (ordinal) |
| `WithErrorCode(string)` | result / member | `ValidationError.ErrorCode` (ordinal) |
| `WithSeverity(ValidationErrorSeverity)` | result / member | error severity |
| `WithCustomState(object?)` | member | custom state (`Equals`) |
| `ForMember(string)` | result | narrows to one member and requires at least one error |
| `ShouldNotHaveErrors()` | result | the result is valid |
| `ShouldNotHaveValidationErrorFor(string)` | test result | no errors for the member |

## Failure contract

- Assertions throw `ValidationAssertionException`; the message lists the actual errors
  (member, message key/message, code, severity).
- `TestValidate` / `TestValidateAsync` throw `ArgumentNullException` for a null validator or a
  null model instance.
- `ValidationResult` is a value type, so there is no null-result path.

## Dependencies

- `Jeninnet.Validation`
- `Jeninnet.Validation.Abstractions`

No test-framework packages, no reflection, and the assembly is built with `IsAotCompatible`.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
