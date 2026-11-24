//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.AspNetCore.Tests;

internal sealed class HostConformanceRequest {
    public string? Name { get; set; }

    public string? Email { get; set; }
}

internal sealed class HostConformanceAsyncValidator : IValidator<HostConformanceRequest> {
    public int SyncCalls { get; private set; }

    public int AsyncCalls { get; private set; }

    public ValidationResult Validate(HostConformanceRequest instance) {
        SyncCalls++;
        throw new AsyncValidatorInvokedSynchronouslyException(nameof(HostConformanceAsyncValidator), 2);
    }

    public ValidationResult Validate(HostConformanceRequest instance, IEnumerable<string> ruleSets) =>
        Validate(instance);

    public ValueTask<ValidationResult> ValidateAsync(
        HostConformanceRequest instance,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(instance, [], cancellationToken);

    public ValueTask<ValidationResult> ValidateAsync(
        HostConformanceRequest instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) {
        AsyncCalls++;
        var errors = new ValidationErrorCollector {
            { nameof(HostConformanceRequest.Name), "Name is required." },
            { nameof(HostConformanceRequest.Email), "Email is invalid." }
        };

        return ValueTask.FromResult(ValidationResult.FromErrors(errors));
    }
}

internal sealed class HostConformanceCancelingValidator : IValidator<HostConformanceRequest> {
    public ValidationResult Validate(HostConformanceRequest instance) =>
        throw new NotSupportedException("Use the asynchronous path.");

    public ValidationResult Validate(HostConformanceRequest instance, IEnumerable<string> ruleSets) =>
        throw new NotSupportedException("Use the asynchronous path.");

