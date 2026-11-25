//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Localization;

/// <summary>
/// RTL/bidi and CLDR cardinal-category conformance for the localization boundary. These tests do
/// not claim native-speaker review; they prove that placeholders and punctuation survive RTL
/// rendering and that the platform's plural rules expose the expected categories.
/// </summary>
[TestClass]
public sealed class LocalizationConformanceTests {
    [TestMethod]
    public async Task RtlMessages_PreserveLatinPlaceholdersAndPunctuationAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Order",
            new MessageId(ValidationMessageKeys.COUNT),
            ValidationMessageKeys.COUNT,
            new Dictionary<string, object> {
                ["Min"] = 1,
                ["Max"] = 3,
                ["Count"] = 5
            });

        var arabic = await localizer.LocalizeValidationErrorAsync(
            error,
            new CultureInfo("ar"),
            TestCancellation) ?? string.Empty;

        MSTestAssertions.That(() => arabic.Should().Contain("Order"));
        MSTestAssertions.That(() => arabic.Should().Contain("1"));
        MSTestAssertions.That(() => arabic.Should().Contain("3"));
        MSTestAssertions.That(() => arabic.Should().Contain("5"));
        MSTestAssertions.That(() => arabic.Should().Contain("؛"));
        MSTestAssertions.That(() => arabic.Any(static character => character is >= '\u0600' and <= '\u06FF')
            .Should().BeTrue());
        MSTestAssertions.That(() => arabic.Contains('\uFFFD').Should().BeFalse());
    }

    [TestMethod]
    public void PluralRules_ExposeCardinalCategoriesForArabicEnglishAndRussian() {
        AssertCategory("ar", 0, PluralCategory.Zero);
        AssertCategory("ar", 1, PluralCategory.One);
        AssertCategory("ar", 2, PluralCategory.Two);
        AssertCategory("ar", 3, PluralCategory.Few);
        AssertCategory("ar", 11, PluralCategory.Many);
        AssertCategory("ar", 100, PluralCategory.Other);

        AssertCategory("en", 1, PluralCategory.One);
        AssertCategory("en", 0, PluralCategory.Other);
        AssertCategory("en", 2, PluralCategory.Other);

        AssertCategory("ru", 1, PluralCategory.One);
        AssertCategory("ru", 2, PluralCategory.Few);
        AssertCategory("ru", 5, PluralCategory.Many);
        AssertCategory("ru", 21, PluralCategory.One);
        AssertCategory("ru", 22, PluralCategory.Few);
    }

    [TestMethod]
    public void PluralRules_ExposeEnglishOrdinalCategories() {
        AssertOrdinal("en", 1, PluralCategory.One);
        AssertOrdinal("en", 2, PluralCategory.Two);
        AssertOrdinal("en", 3, PluralCategory.Few);
        AssertOrdinal("en", 4, PluralCategory.Other);
        AssertOrdinal("en", 11, PluralCategory.Other);
        AssertOrdinal("en", 21, PluralCategory.One);
    }

    private static void AssertCategory(string culture, decimal count, PluralCategory expected) {
        var actual = PluralRules.GetCardinalCategory(CultureInfo.GetCultureInfo(culture), count);
        MSTestAssertions.That(() => actual.Should().Be(
            expected,
            $"Culture '{culture}' count {count} should be {expected}."));
    }

    private static void AssertOrdinal(string culture, decimal count, PluralCategory expected) {
        var actual = PluralRules.GetOrdinalCategory(CultureInfo.GetCultureInfo(culture), count);
        MSTestAssertions.That(() => actual.Should().Be(
            expected,
            $"Culture '{culture}' ordinal {count} should be {expected}."));
    }

    private static ServiceProvider BuildProvider() {
        var services = new ServiceCollection();
        services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
        JeninnetLocalizationExtensions.AddLocalization(services);
        services.AddDefaultMessagePacks();
        return services.BuildServiceProvider();
    }

    private CancellationToken TestCancellation => TestContext?.CancellationToken ?? CancellationToken.None;

    public TestContext? TestContext { get; set; }
}
