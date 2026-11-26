# Full Demo Sample

Reference sample demonstrating every major feature of Jeninnet.Validation together in one scenario.

## Features Demonstrated

| Feature | Usage |
|---------|-------|
| **Localization** | Memory provider with standard validation keys |
| **RuleSet** | Default + Premium rule sets for context-specific validation |
| **Include** | Inline extra rules on OrderNumber |
| **SetValidator** | AddressValidator reused inside Customer |
| **RuleForEach** | OrderLineValidator applied to each Lines item |
| **ChildRules** | Inline nested validation for Customer properties |
| **ValidateAndThrow** | Throws ValidationException with all errors |
| **CustomState** | Discount rule attaches suggestion metadata |
| **Severity** | Error, Warning, and Info levels on different rules |
| **ErrorCode** | Programmatic error codes on every rule |
| **Nested Validation** | Full property paths: Customer.Address.City, Lines[0].ProductName |
| **Inheritance** | Runtime dispatch to card/bank payment validators |
| **Filtered collections** | `Where` and `WhereAsync` element filters preserve original indices |
| **Rule templates** | Reusable `RuleTemplate<string>` for placeholder rejection |

## Scenario

An e-commerce order processing system validates incoming orders with:
- Basic field validation (always applied via Default RuleSet)
- Premium order checks (optional, via Premium RuleSet)
- Line item validation via RuleForEach
- Address validation via SetValidator
- Discount warnings via Info severity and CustomState

## Output

```
=== JENINNET.VALIDATION FULL DEMO ===
...
=== FULL DEMO COMPLETE ===
```

## Advanced examples

- [Minimal API](../Jeninnet.Validation.Sample.MinimalApi/README.md) — endpoint filters and ProblemDetails.
- [MVC](../Jeninnet.Validation.Sample.Mvc/README.md) — controllers and model-state projection.
- [MediatR](../Jeninnet.Validation.Sample.MediatR/README.md) — pipeline behavior and result adapters.
- [Nested validation](../Jeninnet.Validation.Sample.NestedValidation/README.md) — deep object graphs.
- [E-commerce](../Jeninnet.Validation.Sample.ECommerce/README.md) — SetValidator, RuleForEach, ChildRules, Include.
- [Authentication](../Jeninnet.Validation.Sample.Authentication/README.md) — RuleSet and localization.
- [Clean architecture](../CleanArchitecture/README.md) — layered application structure.
- [Localization](../Jeninnet.Validation.Sample.Localization.Composite/README.md) — provider composition.
- [Customization](../Jeninnet.Validation.Sample.Customization/README.md) — application-specific rules.

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.FullDemo/Jeninnet.Validation.Sample.FullDemo.csproj
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
