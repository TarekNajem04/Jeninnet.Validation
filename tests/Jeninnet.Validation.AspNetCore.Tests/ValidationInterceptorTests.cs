//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.AspNetCore.Tests;

// ---------------------------------------------------------------------------
// Validators and interceptors
// ---------------------------------------------------------------------------

internal sealed class InterceptorEndpointValidator(ValidationResult result) : IValidator<EndpointRequest> {
    public int AsyncCalls { get; private set; }

    public ValidationResult Validate(EndpointRequest instance) => result;

    public ValidationResult Validate(EndpointRequest instance, IEnumerable<string> ruleSets) => result;

    public ValueTask<ValidationResult> ValidateAsync(
        EndpointRequest instance,
        CancellationToken cancellationToken = default) {
        AsyncCalls++;
        return ValueTask.FromResult(result);
    }

    public ValueTask<ValidationResult> ValidateAsync(
        EndpointRequest instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) {
        AsyncCalls++;
        return ValueTask.FromResult(result);
    }
}

internal sealed class InterceptorMvcValidator(ValidationResult result) : IValidator<MvcModel> {
    public int AsyncCalls { get; private set; }

    public ValidationResult Validate(MvcModel instance) => result;

    public ValidationResult Validate(MvcModel instance, IEnumerable<string> ruleSets) => result;

    public ValueTask<ValidationResult> ValidateAsync(
        MvcModel instance,
        CancellationToken cancellationToken = default) {
        AsyncCalls++;
        return ValueTask.FromResult(result);
    }

    public ValueTask<ValidationResult> ValidateAsync(
        MvcModel instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) {
        AsyncCalls++;
        return ValueTask.FromResult(result);
    }
}

internal sealed class TestInterceptor(
    string name,
    List<string> log,
    Func<ValidationInterceptorContext, ValidationResult?>? before = null,
    Func<ValidationResult, ValidationResult>? after = null,
    Action<ValidationInterceptorContext>? captured = null) : IValidationInterceptor {
    public ValueTask<ValidationResult?> BeforeValidationAsync(
        ValidationInterceptorContext context,
        CancellationToken cancellationToken = default) {
        captured?.Invoke(context);
        log.Add($"{name}:before");
        return ValueTask.FromResult(before?.Invoke(context));
    }

    public ValueTask<ValidationResult> AfterValidationAsync(
        ValidationInterceptorContext context,
        ValidationResult result,
        CancellationToken cancellationToken = default) {
        log.Add($"{name}:after");
        return ValueTask.FromResult(after is null ? result : after(result));
    }
}

internal sealed class CancelingInterceptor : IValidationInterceptor {
    public ValueTask<ValidationResult?> BeforeValidationAsync(
        ValidationInterceptorContext context,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<ValidationResult?>(null);
    }

