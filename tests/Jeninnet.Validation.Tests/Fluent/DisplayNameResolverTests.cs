//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Fluent;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

internal sealed class DisplayModel {
    public string? Email { get; set; }
}

file sealed class DisplayNameValidator : Validator<DisplayModel> {
    protected override void Configure(ValidatorRuleBuilder<DisplayModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Email, nameof(DisplayModel.Email))
               .NotEmpty();
    }
}

file sealed class NamedDisplayNameValidator : Validator<DisplayModel> {
    protected override void Configure(ValidatorRuleBuilder<DisplayModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Email, nameof(DisplayModel.Email))
               .NotEmpty()
               .WithName("Login");
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
// Mutates the process-wide validation options and restores them.
[DoNotParallelize]
public sealed class DisplayNameResolverTests {
    [TestMethod]
    public void GlobalResolver_ProvidesPropertyNameWithoutChangingMemberName() =>
        WithResolver(
            static (type, name) => type == typeof(DisplayModel) && name == nameof(DisplayModel.Email)
                ? "Email address"
                : name,
            () => {
                var result = new DisplayNameValidator().Validate(new DisplayModel { Email = "" });
                var error = result.Errors.Single();

                MSTestAssertions.That(() => error.MemberName.Should().Be(nameof(DisplayModel.Email)));
                MSTestAssertions.That(() => error.MessageParameters!["PropertyName"].Should().Be("Email address"));
            });

    [TestMethod]
    public void InstanceResolver_ReceivesTheModelAndWinsOverLegacyResolver() =>
        WithResolvers(
            static (_, _) => "legacy",
            static (type, name, instance) =>
                type == typeof(DisplayModel) && name == nameof(DisplayModel.Email) && instance is DisplayModel model
                    ? $"Email of {model.Email}"
                    : name,
            () => {
                var result = new DisplayNameValidator().Validate(new DisplayModel { Email = "" });
                var error = result.Errors.Single();

                MSTestAssertions.That(() => error.MemberName.Should().Be(nameof(DisplayModel.Email)));
                MSTestAssertions.That(() => error.MessageParameters!["PropertyName"].Should().Be("Email of "));
            });

    [TestMethod]
    public void InstanceResolver_EmptyResultFallsBackToLegacyResolver() =>
        WithResolvers(
            static (_, _) => "legacy",
            static (_, _, _) => " ",
            () => {
                var result = new DisplayNameValidator().Validate(new DisplayModel { Email = "" });
                var error = result.Errors.Single();

                MSTestAssertions.That(() => error.MessageParameters!["PropertyName"].Should().Be("legacy"));
            });

    [TestMethod]
    public void RuleOverride_WinsOverGlobalResolver() =>
        WithResolver(
            static (_, _) => "Global name",
            () => {
                var result = new NamedDisplayNameValidator().Validate(new DisplayModel { Email = "" });
                var error = result.Errors.Single();

                MSTestAssertions.That(() => error.MemberName.Should().Be("Login"));
                MSTestAssertions.That(() => error.MessageParameters!["PropertyName"].Should().Be("Login"));
            });

    [TestMethod]
    public void NoResolver_FallsBackToMemberName() =>
        WithResolver(
            resolver: null,
            () => {
                var result = new DisplayNameValidator().Validate(new DisplayModel { Email = "" });
                var error = result.Errors.Single();

                MSTestAssertions.That(() => error.MemberName.Should().Be(nameof(DisplayModel.Email)));
                MSTestAssertions.That(() => error.MessageParameters!["PropertyName"].Should().Be(nameof(DisplayModel.Email)));
                MSTestAssertions.That(() => error.MessageParameters!["PropertyPath"].Should().Be(nameof(DisplayModel.Email)));
            });

    [TestMethod]
    public async Task Localization_UsesTheResolvedDisplayNameAsync() =>
        await WithResolverAsync(
            static (type, name) => type == typeof(DisplayModel) && name == nameof(DisplayModel.Email)
                ? "Email address"
                : name,
            async () => {
                var services = new ServiceCollection();
                services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
                JeninnetLocalizationExtensions.AddLocalization(services);
                services.AddDefaultMessagePacks();

                await using var provider = services.BuildServiceProvider();
                var localizer = provider.GetRequiredService<IMessageLocalizer>();
                var result = await new DisplayNameValidator().ValidateAsync(
                    new DisplayModel { Email = "" },
                    TestContext?.CancellationToken ?? CancellationToken.None);

                var message = await localizer.LocalizeValidationErrorAsync(
                    result.Errors[0],
                    new CultureInfo("en"),
                    TestContext?.CancellationToken ?? CancellationToken.None);

                MSTestAssertions.That(() => message.Should().Be("Email address must not be empty."));
            });

    private static void WithResolvers(
        Func<Type, string, string>? resolver,
        Func<Type, string, object?, string>? instanceResolver,
        Action assertion) {
        var previous = ValidationConfiguration.Options;

        try {
            ValidationConfiguration.Configure(options => options with {
                DisplayNameResolver = resolver,
                DisplayNameResolverForInstance = instanceResolver
            });
            assertion();
        }
        finally {
            ValidationConfiguration.Configure(_ => previous);
        }
    }

    private static void WithResolver(Func<Type, string, string>? resolver, Action assertion) {
        var previous = ValidationConfiguration.Options;

        try {
            ValidationConfiguration.Configure(options => options with { DisplayNameResolver = resolver });
            assertion();
        }
        finally {
            ValidationConfiguration.Configure(_ => previous);
        }
    }

    private static async Task WithResolverAsync(Func<Type, string, string>? resolver, Func<Task> assertion) {
        var previous = ValidationConfiguration.Options;

        try {
            ValidationConfiguration.Configure(options => options with { DisplayNameResolver = resolver });
            await assertion().ConfigureAwait(false);
        }
        finally {
            ValidationConfiguration.Configure(_ => previous);
        }
    }

    public TestContext? TestContext { get; set; }
}