    public ValueTask<ValidationResult> ValidateAsync(
        HostConformanceRequest instance,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(instance, [], cancellationToken);

    public ValueTask<ValidationResult> ValidateAsync(
        HostConformanceRequest instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ValidationResult.Success);
    }
}

[TestClass]
[DoNotParallelize]
public sealed class HostConformanceTests {
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_MinimalApi_AsyncOnlyInvalidRequest_DoesNotCallHandlerAsync() {
        var validator = new HostConformanceAsyncValidator();
        await using var provider = CreateProvider(validator);
        var filter = new ValidationEndpointFilter<HostConformanceRequest>(provider);
        var context = CreateEndpointContext(
            provider,
            new HostConformanceRequest { Name = string.Empty, Email = "bad" },
            TestContext.CancellationToken);
        var handlerCalled = false;

        var result = await filter.InvokeAsync(context, _ => {
            handlerCalled = true;
            return ValueTask.FromResult<object?>("next");
        });

        if(result is not ProblemHttpResult { ProblemDetails: HttpValidationProblemDetails details }) {
            Assert.Fail("Expected a validation problem result.");
            return;
        }

        MSTestAssertions.That(() => details.Errors[nameof(HostConformanceRequest.Name)].Should().ContainSingle());
        MSTestAssertions.That(() => details.Errors[nameof(HostConformanceRequest.Email)].Should().ContainSingle());
        MSTestAssertions.That(() => validator.SyncCalls.Should().Be(0));
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(1));
        MSTestAssertions.That(() => handlerCalled.Should().BeFalse());
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_MinimalApi_MissingValidator_PassesThroughAsync() {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var filter = new ValidationEndpointFilter<HostConformanceRequest>(provider);
        var context = CreateEndpointContext(
            provider,
            new HostConformanceRequest { Name = string.Empty },
            TestContext.CancellationToken);
        var handlerCalled = false;

        var result = await filter.InvokeAsync(context, _ => {
            handlerCalled = true;
            return ValueTask.FromResult<object?>("next");
        });

        MSTestAssertions.That(() => result.Should().Be("next"));
        MSTestAssertions.That(() => handlerCalled.Should().BeTrue());
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_MinimalApi_CancelledRequest_DoesNotCallHandlerAsync() {
        var validator = new HostConformanceCancelingValidator();
        await using var provider = CreateProvider(validator);
        var filter = new ValidationEndpointFilter<HostConformanceRequest>(provider);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var context = CreateEndpointContext(
            provider,
            new HostConformanceRequest(),
            cancellation.Token);
        var handlerCalled = false;
        Exception? caught = null;

        try {
            await filter.InvokeAsync(context, _ => {
                handlerCalled = true;
                return ValueTask.FromResult<object?>("next");
            });
        }
        catch(Exception exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().BeOfType<OperationCanceledException>());
        MSTestAssertions.That(() => handlerCalled.Should().BeFalse());
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_Mvc_AsyncOnlyInvalidRequest_DoesNotCallActionAsync() {
        var validator = new HostConformanceAsyncValidator();
        await using var provider = CreateProvider(validator);
        var filter = new JeninnetValidationFilter(new MvcValidationOptions());
        var (context, actionContext) = CreateMvcContext(
            provider,
            new HostConformanceRequest { Name = string.Empty, Email = "bad" },
            TestContext.CancellationToken);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(
            context,
            Next(actionContext, () => nextCalled = true));

        if(context.Result is not BadRequestObjectResult { Value: ValidationProblemDetails details }) {
            Assert.Fail("Expected a bad-request validation result.");
            return;
        }

        MSTestAssertions.That(() => details.Errors[nameof(HostConformanceRequest.Name)].Should().ContainSingle());
        MSTestAssertions.That(() => details.Errors[nameof(HostConformanceRequest.Email)].Should().ContainSingle());
        MSTestAssertions.That(() => validator.SyncCalls.Should().Be(0));
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(1));
        MSTestAssertions.That(() => nextCalled.Should().BeFalse());
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_Mvc_MissingValidator_PassesThroughAsync() {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var filter = new JeninnetValidationFilter(new MvcValidationOptions());
        var (context, actionContext) = CreateMvcContext(
            provider,
            new HostConformanceRequest { Name = string.Empty },
            TestContext.CancellationToken);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(
            context,
            Next(actionContext, () => nextCalled = true));

        MSTestAssertions.That(() => context.Result.Should().BeNull());
        MSTestAssertions.That(() => nextCalled.Should().BeTrue());
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_Mvc_CancelledRequest_DoesNotCallActionAsync() {
        var validator = new HostConformanceCancelingValidator();
        await using var provider = CreateProvider(validator);
        var filter = new JeninnetValidationFilter(new MvcValidationOptions());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var (context, actionContext) = CreateMvcContext(
            provider,
            new HostConformanceRequest(),
            cancellation.Token);
        var nextCalled = false;
        Exception? caught = null;

        try {
            await filter.OnActionExecutionAsync(
                context,
                Next(actionContext, () => nextCalled = true));
        }
        catch(Exception exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().BeOfType<OperationCanceledException>());
        MSTestAssertions.That(() => nextCalled.Should().BeFalse());
    }

    private static ServiceProvider CreateProvider(IValidator<HostConformanceRequest> validator) {
        var services = new ServiceCollection();
        services.AddSingleton(validator);
        return services.BuildServiceProvider();
    }

    private static EndpointFilterContext CreateEndpointContext(
        IServiceProvider provider,
        object? argument,
        CancellationToken requestAborted = default) {
        var httpContext = new DefaultHttpContext {
            RequestServices = provider,
            RequestAborted = requestAborted
        };

        return new EndpointFilterContext(httpContext, argument);
    }

    private static (ActionExecutingContext Context, ActionContext ActionContext) CreateMvcContext(
        IServiceProvider provider,
        HostConformanceRequest? model,
        CancellationToken requestAborted = default) {
        var httpContext = new DefaultHttpContext {
            RequestServices = provider,
            RequestAborted = requestAborted
        };
        var actionDescriptor = new ActionDescriptor {
            EndpointMetadata = []
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        var arguments = new Dictionary<string, object?> { ["model"] = model };

        return (new ActionExecutingContext(actionContext, [], arguments, new object()), actionContext);
    }

    private static ActionExecutionDelegate Next(ActionContext actionContext, Action onNext) =>
        () => {
            onNext();
            return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
        };
}
