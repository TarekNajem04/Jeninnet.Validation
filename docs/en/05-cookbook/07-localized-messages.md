# Localized Validation Messages

## Problem

Validation messages must be displayed in several cultures, with custom overrides for specific keys.

## Solution

```csharp
using Jeninnet.Localization.Abstractions.Options;
using Jeninnet.Localization.Core.Extensions;
using Jeninnet.Validation.Localization;

services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(services);

services.AddDefaultMessagePacks(options =>
{
    options.Override("en", ValidationMessageKeys.NOT_NULL, "This field is required.");
    options.AddPack("nl", new Dictionary<string, string>
    {
        [ValidationMessageKeys.NOT_NULL] = "{PropertyName} is verplicht."
    });
});
```

The built-in packs cover `en`, `ar`, `fr`, `es`, `de`, `tr`, `ur`, `fa`, `hi`, `zh-Hans`, `ja`,
`pt-BR`, and `ru`.

Resolve an error:

```csharp
var localizer = provider.GetRequiredService<IMessageLocalizer>();

foreach (var error in result.Errors)
{
    var message = await localizer.LocalizeValidationErrorAsync(
        error,
        new CultureInfo("ar"));
}
```

`LocalizeValidationErrorAsync` uses `{PropertyName}` from the rule (static/dynamic `WithName`, a
display-name resolver, or `ValidationError.MemberName`) and merges the rule parameters.

## Resx through IStringLocalizer

When the messages live in `.resx` resources instead of packs, add the ASP.NET Core bridge:

```csharp
using Jeninnet.Validation.Localization.AspNetCore;

services.AddJeninnetStringLocalizer(options =>
{
    options.ResourceType = typeof(SharedResources);
    options.DefaultCulture = "en";
});

var resolver = provider.GetRequiredService<IValidationMessageResolver>();
var arabic = await resolver.ResolveAsync(error, new CultureInfo("ar"));
```

The resolver checks `IStringLocalizer[error.MessageKey]` first, then the registered packs, then
`ValidationError.Message`. `ResourceType` is the type whose full name is the resx base name.
`ForceCulture` overrides the supplied culture, `DefaultCulture` applies when none is supplied, and
`DisableLocalization` skips the resx path. Culture control is scoped to the lookup; the global UI
culture is restored immediately and `CurrentCulture` is never changed.

## ASP.NET Core response

```csharp
app.MapPost("/users", async (CreateUser request, CreateUserValidator validator,
    IMessageLocalizer localizer) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        var messages = new Dictionary<string, string[]>();

        foreach (var group in result.Errors.GroupBy(e => e.MemberName))
        {
            var localized = new List<string>();
            foreach (var error in group)
            {
                localized.Add(await localizer.LocalizeValidationErrorAsync(
                    error,
                    CultureInfo.CurrentUICulture));
            }
            messages[group.Key] = [.. localized];
        }

        return Results.ValidationProblem(messages);
    }

    return Results.Ok();
});
```

## Exactly one provider

The localization platform enforces one `IInnerMessageProvider`. Registering another provider
alongside the default packs throws. Add custom packs/overrides through the options callback. The
`Jeninnet.Validation.Localization.AspNetCore` bridge is a reader and does not register a provider.

## Verify

- Request with `Accept-Language: ar` returns Arabic messages.
- `fr-FR` returns French and `zh-CN` returns Simplified Chinese through the parent chain.
- An unknown culture falls back to the default culture.
- A custom override wins over the built-in template for the same key.
- With the bridge enabled, a resx entry wins over a pack template for the same key, and a missing
  resx key falls back to the pack.

## Related

- [Localization guide](../02-guides/13-localization.md)
- [Rules and messages](../02-guides/03-rules-and-messages.md)
- [Integration packages](../03-reference/05-integration-packages.md)
