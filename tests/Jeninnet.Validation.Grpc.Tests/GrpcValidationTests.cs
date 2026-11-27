//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Grpc.Tests;

// ---------------------------------------------------------------------------
// Validators and service
// ---------------------------------------------------------------------------

public sealed class CreateRequestMessageValidator : Validator<CreateRequestMessage> {
    protected override void Configure(ValidatorRuleBuilder<CreateRequestMessage> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(CreateRequestMessage.Name))
               .NotEmpty()
               .WithMessage("Name is required.")
               .WithCode("NAME_REQUIRED");

        builder.RuleFor(static x => x.Email, nameof(CreateRequestMessage.Email))
               .EmailAddress()
               .WithMessage("Email is invalid.")
               .WithCode("EMAIL_INVALID");
    }
}

[GrpcValidationRuleSets("strict")]
public sealed partial class StrictRequestMessage;

public sealed class StrictRequestMessageValidator : Validator<StrictRequestMessage> {
    protected override void Configure(ValidatorRuleBuilder<StrictRequestMessage> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(StrictRequestMessage.Name))
               .NotEmpty();

        builder.RuleSet("strict", strict =>
            strict.RuleFor(static x => x.Name, nameof(StrictRequestMessage.Name))
                  .MaximumLength(3));
    }
}

public sealed class AsyncRequestMessageValidator : Validator<AsyncRequestMessage> {
    public static CancellationToken Captured { get; private set; }

    protected override void Configure(ValidatorRuleBuilder<AsyncRequestMessage> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Code, nameof(AsyncRequestMessage.Code))
               .Must(
                   static (_, cancellationToken) => {
                       Captured = cancellationToken;
                       return ValueTask.FromResult(false);
                   },
                   "Async failure.");
    }
}

/// <summary>
/// Returns the same fixed result for HTTP and gRPC parity checks: three member errors (including a
/// nested path) with codes and severities, plus one model-level error.
/// </summary>
public sealed class ParityRequestValidator : IValidator<CreateRequestMessage> {
    public static ValidationResult Result { get; } = CreateResult();

    public ValidationResult Validate(CreateRequestMessage instance) => Result;

    public ValidationResult Validate(CreateRequestMessage instance, IEnumerable<string> ruleSets) => Result;

    public ValueTask<ValidationResult> ValidateAsync(
        CreateRequestMessage instance,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Result);

    public ValueTask<ValidationResult> ValidateAsync(
        CreateRequestMessage instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Result);

    private static ValidationResult CreateResult() {
        var errors = new ValidationErrorCollector {
            new ValidationError(
                nameof(CreateRequestMessage.Name),
                messageId: new MessageId("NameRequired"),
                messageKey: null,
                messageParameters: null,
                message: "Name is required.",
                errorCode: "NAME_REQUIRED",
                severity: ValidationErrorSeverity.Error),
            new ValidationError(
                nameof(CreateRequestMessage.Email),
                messageId: new MessageId("EmailSuspicious"),
                messageKey: null,
                messageParameters: null,
                message: "Email is suspicious.",
                errorCode: "EMAIL_SUSPICIOUS",
                severity: ValidationErrorSeverity.Warning),
            new ValidationError(
                "Address.Street",
                messageId: new MessageId("StreetRequired"),
                messageKey: null,
                messageParameters: null,
                message: "Street is required.",
                errorCode: "STREET_REQUIRED",
                severity: ValidationErrorSeverity.Error),
            new ValidationError(
                memberName: string.Empty,
                messageId: new MessageId("RequestInvalid"),
                messageKey: null,
                messageParameters: null,
                message: "Request is invalid.",
                errorCode: "REQUEST_INVALID",
                severity: ValidationErrorSeverity.Error)
        };

        return ValidationResult.FromErrors(errors);
    }
}

public sealed class ValidationTestService : ValidationTest.ValidationTestBase {
    public static int Calls { get; set; }

    public override Task<CreateReplyMessage> Create(
        CreateRequestMessage request,
        ServerCallContext context) {
        Calls++;
        return Task.FromResult(new CreateReplyMessage { Name = request.Name });
    }

    public override Task<CreateReplyMessage> Strict(
        StrictRequestMessage request,
        ServerCallContext context) {
        Calls++;
        return Task.FromResult(new CreateReplyMessage { Name = request.Name });
    }

