//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
var baseDir = AppContext.BaseDirectory;

Console.WriteLine("=== XML LOCALIZATION SHOWCASE ===");
Console.WriteLine("XML files contain both custom keys (Required, Email, Length, Range)");
Console.WriteLine("and standard keys (NotEmpty, Email, GreaterThan, InclusiveBetween).");
Console.WriteLine();

// ============================================================
// Part 1: Simple AddXmlLocalization() with a single file
// ============================================================
Console.WriteLine("--- Part 1: AddXmlLocalization() API ---");
Console.WriteLine("Single XML file loaded into a flat dictionary.");
Console.WriteLine("Culture parameter is ignored by the provider.");
Console.WriteLine();

var s1 = new ServiceCollection();
s1.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(s1);
s1.AddXmlLocalization(Path.Combine(baseDir, "Messages", "en.xml"));

var sp1 = s1.BuildServiceProvider();
var localizer1 = sp1.GetRequiredService<IMessageLocalizer>();

Console.WriteLine("Same English file regardless of culture:");

await Helper.ShowMessageAsync(localizer1, nameof(Product.Name), Constants.NOT_EMPTY_MESSAGE_KEY, new(Constants.EN_US_CULTURE));
await Helper.ShowMessageAsync(localizer1, nameof(Product.Name), Constants.NOT_EMPTY_MESSAGE_KEY, new(Constants.AR_SY_CULTURE));
await Helper.ShowMessageAsync(localizer1, nameof(Product.Price), Constants.GREATER_THAN_MESSAGE_KEY, new(Constants.EN_US_CULTURE));
await Helper.ShowMessageAsync(localizer1, nameof(Product.Price), Constants.GREATER_THAN_MESSAGE_KEY, new(Constants.AR_SY_CULTURE));

Console.WriteLine();

// ============================================================
// Part 2: Multi-culture registration using the library's CultureAwareMessageProvider
// (replaces the previously duplicated custom CultureAwareXmlProvider)
// ============================================================
Console.WriteLine("--- Part 2: Culture-Aware Wrapper ---");
Console.WriteLine("Selects the per-culture XML file at runtime.");
Console.WriteLine("Falls back to English when a key is missing.");
Console.WriteLine();

var s2 = new ServiceCollection();
s2.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(s2);

var enProvider = new XmlMessageProvider(Path.Combine(baseDir, "Messages", "en.xml"));
var arProvider = new XmlMessageProvider(Path.Combine(baseDir, "Messages", "ar.xml"));

s2.AddSingleton<IInnerMessageProvider>(
    new CultureAwareMessageProvider(new Dictionary<string, IMessageProvider> {
        ["en"] = enProvider,
        ["ar"] = arProvider,
    })
);

var sp2 = s2.BuildServiceProvider();
var localizer2 = sp2.GetRequiredService<IMessageLocalizer>();

Console.WriteLine("Per-culture XML files with culture-aware wrapper:");
Console.WriteLine("Custom keys (Required, Email, Length, Range):");

await Helper.ShowMessageAsync(localizer1, nameof(Product.Name), Constants.REQUIRED_MESSAGE_KEY, new(Constants.EN_US_CULTURE), from: 1, to: 100);
await Helper.ShowMessageAsync(localizer1, nameof(Product.Name), Constants.REQUIRED_MESSAGE_KEY, new(Constants.AR_SY_CULTURE), from: 1, to: 100);
await Helper.ShowMessageAsync(localizer1, nameof(Product.Price), Constants.RANGE_MESSAGE_KEY, new(Constants.EN_US_CULTURE), from: 1, to: 100);
await Helper.ShowMessageAsync(localizer1, nameof(Product.Price), Constants.RANGE_MESSAGE_KEY, new(Constants.AR_SY_CULTURE), from: 1, to: 100);
await Helper.ShowMessageAsync(localizer1, nameof(Product.Quantity), Constants.INCLUSIVE_BETWEEN_MESSAGE_KEY, new(Constants.EN_US_CULTURE), from: 1, to: 100);
await Helper.ShowMessageAsync(localizer1, nameof(Product.Quantity), Constants.INCLUSIVE_BETWEEN_MESSAGE_KEY, new(Constants.AR_SY_CULTURE), from: 1, to: 100);

Console.WriteLine();
Console.WriteLine("Standard keys (NotEmpty, GreaterThan, InclusiveBetween):");

await Helper.ShowMessageAsync(localizer1, nameof(Product.Name), Constants.NOT_EMPTY_MESSAGE_KEY, new(Constants.EN_US_CULTURE), from: 1, to: 100);
await Helper.ShowMessageAsync(localizer1, nameof(Product.Name), Constants.NOT_EMPTY_MESSAGE_KEY, new(Constants.AR_SY_CULTURE), from: 1, to: 100);
await Helper.ShowMessageAsync(localizer1, nameof(Product.Price), Constants.GREATER_THAN_MESSAGE_KEY, new(Constants.EN_US_CULTURE), from: 1, to: 100);
await Helper.ShowMessageAsync(localizer1, nameof(Product.Price), Constants.GREATER_THAN_MESSAGE_KEY, new(Constants.AR_SY_CULTURE), from: 1, to: 100);
await Helper.ShowMessageAsync(localizer1, nameof(Product.Quantity), Constants.INCLUSIVE_BETWEEN_MESSAGE_KEY, new(Constants.EN_US_CULTURE), from: 1, to: 100);
await Helper.ShowMessageAsync(localizer1, nameof(Product.Quantity), Constants.INCLUSIVE_BETWEEN_MESSAGE_KEY, new(Constants.AR_SY_CULTURE), from: 1, to: 100);

Console.WriteLine();

// ---- Part 3: Validation with localized messages ----
Console.WriteLine("--- Part 3: Validation with XML Localization ---");

var validator = new ProductValidator();
var invalidProduct = new Product("", -5, 0);

await Helper.ShowLocalizedErrorsAsync(
    validator,
    localizer2,
    invalidProduct,
    [
        new(Constants.EN_US_CULTURE),
        new(Constants.AR_SY_CULTURE)
    ]
);

// ---- Part 4: Missing-key fallback ----
Console.WriteLine("--- Part 4: Missing-Key Fallback ---");
Console.WriteLine("Requesting a key that doesn't exist in any XML file:");

var fallbackMsg = await localizer2.LocalizeAsync(
    "MissingKey",
    new Dictionary<string, object?> {
        ["PropertyName"] = "Test",
    },
    new CultureInfo("en-US")
);

Console.WriteLine($"  Fallback result: '{fallbackMsg}'");
Console.WriteLine("  (falls back through culture hierarchy, then returns the key as string)");
Console.WriteLine();

Console.WriteLine("Done.");
