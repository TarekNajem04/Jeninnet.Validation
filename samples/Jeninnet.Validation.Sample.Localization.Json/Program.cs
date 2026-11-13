//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
var baseDir = AppContext.BaseDirectory;

// To support the Arabic language
Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("=== JSON LOCALIZATION SHOWCASE ===");
Console.WriteLine("JSON files contain both custom keys (Required, Email, Length, Range)");
Console.WriteLine("and standard keys (NotEmpty, Email, Length) for auto-lookup.");
Console.WriteLine();

// ============================================================
// Part 1: Simple AddJsonLocalization() with a single file
// ============================================================
Console.WriteLine("--- Part 1: AddJsonLocalization() API ---");
Console.WriteLine("Single JSON file loaded into a flat dictionary.");
Console.WriteLine("Culture parameter is ignored by the provider.");
Console.WriteLine();

var s1 = new ServiceCollection();

s1.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(s1);
s1.AddJsonLocalization(Path.Combine(baseDir, "Messages", "en.json"));

var serviceProvider = s1.BuildServiceProvider();
var localizer = serviceProvider.GetRequiredService<IMessageLocalizer>();

Console.WriteLine("Same English file regardless of culture:");
await Helper.ShowMessageForUserAsync(localizer, [new(Constants.EN_US_CULTURE), new(Constants.AR_SY_CULTURE)]);

Console.WriteLine();

// ============================================================
// Part 2: Multi-culture with AddJsonLocalizationProviders()
// ============================================================
Console.WriteLine("--- Part 2: AddJsonLocalizationProviders() API ---");
Console.WriteLine("Registers per-culture JSON files in one call.");
Console.WriteLine("Uses CultureAwareMessageProvider internally");
Console.WriteLine("to walk the culture hierarchy at resolution time.");
Console.WriteLine();

var s2 = new ServiceCollection();
s2.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(s2);

#pragma warning disable CA1861
// Uses explicit culture list overload: resolves {baseDir}/Messages/{culture}.json
s2.AddJsonLocalizationProviders(
    baseDirectory: baseDir,
    cultures: ["en", "ar", "nl"],
    subdirectory: "Messages"
);

var sp2 = s2.BuildServiceProvider();
var localizer2 = sp2.GetRequiredService<IMessageLocalizer>();

Console.WriteLine("Per-culture JSON files with culture-aware wrapper:");
Console.WriteLine("Custom keys (Required, Length):");

await Helper.ShowMessageForUserAsync(localizer2, [new(Constants.EN_US_CULTURE), new(Constants.AR_SY_CULTURE)]);

Console.WriteLine();

// ============================================================
// Part 3: Manual provider instantiation
// ============================================================
Console.WriteLine("--- Part 3: Manual Provider Setup ---");
Console.WriteLine("Each JsonMessageProvider is created explicitly.");
Console.WriteLine("CultureAwareMessageProvider (library) wraps them.");
Console.WriteLine();

var s3 = new ServiceCollection();
s3.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(s3);

var enProvider = new JsonMessageProvider(Path.Combine(baseDir, "Messages", "en.json"));
var arProvider = new JsonMessageProvider(Path.Combine(baseDir, "Messages", "ar.json"));
var nlProvider = new JsonMessageProvider(Path.Combine(baseDir, "Messages", "nl.json"));

s3.AddSingleton<IInnerMessageProvider>(
    new CultureAwareMessageProvider(
        new Dictionary<string, IMessageProvider> {
            ["en"] = enProvider,
            ["ar"] = arProvider,
            ["nl"] = nlProvider,
        }
    )
);

var sp3 = s3.BuildServiceProvider();
var localizer3 = sp3.GetRequiredService<IMessageLocalizer>();

await Helper.ShowMessageForUserAsync(
    localizer3,
    [
        new(Constants.EN_US_CULTURE),
        new(Constants.AR_SY_CULTURE),
        new(Constants.NL_NL_CULTURE)
    ]
);

Console.WriteLine();

// ============================================================
// Part 4: Builder pattern
// ============================================================
Console.WriteLine("--- Part 4: Builder Pattern ---");
Console.WriteLine("Uses AddJsonLocalizationProviders(Action<builder>)");
Console.WriteLine("for per-culture file path control.");
Console.WriteLine();

var s4 = new ServiceCollection();

s4.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(s4);

s4.AddJsonLocalizationProviders(
    builder => builder.AddCulture("en", Path.Combine(baseDir, "Messages", "en.json"))
                      .AddCulture("ar", Path.Combine(baseDir, "Messages", "ar.json"))
                      .AddCulture("nl", Path.Combine(baseDir, "Messages", "nl.json"))
);

var sp4 = s4.BuildServiceProvider();
var localizer4 = sp4.GetRequiredService<IMessageLocalizer>();

await Helper.ShowMessageForUserAsync(
    localizer4,
    [
        new(Constants.EN_US_CULTURE),
        new(Constants.AR_SY_CULTURE),
        new(Constants.NL_NL_CULTURE)
    ]
);

Console.WriteLine();

// ---- Part 5: Validation with localized messages ----
Console.WriteLine("--- Part 5: Validation with JSON Localization ---");

await Helper.ShowLocalizedErrorsAsync(
    UserValidator.Instance,
    localizer4,
    User.Invalid,
    [
        new(Constants.EN_US_CULTURE),
        new(Constants.AR_SY_CULTURE),
        new(Constants.NL_NL_CULTURE)
    ]
);

// ---- Part 6: Missing-key fallback ----
Console.WriteLine("--- Part 6: Missing-Key Fallback ---");
Console.WriteLine("Requesting a key that doesn't exist in any JSON file:");

var fallbackMsg = await localizer4.LocalizeAsync(
    "NonExistentKey",
    new Dictionary<string, object?> {
        ["PropertyName"] = "Test",
    },
    new CultureInfo(Constants.EN_US_CULTURE)
);

Console.WriteLine($"  Fallback result: '{fallbackMsg}'");
Console.WriteLine("  (falls back through culture hierarchy, returns empty if not found)");
Console.WriteLine();
Console.WriteLine("Done.");
