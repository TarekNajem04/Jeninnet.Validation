# JSON Localization Sample

Demonstrates loading validation messages from JSON files using the JSON localization provider.

## Parts

- **Part 1 — `AddJsonLocalization()`**: single JSON file loaded into a flat dictionary (culture parameter ignored by the provider).
- **Part 2 — `AddJsonLocalizationProviders(baseDirectory, cultures, subdirectory)`**: per-culture files (`Messages/{culture}.json`) wrapped in the library's `CultureAwareMessageProvider`.
- **Part 3 — Manual registration**: each `JsonMessageProvider` created explicitly and composed with `CultureAwareMessageProvider` via `IInnerMessageProvider`.
- **Part 4 — Builder pattern**: `AddJsonLocalizationProviders(builder => ...)` for per-culture file path control.
- **Part 5 — Validation**: end-to-end validation with localized error messages.
- **Part 6 — Missing-key fallback**: behavior for unknown keys.

## Package References

- Jeninnet.Validation
- Jeninnet.Localization.Core
- Jeninnet.Localization.Providers.Json
- Jeninnet.Validation.Sample.Localization.Base

## Folder Structure

```
Messages/
  en.json                      English messages
  ar.json                      Arabic messages
  nl.json                      Dutch messages
Program.cs                     DI setup, message lookup, validation demo (6 parts)
GlobalUsings.cs                Global using directives
```

## Running

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.Localization.Json
```

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.Localization.Json/Jeninnet.Validation.Sample.Localization.Json.csproj
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
