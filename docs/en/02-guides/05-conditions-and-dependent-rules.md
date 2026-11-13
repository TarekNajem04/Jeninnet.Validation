# Conditions and Dependent Rules

## Per-rule conditions

Attach a condition to the last configured rule:

```csharp
builder.RuleFor(x => x.Tax, nameof(Order.Tax))
       .GreaterThan(0)
       .LessThan(100)
       .When(x => x.Country == "US");
```

Apply the condition to every rule already configured for the member:

```csharp
builder.RuleFor(x => x.Tax, nameof(Order.Tax))
       .GreaterThan(0)
       .LessThan(100)
       .When(x => x.Country == "US", ApplyConditionTo.AllValidators);
```

| Method | Path |
|---|---|
| `When` / `Unless` | sync and async validation |
| `WhenAsync` / `UnlessAsync` | async path only |

Async conditions are never evaluated on the synchronous path. A sync call skips the gated rule; an
async-only validator still throws on the sync path.

## Group-level conditions

`ValidatorRuleBuilder<T>.When` / `Unless` gate a whole group:

```csharp
builder.When(x => x.Country == "US", us =>
{
    us.RuleFor(x => x.State, nameof(Order.State)).NotEmpty();
});
```

The async variants are `WhenAsyncRule` / `UnlessAsyncRule`.

## Conditions in attributes

`[WhenCondition]` and `[UnlessCondition]` apply a condition to every generated rule on a property or
type. The named method must return `bool` and accept `(T instance)` or
`(T instance, ValidationContext<T> context)`; it can be an instance or static method.

```csharp
[Validate]
[WhenCondition(nameof(IsEditable))]
public sealed class Order
{
    [WhenCondition(nameof(IsTaxRequired))]
    public decimal Tax { get; init; }

    public bool IsEditable(Order instance) => instance.Status != "closed";

    public bool IsTaxRequired(Order instance) => instance.Country == "US";
}
```

A property-level condition overrides a class-level condition. Unlike `WhenAsync`/`UnlessAsync`,
synchronous attribute conditions run on both the sync and async validation paths.

## Dependent rules

`DependentRules` attaches a block to the rule immediately before it. The block runs only when that
rule produced no errors.

```csharp
builder.RuleFor(x => x.Discount, nameof(Order.Discount))
       .InclusiveBetween(0, 90)
       .DependentRules(dependent =>
       {
           dependent.RuleFor(x => x.Rate, nameof(Order.Rate)).GreaterThan(0);
           dependent.RuleFor(x => x.Code, nameof(Order.Code)).NotEmpty();
       });
```

| Situation | Behavior |
|---|---|
| Parent passes | Dependent rules run in declaration order. |
| Parent fails | Dependent rules do not run. |
| `CascadeMode.Stop` on the member | Stops after the first failing dependent. |
| `FailFast` | Stops the whole walk after the first error. |
| Nested dependent blocks | Each block is gated by its own parent, depth-first. |
| Sync parent, async dependents | The gate is recorded in the sync phase and honored in the async phase. |
| Async parent, sync dependents | The parent must run on the async path; the sync path never runs the dependents. |

There is no attribute equivalent for dependent rules.

## When to use which

- **Per-rule condition** — one rule depends on another value.
- **Group condition** — a set of rules applies to one scenario.
- **Dependent rules** — later rules only make sense after an earlier rule passed (state-dependent
  validation without putting everything in one predicate).

## Related

- [Rule sets and cascade](04-rule-sets-and-cascade.md)
- [Custom rules](07-custom-rules.md)
