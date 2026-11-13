# Nested Validation Showcase

Demonstrates deep object graph validation with Jeninnet.Validation.

## Scenarios

| # | Scenario | Depth | Techniques |
|---|----------|-------|------------|
| 1 | Customer with Address | 2 levels | `SetValidator` + `ChildRules` |
| 2 | Order → Customer → Address → Country | 4 levels | `ChildRules` nesting |
| 3 | Invoice + line items | 2 levels | `ForEach` + `SetValidator` |
| 4 | Company → Departments → Employees → Address → Country | 5 levels | `ForEach` + `SetValidator` + `ChildRules` |

## Highlights

- Deep path propagation: `Departments[0].Employees[0].Address.Street`
- Mixed `SetValidator`, `ChildRules`, `ForEach` in the same graph
- Collection index paths: `Lines[0].ProductName`, `Departments[1].Employees[2].Name`
- Validator reuse: `AddressValidator` reused across Customer, Employee, and deep nesting

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.NestedValidation
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
