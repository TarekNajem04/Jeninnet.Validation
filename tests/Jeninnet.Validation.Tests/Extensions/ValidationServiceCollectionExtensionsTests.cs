//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Extensions;

internal sealed class GeneratedRegistrationModel {
    public string? Name { get; set; }
}

internal sealed class GeneratedRegistrationValidator : IValidator<GeneratedRegistrationModel>, IGeneratedValidator {
    public ValidationResult Validate(GeneratedRegistrationModel instance) => ValidationResult.Success;

    public ValidationResult Validate(GeneratedRegistrationModel instance, IEnumerable<string> ruleSets) =>
        ValidationResult.Success;

    public ValueTask<ValidationResult> ValidateAsync(
        GeneratedRegistrationModel instance,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValidationResult.Success);

    public ValueTask<ValidationResult> ValidateAsync(
        GeneratedRegistrationModel instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValidationResult.Success);
}

[TestClass]
public sealed class ValidationServiceCollectionExtensionsTests {
    [TestMethod]
    public void AddValidation_RegistersInfrastructure() {
        var services = new ServiceCollection();

        var returned = services.AddValidation();
        using var provider = services.BuildServiceProvider();

        MSTestAssertions.That(() => ReferenceEquals(returned, services).Should().BeTrue());
        MSTestAssertions.That(() => provider.GetService<IValidatorFactory>().Should().NotBeNull());
        MSTestAssertions.That(() => services.Count(descriptor => descriptor.ServiceType == typeof(IValidatorFactory))
            .Should().Be(1));
    }

    [TestMethod]
    public void AddValidationCore_DoesNotRegisterLocalization() {
        var services = new ServiceCollection();
        services.AddValidationCore();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        MSTestAssertions.That(() => provider
            .GetService<Jeninnet.Localization.Abstractions.IMessageLocalizer>()
            .Should().BeNull());
        MSTestAssertions.That(() => services.Any(descriptor =>
            descriptor.ServiceType.FullName == "Microsoft.Extensions.Localization.IStringLocalizerFactory")
            .Should().BeFalse());
    }

    [TestMethod]
    public void AddValidationLocalization_RegistersThePipeline() {
        var services = new ServiceCollection();
        services.AddValidationLocalization();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        MSTestAssertions.That(() => provider
            .GetService<Jeninnet.Localization.Abstractions.IMessageLocalizer>()
            .Should().NotBeNull());
    }

    [TestMethod]
    public void AddValidationCore_ResolvesValidatorFactoryFromRootWithScopeValidation() {
        var services = new ServiceCollection();
        services.AddValidationCore();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        var factory = provider.GetRequiredService<IValidatorFactory>();
        var accessor = factory.Accessor<GeneratedRegistrationModel, string?>(
            static model => model.Name,
            nameof(GeneratedRegistrationModel.Name));

        MSTestAssertions.That(() => accessor.MemberName.Should().Be(nameof(GeneratedRegistrationModel.Name)));
    }

    [TestMethod]
    public void AddValidationCore_WithLocalizationStore_DoesNotRegisterAStringLocalizerFactory() {
        var services = new ServiceCollection();
        services.AddSingleton<ILocalizationStore>(_ => throw new NotSupportedException());
        services.AddValidationCore();

        MSTestAssertions.That(() => services.Any(descriptor =>
            descriptor.ServiceType == typeof(IStringLocalizerFactory))
            .Should().BeFalse());
    }

    [TestMethod]
    public void AddValidationCore_PreservesACustomStringLocalizerFactory() {
        var services = new ServiceCollection();
        services.AddSingleton<IStringLocalizerFactory>(_ => throw new NotSupportedException());
        services.AddValidationCore();

        MSTestAssertions.That(() => services.Count(descriptor =>
            descriptor.ServiceType == typeof(IStringLocalizerFactory))
            .Should().Be(1));
    }

    [TestMethod]
    public void AddValidationCore_ThenAddLocalizationWithOptions_KeepsTheConfiguredOptions() {
        var services = new ServiceCollection();
        services.AddValidationCore();
        services.AddLocalization(options => options.DefaultCulture = "ar");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Jeninnet.Localization.Abstractions.Options.LocalizationOptions>();

        MSTestAssertions.That(() => options.DefaultCulture.Should().Be("ar"));
    }

