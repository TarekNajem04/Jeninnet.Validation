//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

#if NET11_0_OR_GREATER
using ValidatableTypeInfo = Microsoft.Extensions.Validation.IValidatableTypeInfo;
#else
using ValidatableTypeInfo = Microsoft.Extensions.Validation.IValidatableInfo;
#endif

namespace Jeninnet.Validation.Extensions.Validation.Tests;

internal sealed class BridgeModel {
    public string? Name { get; set; }
}

file sealed class BridgeModelValidator : Validator<BridgeModel> {
    protected override void Configure(ValidatorRuleBuilder<BridgeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static model => model.Name, nameof(BridgeModel.Name)).NotEmpty();
    }
}

[Jeninnet.Validation.DataAnnotations.JeninnetValidatable(typeof(BridgeModelValidator))]
internal sealed class AttributedBridgeModel {
    public string? Name { get; set; }
}

file sealed class AsyncBridgeModelValidator : Validator<BridgeModel> {
    protected override void Configure(ValidatorRuleBuilder<BridgeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(BridgeModel.Name))
               .Must(static (value, _) => new ValueTask<bool>(value == "async-ok"));
    }
}

[TestClass]
public sealed class ExtensionsValidationBridgeTests {
    public TestContext TestContext { get; set; } = null!;
    [TestMethod]
    public async Task AddJeninnetValidation_AddsResolverAndWritesMemberErrorsAsync() {
        var services = new ServiceCollection();
        services.AddValidation();
        services.AddScoped<IValidator<BridgeModel>, BridgeModelValidator>();
        services.AddJeninnetValidation();

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        var found = options.TryGetValidatableTypeInfo(typeof(BridgeModel), out var info);

        MSTestAssertions.That(() => found.Should().BeTrue());

        if(info is null) {
            Assert.Fail("The Jeninnet validation resolver was not selected.");
            return;
        }

        var model = new BridgeModel { Name = string.Empty };
        var context = CreateContext(model, provider, options);

        await info.ValidateAsync(model, context, CancellationToken.None);

        var errors = context.ValidationErrors!;
        MSTestAssertions.That(() => errors.ContainsKey(nameof(BridgeModel.Name)).Should().BeTrue());
        MSTestAssertions.That(() => errors[nameof(BridgeModel.Name)].Should().HaveCount(1));
    }

    [TestMethod]
    public async Task GenericRegistration_IsReflectionFreeAndWorksAsync() {
        var services = new ServiceCollection();
        services.AddValidation();
        services.AddScoped<IValidator<BridgeModel>, BridgeModelValidator>();
        services.AddJeninnetValidation<BridgeModel>();

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;
        var found = options.TryGetValidatableTypeInfo(typeof(BridgeModel), out var info);

        MSTestAssertions.That(() => found.Should().BeTrue());

        if(info is null) {
            Assert.Fail("The Jeninnet validation resolver was not selected.");
            return;
        }

        var model = new BridgeModel { Name = "valid" };
        var context = CreateContext(model, provider, options);

        await info.ValidateAsync(model, context, CancellationToken.None);

        MSTestAssertions.That(() => (context.ValidationErrors?.Count ?? 0).Should().Be(0));
    }

    [TestMethod]
    public async Task AsyncValidatorRules_RunThroughTheBridgeAsync() {
        var services = new ServiceCollection();
        services.AddValidation();
        services.AddScoped<IValidator<BridgeModel>, AsyncBridgeModelValidator>();
        services.AddJeninnetValidation<BridgeModel>();

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;
        var found = options.TryGetValidatableTypeInfo(typeof(BridgeModel), out var info);

        MSTestAssertions.That(() => found.Should().BeTrue());

        if(info is null) {
            Assert.Fail("The Jeninnet validation resolver was not selected.");
            return;
        }

        var model = new BridgeModel { Name = "wrong" };
        var context = CreateContext(model, provider, options);

        await info.ValidateAsync(model, context, CancellationToken.None);

        MSTestAssertions.That(() => context.ValidationErrors!.ContainsKey(nameof(BridgeModel.Name)).Should().BeTrue());
    }

    [TestMethod]
    public void Resolver_SkipsTypesAlreadyUsingTheDataAnnotationsBridge() {
        var resolver = new JeninnetValidatableInfoResolver(
            new Dictionary<Type, Func<ValidatableTypeInfo>> {
                [typeof(AttributedBridgeModel)] = static () => new JeninnetValidatableInfo<AttributedBridgeModel>()
            });

        MSTestAssertions.That(() => resolver.TryGetValidatableTypeInfo(typeof(AttributedBridgeModel), out _).Should().BeFalse());
    }

    [TestMethod]
    public async Task MinimalApiWithAddValidation_ReturnsValidationProblemAsync() {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddValidation();
        builder.Services.AddScoped<IValidator<BridgeModel>, BridgeModelValidator>();
        builder.Services.AddJeninnetValidation();

        await using var app = builder.Build();
        app.MapPost("/bridge", (BridgeModel model) => Results.Ok(model));

        await app.StartAsync(TestContext.CancellationToken);
        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync(
            "/bridge",
            new BridgeModel { Name = string.Empty },
            CancellationToken.None);

        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(CancellationToken.None);

        MSTestAssertions.That(() => response.StatusCode.Should().Be(HttpStatusCode.BadRequest));
        MSTestAssertions.That(() => problem!["errors"]!.AsObject()
            .ContainsKey(nameof(BridgeModel.Name)).Should().BeTrue());
    }

    private static ValidateContext CreateContext(
        object model,
        IServiceProvider provider,
        ValidationOptions options) {
#if NET11_0_OR_GREATER
        _ = model;
        return new ValidateContext {
            ServiceProvider = provider,
            ValidationOptions = options,
            CurrentValidationPath = string.Empty
        };
#else
        var validationContext = new ValidationContext(model, provider, items: null);

        return new ValidateContext {
            ValidationContext = validationContext,
            ValidationOptions = options,
            ValidationErrors = [with(StringComparer.Ordinal)],
            CurrentValidationPath = string.Empty
        };
#endif
    }
}
