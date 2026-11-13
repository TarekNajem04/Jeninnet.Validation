# Testing

The `Jeninnet.Validation.Assertions` package provides stack-agnostic assertions. It does not
reference MSTest, xUnit, or FluentAssertions; failures throw `ValidationAssertionException` with the
failed expectation and the actual errors.

## Install

```xml
<PackageReference Include="Jeninnet.Validation.Assertions" Version="1.0.0-preview.1" />
```

## Assert a result

```csharp
using Jeninnet.Validation.Assertions;

var invalid = validator.Validate(invalidModel);

invalid.ShouldHaveValidationErrors()
       .ForMember(nameof(User.Email))
       .ContainsRule(ValidationMessageKeys.EMAIL);

validator.Validate(validModel).ShouldNotHaveValidationErrors();
```

`ForMember` requires at least one error for the member and returns a member-scoped assertion set.

## Test a validator

```csharp
var test = validator.TestValidate(model);

test.ShouldHaveValidationErrorFor(nameof(User.Email))
    .ContainsRule(ValidationMessageKeys.EMAIL);
test.ShouldNotHaveValidationErrorFor(nameof(User.Age));

var asyncTest = await validator.TestValidateAsync(model, cancellationToken);
asyncTest.ShouldNotHaveValidationErrors();
```

`TestValidate` uses the synchronous contract. Use `TestValidateAsync` for async rules; async-only
validators throw on the synchronous path by design.

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

- Assertions throw `ValidationAssertionException`; the message lists the actual errors.
- `TestValidate` / `TestValidateAsync` throw `ArgumentNullException` for a null validator or model.
- `ValidationResult` is a value type; there is no null-result path.
- Wrap the exception in your framework if you prefer.

## Generated/fluent conformance suite

`GeneratedConformanceTests` compiles a real source-generated validator and a hand-written fluent
validator for the same model in one assembly, then compares `IsValid`, error count, member name,
message key, error code, severity, and custom state across a 23-case matrix. The old simulated
`ParityTests` harness was removed so parity is never asserted against a hand-built imitation.

## Test strategy

For every validator, cover:

1. a valid model (no errors);
2. each member’s invalid case;
3. rule sets and conditions;
4. async rules on the async path;
5. the generated/fluent parity case when both authoring styles exist.

## Related

- [Package map](../appendix/02-package-map.md)
- [Coverage and quality](../04-operations/03-coverage-and-quality.md)
