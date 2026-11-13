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

internal sealed class EndpointRequest {
    public string? Name { get; set; }
}

file sealed class EndpointRequestValidator : Validator<EndpointRequest> {
    protected override void Configure(ValidatorRuleBuilder<EndpointRequest> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(EndpointRequest.Name))
               .NotEmpty("Name is required.");
    }
}

internal sealed class RecordingEndpointValidator : IValidator<EndpointRequest> {
    public static string[]? LastRuleSets { get; private set; }

    public static void Reset() => LastRuleSets = null;

    public ValidationResult Validate(EndpointRequest instance) => ValidationResult.Success;

    public ValidationResult Validate(EndpointRequest instance, IEnumerable<string> ruleSets) {
        LastRuleSets = [.. ruleSets];
        return ValidationResult.Success;
    }

    public ValueTask<ValidationResult> ValidateAsync(EndpointRequest instance, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValidationResult.Success);

    public ValueTask<ValidationResult> ValidateAsync(
        EndpointRequest instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) {
        LastRuleSets = [.. ruleSets];
        return ValueTask.FromResult(ValidationResult.Success);
    }
}

internal sealed class PositiveIntValidator : IValidator<int> {
    public ValidationResult Validate(int instance) => ValidateValue(instance);

    public ValidationResult Validate(int instance, IEnumerable<string> ruleSets) => ValidateValue(instance);

    public ValueTask<ValidationResult> ValidateAsync(int instance, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValidateValue(instance));

    public ValueTask<ValidationResult> ValidateAsync(
        int instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValidateValue(instance));

    private static ValidationResult ValidateValue(int instance) {
        if(instance > 0) {
            return ValidationResult.Success;
        }

        var errors = new ValidationErrorCollector {
            { "Value", "Value must be positive." }
        };
        return ValidationResult.FromErrors(errors);
    }
}

internal sealed class NonEmptyTextValidator : IValidator<string> {
    public ValidationResult Validate(string instance) => ValidateValue(instance);

    public ValidationResult Validate(string instance, IEnumerable<string> ruleSets) => ValidateValue(instance);

    public ValueTask<ValidationResult> ValidateAsync(string instance, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValidateValue(instance));

    public ValueTask<ValidationResult> ValidateAsync(
        string instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValidateValue(instance));

    private static ValidationResult ValidateValue(string instance) {
        if(!string.IsNullOrWhiteSpace(instance)) {
            return ValidationResult.Success;
        }

        var errors = new ValidationErrorCollector {
            { "Value", "Text is required." }
        };
        return ValidationResult.FromErrors(errors);
    }
}

internal sealed class EndpointFilterContext(HttpContext httpContext, params object?[] arguments) : EndpointFilterInvocationContext {
    public override IList<object?> Arguments { get; } = arguments;

    public override HttpContext HttpContext { get; } = httpContext;

