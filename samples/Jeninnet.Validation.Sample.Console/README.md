# Console Sample

A runnable tour of the core validation engine. The sample builds a small order domain, validates
it with fluent validators, and prints every error path, message, code, and severity.

## What it demonstrates

- Multiple fluent validators registered in a `ServiceCollection`.
- Nested models and child-rule validation with fully qualified paths.
- Rule sets, `ValidateAndThrow`, custom messages, error codes, severities, and custom state.
- Synchronous and asynchronous validation on the same model graph.
- Collection and enum rules, including `Empty`, `IsEnumName`, and nested `ChildRules`.

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.Console
```

## Key files

| File | Purpose |
|---|---|
| `Program.cs` | Registers validators and runs the two demo tasks. |
| `OrderValidatorTask.cs` | End-to-end order validation with nested models. |
| `AdvancedFeaturesTask.cs` | Messages, codes, severities, conditions, and `ValidateAndThrow`. |
| `AddressValidator.cs`, `CustomerValidator.cs`, `OrderValidator.cs` | Fluent validator definitions. |

## Related

- [Getting started](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/01-getting-started/01-introduction.md)
- [Fluent authoring](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/02-guides/01-fluent-authoring.md)
- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
