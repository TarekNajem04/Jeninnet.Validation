//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
var services = new ServiceCollection();

services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(services);

// Manual registration with inline dictionaries (MemoryMessageProvider equivalent)
// Each culture has its own message set; CultureAwareProvider resolves by culture hierarchy.
// For file-based alternatives, see AddJsonLocalizationProviders() or AddXmlLocalizationProviders().
services.AddSingleton<IInnerMessageProvider>(
    new CultureAwareMessageProvider(new Dictionary<string, IMessageProvider> {
        ["en"] = new MemoryMessageProvider(new MemoryLocalizationOptions {
            Messages = new Dictionary<string, string> {
                [Constants.NOT_EMPTY_MESSAGE_KEY] = "{PropertyName} must not be empty.",
                [Constants.EMAIL_MESSAGE_KEY] = "{PropertyName} is not a valid email address.",
                [Constants.GREATER_THAN_MESSAGE_KEY] = "{PropertyName} must be greater than {ComparisonValue}.",
                [Constants.INCLUSIVE_BETWEEN_MESSAGE_KEY] = "{PropertyName} must be between {From} and {To}.",
            },
        }),
        ["nl"] = new MemoryMessageProvider(new MemoryLocalizationOptions {
            Messages = new Dictionary<string, string> {
                [Constants.NOT_EMPTY_MESSAGE_KEY] = "{PropertyName} mag niet leeg zijn.",
                [Constants.EMAIL_MESSAGE_KEY] = "{PropertyName} is geen geldig e-mailadres.",
                [Constants.GREATER_THAN_MESSAGE_KEY] = "{PropertyName} moet groter zijn dan {ComparisonValue}.",
                [Constants.INCLUSIVE_BETWEEN_MESSAGE_KEY] = "{PropertyName} moet tussen {From} en {To} liggen.",
            },
        }),
        ["fr"] = new MemoryMessageProvider(new MemoryLocalizationOptions {
            Messages = new Dictionary<string, string> {
                [Constants.NOT_EMPTY_MESSAGE_KEY] = "{PropertyName} ne doit pas être vide.",
                [Constants.EMAIL_MESSAGE_KEY] = "{PropertyName} n'est pas une adresse e-mail valide.",
                [Constants.GREATER_THAN_MESSAGE_KEY] = "{PropertyName} doit être supérieur à {ComparisonValue}.",
                [Constants.INCLUSIVE_BETWEEN_MESSAGE_KEY] = "{PropertyName} doit être compris entre {From} et {To}.",
            },
        }),
        ["de"] = new MemoryMessageProvider(new MemoryLocalizationOptions {
            Messages = new Dictionary<string, string> {
                [Constants.NOT_EMPTY_MESSAGE_KEY] = "{PropertyName} darf nicht leer sein.",
                [Constants.EMAIL_MESSAGE_KEY] = "{PropertyName} ist keine gültige E-Mail-Adresse.",
                [Constants.GREATER_THAN_MESSAGE_KEY] = "{PropertyName} muss größer als {ComparisonValue} sein.",
                [Constants.INCLUSIVE_BETWEEN_MESSAGE_KEY] = "{PropertyName} muss zwischen {From} und {To} liegen.",
            },
        }),
    }));

var provider = services.BuildServiceProvider();
var localizer = provider.GetRequiredService<IMessageLocalizer>();

Console.WriteLine("=== LOCALIZATION SHOWCASE ===");
Console.WriteLine();

Console.WriteLine("Message resolution by locale (fallback through culture hierarchy):");
await Helper.ShowMessageForUserAsync(
    localizer,
    [
        new(Constants.EN_US_CULTURE),
        new(Constants.EN_GB_CULTURE),
        new(Constants.NL_NL_CULTURE),
        new(Constants.NL_BE_CULTURE),
        new(Constants.FR_FR_CULTURE),
        new(Constants.FR_CA_CULTURE),
        new(Constants.DE_DE_CULTURE),
        new(Constants.DE_DE_CULTURE),
        new(Constants.AR_SY_CULTURE),
    ]
);

Console.WriteLine();

// --- Demonstrate integration with validation ---
Console.WriteLine("Validation with localized error messages:");

await Helper.ShowLocalizedErrorsAsync(
    UserValidator.Instance,
    localizer,
    User.Invalid,
    [
        new(Constants.EN_US_CULTURE),
        new(Constants.EN_GB_CULTURE),
        new(Constants.NL_NL_CULTURE),
        new(Constants.NL_BE_CULTURE),
        new(Constants.FR_FR_CULTURE),
        new(Constants.FR_CA_CULTURE),
        new(Constants.DE_DE_CULTURE),
        new(Constants.DE_DE_CULTURE),
        new(Constants.AR_SY_CULTURE),
    ]
);

Console.WriteLine("Done.");
