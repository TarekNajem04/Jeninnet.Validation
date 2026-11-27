//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Extensions.Validation.Tests;

internal sealed class AsyncOptionsSettings {
    public string? Name { get; set; }
}

file sealed class AsyncOptionsSettingsValidator : Validator<AsyncOptionsSettings> {
    protected override void Configure(ValidatorRuleBuilder<AsyncOptionsSettings> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static options => options.Name, nameof(AsyncOptionsSettings.Name))
               .Must(static (value, cancellationToken) => IsValidNameAsync(value, cancellationToken), "Name is required.")
               .WithCode("NAME_REQUIRED");
    }

    private static async ValueTask<bool> IsValidNameAsync(
        string? value,
        CancellationToken cancellationToken) {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        return !string.IsNullOrWhiteSpace(value);
    }
}

[TestClass]
public sealed class AsyncOptionsValidationTests {
    public TestContext? TestContext { get; set; }

    [TestMethod]
    public async Task AsyncStartupValidator_ValidOptions_PassAsync() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AsyncOptionsSettings>, AsyncOptionsSettingsValidator>();
        services.AddOptions<AsyncOptionsSettings>()
                .Configure(static options => options.Name = "worker")
                .AddValidatedOptions();

        await using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IAsyncStartupValidator>();

        await validator.ValidateAsync(TestContext!.CancellationToken);
    }

    [TestMethod]
    public async Task AsyncStartupValidator_InvalidOptions_ReportFieldAndCodeAsync() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AsyncOptionsSettings>, AsyncOptionsSettingsValidator>();
        services.AddOptions<AsyncOptionsSettings>()
                .Configure(static options => options.Name = string.Empty)
                .AddValidatedOptions();

        await using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IAsyncStartupValidator>();
        var act = async () => await validator.ValidateAsync(TestContext!.CancellationToken);

        await MSTestAssertions.ThatAsync(async () => {
            var exception = await act.Should().ThrowAsync<OptionsValidationException>();
            exception.Which.Failures.Any(static failure =>
                failure.Contains(nameof(AsyncOptionsSettings.Name), StringComparison.Ordinal) &&
                failure.Contains("NAME_REQUIRED", StringComparison.Ordinal))
                .Should().BeTrue();
        });
    }

    [TestMethod]
    public async Task AsyncStartupValidator_NamedOptions_ReportNamePrefixAsync() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AsyncOptionsSettings>, AsyncOptionsSettingsValidator>();
        services.AddOptions<AsyncOptionsSettings>("worker")
                .Configure(static options => options.Name = string.Empty)
                .AddValidatedOptions();

        await using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IAsyncStartupValidator>();
        var act = async () => await validator.ValidateAsync(TestContext!.CancellationToken);

        await MSTestAssertions.ThatAsync(async () => {
            var exception = await act.Should().ThrowAsync<OptionsValidationException>();
            exception.Which.Failures.All(static failure =>
                failure.StartsWith("worker: ", StringComparison.Ordinal))
                .Should().BeTrue();
        });
    }

    [TestMethod]
    public async Task AsyncStartupValidator_MissingValidator_FailsClearlyAsync() {
        var services = new ServiceCollection();
        services.AddOptions<AsyncOptionsSettings>()
                .Configure(static options => options.Name = "worker")
                .AddValidatedOptions();

        await using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IAsyncStartupValidator>();
        var act = async () => await validator.ValidateAsync(TestContext!.CancellationToken);

        await MSTestAssertions.ThatAsync(async () => {
            var exception = await act.Should().ThrowAsync<OptionsValidationException>();
            exception.Which.Failures.Any(static failure =>
                failure.Contains("No IValidator<AsyncOptionsSettings>", StringComparison.Ordinal))
                .Should().BeTrue();
        });
    }

    [TestMethod]
    public async Task AsyncStartupValidator_Cancellation_IsObservedAsync() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AsyncOptionsSettings>, AsyncOptionsSettingsValidator>();
        services.AddOptions<AsyncOptionsSettings>()
                .Configure(static options => options.Name = string.Empty)
                .AddValidatedOptions();

        await using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IAsyncStartupValidator>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var act = async () => await validator.ValidateAsync(cancellation.Token);

        await MSTestAssertions.ThatAsync(async () => await act.Should().ThrowAsync<OperationCanceledException>());
    }

    [TestMethod]
    public void SyncPath_OnAsyncOnlyValidator_ReturnsSkipOnNet11() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AsyncOptionsSettings>, AsyncOptionsSettingsValidator>();
        services.AddOptions<AsyncOptionsSettings>()
                .Configure(static options => options.Name = "worker")
                .AddValidatedOptions();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AsyncOptionsSettings>>().Value;

        MSTestAssertions.That(() => options.Name.Should().Be("worker"));
    }

    [TestMethod]
    public void AddValidatedOptions_RegistersOneAsyncValidator() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AsyncOptionsSettings>, AsyncOptionsSettingsValidator>();
        services.AddValidatedOptions<AsyncOptionsSettings>();
        services.AddValidatedOptions<AsyncOptionsSettings>();

        using var provider = services.BuildServiceProvider();
        var validators = provider.GetServices<IValidateOptions<AsyncOptionsSettings>>().ToArray();

        MSTestAssertions.That(() => validators.Should().HaveCount(2));
        MSTestAssertions.That(() => validators.OfType<IAsyncValidateOptions<AsyncOptionsSettings>>().Should().ContainSingle());
    }
}
