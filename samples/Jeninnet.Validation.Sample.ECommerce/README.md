# E-Commerce Sample

Demonstrates SetValidator, RuleForEach, ChildRules, Include, and nested validation with full property paths.

## Entities

| Entity | Properties |
|--------|-----------|
| Address | Street, City, ZipCode, Country |
| Customer | Name, Email, Address |
| OrderLine | ProductName, Quantity, UnitPrice |
| Payment | Method, TransactionId, Amount |
| Order | OrderNumber, Customer, Lines, Payment |

## Validators

| Validator | Features | Validates |
|-----------|----------|-----------|
| AddressValidator | NotEmpty, Length, Matches | Basic address fields |
| CustomerValidator | SetValidator | Customer + Address |
| OrderLineValidator | GreaterThan, NotEmpty | Line items |
| PaymentValidator | Must, NotEmpty, GreaterThan | Payment info |
| OrderValidator | Include, SetValidator, RuleForEach, ChildRules | Full order graph |

## Demonstrations

1. **SetValidator** — AddressValidator reused inside CustomerValidator.
2. **RuleForEach** — OrderLineValidator applied to each item in Lines collection.
3. **ChildRules** — Inline nested rules for Payment without a separate validator.
4. **Include** — Additional OrderNumber rules added inline.
5. **Property paths** — Errors include full paths like `Order.Customer.Address.City` and `Order.Lines[2].ProductName`.

## Output

```
=== E-COMMERCE SAMPLE ===
...
```

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.ECommerce/Jeninnet.Validation.Sample.ECommerce.csproj
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
