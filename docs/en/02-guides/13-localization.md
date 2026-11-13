# Localization

Message localization is provided by the sibling `Jeninnet.Localization` pipeline.
`Jeninnet.Validation.Localization` supplies built-in packs for 40 cultures and every
`ValidationMessageKeys` constant. The optional `Jeninnet.Validation.Localization.AspNetCore`
package adds an ASP.NET Core `IStringLocalizer`/resx bridge with explicit culture control.

## Register

```csharp
using Jeninnet.Localization.Abstractions.Options;
using Jeninnet.Localization.Core.Extensions;
using Jeninnet.Validation.Localization;

services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
services.AddDefaultMessagePacks();
```

`AddDefaultMessagePacks` registers the validation message pipeline (`IMessageLocalizer`,
`IMessageProvider`, and `IMessageFormatter`) as well as the packs. Call
`services.AddValidationLocalization()` without packs when the host already supplies its own
`IInnerMessageProvider`, or call `JeninnetLocalizationExtensions.AddLocalization(services)` first
when the host wants the sibling platform's store-backed `IStringLocalizerFactory`.

The platform enforces **exactly one** `IInnerMessageProvider`. Add custom packs or overrides through
the pack options instead of registering a second provider:

```csharp
services.AddDefaultMessagePacks(options =>
{
    options.Override("en", ValidationMessageKeys.NOT_NULL, "This field is required.");
    options.AddPack("pl", new Dictionary<string, string>
    {
        [ValidationMessageKeys.NOT_NULL] = "{PropertyName} jest wymagane."
    });
});
```

Custom packs may be partial; missing keys fall back per key. Registering another provider
alongside the default packs throws an `InvalidOperationException`.
Overrides target the culture name you pass; use `pt-BR` to override the Brazilian pack and `pt` for
neutral Portuguese, and use `zh-Hans` for the Simplified Chinese pack.

## Built-in cultures

The package ships `en`, `ar`, `fr`, `es`, `de`, `tr`, `ur`, `fa`, `hi`, `zh-Hans`, `ja`, `pt-BR`,
`ru`, `nl`, `it`, `pl`, `uk`, `ko`, `vi`, `th`, `id`, `cs`, `sv`, `da`, `fi`, `no`, `el`, `he`,
`hu`, `ro`, `sk`, `sl`, `hr`, `sr`, `bg`, `lt`, `lv`, `et`, `ca`, and `af`. Each pack
covers every `ValidationMessageKeys` constant and preserves the placeholders of the English
catalog. `ar`, `he`, `ur`, and `fa` are RTL; the package supplies text only, and the host owns UI
direction. Initial translations are machine-assisted drafts; native-speaker review is tracked
before the stable release. `no` is Norwegian Bokmål; Nynorsk is not included.

Culture names keep script and region subtags. `pt-BR` is distinct from neutral `pt`, and
`zh-Hans` is reached from `zh-CN` through the pipeline's parent chain.

## Dynamic messages

`AddDefaultMessagePacks` also registers `IValidationMessageCatalog`, a thread-safe catalog layered
over the built-in packs. Use it to add a language, override individual messages, or remove
templates/cultures at runtime:

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

Changes take effect for the next lookup; reads are lock-free copy-on-write snapshots. The
catalog is process-local and in-memory: changes do not survive a restart and are not shared
between instances. For persistent or multi-instance messages, register an `ILocalizationStore`
and use `AddLocalization`/`AddJeninnetStringLocalizer` instead. Removing a culture or key falls
back through the normal pipeline and finally to `ValidationError.Message`.

The catalog is the supported way to extend the built-in provider; do not register a second
`IInnerMessageProvider` (the platform enforces exactly one).

## Localize an error

```csharp
using System.Globalization;

var localizer = provider.GetRequiredService<IMessageLocalizer>();

foreach (var error in result.Errors)
{
    var message = await localizer.LocalizeValidationErrorAsync(
        error,
        new CultureInfo("ar"));
}
```

`LocalizeValidationErrorAsync` merges the rule parameters and uses an existing `{PropertyName}`
parameter when one is present (for example from `WithName`, a dynamic display name, or
`ValidationOptions.DisplayNameResolver`/`DisplayNameResolverForInstance`); otherwise it injects
`ValidationError.MemberName`. The instance-aware resolver receives the model currently being
validated and takes precedence over the legacy resolver.

## ASP.NET Core resx bridge

The optional `Jeninnet.Validation.Localization.AspNetCore` package resolves messages through
ASP.NET Core `IStringLocalizerFactory` before falling back to the packs:

```csharp
using Jeninnet.Validation.Localization.AspNetCore;

services.AddJeninnetStringLocalizer(options =>
{
    options.ResourceType = typeof(SharedResources);
    options.DefaultCulture = "en";
});

var resolver = provider.GetRequiredService<IValidationMessageResolver>();
var message = await resolver.ResolveAsync(error, new CultureInfo("ar"));
```

