//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Localization;

/// <summary>
/// Covers the mutable validation message catalog: built-in baseline, overrides, removals, new
/// cultures, fallback, and concurrent updates.
/// </summary>
[TestClass]
public sealed class ValidationMessageCatalogTests {
    [TestMethod]
    public void Catalog_ExposesBuiltInCulturesAndTemplates() {
        using var provider = BuildProvider();
        var catalog = provider.GetRequiredService<IValidationMessageCatalog>();

        MSTestAssertions.That(() => catalog.Cultures.Should().HaveCount(40));

        var found = catalog.TryGetTemplate("en", ValidationMessageKeys.NOT_NULL, out var template);

        MSTestAssertions.That(() => found.Should().BeTrue());
        MSTestAssertions.That(() => template.Should().Contain("{PropertyName}"));
    }

    [TestMethod]
    public async Task Catalog_SetTemplate_AffectsTheLocalizerImmediatelyAsync() {
        await using var provider = BuildProvider();
        var catalog = provider.GetRequiredService<IValidationMessageCatalog>();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_NULL),
            ValidationMessageKeys.NOT_NULL);

        catalog.SetTemplate("ar", ValidationMessageKeys.NOT_NULL, "{PropertyName} مطلوب.");

        var message = await localizer.LocalizeValidationErrorAsync(
            error,
            new CultureInfo("ar"),
            TestContext?.CancellationToken ?? CancellationToken.None);

        MSTestAssertions.That(() => message.Should().Be("Name مطلوب."));
    }

    [TestMethod]
    public async Task Catalog_RemoveTemplate_FallsBackToTheErrorMessageAsync() {
        await using var provider = BuildProvider();
        var catalog = provider.GetRequiredService<IValidationMessageCatalog>();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_EMPTY),
            ValidationMessageKeys.NOT_EMPTY,
            message: "fallback text");

        catalog.RemoveTemplate("en", ValidationMessageKeys.NOT_EMPTY).Should().BeTrue();

        var message = await localizer.LocalizeValidationErrorAsync(
            error,
            new CultureInfo("en"),
            TestContext?.CancellationToken ?? CancellationToken.None);

        MSTestAssertions.That(() => message.Should().Be("fallback text"));
    }

    [TestMethod]
    public async Task Catalog_AddPack_AddsANewCultureAsync() {
        await using var provider = BuildProvider();
        var catalog = provider.GetRequiredService<IValidationMessageCatalog>();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_NULL),
            ValidationMessageKeys.NOT_NULL);

        catalog.AddPack("sw", new Dictionary<string, string> {
            [ValidationMessageKeys.NOT_NULL] = "{PropertyName} inahitajika."
        });

        var message = await localizer.LocalizeValidationErrorAsync(
            error,
            new CultureInfo("sw"),
            TestContext?.CancellationToken ?? CancellationToken.None);

        MSTestAssertions.That(() => catalog.Cultures.Should().Contain("sw"));
        MSTestAssertions.That(() => message.Should().Be("Name inahitajika."));
    }

    [TestMethod]
    public async Task Catalog_RemoveCulture_FallsBackToTheDefaultCultureAsync() {
        await using var provider = BuildProvider();
        var catalog = provider.GetRequiredService<IValidationMessageCatalog>();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_EMPTY),
            ValidationMessageKeys.NOT_EMPTY);

        catalog.RemoveCulture("fr").Should().BeTrue();

        var message = await localizer.LocalizeValidationErrorAsync(
            error,
            new CultureInfo("fr-FR"),
            TestContext?.CancellationToken ?? CancellationToken.None);

        MSTestAssertions.That(() => catalog.Cultures.Should().NotContain("fr"));
        MSTestAssertions.That(() => message.Should().Be("Name must not be empty."));
    }

    [TestMethod]
    public void Catalog_InitialCustomPack_IsAvailable() {
        using var provider = BuildProvider(options => options.AddPack("sw", new Dictionary<string, string> {
            [ValidationMessageKeys.NOT_NULL] = "{PropertyName} inahitajika."
        }));
        var catalog = provider.GetRequiredService<IValidationMessageCatalog>();

        var found = catalog.TryGetTemplate("sw", ValidationMessageKeys.NOT_NULL, out var template);

        MSTestAssertions.That(() => found.Should().BeTrue());
        MSTestAssertions.That(() => template.Should().Be("{PropertyName} inahitajika."));
    }

    [TestMethod]
    public async Task Catalog_ConcurrentUpdates_AreSafeAsync() {
        await using var provider = BuildProvider();
        var catalog = provider.GetRequiredService<IValidationMessageCatalog>();

        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        var tasks = Enumerable.Range(0, 8).Select(worker => Task.Run(() => {
            for(var i = 0; i < 50; i++) {
                catalog.SetTemplate("en", ValidationMessageKeys.NOT_NULL, $"v{worker}-{i} " + "{PropertyName}");
                _ = catalog.TryGetTemplate("en", ValidationMessageKeys.NOT_NULL, out _);
            }
        }, cancellationToken));

        await Task.WhenAll(tasks);

        var found = catalog.TryGetTemplate("en", ValidationMessageKeys.NOT_NULL, out var template);

        MSTestAssertions.That(() => found.Should().BeTrue());
        MSTestAssertions.That(() => template.Should().Contain("{PropertyName}"));
    }

    private static ServiceProvider BuildProvider(Action<ValidationMessagePackOptions>? configure = null) {
        var services = new ServiceCollection();
        services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
        services.AddValidation();
        services.AddDefaultMessagePacks(configure ?? (static _ => { }));

        return services.BuildServiceProvider();
    }

    public TestContext? TestContext { get; set; }
}
