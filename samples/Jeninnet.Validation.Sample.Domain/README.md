# Domain Sample

A class-library sample that models a realistic order domain for validation examples and tests. It
contains no executable entry point; other samples and tests consume the model and sample data.

## What it contains

- Domain entities, value objects, and DTOs used across the validation samples.
- Attribute-decorated models that exercise common, string, comparison, and nested rules.
- `SampleData.CreateInvalidOrder()`, which returns a deliberately invalid graph covering empty
  strings, invalid email/phone values, future dates, negative amounts, and missing nested objects.

## Use it

Reference the project from another sample or test:

```xml
<ProjectReference Include="..\Jeninnet.Validation.Sample.Domain\Jeninnet.Validation.Sample.Domain.csproj" />
```

```csharp
var order = SampleData.CreateInvalidOrder();
var result = new OrderValidator().Validate(order);
```

## Key files

| File | Purpose |
|---|---|
| `SampleData.cs` | Builds the intentionally invalid order graph. |
| `Models.cs` | Domain model definitions. |
| `Entities/`, `DataTransfareObjects/`, `ValueObjects/` | Layered domain types used by the samples. |

## Related

- [Attribute authoring](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/02-guides/02-attribute-authoring.md)
- [Nested validation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/02-guides/06-custom-rules-with-context.md)
- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
