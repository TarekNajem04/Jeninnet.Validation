//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Localization;

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed partial class DefaultMessagePackTests {
    private static readonly HashSet<string> _allowedPlaceholders =
    [
        "PropertyName",
        "0",
        "1",
        "Min",
        "Max",
        "Count",
        "Length",
        "Extensions",
        "CollectionIndex",
        "Country",
        "Policy",
        "MissingRequirements"
    ];

    [TestMethod]
    public void Packs_CoverEveryValidationMessageKeyInEveryCulture() {
        var keys = typeof(ValidationMessageKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(static field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        var cultures = DefaultValidationMessageProvider.SupportedCultures;

        MSTestAssertions.That(() => (cultures.Count >= 12).Should().BeTrue(
            $"Expected at least 12 built-in packs, found {cultures.Count}."));

        foreach(var culture in cultures) {
            var actual = DefaultValidationMessageProvider.GetPack(culture).Keys.ToHashSet(StringComparer.Ordinal);

            MSTestAssertions.That(() => actual.SetEquals(keys).Should().BeTrue(
                $"Pack '{culture}' must contain every key. Missing: {string.Join(", ", keys.Except(actual))}; extra: {string.Join(", ", actual.Except(keys))}."));
        }
    }

    [TestMethod]
    public void SupportedCultures_IncludeTheExpectedCultures() {
        string[] expected = [
            "en", "ar", "fr", "es", "de", "tr", "ur", "fa", "hi", "zh-Hans", "ja", "pt-BR", "ru", "nl",
            "it", "pl", "uk", "ko", "vi", "th", "id", "cs", "sv", "da", "fi", "no",
            "el", "he", "hu", "ro", "sk", "sl", "hr", "sr", "bg", "lt", "lv", "et", "ca", "af"
        ];
        var actual = DefaultValidationMessageProvider.SupportedCultures.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach(var culture in expected) {
            MSTestAssertions.That(() => actual.Contains(culture).Should().BeTrue(
                $"The built-in packs must include '{culture}'."));
        }
    }

    [TestMethod]
    public void Packs_UseOnlyKnownPlaceholders() {
        foreach(var culture in DefaultValidationMessageProvider.SupportedCultures) {
            foreach(var template in DefaultValidationMessageProvider.GetPack(culture).Values) {
                AssertPlaceholders(template, culture);
            }
        }

        static void AssertPlaceholders(string template, string culture) {
            foreach(var match in PlaceholderRegex().Matches(template).Cast<Match>()) {
                var key = match.Groups["key"].Value;
                MSTestAssertions.That(() => _allowedPlaceholders.Contains(key).Should().BeTrue(
                    $"Pack '{culture}' uses unknown placeholder '{{{key}}}' in: {template}"));
            }
        }
    }

    [TestMethod]
    public void Packs_PreserveTheEnglishPlaceholderSetPerKey() {
        var english = DefaultValidationMessageProvider.GetPack("en");

        foreach(var culture in DefaultValidationMessageProvider.SupportedCultures) {
            var pack = DefaultValidationMessageProvider.GetPack(culture);

            foreach(var (key, template) in english) {
                var expected = ExtractPlaceholders(template);
                var actual = ExtractPlaceholders(pack[key]);

                MSTestAssertions.That(() => actual.SetEquals(expected).Should().BeTrue(
                    $"Pack '{culture}' key '{key}' must use placeholders [{string.Join(", ", expected)}]; found [{string.Join(", ", actual)}]."));
            }
        }

        static HashSet<string> ExtractPlaceholders(string template) =>
            PlaceholderRegex().Matches(template)
                .Cast<Match>()
                .Select(static match => match.Groups["key"].Value)
                .ToHashSet(StringComparer.Ordinal);
    }

    [TestMethod]
    public void Packs_HaveNonEmptyTemplatesAndNoReplacementCharacters() {
        foreach(var culture in DefaultValidationMessageProvider.SupportedCultures) {
            foreach(var (key, template) in DefaultValidationMessageProvider.GetPack(culture)) {
                MSTestAssertions.That(() => string.IsNullOrWhiteSpace(template).Should().BeFalse(
                    $"Pack '{culture}' key '{key}' must not be empty."));
                MSTestAssertions.That(() => template.Contains('\uFFFD').Should().BeFalse(
                    $"Pack '{culture}' key '{key}' contains a replacement character."));
            }
        }
    }

    [TestMethod]
    public void NewPacks_FormatMultiParameterTemplates() {
        string[] newCultures = [
            "it", "pl", "uk", "ko", "vi", "th", "id", "cs", "sv", "da", "fi", "no",
            "el", "he", "hu", "ro", "sk", "sl", "hr", "sr", "bg", "lt", "lv", "et", "ca", "af"
        ];

        foreach(var culture in newCultures) {
            var pack = DefaultValidationMessageProvider.GetPack(culture);
            var template = pack[ValidationMessageKeys.INCLUSIVE_BETWEEN]
                .Replace("{PropertyName}", "Amount", StringComparison.Ordinal);
            var formatted = string.Format(CultureInfo.InvariantCulture, template, 1, 2);

            MSTestAssertions.That(() => formatted.Should().Contain("1"));
            MSTestAssertions.That(() => formatted.Should().Contain("2"));
            MSTestAssertions.That(() => formatted.Should().NotContain("{0}"));
            MSTestAssertions.That(() => formatted.Should().NotContain("{1}"));
        }
    }

    [TestMethod]
    public async Task CultureFallback_UsesBaseLanguageAndEnglishAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_NULL),
            ValidationMessageKeys.NOT_NULL);

        var italian = await localizer.LocalizeValidationErrorAsync(
            error,
            new CultureInfo("it-CH"),
            TestCancellation);
        var unknown = await localizer.LocalizeValidationErrorAsync(
            error,
            new CultureInfo("xh-ZA"),
            TestCancellation);

        MSTestAssertions.That(() => italian.Should().Contain("obbligatorio"));
        MSTestAssertions.That(() => unknown.Should().Be("Name is required."));
    }

    [TestMethod]
    public void RtlPacks_UseArabicScript() {
        foreach(var culture in new[] { "ar", "ur", "fa" }) {
            var text = string.Join(" ", DefaultValidationMessageProvider.GetPack(culture).Values);

            MSTestAssertions.That(() => text.Any(static character => character is >= '\u0600' and <= '\u06FF')
                .Should().BeTrue($"Pack '{culture}' must contain Arabic-script text."));
        }
    }

    [GeneratedRegex(@"\{(?<key>[^}]+)\}")]
    private static partial Regex PlaceholderRegex();

    [TestMethod]
    public async Task LocalizeValidationError_RendersEnglishAndArabicWithPropertyNameAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_NULL),
            ValidationMessageKeys.NOT_NULL);

        var english = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("en-US"), TestCancellation);
        var arabic = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("ar-SA"), TestCancellation);

        MSTestAssertions.That(() => english.Should().Be("Name is required."));
        MSTestAssertions.That(() => arabic.Should().Be("Name مطلوب."));
    }

    [TestMethod]
    public async Task ConsumerOverride_WinsOverTheDefaultPackAsync() {
        await using var provider = BuildProvider(options => options.Override(
            "en",
            ValidationMessageKeys.NOT_NULL,
            "CUSTOM REQUIRED"));
        var localizer = provider.GetRequiredService<IMessageLocalizer>();

        var overridden = await localizer.LocalizeValidationErrorAsync(
            new ValidationError("Name", new MessageId(ValidationMessageKeys.NOT_NULL), ValidationMessageKeys.NOT_NULL),
            new CultureInfo("en"), TestCancellation);

        var fallback = await localizer.LocalizeValidationErrorAsync(
            new ValidationError("Name", new MessageId(ValidationMessageKeys.NOT_EMPTY), ValidationMessageKeys.NOT_EMPTY),
            new CultureInfo("en"), TestCancellation);

        MSTestAssertions.That(() => overridden.Should().Be("CUSTOM REQUIRED"));
        MSTestAssertions.That(() => fallback.Should().Be("Name must not be empty."));
    }

    [TestMethod]
    public async Task CustomPack_WinsOverTheDefaultPackAsync() {
        await using var provider = BuildProvider(options => options.AddPack(
            "ar",
            new Dictionary<string, string> { [ValidationMessageKeys.NOT_NULL] = "CUSTOM ARABIC" }));
        var localizer = provider.GetRequiredService<IMessageLocalizer>();

        var message = await localizer.LocalizeValidationErrorAsync(
            new ValidationError("Name", new MessageId(ValidationMessageKeys.NOT_NULL), ValidationMessageKeys.NOT_NULL),
            new CultureInfo("ar"), TestCancellation);

        MSTestAssertions.That(() => message.Should().Be("CUSTOM ARABIC"));
    }

    [TestMethod]
    public async Task AuthoredMessage_WinsOverPackAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_EMPTY),
            ValidationMessageKeys.NOT_EMPTY,
            message: "authored text") {
            MessageIsAuthored = true
        };

        var english = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("en"), TestCancellation);
        var arabic = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("ar"), TestCancellation);

        MSTestAssertions.That(() => english.Should().Be("authored text"));
        MSTestAssertions.That(() => arabic.Should().Be("authored text"));
    }

    [TestMethod]
    public async Task NonAuthoredMessage_StillUsesPackAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_EMPTY),
            ValidationMessageKeys.NOT_EMPTY,
            message: "fallback text");

        var arabic = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("ar"), TestCancellation);

        MSTestAssertions.That(() => arabic.Should().Be("Name يجب ألا يكون فارغاً."));
    }

    [TestMethod]
    public async Task CultureFallback_UsesParentAndDefaultCultureAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_EMPTY),
            ValidationMessageKeys.NOT_EMPTY);

        var french = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("fr-FR"), TestCancellation);
        var arabic = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("ar-EG"), TestCancellation);
        var english = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("en-GB"), TestCancellation);
        var italian = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("it-IT"), TestCancellation);

        MSTestAssertions.That(() => french.Should().Be("Name ne doit pas être vide."));
        MSTestAssertions.That(() => arabic.Should().Be("Name يجب ألا يكون فارغاً."));
        MSTestAssertions.That(() => english.Should().Be("Name must not be empty."));
        MSTestAssertions.That(() => italian.Should().Be("Name non deve essere vuoto."));
    }

    [TestMethod]
    public async Task NewCultureFallback_UsesTheNeutralPackAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_NULL),
            ValidationMessageKeys.NOT_NULL);

        var greek = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("el-GR"), TestCancellation);

        MSTestAssertions.That(() => greek.Should().Be("Το πεδίο Name είναι υποχρεωτικό."));
    }

    [TestMethod]
    public async Task ScriptCulture_FallsBackThroughTheParentChainAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_NULL),
            ValidationMessageKeys.NOT_NULL);

        var message = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("zh-CN"), TestCancellation);

        MSTestAssertions.That(() => message.Should().Be("Name 为必填项。"));
    }

    [TestMethod]
    public async Task RegionalOverride_WinsOverTheSpecificBuiltInPackAsync() {
        await using var provider = BuildProvider(options => options.Override(
            "pt-BR",
            ValidationMessageKeys.NOT_NULL,
            "CUSTOM PT-BR"));
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_NULL),
            ValidationMessageKeys.NOT_NULL);

        var message = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("pt-BR"), TestCancellation);

        MSTestAssertions.That(() => message.Should().Be("CUSTOM PT-BR"));
    }

    [TestMethod]
    public async Task CountParameters_AreMergedAndFormattedAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Items",
            new MessageId(ValidationMessageKeys.COUNT),
            ValidationMessageKeys.COUNT,
            new Dictionary<string, object> {
                ["Min"] = 1,
                ["Max"] = 3,
                ["Count"] = 5
            });

        var arabic = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("ar"), TestCancellation);

        MSTestAssertions.That(() => arabic.Should().Contain("Items"));
        MSTestAssertions.That(() => arabic.Should().Contain("1"));
        MSTestAssertions.That(() => arabic.Should().Contain("3"));
        MSTestAssertions.That(() => arabic.Should().Contain("5"));
    }

    [TestMethod]
    public async Task DynamicMessage_DoesNotOverrideMessagePackTemplateAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_EMPTY),
            ValidationMessageKeys.NOT_EMPTY,
            message: "dynamic-message");

        var message = await localizer.LocalizeValidationErrorAsync(
            error,
            new CultureInfo("en"),
            TestCancellation);

        MSTestAssertions.That(() => message.Should().Be("Name must not be empty."));
    }

    [TestMethod]
    public async Task CollectionIndex_IsAvailableToMessageTemplatesAsync() {
        await using var provider = BuildProvider(options => options.Override(
            "en",
            ValidationMessageKeys.NOT_NULL,
            "{PropertyName} at {CollectionIndex}"));
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Items[2]",
            new MessageId(ValidationMessageKeys.NOT_NULL),
            ValidationMessageKeys.NOT_NULL,
            new Dictionary<string, object> { ["CollectionIndex"] = 2 });

        var message = await localizer.LocalizeValidationErrorAsync(
            error,
            new CultureInfo("en"),
            TestCancellation);

        MSTestAssertions.That(() => message.Should().Be("Items[2] at 2"));
    }

    [TestMethod]
    public async Task ErrorWithoutKey_FallsBackToItsMessageAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError("Name", "custom message");

        var message = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("ar"), TestCancellation);

        MSTestAssertions.That(() => message.Should().Be("custom message"));
    }

    [TestMethod]
    public void ValidationMessagePack_PreservesTheNormalizedCultureName() {
        var regional = new ValidationMessagePack("ar-SA", new Dictionary<string, string>());
        var script = new ValidationMessagePack("zh-Hans", new Dictionary<string, string>());

        MSTestAssertions.That(() => regional.Culture.Should().Be("ar-SA"));
        MSTestAssertions.That(() => script.Culture.Should().Be("zh-Hans"));
    }

    [TestMethod]
    public void AddDefaultMessagePacks_ThrowsWhenAProviderIsAlreadyRegistered() {
        var services = new ServiceCollection();
        services.AddSingleton<IInnerMessageProvider>(new DefaultValidationMessageProvider());

        Action act = () => services.AddDefaultMessagePacks();

        MSTestAssertions.That(() => act.Should().ThrowExactly<InvalidOperationException>());
    }

    [TestMethod]
    public async Task ComparableMessage_UsesTheThresholdParameterAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Score",
            new MessageId(ValidationMessageKeys.COMPARABLE),
            ValidationMessageKeys.COMPARABLE,
            new Dictionary<string, object> { ["0"] = 100 });

        var english = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("en"), TestCancellation);
        var arabic = await localizer.LocalizeValidationErrorAsync(error, new CultureInfo("ar"), TestCancellation);

        MSTestAssertions.That(() => english.Should().Be("Score does not meet the comparison criteria against 100."));
        MSTestAssertions.That(() => arabic.Should().Be("Score لا يستوفي معايير المقارنة مقابل 100."));
    }

    private CancellationToken TestCancellation => TestContext?.CancellationToken ?? CancellationToken.None;

    private static ServiceProvider BuildProvider(Action<ValidationMessagePackOptions>? configurePacks = null) {
        var services = new ServiceCollection();
        services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
        JeninnetLocalizationExtensions.AddLocalization(services);
        services.AddDefaultMessagePacks(configurePacks ?? (static _ => { }));
        return services.BuildServiceProvider();
    }

    public TestContext? TestContext { get; set; }
}
