# About Jeninnet.Validation.Abstractions

`Jeninnet.Validation.Abstractions` contains the stable public contracts shared by the
Jeninnet.Validation packages:

- `IValidator<T>`, `ISyncValidator<T>`, and `IAsyncValidator<T>`.
- `ValidationResult`, `ValidationError`, `MessageId`, and `ValidationException`.
- Validation error collection and severity contracts.
- Rule metadata and descriptor contracts.
- Validation attributes that do not depend on the runtime engine.
- Diagnostics and message-resolution abstractions.

The package has no runtime dependencies. It is intended for libraries and integration packages
that need to expose or consume validation contracts without taking a dependency on the core
engine or any specific integration.

Most applications should reference `Jeninnet.Validation` instead; it brings these contracts
transitively together with the engine, fluent API, and built-in rules.

## Links

- Repository: <https://github.com/TarekNajem04/Jeninnet.Validation>
- Documentation: <https://tareknajem04.github.io/Jeninnet.Validation/>
- API reference: <https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/03-reference/08-api-reference.md>

## License

MIT. See <https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE>.
