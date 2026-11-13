//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
var services = new ServiceCollection();

services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(services);

services.AddResXLocalization(options => {
    options.BaseName = "Jeninnet.Validation.Sample.Localization.ResX.Resources.ValidationMessages";
    options.ResourceAssembly = Assembly.GetExecutingAssembly();
});

var provider = services.BuildServiceProvider();
var localizer = provider.GetRequiredService<IMessageLocalizer>();

// To support the Arabic language
Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("=== RESX LOCALIZATION SHOWCASE ===");
Console.WriteLine();

// ---- Part 1: AddResXLocalization() with embedded resources ----
Console.WriteLine("--- Part 1: AddResXLocalization() API ---");
Console.WriteLine("Message resolution by locale (ResX satellite assemblies):");

await Helper.ShowMessageForUserAsync(
    localizer,
    [
        new(Constants.EN_US_CULTURE),
        new(Constants.AR_SY_CULTURE),
        new(Constants.NL_NL_CULTURE)
    ]
);

Console.WriteLine();

Console.WriteLine("Validation with localized error messages:");

await Helper.ShowLocalizedErrorsAsync(
    UserValidator.Instance,
    localizer,
    User.Invalid,
    [
        new(Constants.EN_US_CULTURE),
        new(Constants.AR_SY_CULTURE),
        new(Constants.NL_NL_CULTURE)
    ]
);

Console.WriteLine();

// ---- Part 2: Manual provider registration ----
// Same embedded .resx files registered directly via IInnerMessageProvider.
// Demonstrates consumer-owned registration without the AddResXLocalization() helper.
Console.WriteLine("--- Part 2: Manual Provider Registration ---");

var manualServices = new ServiceCollection();
manualServices.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(manualServices);
manualServices.AddSingleton<IInnerMessageProvider>(
    new ResXMessageProvider(
        new ResXLocalizationOptions {
            BaseName = "Jeninnet.Validation.Sample.Localization.ResX.Resources.ValidationMessages",
            ResourceAssembly = Assembly.GetExecutingAssembly(),
        }
    )
);

var manualProvider = manualServices.BuildServiceProvider();
var manualLocalizer = manualProvider.GetRequiredService<IMessageLocalizer>();

Console.WriteLine("Standard keys (NotEmpty, Email) and custom keys (Required, Length):");
await Helper.ShowMessageForUserAsync(manualLocalizer, [new(Constants.EN_US_CULTURE)]);
Console.WriteLine();

Console.WriteLine("Fallback to default culture (fr-FR is not shipped, resolves English):");
await Helper.ShowMessageForUserAsync(manualLocalizer, [new CultureInfo("fr-FR")]);
Console.WriteLine();

Console.WriteLine("Done.");
