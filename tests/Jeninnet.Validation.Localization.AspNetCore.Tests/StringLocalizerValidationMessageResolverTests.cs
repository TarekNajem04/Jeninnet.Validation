//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Localization.AspNetCore.Tests;

[TestClass]
// The resolver tests exercise process-wide culture and localization state.
[DoNotParallelize]
public sealed class StringLocalizerValidationMessageResolverTests {
    public TestContext TestContext { get; set; } = null!;
    private static readonly CultureInfo _english = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo _arabic = CultureInfo.GetCultureInfo("ar");

    [TestMethod]
    public async Task Resx_Localizer_ResolvesEnglishAndArabic_WithParametersAsync() {
        await using var provider = BuildResxProvider();
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();
        var error = CreateError(ValidationMessageKeys.NOT_NULL);

        var beforeUiCulture = CultureInfo.CurrentUICulture;
        var beforeCulture = CultureInfo.CurrentCulture;

        var english = await resolver.ResolveAsync(error, _english, TestContext.CancellationToken);
        var arabic = await resolver.ResolveAsync(error, _arabic, TestContext.CancellationToken);

        MSTestAssertions.That(() => english.Should().Be("resx(NotNull): Name is required."));
        MSTestAssertions.That(() => arabic.Should().Be("resx(NotNull): Name مطلوب."));
        MSTestAssertions.That(() => CultureInfo.CurrentUICulture.Name.Should().Be(beforeUiCulture.Name));
        MSTestAssertions.That(() => CultureInfo.CurrentCulture.Name.Should().Be(beforeCulture.Name));
    }

    [TestMethod]
    public async Task AuthoredMessage_SkipsResxAndPackAsync() {
        await using var provider = BuildResxProvider();
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();
        var error = new ValidationError(
            "Name",
            new MessageId(ValidationMessageKeys.NOT_NULL),
            ValidationMessageKeys.NOT_NULL,
            message: "authored") {
            MessageIsAuthored = true
        };

        var message = await resolver.ResolveAsync(error, _arabic, TestContext.CancellationToken);

        MSTestAssertions.That(() => message.Should().Be("authored"));
    }

    [TestMethod]
    public async Task Resx_FormatsNamedParameters_WithTheRequestedCultureAsync() {
        await using var provider = BuildResxProvider();
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();
        var error = new ValidationError(
            "Age",
            new MessageId(ValidationMessageKeys.RANGE),
            ValidationMessageKeys.RANGE,
            new Dictionary<string, object> {
                ["Min"] = 18,
                ["Max"] = 65
            },
            message: "fallback");

        var english = await resolver.ResolveAsync(error, _english, TestContext.CancellationToken);
        var arabic = await resolver.ResolveAsync(error, _arabic, TestContext.CancellationToken);

        MSTestAssertions.That(() => english.Should().Be("resx(Range): Age must be between 18 and 65."));
        MSTestAssertions.That(() => arabic.Should().Be("resx(Range): Age يجب أن يكون بين 18 و 65."));
    }

    [TestMethod]
    public async Task Resx_WorksWithoutMessagePacksAsync() {
        await using var provider = BuildResxProvider();
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();

        MSTestAssertions.That(() => provider.GetService<IMessageLocalizer>().Should().BeNull());

        var message = await resolver.ResolveAsync(CreateError(ValidationMessageKeys.NOT_NULL), _english, TestContext.CancellationToken);

        MSTestAssertions.That(() => message.Should().Be("resx(NotNull): Name is required."));
    }

    [TestMethod]
    public async Task MissingResxKey_FallsBackToMessagePackAsync() {
        await using var provider = BuildPackProvider();
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();
        var error = CreateError(ValidationMessageKeys.EMAIL);

        var message = await resolver.ResolveAsync(error, _english, TestContext.CancellationToken);

        MSTestAssertions.That(() => message.Should().Be("Name must be a valid email address."));
    }

    [TestMethod]
    public async Task MissingEverywhere_FallsBackToTheErrorMessageAsync() {
        await using var provider = BuildResxProvider();
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();
        var error = new ValidationError(
            "Name",
            new MessageId("Unknown"),
            "Unknown",
            message: "custom fallback");

        var message = await resolver.ResolveAsync(error, _english, TestContext.CancellationToken);

        MSTestAssertions.That(() => message.Should().Be("custom fallback"));
    }

    [TestMethod]
    public async Task MissingResourceType_SkipsResxAndFallsBackToTheErrorMessageAsync() {
        await using var provider = BuildResxProvider(options => options.ResourceType = null);
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();
        var error = CreateError(ValidationMessageKeys.NOT_NULL);

        var message = await resolver.ResolveAsync(error, _english, TestContext.CancellationToken);

        MSTestAssertions.That(() => message.Should().Be("fallback"));
    }

    [TestMethod]
    public async Task MissingKey_WithNoMessage_ReturnsNullAsync() {
        await using var provider = BuildResxProvider();
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();
        var error = new ValidationError("Name", new MessageId("Missing"), "Missing");

        var message = await resolver.ResolveAsync(error, _english, TestContext.CancellationToken);

        MSTestAssertions.That(() => message.Should().BeNull());
    }

