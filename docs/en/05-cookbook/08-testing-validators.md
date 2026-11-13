# Test Validators

## Problem

You want readable, stack-agnostic assertions for valid and invalid models without depending on a
specific test framework.

## Solution

```xml
<PackageReference Include="Jeninnet.Validation.Assertions" Version="1.0.0-preview.1" />
```

```csharp
using Jeninnet.Validation.Assertions;

[TestClass]
public sealed class CustomerValidatorTests
{
    [TestMethod]
    public void Valid_model_has_no_errors()
    {
        var result = new CustomerValidator().Validate(new Customer
        {
            Name = "Ada",
            Email = "ada@example.com",
            Age = 30
        });

        result.ShouldNotHaveValidationErrors();
    }

    [TestMethod]
    public void Invalid_model_reports_each_member()
    {
        var result = new CustomerValidator().Validate(new Customer
        {
            Name = "",
            Email = "not-an-email",
            Age = 12
        });

        result.ShouldHaveValidationErrors()
              .WithErrorCount(4)
              .ForMember(nameof(Customer.Name))
              .ContainsRule(ValidationMessageKeys.NOT_EMPTY);

        result.ShouldHaveValidationErrors()
              .ForMember(nameof(Customer.Email))
              .ContainsRule(ValidationMessageKeys.EMAIL);
    }
}
```

The example uses MSTest as the host; the assertions package itself is framework-agnostic. Replace the
attributes with your test framework’s equivalents.

## `TestValidate`

```csharp
var test = new CustomerValidator().TestValidate(model);

test.ShouldHaveValidationErrorFor(nameof(Customer.Email))
    .ContainsRule(ValidationMessageKeys.EMAIL);

test.ShouldNotHaveValidationErrorFor(nameof(Customer.Age));
```

For async validators:

```csharp
var test = await validator.TestValidateAsync(model, cancellationToken);
test.ShouldNotHaveValidationErrors();
```

## Test generated and fluent parity

```csharp
var fluent = new CustomerValidator().Validate(model);
var generated = new GeneratedCustomerValidator().Validate(model);

var fluentMembers = fluent.Errors.Select(error => error.MemberName);
var generatedMembers = generated.Errors.Select(error => error.MemberName);

if (!fluentMembers.SequenceEqual(generatedMembers))
{
    throw new InvalidOperationException("Fluent and generated validators disagree.");
}
```

## Verify

Run the test suite:

```bash
dotnet test tests/Jeninnet.Validation.Tests/Jeninnet.Validation.Tests.csproj
```

## Related

- [Testing guide](../02-guides/14-testing.md)
- [Package map](../appendix/02-package-map.md)
