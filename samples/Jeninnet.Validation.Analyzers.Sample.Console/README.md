# Analyzers Sample — Console

Demonstrates attribute-driven validation with the Roslyn source generator and analyzers. The
sample declares models with validation attributes, lets `Jeninnet.Validation.Analyzers` generate
the validators at compile time, and resolves them through dependency injection.

## What it demonstrates

- `[Validate]` models and generated `IValidator<T>` implementations.
- Generated validator registration through `AddGeneratedValidators`.
- Synchronous and asynchronous validation of an intentionally invalid order.
- Rule-set selection (`Billing`) and generated error output.
- Analyzer diagnostics while editing the sample in an IDE or build.

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Analyzers.Sample.Console
```

## Key files

| File | Purpose |
|---|---|
| `Program.cs` | Builds the container and runs the generated validators. |
| `SampleData.cs` | Creates an invalid order for the demo. |
| `Models.cs` | Attribute-decorated model types. |
| `UserValidatorTask.cs`, `OrderValidatorTask.cs` | Demo scenarios for generated validation. |

## Related

- [Attribute authoring](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/02-guides/02-attribute-authoring.md)
- [Analyzer diagnostics](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/03-reference/03-analyzer-diagnostics.md)
- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
