# Jeninnet.Validation.Localization.AspNetCore

Bridges ASP.NET Core `IStringLocalizer`/resx resources into validation messages and adds
explicit culture control without mutating the caller's global culture.

## Requirements

- .NET 10.0 or later (including .NET 11).
- C# 15 when consuming the source contracts.

## Installation

```xml
<PackageReference Include="Jeninnet.Validation.Localization.AspNetCore" Version="1.0.0-preview.1" />
```

## Register

```csharp
using Jeninnet.Validation.Localization.AspNetCore;

builder.Services.AddJeninnetStringLocalizer(options =>
{
    options.ResourceType = typeof(SharedResources);
    options.DefaultCulture = "en";
});
```

`ResourceType` is the type whose full name is the resx base name. `AddJeninnetStringLocalizer`
calls `AddLogging()` and `AddLocalization()` with `TryAdd`, so a factory registered earlier is
preserved. The store-backed factory from `Jeninnet.Localization.Core` requires an
`ILocalizationStore`; register a store provider when that factory is active. The bridge does not
register a second `IInnerMessageProvider`; add the built-in packs separately when needed.

## Resolve

```csharp
public sealed class CreateUserEndpoint(IValidationMessageResolver resolver)
{
    public async Task<IResult> HandleAsync(ValidationResult result, CancellationToken cancellationToken)
    {
        if (result.IsValid)
        {
            return Results.Ok();
        }

        var messages = await resolver.ResolveValidationErrorsAsync(result.Errors, cancellationToken: cancellationToken);
        return Results.ValidationProblem(
            result.Errors
                .Zip(messages, (error, message) => (error.MemberName, Message: message))
                .GroupBy(item => item.MemberName, item => item.Message)
                .ToDictionary(group => group.Key, group => group.ToArray()));
    }
}
```

## Resolution order

1. `IStringLocalizer[error.MessageKey]` for `StringLocalizerValidationOptions.ResourceType`.
2. The registered `IMessageLocalizer`, which serves the built-in or custom message packs.
3. `ValidationError.Message`.

The string-localizer source is skipped when `DisableLocalization` is `true` or `ResourceType` is
`null`. Missing resources are detected through `LocalizedString.ResourceNotFound`, so a localizer
that echoes the key does not hide the pack fallback.

## Culture control

`ForceCulture` wins over the caller-supplied culture, then `DefaultCulture`, then
`CultureInfo.CurrentUICulture`. `ForceCulture` and `DefaultCulture` accept names such as `ar` or
`ar-SA`.

`IStringLocalizer` has no culture parameter. The resolver creates the localizer and reads the
resource inside a scoped `CultureInfo.CurrentUICulture` assignment and restores the original value in
a `finally` block. `CurrentCulture` is never changed and the culture does not leak beyond the call.

## Formatting

Templates use named placeholders such as `{PropertyName}`, `{Min}`, and `{Count}`. The resolver
uses the registered `ICultureAwareMessageFormatter`/`IMessageFormatter` when present, otherwise a
small named-placeholder formatter with explicit culture. Positional `IStringLocalizer[key, args]`
formatting is intentionally not used because validation parameters are named.

## Scope

- Optional package; Core and Abstractions have no ASP.NET Core or localization dependency.
- No machine translation and no RTL rendering.
- `WithMessage` remains a runtime fallback and does not override a resx or pack template for the
  same `MessageKey`.
- The bridge is a reader, not a second message provider.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
