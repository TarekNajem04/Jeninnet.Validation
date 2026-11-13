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

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.FullDemo/Jeninnet.Validation.Sample.FullDemo.csproj
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