    public ValueTask<ValidationResult> AfterValidationAsync(
        ValidationInterceptorContext context,
        ValidationResult result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(result);
}

internal sealed class FailingInterceptor : IValidationInterceptor {
    public ValueTask<ValidationResult?> BeforeValidationAsync(
        ValidationInterceptorContext context,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromException<ValidationResult?>(new InvalidOperationException("boom"));

    public ValueTask<ValidationResult> AfterValidationAsync(
        ValidationInterceptorContext context,
        ValidationResult result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(result);
}

internal sealed class FirstInterceptor : IValidationInterceptor {
    public ValueTask<ValidationResult?> BeforeValidationAsync(
        ValidationInterceptorContext context,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<ValidationResult?>(null);

    public ValueTask<ValidationResult> AfterValidationAsync(
        ValidationInterceptorContext context,
        ValidationResult result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(result);
}

internal sealed class SecondInterceptor : IValidationInterceptor {
    public ValueTask<ValidationResult?> BeforeValidationAsync(
        ValidationInterceptorContext context,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<ValidationResult?>(null);

    public ValueTask<ValidationResult> AfterValidationAsync(
        ValidationInterceptorContext context,
        ValidationResult result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(result);
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class ValidationInterceptorTests {
    public TestContext TestContext { get; set; } = null!;
    [TestMethod]
    public async Task MinimalApi_BeforeShortCircuitsValidatorAndHandlerAsync() {
        var validator = new InterceptorEndpointValidator(ValidationResult.Success);
        var log = new List<string>();
        var interceptor = new TestInterceptor(
            "short",
            log,
            before: static _ => ErrorResult("Name", "Intercepted."));
        await using var provider = CreateProvider(validator, interceptor);
        var filter = new ValidationEndpointFilter<EndpointRequest>(provider);
        var context = CreateContext(provider, new EndpointRequest(), TestContext.CancellationToken);
        var handlerCalled = false;

        var result = await filter.InvokeAsync(context, _ => {
            handlerCalled = true;
            return ValueTask.FromResult<object?>("next");
        });

        if(result is not ProblemHttpResult { ProblemDetails: HttpValidationProblemDetails details } problem) {
            Assert.Fail("Expected a validation problem result.");
            return;
        }

        MSTestAssertions.That(() => problem.StatusCode.Should().Be(StatusCodes.Status400BadRequest));
        MSTestAssertions.That(() => details.Errors["Name"][0].Should().Be("Intercepted."));
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(0));
        MSTestAssertions.That(() => handlerCalled.Should().BeFalse());
        MSTestAssertions.That(() => log.SequenceEqual(["short:before", "short:after"]).Should().BeTrue());
    }

    [TestMethod]
    public async Task MinimalApi_BeforeWithSuccessResultSkipsValidatorButRunsHandlerAsync() {
        var validator = new InterceptorEndpointValidator(ErrorResult("Name", "Original."));
        var log = new List<string>();
        var interceptor = new TestInterceptor(
            "short",
            log,
            before: static _ => ValidationResult.Success);
        await using var provider = CreateProvider(validator, interceptor);
        var filter = new ValidationEndpointFilter<EndpointRequest>(provider);
        var context = CreateContext(provider, new EndpointRequest(), TestContext.CancellationToken);
        var handlerCalled = false;

        var result = await filter.InvokeAsync(context, _ => {
            handlerCalled = true;
            return ValueTask.FromResult<object?>("next");
        });

        MSTestAssertions.That(() => result.Should().Be("next"));
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(0));
        MSTestAssertions.That(() => handlerCalled.Should().BeTrue());
        MSTestAssertions.That(() => log.SequenceEqual(["short:before", "short:after"]).Should().BeTrue());
    }

    [TestMethod]
    public async Task MinimalApi_AfterCanAddErrorsAsync() {
        var validator = new InterceptorEndpointValidator(ValidationResult.Success);
        ValidationInterceptorContext? captured = null;
        var log = new List<string>();
        var interceptor = new TestInterceptor(
            "after",
            log,
            after: static _ => ErrorResult("Name", "Added by interceptor."),
            captured: context => captured = context);
        await using var provider = CreateProvider(validator, interceptor);
        var filter = new ValidationEndpointFilter<EndpointRequest>(provider);
        var context = CreateContext(provider, new EndpointRequest { Name = "valid" }, TestContext.CancellationToken);

        var result = await filter.InvokeAsync(context, NextAsync);

        if(result is not ProblemHttpResult { ProblemDetails: HttpValidationProblemDetails details } problem) {
            Assert.Fail("Expected a validation problem result.");
            return;
        }

        MSTestAssertions.That(() => problem.StatusCode.Should().Be(StatusCodes.Status400BadRequest));
        MSTestAssertions.That(() => details.Errors["Name"][0].Should().Be("Added by interceptor."));
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(1));
        MSTestAssertions.That(() => captured.Should().NotBeNull());
        MSTestAssertions.That(() => captured!.ModelType.Should().Be(typeof(EndpointRequest)));
        MSTestAssertions.That(() => captured!.ValidatorName.Should().Contain(nameof(InterceptorEndpointValidator)));
        MSTestAssertions.That(() => captured!.Endpoint.Should().BeNull());
        MSTestAssertions.That(() => log.SequenceEqual(["after:before", "after:after"]).Should().BeTrue());
    }

    [TestMethod]
    public async Task MinimalApi_InterceptorsRunInRegistrationOrderAsync() {
        var validator = new InterceptorEndpointValidator(ValidationResult.Success);
        var log = new List<string>();
        var first = new TestInterceptor("first", log);
        var second = new TestInterceptor("second", log);
        await using var provider = CreateProvider(validator, first, second);
        var filter = new ValidationEndpointFilter<EndpointRequest>(provider);
        var context = CreateContext(provider, new EndpointRequest(), TestContext.CancellationToken);

        var result = await filter.InvokeAsync(context, NextAsync);

        MSTestAssertions.That(() => result.Should().Be("next"));
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(1));
        MSTestAssertions.That(() => log
            .SequenceEqual(["first:before", "second:before", "first:after", "second:after"])
            .Should().BeTrue());
    }

    [TestMethod]
    public async Task MinimalApi_WithoutInterceptorsKeepsExistingBehaviorAsync() {
        var invalidValidator = new InterceptorEndpointValidator(ErrorResult("Name", "Original."));
        await using var invalidProvider = CreateProvider(invalidValidator);
        var invalidFilter = new ValidationEndpointFilter<EndpointRequest>(invalidProvider);
        var invalidContext = CreateContext(invalidProvider, new EndpointRequest(), TestContext.CancellationToken);

        var invalidResult = await invalidFilter.InvokeAsync(invalidContext, NextAsync);

        if(invalidResult is not ProblemHttpResult { ProblemDetails: HttpValidationProblemDetails details } problem) {
            Assert.Fail("Expected a validation problem result.");
            return;
        }

        MSTestAssertions.That(() => problem.StatusCode.Should().Be(StatusCodes.Status400BadRequest));
        MSTestAssertions.That(() => details.Errors["Name"][0].Should().Be("Original."));
        MSTestAssertions.That(() => invalidValidator.AsyncCalls.Should().Be(1));

        var validValidator = new InterceptorEndpointValidator(ValidationResult.Success);
        await using var validProvider = CreateProvider(validValidator);
        var validFilter = new ValidationEndpointFilter<EndpointRequest>(validProvider);
        var validContext = CreateContext(validProvider, new EndpointRequest(), TestContext.CancellationToken);

        var validResult = await validFilter.InvokeAsync(validContext, NextAsync);

        MSTestAssertions.That(() => validResult.Should().Be("next"));
        MSTestAssertions.That(() => validValidator.AsyncCalls.Should().Be(1));
    }

    [TestMethod]
    public async Task MinimalApi_OperationCanceledExceptionIsNotSwallowedAsync() {
        var validator = new InterceptorEndpointValidator(ValidationResult.Success);
        await using var provider = CreateProvider(validator, new CancelingInterceptor());
        var filter = new ValidationEndpointFilter<EndpointRequest>(provider);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var context = CreateContext(provider, new EndpointRequest(), cancellation.Token);
        var act = async () => await filter.InvokeAsync(context, NextAsync);

        await MSTestAssertions.ThatAsync(async () => await act.Should().ThrowAsync<OperationCanceledException>());

        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(0));
    }

    [TestMethod]
    public async Task MinimalApi_InterceptorFailureIsWrappedWithInterceptorNameAsync() {
        var validator = new InterceptorEndpointValidator(ValidationResult.Success);
        await using var provider = CreateProvider(validator, new FailingInterceptor());
        var filter = new ValidationEndpointFilter<EndpointRequest>(provider);
        var context = CreateContext(provider, new EndpointRequest(), TestContext.CancellationToken);
        var act = async () => await filter.InvokeAsync(context, NextAsync);

        await MSTestAssertions.ThatAsync(async () => {
            var exception = await act.Should().ThrowAsync<ValidationException>();
            exception.Which.Message.Should().Contain(nameof(FailingInterceptor));
            exception.Which.InnerException.Should().BeOfType<InvalidOperationException>();
        });
    }

    [TestMethod]
    public async Task Mvc_BeforeShortCircuitsValidatorAndAddsModelStateAsync() {
        var validator = new InterceptorMvcValidator(ValidationResult.Success);
        ValidationInterceptorContext? captured = null;
        var log = new List<string>();
        var interceptor = new TestInterceptor(
            "short",
            log,
            before: static _ => ErrorResult("Name", "Intercepted."),
            captured: context => captured = context);
        await using var provider = CreateMvcProvider(validator, interceptor);
        var filter = new JeninnetValidationFilter(new MvcValidationOptions(), [interceptor]);
        var (context, actionContext) = CreateMvcContext(provider, new MvcModel { Name = "" });
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () => {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
        });

        MSTestAssertions.That(() => context.Result.Should().BeOfType<BadRequestObjectResult>());
        MSTestAssertions.That(() => context.ModelState["Name"]!.Errors[0].ErrorMessage.Should().Be("Intercepted."));
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(0));
        MSTestAssertions.That(() => nextCalled.Should().BeFalse());
        MSTestAssertions.That(() => captured.Should().NotBeNull());
        MSTestAssertions.That(() => captured!.ModelType.Should().Be(typeof(MvcModel)));
        MSTestAssertions.That(() => captured!.ActionDescriptor.Should().NotBeNull());
        MSTestAssertions.That(() => log.SequenceEqual(["short:before", "short:after"]).Should().BeTrue());
    }

