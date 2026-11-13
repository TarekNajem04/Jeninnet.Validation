//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.AspNetCore.Tests;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

internal sealed class MvcModel {
    public string? Name { get; set; }
}

file sealed class MvcModelValidator : Validator<MvcModel> {
    protected override void Configure(ValidatorRuleBuilder<MvcModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(MvcModel.Name))
               .NotEmpty("Name is required.");
    }
}

file sealed class RecordingValidator : IValidator<MvcModel> {
    public IEnumerable<string>? LastRuleSets { get; private set; }

    public ValidationResult Validate(MvcModel instance) => ValidationResult.Success;

    public ValidationResult Validate(MvcModel instance, IEnumerable<string>? ruleSets) {
        LastRuleSets = ruleSets;
        return ValidationResult.Success;
    }

    public ValueTask<ValidationResult> ValidateAsync(
        MvcModel instance,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValidationResult.Success);

    public ValueTask<ValidationResult> ValidateAsync(
        MvcModel instance,
        IEnumerable<string>? ruleSets,
        CancellationToken cancellationToken = default) {
        LastRuleSets = ruleSets;
        return ValueTask.FromResult(ValidationResult.Success);
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
// Mutates the process-wide ValidationConfiguration fallback and restores it.
[DoNotParallelize]
public sealed class JeninnetValidationFilterTests {
    [TestMethod]
    public async Task Filter_AddsModelStateErrorsAndReturnsBadRequestAsync() {
        await using var provider = CreateProvider(new MvcModelValidator());
        var filter = new JeninnetValidationFilter(new MvcValidationOptions());
        var (context, actionContext) = CreateContext(provider, new MvcModel { Name = "" });

        await filter.OnActionExecutionAsync(context, Next(actionContext));

        var badRequest = context.Result as BadRequestObjectResult;
        MSTestAssertions.That(() => badRequest.Should().NotBeNull());
        MSTestAssertions.That(() => context.ModelState.IsValid.Should().BeFalse());
        MSTestAssertions.That(() => context.ModelState["Name"]!.Errors[0].ErrorMessage.Should().Be("Name is required."));
    }

    [TestMethod]
    public async Task Filter_SkipsWhenAttributeIsPresentAsync() {
        await using var provider = CreateProvider(new MvcModelValidator());
        var filter = new JeninnetValidationFilter(new MvcValidationOptions());
        var (context, actionContext) = CreateContext(provider, new MvcModel { Name = "" });
        actionContext.ActionDescriptor.EndpointMetadata.Add(new SkipJeninnetValidationAttribute());

        var nextCalled = false;
        await filter.OnActionExecutionAsync(context, () => {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
        });

        MSTestAssertions.That(() => context.Result.Should().BeNull());
        MSTestAssertions.That(() => nextCalled.Should().BeTrue());
    }

    [TestMethod]
    public async Task Filter_ForwardsRuleSetsAsync() {
        var validator = new RecordingValidator();
        await using var provider = CreateProvider(validator);
        var filter = new JeninnetValidationFilter(new MvcValidationOptions());
        var (context, actionContext) = CreateContext(provider, new MvcModel { Name = "Valid" });
        actionContext.ActionDescriptor.EndpointMetadata.Add(new JeninnetValidationRuleSetsAttribute("Admin"));

        await filter.OnActionExecutionAsync(context, Next(actionContext));

        MSTestAssertions.That(() => context.Result.Should().BeNull());
        MSTestAssertions.That(() => validator.LastRuleSets!.SequenceEqual(["Admin"]).Should().BeTrue());
    }

    [TestMethod]
    public async Task Filter_PassesThroughWhenValidatorIsMissingAsync() {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var filter = new JeninnetValidationFilter(new MvcValidationOptions());
        var (context, actionContext) = CreateContext(provider, new MvcModel { Name = "" });

        await filter.OnActionExecutionAsync(context, Next(actionContext));

        MSTestAssertions.That(() => context.Result.Should().BeNull());
    }

    [TestMethod]
    public async Task Filter_PassesThroughNullArgumentsAsync() {
        await using var provider = CreateProvider(new MvcModelValidator());
        var filter = new JeninnetValidationFilter(new MvcValidationOptions());
        var (context, actionContext) = CreateContext(provider, model: null);

        await filter.OnActionExecutionAsync(context, Next(actionContext));

        MSTestAssertions.That(() => context.Result.Should().BeNull());
    }

    [TestMethod]
    public async Task Filter_SkipsParameterMarkedSkipValidationAsync() {
        await using var provider = CreateProvider(new MvcModelValidator());
        var filter = new JeninnetValidationFilter(new MvcValidationOptions());
        var (context, actionContext) = CreateControllerContext(provider, new MvcModel { Name = "" });
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () => {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
        });

        MSTestAssertions.That(() => context.Result.Should().BeNull());
        MSTestAssertions.That(() => context.ModelState.IsValid.Should().BeTrue());
        MSTestAssertions.That(() => nextCalled.Should().BeTrue());
    }

    [TestMethod]
    public async Task Filter_UsesGlobalValidatorFactoryWhenNotRegisteredAsync() {
        var previous = ValidationConfiguration.Options;

        try {
            ValidationConfiguration.Configure(static options => options with {
                ValidatorFactory = static (type, _) =>
                    type == typeof(MvcModel) ? new MvcModelValidator() : null
            });

            await using var provider = new ServiceCollection().BuildServiceProvider();
            var filter = new JeninnetValidationFilter(new MvcValidationOptions());
            var (context, actionContext) = CreateContext(provider, new MvcModel { Name = "" });

            await filter.OnActionExecutionAsync(context, Next(actionContext));

            MSTestAssertions.That(() => (context.Result is BadRequestObjectResult).Should().BeTrue());
            MSTestAssertions.That(() => context.ModelState["Name"]!.Errors[0].ErrorMessage.Should().Be("Name is required."));
        }
        finally {
            ValidationConfiguration.Configure(_ => previous);
        }
    }

    private static void SkippedAction([SkipValidation] MvcModel model) => _ = model;

    private static (ActionExecutingContext Context, ActionContext ActionContext) CreateControllerContext(
        ServiceProvider provider,
        MvcModel? model) {
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        var method = typeof(JeninnetValidationFilterTests).GetMethod(
            nameof(SkippedAction),
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var actionDescriptor = new ControllerActionDescriptor {
            EndpointMetadata = [],
            MethodInfo = method,
            ControllerTypeInfo = typeof(JeninnetValidationFilterTests).GetTypeInfo(),
            Parameters = [
                new ControllerParameterDescriptor {
                    Name = "model",
                    ParameterType = typeof(MvcModel),
                    ParameterInfo = method.GetParameters()[0]
                }
            ]
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        var arguments = new Dictionary<string, object?> { ["model"] = model };

        return (new ActionExecutingContext(actionContext, [], arguments, new object()), actionContext);
    }

    private static ServiceProvider CreateProvider(IValidator<MvcModel> validator) {
        var services = new ServiceCollection();
        services.AddSingleton(validator);
        return services.BuildServiceProvider();
    }

    private static (ActionExecutingContext Context, ActionContext ActionContext) CreateContext(
        ServiceProvider provider,
        MvcModel? model) {
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        var actionDescriptor = new ActionDescriptor {
            EndpointMetadata = []
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        var arguments = new Dictionary<string, object?> { ["model"] = model };
        var context = new ActionExecutingContext(actionContext, [], arguments, new object());

        return (context, actionContext);
    }

    private static ActionExecutionDelegate Next(ActionContext actionContext) =>
        () => Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
}
