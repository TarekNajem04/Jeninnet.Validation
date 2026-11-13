# Jeninnet.Validation.Localization

Built-in validation message packs for `Jeninnet.Validation`, registered through the sibling
`Jeninnet.Localization` provider pipeline. The package ships **40 cultures**: English (`en`), Arabic
(`ar`), French (`fr`), Spanish (`es`), German (`de`), Turkish (`tr`), Urdu (`ur`), Persian (`fa`),
Hindi (`hi`), Simplified Chinese (`zh-Hans`), Japanese (`ja`), Brazilian Portuguese (`pt-BR`),
Russian (`ru`), Dutch (`nl`), Italian (`it`), Polish (`pl`), Ukrainian (`uk`), Korean (`ko`),
Vietnamese (`vi`), Thai (`th`), Indonesian (`id`), Czech (`cs`), Swedish (`sv`), Danish (`da`),
Finnish (`fi`), Norwegian Bokmål (`no`), Greek (`el`), Hebrew (`he`), Hungarian (`hu`), Romanian
(`ro`), Slovak (`sk`), Slovenian (`sl`), Croatian (`hr`), Serbian (`sr`), Bulgarian (`bg`),
Lithuanian (`lt`), Latvian (`lv`), Estonian (`et`), Catalan (`ca`), and Afrikaans (`af`). Initial
translations are machine-assisted drafts; native-speaker review is tracked before the stable
release, and corrections are welcome through the contribution path. Norwegian Nynorsk is out of
scope; `no` is Bokmål.

## Requirements

- .NET 11.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```xml
<PackageReference Include="Jeninnet.Validation.Localization" Version="1.0.0-preview.1" />
```

## Register

The sibling localization platform enforces **exactly one** registered message provider. Add the
pack as that provider:

```csharp
using Jeninnet.Localization.Abstractions.Options;
using Jeninnet.Localization.Core.Extensions;
using Jeninnet.Validation.Localization;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(services);

services.AddDefaultMessagePacks();
```

## Override and extend

Use the options callback instead of registering a second provider:

```csharp
services.AddDefaultMessagePacks(options =>
{
    // Per-key override (consumer wins over the pack)
    options.Override("en", ValidationMessageKeys.NOT_NULL, "This field is required.");

    // A custom pack for a culture that is not built in; missing keys fall back per key
    options.AddPack("pl", new Dictionary<string, string>
    {
        [ValidationMessageKeys.NOT_NULL] = "{PropertyName} jest wymagane.",
        [ValidationMessageKeys.NOT_EMPTY] = "{PropertyName} nie może być pusty."
    });
});
```

Overrides target the culture name you pass. Use `pt-BR` to override the Brazilian pack and `pt`
for neutral Portuguese; use `zh-Hans` for Simplified Chinese.

## Localize an error

`ValidationError` carries the message key and the rule parameters; the helper supplies the member
name as `{PropertyName}`:

```csharp
var localizer = services.BuildServiceProvider().GetRequiredService<IMessageLocalizer>();

foreach (var error in result.Errors)
{
    var message = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("ar"));
    Console.WriteLine(message);
}
```

## Fallback

1. Custom packs and per-key overrides configured on `ValidationMessagePackOptions`.
2. The built-in pack for the exact culture name, then the two-letter language code. This keeps
   `pt-BR` and `zh-Hans` distinct while still resolving `fr-FR` through `fr` and `ar-EG` through
   `ar`.
3. The localization pipeline's parent-culture and `DefaultCulture` fallback.

The packs cover every `ValidationMessageKeys` constant in all 40 cultures. Arabic, Hebrew, Urdu,
and Persian are RTL; the Arabic resource is reviewed by `scripts/docs/check-arabic-text.ps1`.

## Dynamic messages

`AddDefaultMessagePacks` registers `IValidationMessageCatalog` alongside the provider. The catalog
is layered over the built-in packs and is safe to update at runtime:

```csharp
var catalog = provider.GetRequiredService<IValidationMessageCatalog>();

catalog.SetTemplate("ar", ValidationMessageKeys.NOT_NULL, "{PropertyName} مطلوب.");
catalog.AddPack("sw", new Dictionary<string, string>
{
    [ValidationMessageKeys.NOT_NULL] = "{PropertyName} inahitajika."
});
catalog.RemoveTemplate("en", ValidationMessageKeys.NOT_EMPTY);
catalog.RemoveCulture("vi");
```

Updates take effect on the next lookup. Reads use lock-free copy-on-write snapshots. The catalog
is in-memory and process-local; use the sibling `ILocalizationStore` when messages must persist or
be shared across instances. Removing a template or culture falls back through the normal pipeline
and finally to `ValidationError.Message`.

## Message-key catalog

`ValidationMessageKeys` is the single source of truth. A build-time gate keeps the constants
and the packs aligned in both directions:

```powershell
pwsh scripts/localization/check-message-keys.ps1
```

The check reads the constants from
`src/Jeninnet.Validation.Abstractions/Configuration/ValidationMessageKeys.cs` and compares them
with every `Resources/*.json` pack per culture. A constant without a pack entry and a pack entry
without a constant both fail the build; the CI `message-keys` job runs the same script and
uploads the deterministic report from `artifacts/message-keys/`. The `MessageKeyCatalogTests`
suite repeats the comparison inside the test run.

Pack keys that are deliberate without a constant (for example a transitional key during a
rename) go in `MessageKeyAllowList.json`. Custom consumer keys need no entry: rule-template keys,
caller-supplied codes, and resx keys live outside the constants and the packs by design. Stale
allow-list entries fail the check; remove the entry when the transition ends. See
[scripts/localization/README.md](../../scripts/localization/README.md).

## ASP.NET Core resx bridge

If messages live in `.resx` resources, add `Jeninnet.Validation.Localization.AspNetCore` and call
`AddJeninnetStringLocalizer(options => options.ResourceType = typeof(SharedResources))`. The bridge
reads `IStringLocalizer[error.MessageKey]` first, then this pack through `IMessageLocalizer`, then
`ValidationError.Message`. It is a reader, not a second `IInnerMessageProvider`. Culture control
(`ForceCulture`, `DefaultCulture`, `DisableLocalization`) is scoped to the lookup and does not
mutate the caller's global culture.

## Adding a language

1. Copy `Resources/en.json` to `Resources/<culture>.json`, where `<culture>` is a valid .NET culture
   name (`it`, `pt-BR`, `zh-Hans`, ...).
2. Translate every value. Keep every key and every placeholder exactly
   (`{PropertyName}`, `{0}`, `{1}`, `{Min}`, `{Max}`, `{Count}`, `{Length}`, `{Extensions}`).
3. Run the catalog tests:
   `dotnet test tests/Jeninnet.Validation.Tests --filter DefaultMessagePackTests`.
   The tests enforce key completeness, placeholder parity with English, non-empty values, and RTL
   script presence for `ar`, `he`, `ur`, and `fa`.
4. Obtain a native-speaker review before merging. Machine translation without review is not
   accepted.
5. Add the culture to the supported-cultures list in the localization guide and CHANGELOG.

The provider discovers every embedded `Resources/*.json` file, so no registration list needs to be
updated.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