`ResourceType` is the type whose full name is the resx base name. `AddJeninnetStringLocalizer`
registers the default localization services, preserves a factory that was already registered, and
never registers a second `IInnerMessageProvider`. When the sibling store-backed factory from
`Jeninnet.Localization.Core` is active, register a store provider.

Resolution order with the bridge enabled:

1. An authored `ValidationError.Message` (`MessageIsAuthored` is true).
2. `IStringLocalizer[error.MessageKey]` from the configured resource type.
3. The registered `IMessageLocalizer` (the packs in this guide).
4. `ValidationError.Message`.

`IStringLocalizer` has no culture parameter. The bridge creates and reads the localizer inside a
scoped `CultureInfo.CurrentUICulture` assignment and restores the original value immediately;
`CurrentCulture` is never changed. `ForceCulture` overrides the caller-supplied culture,
`DefaultCulture` applies when no culture is supplied, and otherwise `CurrentUICulture` is used.
`DisableLocalization = true` skips the resx source and keeps the pack/message fallback;
`ResourceType = null` does the same when resx is not used.

Templates use the same named placeholders as the packs. Every error exposes `{PropertyName}`,
`{PropertyValue}`, and `{PropertyPath}`; rule families add semantic names such as
`{ComparisonValue}`, `{ComparisonProperty}`, `{Min}`, `{Max}`, `{From}`, `{To}`, `{TotalLength}`,
`{Length}`, `{ExactLength}`, `{Pattern}`, `{Extensions}`, `{Values}`, `{EnumType}`, `{Count}`,
`{Precision}`, `{Scale}`, `{Value}`, `{Country}`, `{Policy}`, `{MinLength}`,
`{MissingRequirements}`, and `{CollectionIndex}`. The old positional keys (`{0}`,
`{1}`) remain for compatibility; see the
[message parameter table](../03-reference/02-rule-reference.md#message-parameters).

The bridge uses a registered `ICultureAwareMessageFormatter`/`IMessageFormatter` when available,
otherwise a small named-placeholder formatter with explicit culture.

## Message precedence

With the resx bridge enabled, a template for `ValidationError.MessageKey` is resolved in the order
resx → pack → `ValidationError.Message`. Without the bridge, the order is pack →
`ValidationError.Message`. `ValidationError.Message` is the fallback when the error has no key or no
template is found. Use a resx entry or a pack override to change localized text; `WithMessage`
(static or dynamic) alone does not override an existing template.

## Culture fallback

The sibling pipeline walks exact culture → parent → two-letter code → invariant →
`DefaultCulture`. The provider checks the exact culture name first, then the two-letter language
code, so `fr-FR` resolves through `fr` while `pt-BR` and `zh-Hans` stay distinct. The resx bridge
uses the same requested culture for the `IStringLocalizer` lookup and the pack fallback. A missing
template falls back to `ValidationError.Message`.

## ICU plural messages

ICU plural/select templates are opt-in through the sibling `IcuMessageFormatter`. Register it before
`AddLocalization`; the validation pipeline resolves the registered `IMessageFormatter` through DI.
The supported subset and examples are in the sibling documentation.

## Message keys

Use `ValidationMessageKeys` constants rather than string literals. Built-in keys cover presence,
comparison, strings, dates, collections, and custom rules. See the [rule reference](../03-reference/02-rule-reference.md).

`ValidationMessageKeys` is the single source of truth for the built-in catalog: every constant
must have an entry in every `Resources/*.json` pack, and every pack entry must map to a
constant. The build-time gate enforces both directions:

```powershell
pwsh scripts/localization/check-message-keys.ps1
```

The CI `message-keys` job runs the same script and uploads the deterministic report from
`artifacts/message-keys/`; `MessageKeyCatalogTests` repeats the comparison inside the test
suite. Custom keys (rule-template names, caller-supplied codes, resx keys) stay outside the
catalog by design; only a pack entry that is deliberate without a constant belongs in
`MessageKeyAllowList.json`, and stale entries fail the check. See the
[localization scripts README](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/scripts/localization/README.md).

## Verify

```csharp
var arabic = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("ar"));
var french = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("fr-FR"));
var chinese = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("zh-CN"));
```

Confirm the Arabic, French, and Simplified Chinese packs resolve and that an unknown culture falls
back to the default culture.

## Adding a language

Add a new `Resources/<culture>.json` file with every key and the same placeholders as `en.json`.
The provider discovers embedded packs automatically, and the catalog test enforces key
completeness, placeholder parity, non-empty values, and RTL text for `ar`, `he`, `ur`, and `fa`. A
native-speaker review is required before merging; see the package README for the contribution
checklist. For a runtime-only language, use `IValidationMessageCatalog.AddPack` instead of adding a
resource file.

## Related

- [Rules and messages](03-rules-and-messages.md)
- [Package map](../appendix/02-package-map.md)
- [Localization package README](https://github.com/TarekNajem04/Jeninnet.Validation/tree/main/src/Jeninnet.Validation.Localization)