    [TestMethod]
    public void AddValidationCore_ThenAddLocalization_UsesTheStoreBackedFactory() {
        var services = new ServiceCollection();
        services.AddSingleton<ILocalizationStore>(_ => throw new NotSupportedException());
        services.AddValidationCore();
        JeninnetLocalizationExtensions.AddLocalization(services);

        AssertStoreBackedFactory(services);
    }

    [TestMethod]
    public void AddLocalization_ThenAddValidationCore_UsesTheStoreBackedFactory() {
        var services = new ServiceCollection();
        services.AddSingleton<ILocalizationStore>(_ => throw new NotSupportedException());
        JeninnetLocalizationExtensions.AddLocalization(services);
        services.AddValidationCore();

        AssertStoreBackedFactory(services);
    }

    private static void AssertStoreBackedFactory(IServiceCollection services) {
        var factories = services.Where(descriptor =>
            descriptor.ServiceType == typeof(IStringLocalizerFactory)).ToList();

        MSTestAssertions.That(() => factories.Should().HaveCount(1));
        MSTestAssertions.That(() => factories[0].ImplementationType?.FullName
            .Should().Be("Jeninnet.Localization.Core.Localization.CustomStringLocalizerFactory"));
    }

    [TestMethod]
    public void AddGeneratedValidators_RegistersConcreteAndInterfaceTypes() {
        var services = new ServiceCollection();

        var returned = services.AddGeneratedValidators(typeof(GeneratedRegistrationValidator));
        using var provider = services.BuildServiceProvider();

        MSTestAssertions.That(() => ReferenceEquals(returned, services).Should().BeTrue());
        MSTestAssertions.That(() => provider.GetService<GeneratedRegistrationValidator>().Should().NotBeNull());
        MSTestAssertions.That(() => provider.GetService<IValidator<GeneratedRegistrationModel>>()
            .Should().BeOfType<GeneratedRegistrationValidator>());
        MSTestAssertions.That(() => provider.GetService<IValidatorFactory>().Should().NotBeNull());
        MSTestAssertions.That(() => provider.GetService<IServiceProvider>().Should().NotBeNull());
    }

    [TestMethod]
    public void AddGeneratedValidators_ScansTheSuppliedAssembly() {
        var services = new ServiceCollection();

        services.AddGeneratedValidators(typeof(GeneratedRegistrationValidator).Assembly);
        using var provider = services.BuildServiceProvider();

        MSTestAssertions.That(() => provider.GetService<IValidator<GeneratedRegistrationModel>>()
            .Should().BeOfType<GeneratedRegistrationValidator>());
    }

    [TestMethod]
    public void AddGeneratedValidators_NullTypeArrayIsANoOp() {
        var services = new ServiceCollection();

        var returned = services.AddGeneratedValidators((Type[])null!);
        using var provider = services.BuildServiceProvider();

        MSTestAssertions.That(() => ReferenceEquals(returned, services).Should().BeTrue());
        MSTestAssertions.That(() => provider.GetService<GeneratedRegistrationValidator>().Should().BeNull());
    }

    [TestMethod]
    public void AddGeneratedValidators_IgnoresNonGeneratedAndAbstractTypes() {
        var services = new ServiceCollection();

        services.AddGeneratedValidators(
            typeof(object),
            typeof(IValidator<GeneratedRegistrationModel>),
            typeof(GeneratedRegistrationValidator));

        using var provider = services.BuildServiceProvider();

        MSTestAssertions.That(() => provider.GetService<GeneratedRegistrationValidator>().Should().NotBeNull());
    }

    [TestMethod]
    public void AddGeneratedValidators_IsIdempotentForInfrastructure() {
        var services = new ServiceCollection();

        services.AddGeneratedValidators(typeof(GeneratedRegistrationValidator));
        services.AddGeneratedValidators(typeof(GeneratedRegistrationValidator));

        using var provider = services.BuildServiceProvider();

        MSTestAssertions.That(() => provider.GetServices<IValidatorFactory>().Should().HaveCount(1));
    }
}