    [TestMethod]
    public async Task ForceCulture_OverridesTheCallerCulture_WithoutMutatingGlobalCultureAsync() {
        await using var provider = BuildResxProvider(options => options.ForceCulture = "ar");
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();
        var beforeUiCulture = CultureInfo.CurrentUICulture;
        var beforeCulture = CultureInfo.CurrentCulture;

        var message = await resolver.ResolveAsync(CreateError(ValidationMessageKeys.NOT_NULL), _english, TestContext.CancellationToken);

        MSTestAssertions.That(() => message.Should().Be("resx(NotNull): Name مطلوب."));
        MSTestAssertions.That(() => CultureInfo.CurrentUICulture.Name.Should().Be(beforeUiCulture.Name));
        MSTestAssertions.That(() => CultureInfo.CurrentCulture.Name.Should().Be(beforeCulture.Name));
    }

    [TestMethod]
    public async Task DefaultCulture_IsUsedWhenNoCultureIsPassedAsync() {
        await using var provider = BuildResxProvider(options => options.DefaultCulture = "ar");
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();
        var beforeUiCulture = CultureInfo.CurrentUICulture;

        try {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

            var message = await resolver.ResolveAsync(CreateError(ValidationMessageKeys.NOT_NULL), cancellationToken: TestContext.CancellationToken);

            MSTestAssertions.That(() => message.Should().Be("resx(NotNull): Name مطلوب."));
            MSTestAssertions.That(() => CultureInfo.CurrentUICulture.Name.Should().Be("fr-FR"));
        }
        finally {
            CultureInfo.CurrentUICulture = beforeUiCulture;
        }
    }

    [TestMethod]
    public async Task DisableLocalization_SkipsResxAndUsesThePackAsync() {
        await using var provider = BuildPackProvider(options => options.DisableLocalization = true);
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();

        var message = await resolver.ResolveAsync(CreateError(ValidationMessageKeys.NOT_NULL), _english, TestContext.CancellationToken);

        MSTestAssertions.That(() => message.Should().Be("Name is required."));
    }

    [TestMethod]
    public async Task DisableLocalization_FallsBackToTheErrorMessageAsync() {
        await using var provider = BuildPackProvider(options => options.DisableLocalization = true);
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();
        var error = new ValidationError(
            "Name",
            new MessageId("Unknown"),
            "Unknown",
            message: "custom fallback");

        var message = await resolver.ResolveAsync(error, _english, TestContext.CancellationToken);

        MSTestAssertions.That(() => message.Should().Be("custom fallback"));
    }

    [TestMethod]
    public async Task ResolveValidationErrorsAsync_PreservesOrderAsync() {
        await using var provider = BuildPackProvider();
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();
        var errors = new[] {
            CreateError(ValidationMessageKeys.NOT_NULL),
            CreateError(ValidationMessageKeys.EMAIL, "Email"),
            new ValidationError("Custom", "custom message")
        };

        var messages = await resolver.ResolveValidationErrorsAsync(errors, _english, TestContext.CancellationToken);

        MSTestAssertions.That(() => messages.Count.Should().Be(3));
        MSTestAssertions.That(() => messages[0].Should().Be("resx(NotNull): Name is required."));
        MSTestAssertions.That(() => messages[1].Should().Be("Email must be a valid email address."));
        MSTestAssertions.That(() => messages[2].Should().Be("custom message"));
    }

    [TestMethod]
    public async Task ResolveAsync_ObservesAPreCancelledTokenAsync() {
        await using var provider = BuildResxProvider();
        var resolver = provider.GetRequiredService<IValidationMessageResolver>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        Exception? caught = null;

        try {
            await resolver.ResolveAsync(CreateError(ValidationMessageKeys.NOT_NULL), _english, cancellation.Token);
        }
        catch(Exception exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => (caught is OperationCanceledException).Should().BeTrue());
    }

    [TestMethod]
    public void AddJeninnetStringLocalizer_RegistersTheResolver() {
        using var provider = BuildResxProvider();

        MSTestAssertions.That(() => provider.GetService<IValidationMessageResolver>().Should().NotBeNull());
        MSTestAssertions.That(() => provider.GetService<StringLocalizerValidationMessageResolver>().Should().NotBeNull());
    }

    [TestMethod]
    public void AddJeninnetStringLocalizer_DoesNotRegisterASecondInnerProvider() {
        var services = new ServiceCollection();
        services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
        JeninnetLocalizationExtensions.AddLocalization(services);
        services.AddDefaultMessagePacks();
        services.AddJeninnetStringLocalizer();

        using var provider = services.BuildServiceProvider();

        MSTestAssertions.That(() => provider.GetServices<IInnerMessageProvider>().Count().Should().Be(1));
    }

    private static ServiceProvider BuildResxProvider(Action<StringLocalizerValidationOptions>? configure = null) {
        var services = new ServiceCollection();
        services.AddJeninnetStringLocalizer(options => {
            options.ResourceType = typeof(TestValidationMessages);
            configure?.Invoke(options);
        });

        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildPackProvider(Action<StringLocalizerValidationOptions>? configure = null) {
        var services = new ServiceCollection();
        var localizationOptions = new LocalizationOptions { DefaultCulture = "en" };
        var packLocalizer = new DefaultMessageLocalizer(
            new DefaultValidationMessageProvider(),
            new DefaultMessageFormatter(),
            localizationOptions);

        services.AddSingleton(localizationOptions);
        services.AddSingleton<IMessageLocalizer>(packLocalizer);
        services.AddJeninnetStringLocalizer(options => {
            options.ResourceType = typeof(TestValidationMessages);
            configure?.Invoke(options);
        });

        return services.BuildServiceProvider();
    }

    private static ValidationError CreateError(string key, string memberName = "Name") =>
        new(
            memberName,
            new MessageId(key),
            key,
            message: "fallback");
}
