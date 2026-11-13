# CQRS + MediatR Sample

Demonstrates Jeninnet.Validation integration with MediatR pipeline behaviors.

## Highlights

- `AddValidationPipeline()` from `Jeninnet.Validation.MediatR` — the shipped `IPipelineBehavior`
- All registered `IValidator<TRequest>` implementations run before the handler
- `ValidationException` carries every aggregated error for fail-fast CQRS
- Separate validator classes per command

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.MediatR
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
