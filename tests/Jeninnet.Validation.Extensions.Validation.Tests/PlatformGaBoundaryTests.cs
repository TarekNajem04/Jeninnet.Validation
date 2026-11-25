//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

#if NET11_0_OR_GREATER

using System.Reflection;
using Microsoft.Extensions.Options;
using JeninnetError = Jeninnet.Validation.ValidationError;

namespace Jeninnet.Validation.Extensions.Validation.Tests;

internal sealed class GaBoundaryModel {
    public string? Street { get; set; }
}

internal sealed class GaBoundaryDependency(string prefix) {
    public string Prefix { get; } = prefix;
}

internal sealed class GaBoundaryValidator(GaBoundaryDependency dependency) : Validator<GaBoundaryModel> {
    protected override void Configure(ValidatorRuleBuilder<GaBoundaryModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Street, nameof(GaBoundaryModel.Street))
               .NotEmpty()
               .WithMessage($"{dependency.Prefix}: Street is required.");
    }
}

internal sealed class GaBoundaryMessageValidator : IValidator<GaBoundaryModel> {
    public ValidationResult Validate(GaBoundaryModel instance) => Result();

    public ValidationResult Validate(GaBoundaryModel instance, IEnumerable<string> ruleSets) => Result();

    public ValueTask<ValidationResult> ValidateAsync(
        GaBoundaryModel instance,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Result());

    public ValueTask<ValidationResult> ValidateAsync(
        GaBoundaryModel instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Result());

    private static ValidationResult Result() {
        var errors = new ValidationErrorCollector {
            new JeninnetError(
                "Street",
                new MessageId("GaKey"),
                messageKey: "GaKey",
                message: "Authored message",
                errorCode: "GaCode"),
            new JeninnetError(
                "City",
                new MessageId("GaKey"),
                messageKey: "GaKey",
                errorCode: "GaCode"),
            new JeninnetError(
                "Zip",
                new MessageId("GaCode"),
                errorCode: "GaCode")
        };

        return ValidationResult.FromErrors(errors);
    }
}

[TestClass]
[DoNotParallelize]
public sealed class PlatformGaBoundaryTests {
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("PlatformGaBoundary")]
    public async Task GaBoundary_TypeInfo_UsesPlatformServiceProviderAndNestedPathAsync() {
        await using var provider = CreateProvider(services => {
            services.AddSingleton(new GaBoundaryDependency("di"));
            services.AddScoped<IValidator<GaBoundaryModel>, GaBoundaryValidator>();
        });
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;
        var found = options.TryGetValidatableTypeInfo(typeof(GaBoundaryModel), out var info);

        MSTestAssertions.That(() => found.Should().BeTrue());

        if(info is null) {
            false.Should().BeTrue("The Jeninnet GA resolver was not selected.");
            return;
        }

        var model = new GaBoundaryModel { Street = string.Empty };
        var context = new ValidateContext {
            ServiceProvider = provider,
            ValidationOptions = options,
            CurrentValidationPath = "Customer.Address"
        };

        await info.ValidateAsync(model, context, TestContext.CancellationToken);

        var errors = context.ValidationErrors!;
        MSTestAssertions.That(() => errors.ContainsKey("Customer.Address.Street").Should().BeTrue());

        var error = errors["Customer.Address.Street"].Single();
        MSTestAssertions.That(() => error.Name.Should().Be("Street"));
        MSTestAssertions.That(() => error.Path.Should().Be("Customer.Address.Street"));
        MSTestAssertions.That(() => error.ErrorMessage.Should().Be("di: Street is required."));
        MSTestAssertions.That(() => error.Container.Should().BeSameAs(model));
    }

    [TestMethod]
    [TestCategory("PlatformGaBoundary")]
    public async Task GaBoundary_ParameterInfo_ResolvesAndValidatesAsync() {
        await using var provider = CreateProvider(services => {
            services.AddSingleton(new GaBoundaryDependency("param"));
            services.AddScoped<IValidator<GaBoundaryModel>, GaBoundaryValidator>();
        });
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;
        var parameter = typeof(PlatformGaBoundaryTests)
            .GetMethod(nameof(HandleGaModel), BindingFlags.NonPublic | BindingFlags.Static)!
            .GetParameters()[0];
        var found = options.TryGetValidatableParameterInfo(parameter, out var info);

        MSTestAssertions.That(() => found.Should().BeTrue());

        if(info is null) {
            false.Should().BeTrue("The Jeninnet GA parameter resolver was not selected.");
            return;
        }

        var model = new GaBoundaryModel { Street = string.Empty };
        var context = new ValidateContext {
            ServiceProvider = provider,
            ValidationOptions = options,
            CurrentValidationPath = "Request"
        };

        await info.ValidateAsync(model, context, TestContext.CancellationToken);

        var errors = context.ValidationErrors!;
        MSTestAssertions.That(() => errors.ContainsKey("Request.Street").Should().BeTrue());
        MSTestAssertions.That(() => errors["Request.Street"].Single().ErrorMessage.Should().Be("param: Street is required."));
    }

    [TestMethod]
    [TestCategory("PlatformGaBoundary")]
    public async Task GaBoundary_MessageOrder_UsesAuthoredThenKeyThenCodeAsync() {
        await using var provider = CreateProvider(services =>
            services.AddSingleton<IValidator<GaBoundaryModel>, GaBoundaryMessageValidator>());
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;
        var found = options.TryGetValidatableTypeInfo(typeof(GaBoundaryModel), out var info);

        MSTestAssertions.That(() => found.Should().BeTrue());

        if(info is null) {
            false.Should().BeTrue("The Jeninnet GA resolver was not selected.");
            return;
        }

        var context = new ValidateContext {
            ServiceProvider = provider,
            ValidationOptions = options
        };

        await info.ValidateAsync(new GaBoundaryModel(), context, TestContext.CancellationToken);

        var errors = context.ValidationErrors!;
        MSTestAssertions.That(() => errors["Street"].Single().ErrorMessage.Should().Be("Authored message"));
        MSTestAssertions.That(() => errors["City"].Single().ErrorMessage.Should().Be("GaKey"));
        MSTestAssertions.That(() => errors["Zip"].Single().ErrorMessage.Should().Be("GaCode"));
    }

    [TestMethod]
    [TestCategory("PlatformGaBoundary")]
    public async Task GaBoundary_DisableValidation_OptOutSkipsBridgeAsync() {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddValidation();
        builder.Services.AddSingleton(new GaBoundaryDependency("disabled"));
        builder.Services.AddScoped<IValidator<GaBoundaryModel>, GaBoundaryValidator>();
        builder.Services.AddJeninnetValidation<GaBoundaryModel>();

        await using var app = builder.Build();
        var handlerCalled = false;
        app.MapPost("/ga-boundary-disabled", (GaBoundaryModel model) => {
            handlerCalled = true;
            return Results.Ok(model);
        }).DisableValidation();

        await app.StartAsync(TestContext.CancellationToken);
        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync(
            "/ga-boundary-disabled",
            new GaBoundaryModel { Street = string.Empty },
            TestContext.CancellationToken);

        MSTestAssertions.That(() => response.StatusCode.Should().Be(HttpStatusCode.OK));
        MSTestAssertions.That(() => handlerCalled.Should().BeTrue());
    }

    private static void HandleGaModel(GaBoundaryModel model) => _ = model;

    private static ServiceProvider CreateProvider(Action<IServiceCollection>? configure = null) {
        var services = new ServiceCollection();
        services.AddValidation();
        services.AddJeninnetValidation<GaBoundaryModel>();
        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }
}

#endif
