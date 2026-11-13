# RESX Localization Sample

Demonstrates Jeninnet.Validation localization using embedded RESX resources with satellite assemblies.

## Parts

- **Part 1 — `AddResXLocalization()`**: embedded `.resx` files resolved through `ResourceManager` with satellite assemblies, followed by validation with localized messages.
- **Part 2 — Manual registration**: the same embedded resources registered directly as `IInnerMessageProvider` via `new ResXMessageProvider(...)`, including custom-key lookup and default-culture fallback.

## Highlights

- Embedded `.resx` files with satellite assemblies for English (en), Arabic (ar), and Dutch (nl)
- Standard keys (`NotEmpty`, `Email`, ...) and custom keys (`Required`, `Length`, `Range`, ...)
- Culture-aware lookup and fallback through the culture hierarchy
- Shared `User` model and constants from the Base project (`Jeninnet.Validation.Sample.Localization.Base`)

## Package References

- Jeninnet.Validation
- Jeninnet.Localization.Core
- Jeninnet.Localization.Providers.ResX
- Jeninnet.Validation.Sample.Localization.Base

## Folder Structure

```
Resources/
  ValidationMessages.resx      English messages (default/neutral)
  ValidationMessages.ar.resx   Arabic satellite assembly
  ValidationMessages.nl.resx   Dutch satellite assembly
Program.cs                     DI setup, message lookup, validation demo
GlobalUsings.cs                Global using directives
```

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.Localization.ResX
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