    public override T GetArgument<T>(int index) => (T)Arguments[index]!;
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class ValidationEndpointFilterTests {
    public TestContext TestContext { get; set; } = null!;
    [TestMethod]
    public async Task Filter_EnrichesProblemDetailsWhenConfiguredAsync() {
        await using var provider = CreateEnrichedProvider();
        var filter = new ValidationEndpointFilter<EndpointRequest>(provider);
        var context = CreateContext(provider, new EndpointRequest { Name = string.Empty });
        context.HttpContext.TraceIdentifier = "trace-1";

        var result = await filter.InvokeAsync(context, NextAsync);

        if(result is not ProblemHttpResult { ProblemDetails: HttpValidationProblemDetails details }) {
            Assert.Fail("Expected a validation problem result.");
            return;
        }

        MSTestAssertions.That(() => details.Extensions["correlationId"].Should().Be("trace-1"));
        MSTestAssertions.That(() => details.Extensions["modelType"].Should().Be(typeof(EndpointRequest).FullName));
        MSTestAssertions.That(() => details.Extensions.ContainsKey("durationMs").Should().BeTrue());
    }

    [TestMethod]
    public async Task Filter_ReturnsProblemDetailsForInvalidModelAsync() {
        await using var provider = CreateProvider();
        var filter = new ValidationEndpointFilter<EndpointRequest>(provider);
        var context = CreateContext(provider, new EndpointRequest { Name = "" });

        var result = await filter.InvokeAsync(context, NextAsync);

        if(result is not ProblemHttpResult { ProblemDetails: HttpValidationProblemDetails details } problem) {
            Assert.Fail("Expected a validation problem result.");
            return;
        }

        MSTestAssertions.That(() => problem.StatusCode.Should().Be(StatusCodes.Status400BadRequest));
        MSTestAssertions.That(() => details.Errors["Name"][0].Should().Be("Name is required."));
    }

    [TestMethod]
    public async Task Filter_PassesThroughValidModelAsync() {
        await using var provider = CreateProvider();
        var filter = new ValidationEndpointFilter<EndpointRequest>(provider);
        var context = CreateContext(provider, new EndpointRequest { Name = "Valid" });

        var result = await filter.InvokeAsync(context, NextAsync);

        MSTestAssertions.That(() => result.Should().Be("next"));
    }

    [TestMethod]
    public async Task Filter_PassesThroughWhenValidatorIsMissingAsync() {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var filter = new ValidationEndpointFilter<EndpointRequest>(provider);
        var context = CreateContext(provider, new EndpointRequest { Name = "" });

        var result = await filter.InvokeAsync(context, NextAsync);

        MSTestAssertions.That(() => result.Should().Be("next"));
    }

    [TestMethod]
    public async Task Filter_PassesThroughWhenModelIsMissingAsync() {
        await using var provider = CreateProvider();
        var filter = new ValidationEndpointFilter<EndpointRequest>(provider);
        var context = CreateContext(provider, arguments: []);

        var result = await filter.InvokeAsync(context, NextAsync);

        MSTestAssertions.That(() => result.Should().Be("next"));
    }

    [TestMethod]
    public async Task NonGenericFilter_ValidatesBodyQueryRouteAndHeaderAsync() {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IValidator<EndpointRequest>, EndpointRequestValidator>();
        builder.Services.AddSingleton<IValidator<int>, PositiveIntValidator>();
        builder.Services.AddSingleton<IValidator<string>, NonEmptyTextValidator>();
        builder.Services.AddSingleton(new ValidationProblemOptions());

        await using var app = builder.Build();
        app.MapPost(
               "/items/{id}",
               ([FromRoute] string id, [FromQuery] int page, [FromHeader(Name = "X-Code")] string code, EndpointRequest body) => {
                   _ = id;
                   _ = page;
                   _ = code;
                   _ = body;
                   return Results.Ok();
               })
           .AddRequestValidation();

        await app.StartAsync(TestContext.CancellationToken);
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/items/%20?page=0") {
            Content = JsonContent.Create(new EndpointRequest { Name = string.Empty })
        };
        request.Headers.Add("X-Code", " ");

        using var response = await client.SendAsync(request, CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(CancellationToken.None);
        var errors = problem!["errors"]!.AsObject();

        MSTestAssertions.That(() => response.StatusCode.Should().Be(HttpStatusCode.BadRequest));
        MSTestAssertions.That(() => errors.ContainsKey("route.id").Should().BeTrue());
        MSTestAssertions.That(() => errors.ContainsKey("query.page").Should().BeTrue());
        MSTestAssertions.That(() => errors.ContainsKey("header.code").Should().BeTrue());
        MSTestAssertions.That(() => errors.ContainsKey("Name").Should().BeTrue());
    }

    [TestMethod]
    public async Task NonGenericFilter_RespectsEndpointAndParameterSkipsAsync() {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IValidator<EndpointRequest>, EndpointRequestValidator>();
        builder.Services.AddSingleton(new ValidationProblemOptions());

        await using var app = builder.Build();
        app.MapPost("/skip", (EndpointRequest body) => Results.Ok(body))
           .AddRequestValidation()
           .WithMetadata(new SkipJeninnetValidationAttribute());
        app.MapPost("/disable-parameter", ([DisableValidation] EndpointRequest body) => Results.Ok(body))
           .AddRequestValidation();
        app.MapPost("/skip-validation-parameter", ([SkipValidation] EndpointRequest body) => Results.Ok(body))
           .AddRequestValidation();

        await app.StartAsync(TestContext.CancellationToken);
        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync(
            "/skip",
            new EndpointRequest { Name = string.Empty },
            CancellationToken.None);

        using var parameterResponse = await client.PostAsJsonAsync(
            "/disable-parameter",
            new EndpointRequest { Name = string.Empty },
            CancellationToken.None);

        using var skipValidationResponse = await client.PostAsJsonAsync(
            "/skip-validation-parameter",
            new EndpointRequest { Name = string.Empty },
            CancellationToken.None);

        MSTestAssertions.That(() => response.StatusCode.Should().Be(HttpStatusCode.OK));
        MSTestAssertions.That(() => parameterResponse.StatusCode.Should().Be(HttpStatusCode.OK));
        MSTestAssertions.That(() => skipValidationResponse.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    [TestMethod]
    public async Task GenericFilter_RespectsSkipValidationParameterAsync() {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IValidator<EndpointRequest>, EndpointRequestValidator>();
        builder.Services.AddSingleton(new ValidationProblemOptions());

        await using var app = builder.Build();
        app.MapPost("/generic-skip-validation", ([SkipValidation] EndpointRequest body) => Results.Ok(body))
           .AddRequestValidation<EndpointRequest>();

        await app.StartAsync(TestContext.CancellationToken);
        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync(
            "/generic-skip-validation",
            new EndpointRequest { Name = string.Empty },
            CancellationToken.None);

        MSTestAssertions.That(() => response.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    [TestMethod]
    public async Task NonGenericFilter_PassesEndpointRuleSetsAsync() {
        RecordingEndpointValidator.Reset();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IValidator<EndpointRequest>, RecordingEndpointValidator>();
        builder.Services.AddSingleton(new ValidationProblemOptions());

        await using var app = builder.Build();
        app.MapPost("/rules", (EndpointRequest body) => Results.Ok(body))
           .AddRequestValidation()
           .WithMetadata(new JeninnetValidationRuleSetsAttribute("strict"));

        await app.StartAsync(TestContext.CancellationToken);
        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync(
            "/rules",
            new EndpointRequest { Name = "valid" },
            CancellationToken.None);

        MSTestAssertions.That(() => response.StatusCode.Should().Be(HttpStatusCode.OK));
        MSTestAssertions.That(() => RecordingEndpointValidator.LastRuleSets.Should().Equal("strict"));
    }

    private static ServiceProvider CreateProvider() {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<EndpointRequest>, EndpointRequestValidator>();
        services.AddSingleton(new ValidationProblemOptions());
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateEnrichedProvider() {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<EndpointRequest>, EndpointRequestValidator>();
        services.AddSingleton(new ValidationProblemOptions {
            IncludeCorrelationId = true,
            IncludeModelType = true,
            IncludeDuration = true
        });
        return services.BuildServiceProvider();
    }

    private static EndpointFilterContext CreateContext(ServiceProvider provider, params object?[] arguments) {
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        return new EndpointFilterContext(httpContext, arguments);
    }

    private static ValueTask<object?> NextAsync(EndpointFilterInvocationContext context) =>
        ValueTask.FromResult<object?>("next");
}
