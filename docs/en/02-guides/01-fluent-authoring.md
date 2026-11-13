# Fluent Authoring

Fluent authoring is the `Validator<T>` path. Use it for dynamic rules, context-heavy predicates, and
validators that need constructor-injected services.

## A validator

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

`RuleFor(expression, memberName)` registers a member group. The member name is used in error paths
and localization. Chained extensions add rules in order.

## Compose validators

```csharp
// Reuse another validator's rules at the root level.
builder.Include(new CustomerDefaultsValidator());

// Reuse a validator for a nested property.
builder.RuleFor(x => x.Address, nameof(Customer.Address))
       .SetValidator(new AddressValidator());

// Inline nested rules when the child has no reusable validator.
builder.RuleFor(x => x.Address, nameof(Customer.Address))
       .ChildRules(address =>
       {
           address.RuleFor(a => a.Street, nameof(Address.Street)).NotEmpty();
           address.RuleFor(a => a.City, nameof(Address.City)).NotEmpty();
       });
```

`Include` composes rules at the validator level. `SetValidator` prefixes nested errors with the
parent path (`Address.Street`). `ChildRules` is the inline equivalent.

## Inheritance and runtime dispatch

```csharp
// Compose a validator declared for a base type or interface.
builder.IncludeBase<Person, Contact>(new ContactValidator());

// Choose a validator from the actual runtime type of a property.
builder.RuleFor(x => x.Primary, nameof(Container.Primary))
       .SetInheritanceValidator(map =>
       {
           map.Add<Person>(new PersonValidator());
           map.Add<Organisation>(new OrganisationValidator());
       });
```

The dispatch map is explicit and exact-type only; unknown runtime types are skipped. See
[Inheritance and polymorphism](20-inheritance-and-polymorphism.md).

## Collections

```csharp
builder.RuleFor(x => x.Lines, nameof(Order.Lines))
       .ForEach(new OrderLineValidator());

builder.RuleFor(x => x.Lines, nameof(Order.Lines))
       .Count(1, 100)
       .ForEach(new OrderLineValidator())
       .Where(line => line.IsActive);
```

Apply value rules directly to elements when a child validator is unnecessary:

```csharp
builder.RuleFor(x => x.Tags, nameof(Order.Tags))
       .ForEachElement(element => element.NotEmpty());
```

`ForEachElement` validates null elements (unlike `ForEach`, which skips null children), supports
`Where`, and exposes the original index as the `CollectionIndex` message parameter.
`OverrideIndexer` changes the displayed path segment. Collection errors use bracket paths:
`Lines[0].Quantity` or a custom indexer such as `Lines[id-0].Quantity`.

## Register and resolve

```csharp
services.AddScoped<CustomerValidator>();
services.AddScoped<IValidator<Customer>, CustomerValidator>();
```

For generated validators, `AddGeneratedValidators(typeof(Program).Assembly)` scans and registers
them. Prefer explicit registration in trimmed or Native AOT applications.

## Sync and async

```csharp
var result = validator.Validate(customer);
var asyncResult = await validator.ValidateAsync(customer, cancellationToken);
```

A validator with async rules but no sync rules throws
`AsyncValidatorInvokedSynchronouslyException` on the sync path. Use the async path for
`MustAsync`, `CustomAsync`, async conditions, and async filters.

## Advanced rule extension

`WithState` also accepts a factory that receives the model, the member value, and the validation
context. The factory runs when an error is emitted, so the state follows the current instance:

```csharp
builder.RuleFor(x => x.Email, nameof(Customer.Email))
       .NotEmpty()
       .WithState((customer, email, context) => new { customer.Id, email, Path = context.MemberPath });
```

Override `OnRuleAdded` to adjust defaults after every rule is fully configured (including
`WithMessage`/`WithState` calls) and before the validator is built:

```csharp
public sealed class CustomerValidator : Validator<Customer>
{
    protected override void OnRuleAdded(IRuleDefinition rule)
    {
        if(rule is IRuleDefinitionOptions options && options.ErrorCode is null)
            options.ErrorCode = "CUSTOMER";
    }

    protected override void Configure(ValidatorRuleBuilder<Customer> builder) { /* ... */ }
}
```

The hook runs once per leaf rule and is not used by generated validators. Use `ValidationPath` for
member chains: `GetParent("Items[0].Name")` returns `Items[0]`, and
`IsChildOf("Items", "Items[0].Name")` returns `true`.

## Options

Register options per DI container. ASP.NET Core filters and any call that supplies
`ValidationCallOptions.ServiceProvider` resolve this instance, so two containers in one process can
use different resolvers and cascade defaults:

```csharp
services.AddValidation(options => options with
{
    PropertyNameResolver = (type, member) =>
        member == nameof(Customer.Email) ? "customer_email" : member,
    DisplayNameResolver = (type, member) => member == nameof(Customer.Email) ? "Email address" : member,
    ValidatorFactory = (modelType, services) =>
        modelType == typeof(Customer) ? new CustomerValidator() : null
});
```

- `PropertyNameResolver` changes `ValidationError.MemberName` for rules that do not call
  `OverridePropertyName`/`WithName(string)`. Return the input name to keep it unchanged.
- `DisplayNameResolver` changes only the `{PropertyName}` message parameter.
- `ValidatorFactory` supplies validators when DI has no `IValidator<T>` registration; ASP.NET Core
  filters use it as a fallback. `ValidationConfiguration.CreateValidator<T>(services)` exposes the
  same lookup.
- `DefaultRuleLevelCascadeMode` (and its `DefaultCascadeMode` alias) sets the rule-group default.
  Pass an explicit cascade to the `Validator<T>` base constructor, or call `.Cascade(...)` per
  rule, to win locally.
- `ValidationConfiguration.Configure` remains the process-wide fallback for validators created
  without a service provider (for example `new CustomerValidator()` in a unit test).

## Verify

Write one valid and one invalid test per member. Use `Jeninnet.Validation.Assertions`:

```csharp
var result = new CustomerValidator().Validate(invalid);

result.ShouldHaveValidationErrors()
      .ForMember(nameof(Customer.Email))
      .ContainsRule("Email");
```

## Related

- [Attribute authoring](02-attribute-authoring.md)
- [Rule sets and cascade](04-rule-sets-and-cascade.md)
- [Collections and nesting](06-collections-and-nesting.md)
