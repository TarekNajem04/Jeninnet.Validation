# Rule Sets and Cascade

## Rule sets

Named sets let a call select a subset of rules. When a call names sets, only those sets run; add
`default` to include ungrouped rules. When `RuleSets` is `null` or empty, all rules run, including
rules in named sets.

```csharp
public sealed class CustomerValidator : Validator<Customer>
{
    protected override void Configure(ValidatorRuleBuilder<Customer> builder)
    {
        builder.RuleFor(x => x.Name, nameof(Customer.Name)).NotEmpty();

        builder.RuleSet("Admin", admin =>
        {
            admin.RuleFor(x => x.InternalScore, nameof(Customer.InternalScore))
                 .InclusiveBetween(0, 100);
        });

        builder.RuleSet("Delete", delete =>
        {
            delete.RuleFor(x => x.DeletedBy, nameof(Customer.DeletedBy)).NotEmpty();
        });
    }
}
```

```csharp
validator.Validate(customer);                          // all rules
validator.Validate(customer, ["Admin"]);               // Admin only
validator.Validate(customer, ["default", "Admin"]);    // ungrouped + Admin
validator.Validate(customer, ["default", "Admin", "Delete"]); // ungrouped + both
validator.Validate(customer, ruleSets: null);          // all rules
```

Set names are case-insensitive. Use `ExcludeRuleSets` to remove named sets, including `"default"`
for ungrouped rules.

## Rule-level rule sets

Use the `RuleSet` named property to put one attribute rule in a set without moving the rest of the
property:

```csharp
[NotEmpty, RuleSet("Strict")]
public string? Name { get; init; }

[NotEmpty(RuleSet = "Admin")] // rule-level: only this rule joins Admin
public string? Code { get; init; }
```

The rule-level property wins over a property-level `[RuleSet]`, which in turn wins over a class-level
`[RuleSet]`. Multiple `[RuleSet]` attributes are allowed on the same property or type.

The fluent equivalent wraps the immediately preceding rule:

```csharp
builder.RuleFor(x => x.Code, nameof(Customer.Code))
       .NotEmpty()
       .RuleSet("Admin");
```

The per-rule `.RuleSet(...)` call is terminal: apply message, condition, and other rule modifiers
before it.

## Cascade within a member

```csharp
builder.RuleFor(x => x.Name, nameof(Customer.Name))
       .Cascade(CascadeMode.Stop)
       .NotNull()
       .NotEmpty()
       .Length(1, 100);
```

`Continue` (default) evaluates every rule. `Stop` skips the remaining rules for that member after the
first failure.

## Class-level cascade

```csharp
ClassLevelCascadeMode = CascadeMode.Stop;
```

Stops after the first failed member while preserving rule-level behavior inside the member.

## Per-call options

```csharp
var result = validator.Validate(order, new ValidationCallOptions
{
    RuleSets = ["strict"],
    CascadeMode = CascadeMode.Stop,
    FailFast = true
});
```

`FailFast` stops at the first error across the whole walk. On the async path, a sync-phase failure
under fail-fast or class-level `Stop` skips the async phase.

Generated `[Validate]` validators implement `IValidationOptionsValidator<T>` and accept the same
`ValidationCallOptions`/`ValidationStrategy` overloads; their generated rules expose member names
for property include/exclude targeting just like fluent member groups.

## Custom selectors

`ValidationCallOptions.Selector` adds an `IValidatorSelector` that runs after rule-set and
include/exclude filtering:

```csharp
var result = validator.Validate(order, new ValidationCallOptions
{
    IncludeProperties = ["Email"],
    Selector = new RulesetValidatorSelector("strict")
});
```

- A selector receives the top-level rule descriptor, the full member path (`""` for model-level
  rules), and the current `ValidationContext<T>`.
- `DefaultValidatorSelector.Instance` selects everything; `RulesetValidatorSelector` selects by
  rule-set metadata; `CompositeValidatorSelector` selects when any contained selector matches.
- Rule-set and include/exclude filters run first; the selector can only narrow the remaining set.
- Selectors apply on both sync and async paths and are inherited by nested validators.
- When no selector is set, the engine keeps its allocation-free path.

## Validation strategy

```csharp
var result = validator.Validate(order, strategy => strategy
    .IncludeProperties("Email", "Name")
    .ExcludeProperties("Name")
    .IncludeRuleSets("strict")
    .ExcludeRuleSets("legacy")
    .WithCascadeMode(CascadeMode.Stop)
    .FailFast());
```

- `IncludeProperties` is restrictive; an empty set runs nothing.
- `ExcludeProperties` wins over inclusion.
- `IncludeAllRuleSets()` selects every named set while still honoring `ExcludeRuleSets`.
- `IncludeRulesNotInRuleSet()` is the explicit form of `IncludeRuleSets("default")`.
- Property targeting applies to fluent member groups; unknown names are a no-op.
- The default path remains allocation-free.

## Throwing on failure

Use `ThrowOnFailures` in options or strategy when a failed result should become an exception:

```csharp
validator.Validate(order, new ValidationCallOptions { ThrowOnFailures = true });
validator.Validate(order, strategy => strategy.ThrowOnFailures());
```

The thrown `ValidationException` carries every collected error, after failure hooks have run.
`ThrowOnFailures` is independent of `FailFast`: fail-fast stops collection, while throw-on-failure
changes how the final result is delivered. `ValidateAndThrow` remains the explicit convenience method
for the same exception contract.

Rule sets are exposed through `IValidationRuleDescriptor.Parameters["RuleSets"]`; client-rule
output includes the same value under `params.RuleSets`.

## Precedence

| Level | Setting | Scope |
|---|---|---|
| Rule | `.Cascade(...)` | rules within one member |
| Type (rule level) | `Validator<T>` base constructor cascade | rules in the validator |
| Type (class level) | `ClassLevelCascadeMode` | members in the validator |
| Call | `ValidationCallOptions` | one call |
| Container (rule level) | `ValidationOptions.DefaultRuleLevelCascadeMode` (`DefaultCascadeMode` alias) | validators validated with that service provider |
| Container (class level) | `ValidationOptions.DefaultClassLevelCascadeMode` | validators validated with that service provider |
| Process (rule/class level) | `ValidationConfiguration.Options` | validators without a service provider |

## Related

- [Conditions and dependent rules](05-conditions-and-dependent-rules.md)
- [Core API](../03-reference/01-core-api.md)
