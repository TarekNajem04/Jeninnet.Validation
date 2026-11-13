# Cheat Sheet

## Packages

```bash
dotnet add package Jeninnet.Validation --version 1.0.0-preview.1
dotnet add package Jeninnet.Validation.Analyzers --version 1.0.0-preview.1
dotnet add package Jeninnet.Validation.AspNetCore --version 1.0.0-preview.1
dotnet add package Jeninnet.Validation.MediatR --version 1.0.0-preview.1
dotnet add package Jeninnet.Validation.DataAnnotations --version 1.0.0-preview.1
dotnet add package Jeninnet.Validation.Blazor --version 1.0.0-preview.1
dotnet add package Jeninnet.Validation.JsonSchema --version 1.0.0-preview.1
dotnet add package Jeninnet.Validation.OpenApi --version 1.0.0-preview.1
dotnet add package Jeninnet.Validation.ClientRules --version 1.0.0-preview.1
dotnet add package Jeninnet.Validation.Localization --version 1.0.0-preview.1
dotnet add package Jeninnet.Validation.Assertions --version 1.0.0-preview.1
```

## Validator skeleton

```csharp
public sealed class CustomerValidator : Validator<Customer>
{
    protected override void Configure(ValidatorRuleBuilder<Customer> builder)
    {
        builder.RuleFor(x => x.Name, nameof(Customer.Name))
               .NotEmpty()
               .Length(1, 100);
    }
}
```

## Run

```csharp
var result = validator.Validate(model);
var asyncResult = await validator.ValidateAsync(model, cancellationToken);
validator.ValidateAndThrow(model);
```

## Rules

| Need | Rule |
|---|---|
| Required | `NotNull`, `NotEmpty` |
| Range | `InclusiveBetween(min, max)`, `ExclusiveBetween` |
| Compare | `GreaterThan`, `LessThan`, `Equal`, `NotEqual` |
| String length | `Length(min, max)`, `ExactLength`, `MinimumLength`, `MaximumLength` |
| Pattern | `Matches(pattern)` |
| Formats | `EmailAddress`, `Phone`, `Url`, `IPAddress`, `CreditCard`, `Base64String` |
| Collections | `Count`, `MinCount`, `MaxCount`, `ForEach`, `Where`, `WhereAsync` |
| Dates | `Before`, `After` |
| Custom | `Must`, `MustAsync`, `Custom`, `CustomAsync` |

## Options

```csharp
.WithMessage("...")        // fallback when no localization template exists
.WithCode("CODE")
.WithSeverity(ValidationErrorSeverity.Warning)
.WithName("ReportedName")   // replaces ValidationError.MemberName
.WithState(new { source = "api" })
```

## Rule sets

```csharp
builder.RuleSet("Admin", admin =>
    admin.RuleFor(x => x.InternalScore, nameof(Customer.InternalScore))
         .InclusiveBetween(0, 100));

validator.Validate(model, ["Admin"]);                 // Admin only
validator.Validate(model, ["default", "Admin"]);      // ungrouped + Admin
validator.Validate(model, ruleSets: null);            // all rules
```

## Cascade and fail-fast

```csharp
.Cascade(CascadeMode.Stop)
ClassLevelCascadeMode = CascadeMode.Stop;

validator.Validate(model, new ValidationCallOptions { FailFast = true });
```

## Conditions and dependents

```csharp
.GreaterThan(0).When(x => x.Country == "US");
.GreaterThan(0).When(x => x.Country == "US", ApplyConditionTo.AllValidators);
.DependentRules(d => d.RuleFor(x => x.Rate, nameof(Order.Rate)).GreaterThan(0));
```

## DI

```csharp
services.AddScoped<IValidator<Customer>, CustomerValidator>(); // explicit, AOT-safe

// Reflection-based discovery; not AOT-safe:
services.AddGeneratedValidators(typeof(CustomerValidator).Assembly);
```

## ASP.NET Core

```csharp
services.AddValidationEndpoints();
app.MapPost("/users", handler).AddRequestValidation<CreateUser>();

services.AddMvcValidation();
```

## MediatR

```csharp
services.AddValidationPipeline();
services.AddScoped<IValidator<CreateOrderCommand>, CreateOrderCommandValidator>();
```

## DataAnnotations

```csharp
[JeninnetValidatable(typeof(CreateOrderValidator))]
public sealed class CreateOrderRequest { /* ... */ }
```

## Blazor

```razor
<JeninnetValidator TModel="Order" />
```

## Schema / OpenAPI / client

```csharp
ValidationSchemaWriter.WriteToString(descriptor);
services.AddSchemaConstraintsFromValidators(new OrderValidator());
ClientRulesWriter.WriteToString([validator]);
```

## Testing

```csharp
result.ShouldHaveValidationErrors()
      .ForMember(nameof(Customer.Email))
      .ContainsRule(ValidationMessageKeys.EMAIL);
```

## Checks

```bash
dotnet build Jeninnet.Validation.slnx
dotnet test Jeninnet.Validation.slnx
dotnet format Jeninnet.Validation.slnx --verify-no-changes --severity info
```
