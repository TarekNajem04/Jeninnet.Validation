# Composite Localization Sample

Demonstrates chaining multiple `IInnerValidationMessageProvider` instances together using the `CompositeValidationMessageProvider` (enabled automatically when multiple providers are registered). Uses `Customer` model and `Helper` methods from the shared Base project.

## Package References

- Jeninnet.Validation
- Jeninnet.Validation.Localization.Memory
- Jeninnet.Validation.Localization.Json
- Jeninnet.Validation.Localization.ResX
- Jeninnet.Validation.Localization.Database.Sqlite
- Jeninnet.Validation.Sample.Localization.Base

## Scenarios

### Scenario 1: Key-coverage fallback

**Chain:** Memory → Json → ResX

| Provider | Keys Covered | Behavior |
|----------|-------------|----------|
| Memory   | NotEmpty     | First-provider hit for NotEmpty |
| Json     | NotEmpty, Email, GreaterThan | Fallback when Memory lacks the key |
| ResX     | All 4 keys (culture-aware) | Fallback when both Memory & Json lack the key |

### Scenario 2: Culture-coverage fallback

**Chain:** Sqlite → Json → ResX

| Provider | Keys + Cultures | Behavior |
|----------|----------------|----------|
| Sqlite   | NotEmpty, Email (en, ar, nl) | First-provider hit for NotEmpty/Email |
| Json     | NotEmpty, Email, GreaterThan (en, ar only via CultureFilter) | Fallback for GreaterThan(ar); misses for nl |
| ResX     | All 4 keys (en, ar, nl) | Fallback when both Sqlite & Json miss |

## Key Concepts

- **First-provider hit:** The first provider in registration order that returns a message wins.
- **Fallback (missing key):** If the provider doesn't have the key, the composite moves to the next.
- **Fallback (missing culture):** Only relevant for culture-aware providers (Sqlite, ResX). For culture-agnostic providers (Memory, Json), if they have the key, they return the message regardless of culture.
- **Missing key:** If no provider has the key, `IValidationMessageLocalizer` uses the `DefaultCulture` and falls back to `key.ToString()`.

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.Localization.Composite
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
