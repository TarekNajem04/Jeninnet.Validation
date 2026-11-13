//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Extensions.Validation.Tests;

internal sealed class OptionsSettings {
    public string? Name { get; set; }

    public int Retries { get; set; }
}

file sealed class OptionsSettingsValidator : Validator<OptionsSettings> {
    protected override void Configure(ValidatorRuleBuilder<OptionsSettings> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static options => options.Name, nameof(OptionsSettings.Name))
               .NotEmpty()
               .WithCode("NAME_REQUIRED");

        builder.RuleFor(static options => options.Retries, nameof(OptionsSettings.Retries))
               .InclusiveBetween(1, 5);
    }
}

file sealed class WarningOptionsValidator : IValidator<OptionsSettings> {
    public ValidationResult Validate(OptionsSettings instance) {
        var errors = new ValidationErrorCollector {
            { nameof(OptionsSettings.Name), "warning only", ValidationErrorSeverity.Warning }
        };
        return ValidationResult.FromErrors(errors);
    }

    public ValidationResult Validate(OptionsSettings instance, IEnumerable<string> ruleSets) => Validate(instance);

    public ValueTask<ValidationResult> ValidateAsync(
        OptionsSettings instance,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Validate(instance));

    public ValueTask<ValidationResult> ValidateAsync(
        OptionsSettings instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Validate(instance));
}

[TestClass]
public sealed class OptionsValidationTests {
    [TestMethod]
    public void AddValidatedOptions_ValidOptions_Pass() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<OptionsSettings>, OptionsSettingsValidator>();
        services.AddOptions<OptionsSettings>()
                .Configure(static options => {
                    options.Name = "worker";
                    options.Retries = 3;
                })
                .AddValidatedOptions();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OptionsSettings>>().Value;

        MSTestAssertions.That(() => options.Name.Should().Be("worker"));
        MSTestAssertions.That(() => options.Retries.Should().Be(3));
    }

    [TestMethod]
    public void AddValidatedOptions_InvalidOptions_ReportFieldAndCode() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<OptionsSettings>, OptionsSettingsValidator>();
        services.AddOptions<OptionsSettings>()
                .Configure(static options => {
                    options.Name = string.Empty;
                    options.Retries = 9;
                })
                .AddValidatedOptions();

        using var provider = services.BuildServiceProvider();
        var act = () => _ = provider.GetRequiredService<IOptions<OptionsSettings>>().Value;

        MSTestAssertions.That(() => act.Should()
            .ThrowExactly<OptionsValidationException>()
            .Which.Failures.Any(static failure =>
                failure.Contains(nameof(OptionsSettings.Name), StringComparison.Ordinal) &&
                failure.Contains("NAME_REQUIRED", StringComparison.Ordinal))
            .Should().BeTrue());
        MSTestAssertions.That(() => act.Should()
            .ThrowExactly<OptionsValidationException>()
            .Which.Failures.Any(static failure =>
                failure.Contains(nameof(OptionsSettings.Retries), StringComparison.Ordinal))
            .Should().BeTrue());
    }

    [TestMethod]
    public void AddValidatedOptions_BindConfiguration_FailsWhenInvalid() {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Worker:Name"] = string.Empty,
                ["Worker:Retries"] = "9"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<IValidator<OptionsSettings>, OptionsSettingsValidator>();
        services.AddOptions<OptionsSettings>()
                .BindConfiguration("Worker")
                .AddValidatedOptions();

        using var provider = services.BuildServiceProvider();
        var act = () => _ = provider.GetRequiredService<IOptions<OptionsSettings>>().Value;

        MSTestAssertions.That(() => act.Should()
            .ThrowExactly<OptionsValidationException>()
            .Which.Failures.Any(static failure =>
                failure.Contains(nameof(OptionsSettings.Name), StringComparison.Ordinal) &&
                failure.Contains("NAME_REQUIRED", StringComparison.Ordinal))
            .Should().BeTrue());
    }

    [TestMethod]
    public void AddValidatedOptions_MissingValidator_FailsClearly() {
        var services = new ServiceCollection();
        services.AddOptions<OptionsSettings>()
                .Configure(static options => options.Name = "worker")
                .AddValidatedOptions();

        using var provider = services.BuildServiceProvider();
        var act = () => _ = provider.GetRequiredService<IOptions<OptionsSettings>>().Value;

        MSTestAssertions.That(() => act.Should()
            .ThrowExactly<OptionsValidationException>()
            .Which.Failures.Any(static failure =>
                failure.Contains("No IValidator<OptionsSettings>", StringComparison.Ordinal))
            .Should().BeTrue());
    }

    [TestMethod]
    public void AddValidatedOptions_WarningOnly_DoesNotFail() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<OptionsSettings>, WarningOptionsValidator>();
        services.AddOptions<OptionsSettings>()
                .Configure(static options => options.Name = "worker")
                .AddValidatedOptions();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OptionsSettings>>().Value;

        MSTestAssertions.That(() => options.Name.Should().Be("worker"));
    }

    [TestMethod]
    public void AddValidatedOptions_NamedOptions_FailWhenInvalid() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<OptionsSettings>, OptionsSettingsValidator>();
        services.AddOptions<OptionsSettings>("worker")
                .Configure(static options => {
                    options.Name = string.Empty;
                    options.Retries = 1;
                })
                .AddValidatedOptions();

        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<OptionsSettings>>();
        var act = () => _ = monitor.Get("worker");

        MSTestAssertions.That(() => act.Should()
            .ThrowExactly<OptionsValidationException>()
            .Which.Failures.All(static failure => failure.StartsWith("worker: ", StringComparison.Ordinal))
            .Should().BeTrue());
    }

    [TestMethod]
    public void AddValidatedOptions_ServiceCollectionOverload_IsIdempotent() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<OptionsSettings>, OptionsSettingsValidator>();
        services.AddValidatedOptions<OptionsSettings>();
        services.AddValidatedOptions<OptionsSettings>();
        services.Configure<OptionsSettings>(static options => {
            options.Name = "worker";
            options.Retries = 2;
        });

        using var provider = services.BuildServiceProvider();
        var validators = provider.GetServices<IValidateOptions<OptionsSettings>>().ToArray();
        var options = provider.GetRequiredService<IOptions<OptionsSettings>>().Value;

        MSTestAssertions.That(() => validators.Should().HaveCount(1));
        MSTestAssertions.That(() => options.Name.Should().Be("worker"));
    }
}