    [TestMethod]
    public async Task Mvc_BeforeWithSuccessResultSkipsValidatorButRunsActionAsync() {
        var validator = new InterceptorMvcValidator(ErrorResult("Name", "Original."));
        var interceptor = new TestInterceptor("short", [], before: static _ => ValidationResult.Success);
        await using var provider = CreateMvcProvider(validator, interceptor);
        var filter = new JeninnetValidationFilter(new MvcValidationOptions(), [interceptor]);
        var (context, actionContext) = CreateMvcContext(provider, new MvcModel { Name = "" });
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () => {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
        });

        MSTestAssertions.That(() => context.Result.Should().BeNull());
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(0));
        MSTestAssertions.That(() => nextCalled.Should().BeTrue());
    }

    [TestMethod]
    public async Task Mvc_AfterCanAddErrorsAsync() {
        var validator = new InterceptorMvcValidator(ValidationResult.Success);
        var interceptor = new TestInterceptor(
            "after",
            [],
            after: static _ => ErrorResult("Name", "Added by interceptor."));
        await using var provider = CreateMvcProvider(validator, interceptor);
        var filter = new JeninnetValidationFilter(new MvcValidationOptions(), [interceptor]);
        var (context, actionContext) = CreateMvcContext(provider, new MvcModel { Name = "valid" });

        await filter.OnActionExecutionAsync(context, Next(actionContext));

        MSTestAssertions.That(() => context.Result.Should().BeOfType<BadRequestObjectResult>());
        MSTestAssertions.That(() => context.ModelState["Name"]!.Errors[0].ErrorMessage.Should().Be("Added by interceptor."));
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(1));
    }

    [TestMethod]
    public async Task Mvc_InterceptorsRunInRegistrationOrderAsync() {
        var validator = new InterceptorMvcValidator(ValidationResult.Success);
        var log = new List<string>();
        var first = new TestInterceptor("first", log);
        var second = new TestInterceptor("second", log);
        await using var provider = CreateMvcProvider(validator, first, second);
        var filter = new JeninnetValidationFilter(new MvcValidationOptions(), [first, second]);
        var (context, actionContext) = CreateMvcContext(provider, new MvcModel { Name = "valid" });
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () => {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
        });

        MSTestAssertions.That(() => context.Result.Should().BeNull());
        MSTestAssertions.That(() => nextCalled.Should().BeTrue());
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(1));
        MSTestAssertions.That(() => log
            .SequenceEqual(["first:before", "second:before", "first:after", "second:after"])
            .Should().BeTrue());
    }

    [TestMethod]
    public async Task NonGenericFilter_RunsInterceptorsAndShortCircuitsAsync() {
        var validator = new InterceptorEndpointValidator(ValidationResult.Success);
        var log = new List<string>();
        var interceptor = new TestInterceptor(
            "binding",
            log,
            before: static _ => ErrorResult("Name", "Intercepted."));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IValidator<EndpointRequest>>(validator);
        builder.Services.AddSingleton<IValidationInterceptor>(interceptor);
        builder.Services.AddSingleton(new ValidationProblemOptions());

        await using var app = builder.Build();
        app.MapPost("/items", (EndpointRequest body) => Results.Ok(body))
           .AddRequestValidation();

        await app.StartAsync(TestContext.CancellationToken);
        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync(
            "/items",
            new EndpointRequest { Name = "valid" },
            CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(CancellationToken.None);

        MSTestAssertions.That(() => response.StatusCode.Should().Be(HttpStatusCode.BadRequest));
        MSTestAssertions.That(() => problem!["errors"]!["Name"]![0]!.GetValue<string>().Should().Be("Intercepted."));
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(0));
        MSTestAssertions.That(() => log.SequenceEqual(["binding:before", "binding:after"]).Should().BeTrue());
    }

    [TestMethod]
    public void AddValidationInterceptor_RegistersInOrderWithConfiguredLifetimes() {
        var services = new ServiceCollection();
        services.AddValidationInterceptor<FirstInterceptor>();
        services.AddValidationInterceptor<SecondInterceptor>(ServiceLifetime.Singleton);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var interceptors = scope.ServiceProvider.GetServices<IValidationInterceptor>().ToArray();
        var lifetimes = services
            .Where(static descriptor => descriptor.ServiceType == typeof(IValidationInterceptor))
            .Select(static descriptor => descriptor.Lifetime)
            .ToArray();

        MSTestAssertions.That(() => interceptors
            .Select(static interceptor => interceptor.GetType())
            .SequenceEqual([typeof(FirstInterceptor), typeof(SecondInterceptor)])
            .Should().BeTrue());
        MSTestAssertions.That(() => lifetimes
            .SequenceEqual([ServiceLifetime.Scoped, ServiceLifetime.Singleton])
            .Should().BeTrue());
    }

    private static ValidationResult ErrorResult(string member, string message) {
        var errors = new ValidationErrorCollector {
            { member, message }
        };
        return ValidationResult.FromErrors(errors);
    }

    private static ServiceProvider CreateProvider(
        IValidator<EndpointRequest> validator,
        params IValidationInterceptor[] interceptors) {
        var services = new ServiceCollection();
        services.AddSingleton(validator);
        services.AddSingleton(new ValidationProblemOptions());

        foreach(var interceptor in interceptors) {
            services.AddSingleton<IValidationInterceptor>(interceptor);
        }

        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateMvcProvider(
        IValidator<MvcModel> validator,
        params IValidationInterceptor[] interceptors) {
        var services = new ServiceCollection();
        services.AddSingleton(validator);

        foreach(var interceptor in interceptors) {
            services.AddSingleton<IValidationInterceptor>(interceptor);
        }

        return services.BuildServiceProvider();
    }

    private static EndpointFilterContext CreateContext(
        ServiceProvider provider,
        EndpointRequest model,
        CancellationToken cancellationToken = default) {
        var httpContext = new DefaultHttpContext {
            RequestServices = provider,
            RequestAborted = cancellationToken
        };

        return new EndpointFilterContext(httpContext, model);
    }

    private static (ActionExecutingContext Context, ActionContext ActionContext) CreateMvcContext(
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

    private static ValueTask<object?> NextAsync(EndpointFilterInvocationContext context) =>
        ValueTask.FromResult<object?>("next");

    private static ActionExecutionDelegate Next(ActionContext actionContext) =>
        () => Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
}