    public override Task<CreateReplyMessage> AsyncCheck(
        AsyncRequestMessage request,
        ServerCallContext context) {
        Calls++;
        return Task.FromResult(new CreateReplyMessage { Name = request.Code });
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class GrpcValidationTests {
    public TestContext TestContext { get; set; } = null!;
    [TestMethod]
    public async Task Unary_InvalidRequest_ReturnsBadRequestDetailsAsync() {
        await using var app = await StartAppAsync(services =>
            services.AddSingleton<IValidator<CreateRequestMessage>, CreateRequestMessageValidator>());
        using var channel = CreateChannel(app);
        var client = new ValidationTest.ValidationTestClient(channel);
        ValidationTestService.Calls = 0;
        RpcException? caught = null;

        try {
            await client.CreateAsync(
                new CreateRequestMessage { Name = string.Empty, Email = "bad" },
                cancellationToken: TestContext.CancellationToken);
        }
        catch(RpcException exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().NotBeNull());
        MSTestAssertions.That(() => caught!.StatusCode.Should().Be(StatusCode.InvalidArgument));
        MSTestAssertions.That(() => ValidationTestService.Calls.Should().Be(0));

        var detailsEntry = caught!.Trailers.Single(entry => entry.Key == "grpc-status-details-bin");
        var status = GoogleRpcStatus.Parser.ParseFrom(detailsEntry.ValueBytes);
        var badRequest = status.Details
            .Where(static detail => detail.Is(GoogleBadRequest.Descriptor))
            .Select(static detail => detail.Unpack<GoogleBadRequest>())
            .Single();

        MSTestAssertions.That(() => badRequest.FieldViolations.Should().HaveCount(2));
        MSTestAssertions.That(() => badRequest.FieldViolations
            .Any(static violation => violation.Field == nameof(CreateRequestMessage.Name))
            .Should().BeTrue());
        MSTestAssertions.That(() => badRequest.FieldViolations
            .Any(static violation => violation.Field == nameof(CreateRequestMessage.Email))
            .Should().BeTrue());
        MSTestAssertions.That(() => badRequest.FieldViolations
            .Single(static violation => violation.Field == nameof(CreateRequestMessage.Name))
            .Description.Should().Be("Name is required."));

        var errorInfos = status.Details
            .Where(static detail => detail.Is(GoogleErrorInfo.Descriptor))
            .Select(static detail => detail.Unpack<GoogleErrorInfo>())
            .ToArray();
        var nameInfo = errorInfos.Single(
            static info => info.Metadata["field"] == nameof(CreateRequestMessage.Name));

        MSTestAssertions.That(() => errorInfos.Should().HaveCount(2));
        MSTestAssertions.That(() => nameInfo.Domain.Should().Be("jeninnet.validation/v1"));
        MSTestAssertions.That(() => nameInfo.Reason.Should().Be("NAME_REQUIRED"));
        MSTestAssertions.That(() => nameInfo.Metadata["code"].Should().Be("NAME_REQUIRED"));
        MSTestAssertions.That(() => nameInfo.Metadata["severity"].Should().Be("Error"));
    }

    [TestMethod]
    public async Task Unary_ModelLevelError_UsesDollarFieldAsync() {
        await using var app = await StartAppAsync(services =>
            services.AddSingleton<IValidator<CreateRequestMessage>, ParityRequestValidator>());
        using var channel = CreateChannel(app);
        var client = new ValidationTest.ValidationTestClient(channel);
        RpcException? caught = null;

        try {
            await client.CreateAsync(
                new CreateRequestMessage { Name = "order-1", Email = "buyer@example.com" },
                cancellationToken: TestContext.CancellationToken);
        }
        catch(RpcException exception) {
            caught = exception;
        }

        var status = ReadStatus(caught!);
        var badRequest = status.Details
            .Where(static detail => detail.Is(GoogleBadRequest.Descriptor))
            .Select(static detail => detail.Unpack<GoogleBadRequest>())
            .Single();
        var modelLevel = badRequest.FieldViolations.Single(
            static violation => violation.Field == ValidationErrorProjection.ModelLevelField);

        MSTestAssertions.That(() => modelLevel.Description.Should().Be("Request is invalid."));
        MSTestAssertions.That(() => modelLevel.Field.Should().Be("$"));
    }

    [TestMethod]
    public async Task Unary_HttpAndGrpc_ExposeTheSameProjectionAsync() {
        await using var app = await StartAppAsync(services =>
            services.AddSingleton<IValidator<CreateRequestMessage>, ParityRequestValidator>());
        using var channel = CreateChannel(app);
        var client = new ValidationTest.ValidationTestClient(channel);
        RpcException? caught = null;

        try {
            await client.CreateAsync(
                new CreateRequestMessage { Name = "order-1", Email = "buyer@example.com" },
                cancellationToken: TestContext.CancellationToken);
        }
        catch(RpcException exception) {
            caught = exception;
        }

        var status = ReadStatus(caught!);
        var badRequest = status.Details
            .Where(static detail => detail.Is(GoogleBadRequest.Descriptor))
            .Select(static detail => detail.Unpack<GoogleBadRequest>())
            .Single();
        var errorInfos = status.Details
            .Where(static detail => detail.Is(GoogleErrorInfo.Descriptor))
            .Select(static detail => detail.Unpack<GoogleErrorInfo>())
            .ToArray();
        var http = ParityRequestValidator.Result.ToProblemErrors(new ValidationProblemOptions {
            IncludeErrorCodes = true,
            IncludeSeverities = true
        });

        MSTestAssertions.That(() => badRequest.FieldViolations.Should().HaveCount(4));
        MSTestAssertions.That(() => errorInfos.Should().HaveCount(4));

        foreach(var violation in badRequest.FieldViolations) {
            MSTestAssertions.That(() => http.ContainsKey(violation.Field).Should().BeTrue());
            var httpMessage = http[violation.Field].Single();
            var info = errorInfos.Single(
                candidate => candidate.Metadata["field"] == violation.Field);

            MSTestAssertions.That(() => httpMessage.Should().StartWith(violation.Description));
            MSTestAssertions.That(() => info.Domain.Should().Be("jeninnet.validation/v1"));

            if(info.Metadata.TryGetValue("code", out var code)) {
                MSTestAssertions.That(() => info.Reason.Should().Be(code));
                MSTestAssertions.That(() => httpMessage.Should().Contain($"[{code}]"));
            }

            MSTestAssertions.That(() => httpMessage.Should().Contain($"({info.Metadata["severity"]})"));
        }
    }

    [TestMethod]
    public async Task Unary_ValidRequest_FlowsToServiceAsync() {
        await using var app = await StartAppAsync(services =>
            services.AddSingleton<IValidator<CreateRequestMessage>, CreateRequestMessageValidator>());
        using var channel = CreateChannel(app);
        var client = new ValidationTest.ValidationTestClient(channel);
        ValidationTestService.Calls = 0;

        var reply = await client.CreateAsync(
            new CreateRequestMessage {
                Name = "order-1",
                Email = "buyer@example.com"
            },
            cancellationToken: TestContext.CancellationToken);

        MSTestAssertions.That(() => reply.Name.Should().Be("order-1"));
        MSTestAssertions.That(() => ValidationTestService.Calls.Should().Be(1));
    }

    [TestMethod]
    public async Task Unary_MissingValidator_BypassesValidationAsync() {
        await using var app = await StartAppAsync();
        using var channel = CreateChannel(app);
        var client = new ValidationTest.ValidationTestClient(channel);
        ValidationTestService.Calls = 0;

        var reply = await client.CreateAsync(
                new CreateRequestMessage { Name = string.Empty, Email = "bad" },
                cancellationToken: TestContext.CancellationToken);

        MSTestAssertions.That(() => reply.Name.Should().BeEmpty());
        MSTestAssertions.That(() => ValidationTestService.Calls.Should().Be(1));
    }

    [TestMethod]
    public async Task Unary_RuleSetsAttribute_SelectsStrictRulesAsync() {
        await using var app = await StartAppAsync(services =>
            services.AddSingleton<IValidator<StrictRequestMessage>, StrictRequestMessageValidator>());
        using var channel = CreateChannel(app);
        var client = new ValidationTest.ValidationTestClient(channel);
        ValidationTestService.Calls = 0;
        RpcException? caught = null;

        try {
            await client.StrictAsync(
                new StrictRequestMessage { Name = "too-long" },
                cancellationToken: TestContext.CancellationToken);
        }
        catch(RpcException exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().NotBeNull());
        MSTestAssertions.That(() => caught!.StatusCode.Should().Be(StatusCode.InvalidArgument));
        MSTestAssertions.That(() => ValidationTestService.Calls.Should().Be(0));
    }

    [TestMethod]
    public async Task Unary_AsyncValidator_ReceivesCancellationTokenAsync() {
        await using var app = await StartAppAsync(services =>
            services.AddSingleton<IValidator<AsyncRequestMessage>, AsyncRequestMessageValidator>());
        using var channel = CreateChannel(app);
        var client = new ValidationTest.ValidationTestClient(channel);
        RpcException? caught = null;

        try {
            await client.AsyncCheckAsync(
                new AsyncRequestMessage { Code = string.Empty },
                cancellationToken: TestContext.CancellationToken);
        }
        catch(RpcException exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().NotBeNull());
        MSTestAssertions.That(() => caught!.StatusCode.Should().Be(StatusCode.InvalidArgument));
        MSTestAssertions.That(() => AsyncRequestMessageValidator.Captured.CanBeCanceled.Should().BeTrue());
    }

    private static GoogleRpcStatus ReadStatus(RpcException exception) {
        var detailsEntry = exception.Trailers.Single(entry => entry.Key == "grpc-status-details-bin");

        return GoogleRpcStatus.Parser.ParseFrom(detailsEntry.ValueBytes);
    }

    private static async Task<WebApplication> StartAppAsync(Action<IServiceCollection>? configure = null) {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc(options => options.Interceptors.Add<ValidationInterceptor>());
        builder.Services.AddJeninnetGrpcValidation();
        builder.Services.AddSingleton<ValidationTestService>();
        configure?.Invoke(builder.Services);

        var app = builder.Build();
        app.MapGrpcService<ValidationTestService>();
        await app.StartAsync();

        return app;
    }

    private static GrpcChannel CreateChannel(WebApplication app) {
        var httpClient = app.GetTestClient();

        return GrpcChannel.ForAddress(
            httpClient.BaseAddress!,
            new GrpcChannelOptions {
                HttpClient = httpClient,
                HttpVersion = new Version(2, 0),
                HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            });
    }
}
