# Clean Architecture Sample

Demonstrates Jeninnet.Validation in a layered Clean Architecture structure.

## Architecture

```
Domain/         — Entities and value objects (no dependencies)
Application/    — DTOs, validators, interfaces, services (depends on Domain + Validation)
Infrastructure/ — Repository implementations (depends on Application)
Web/            — Entry point (depends on Infrastructure)
```

## Highlights

- Validation in the Application layer using `Validator<T>`
- `ValidateAndThrow` for fail-fast service methods
- `ForEach` for collection element validation in order lines
- Separated concerns: validators live in Application, not Domain
- Services use validators as constructor dependencies

## Run

```bash
dotnet run --project samples/CleanArchitecture/Jeninnet.Validation.Sample.CleanArchitecture.Web
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
