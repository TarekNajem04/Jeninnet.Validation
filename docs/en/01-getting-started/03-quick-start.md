# Quick Start

This chapter builds a complete validator, runs it, and inspects the result.

## 1. Define a model

```csharp
public sealed class Customer
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public int Age { get; init; }
}
```

## 2. Write a fluent validator

```csharp
using Jeninnet.Validation;
using Jeninnet.Validation.Fluent;

public sealed class CustomerValidator : Validator<Customer>
{
    protected override void Configure(ValidatorRuleBuilder<Customer> builder)
    {
        builder.RuleFor(x => x.Name, nameof(Customer.Name))
               .NotEmpty()
               .Length(1, 100);

        builder.RuleFor(x => x.Email, nameof(Customer.Email))
               .NotEmpty()
               .EmailAddress();

        builder.RuleFor(x => x.Age, nameof(Customer.Age))
               .InclusiveBetween(18, 120);
    }
}
```

`RuleFor` takes the property expression and the member name used in error paths and localization.

## 3. Validate

```csharp
var validator = new CustomerValidator();

var valid = new Customer { Name = "Ada", Email = "ada@example.com", Age = 30 };
var invalid = new Customer { Name = "", Email = "not-an-email", Age = 12 };

var ok = validator.Validate(valid);
var failed = validator.Validate(invalid);

Console.WriteLine(ok.IsValid);          // True
Console.WriteLine(failed.Errors.Count); // 4

foreach (var error in failed.Errors)
{
    Console.WriteLine($"{error.MemberName}: {error.Message} ({error.ErrorCode})");
}
```

`ValidationResult` exposes `IsValid` and `Errors`. Each `ValidationError` carries `MemberName`,
`MessageId`, `Message`, `MessageKey`, `MessageParameters`, `ErrorCode`, `Severity`, and optional
`CustomState`.

## 4. Throw when invalid

```csharp
try
{
    validator.ValidateAndThrow(invalid);
}
catch (ValidationException exception)
{
    Console.WriteLine(exception.Errors.Count);
}
```

All errors are collected before the exception is thrown.

## 5. Async rules

```csharp
var result = await validator.ValidateAsync(customer, cancellationToken);
```

Use the async path for validators that contain `MustAsync`, `CustomAsync`, or async conditions.
Calling `Validate` on an async-only validator throws
`AsyncValidatorInvokedSynchronouslyException` instead of silently skipping rules.

## 6. Generated equivalent

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

The generated `CustomerValidator` has the same runtime contract. Both authoring styles are pinned by
executable parity tests.

## Next

- [Mental model](04-mental-model.md)
- [Fluent authoring](../02-guides/01-fluent-authoring.md)
- [Attribute authoring](../02-guides/02-attribute-authoring.md)
