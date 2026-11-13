//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Localization;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

internal sealed class IcuPluralModel {
    public List<int>? Items { get; set; }
}

file sealed class IcuPluralValidator : Validator<IcuPluralModel> {
    protected override void Configure(ValidatorRuleBuilder<IcuPluralModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Items, nameof(IcuPluralModel.Items))
               .Count(0, 3);
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class IcuPluralMessageTests {
    [TestMethod]
    public void IcuFormatter_RendersPluralTemplatesPerCulture() {
        var formatter = new IcuMessageFormatter();

        var englishOne = formatter.Format(
            "{PropertyName}: {Count, plural, one {# item} other {# items}}",
            Parameters("Items", 1),
            new CultureInfo("en"));

        var englishMany = formatter.Format(
            "{PropertyName}: {Count, plural, one {# item} other {# items}}",
            Parameters("Items", 5),
            new CultureInfo("en"));

        var arabicOne = formatter.Format(
            "{PropertyName}: {Count, plural, one {عنصر واحد} few {# عناصر} other {# عنصراً}}",
            Parameters("العناصر", 1),
            new CultureInfo("ar"));

        var arabicFew = formatter.Format(
            "{PropertyName}: {Count, plural, one {عنصر واحد} few {# عناصر} other {# عنصراً}}",
            Parameters("العناصر", 5),
            new CultureInfo("ar"));

        MSTestAssertions.That(() => englishOne.Should().Be("Items: 1 item"));
        MSTestAssertions.That(() => englishMany.Should().Be("Items: 5 items"));
        MSTestAssertions.That(() => arabicOne.Should().Be("العناصر: عنصر واحد"));
        MSTestAssertions.That(() => arabicFew.Should().Be("العناصر: 5 عناصر"));
    }

    [TestMethod]
    public async Task IcuFormatter_IsOptInThroughTheMessagePipelineAsync() {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageFormatter>(new IcuMessageFormatter());
        services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
        JeninnetLocalizationExtensions.AddLocalization(services);
        services.AddDefaultMessagePacks(options => options.Override(
                "en",
                ValidationMessageKeys.COUNT,
                "{PropertyName}: {Count, plural, one {# item} other {# items}}")
                .Override(
                "ar",
                ValidationMessageKeys.COUNT,
                "{PropertyName}: {Count, plural, one {عنصر واحد} few {# عناصر} other {# عنصراً}}"));

        await using var provider = services.BuildServiceProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var validator = new IcuPluralValidator();
        var result = await validator.ValidateAsync(
            new IcuPluralModel { Items = [1, 2, 3, 4, 5] },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.ErrorCount(result, 1);
        var error = result.Errors[0];

        var english = await localizer.LocalizeValidationErrorAsync(
            error,
            new CultureInfo("en"),
            TestContext?.CancellationToken ?? CancellationToken.None);

        var arabic = await localizer.LocalizeValidationErrorAsync(
            error,
            new CultureInfo("ar"),
            TestContext?.CancellationToken ?? CancellationToken.None);

        MSTestAssertions.That(() => english.Should().Be("Items: 5 items"));
        MSTestAssertions.That(() => arabic.Should().Be("Items: 5 عناصر"));
    }

    private static Dictionary<string, object?> Parameters(string propertyName, int count) =>
        new(StringComparer.Ordinal) {
            ["PropertyName"] = propertyName,
            ["Count"] = count
        };

    public TestContext? TestContext { get; set; }
}
