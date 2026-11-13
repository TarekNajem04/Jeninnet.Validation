# Rule Sets and Partial Updates

## Problem

A create endpoint requires all fields, an update endpoint only validates the fields present in the
request, and an admin endpoint runs extra rules.

## Solution: named rule sets

```csharp
public sealed class CustomerValidator : Validator<Customer>
{
    protected override void Configure(ValidatorRuleBuilder<Customer> builder)
    {
        builder.RuleFor(x => x.Name, nameof(Customer.Name)).NotEmpty();

        builder.RuleSet("Update", update =>
        {
            update.RuleFor(x => x.Name, nameof(Customer.Name)).Length(1, 100);
        });

        builder.RuleSet("Admin", admin =>
        {
            admin.RuleFor(x => x.InternalScore, nameof(Customer.InternalScore))
                 .InclusiveBetween(0, 100);
        });
    }
}
```

```csharp
validator.Validate(customer);                          // all rules
validator.Validate(customer, ["Update"]);              // Update only
validator.Validate(customer, ["default", "Update"]);   // ungrouped + Update
validator.Validate(customer, ["Admin"]);               // Admin only
validator.Validate(customer, ruleSets: null);          // all rules
```

Set names are case-insensitive. When a set is selected, ungrouped rules run only if `default` is
included.

## Solution: per-call member targeting

For PATCH endpoints, restrict the walk to the fields present in the request:

```csharp
var result = validator.Validate(customer, new ValidationCallOptions
{
    IncludeProperties = ["Name", "Email"],
    ExcludeProperties = ["InternalScore"],
    CascadeMode = CascadeMode.Stop
});
```

`IncludeProperties` is restrictive; an empty set runs nothing. `ExcludeProperties` wins over
inclusion. Unknown names are a no-op. Property targeting applies to fluent member groups.

Chained equivalent:

```csharp
var result = validator.Validate(customer, strategy => strategy
    .IncludeProperties("Name", "Email")
    .ExcludeProperties("InternalScore")
    .WithCascadeMode(CascadeMode.Stop)
    .FailFast());
```

## Combining sets and members

```csharp
var result = validator.Validate(customer, new ValidationCallOptions
{
    RuleSets = ["Update"],
    IncludeProperties = ["Name"]
});
```

## Verify

Send a PATCH request with only `Name`; confirm rules for `Email`/`InternalScore` do not run and the
response contains only `Name` errors.

## Related

- [Rule sets and cascade](../02-guides/04-rule-sets-and-cascade.md)
- [Core API](../03-reference/01-core-api.md)
