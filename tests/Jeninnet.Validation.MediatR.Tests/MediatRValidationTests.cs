//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR.Tests;

// ---------------------------------------------------------------------------
// Requests, handlers, and validators
// ---------------------------------------------------------------------------

public sealed record ValidatableCommand(string Name, string Email) : IRequest<string>;

public sealed class ValidatableHandler : IRequestHandler<ValidatableCommand, string> {
    public Task<string> Handle(ValidatableCommand request, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(request.Name);
    }
}

public sealed class ValidatableCommandValidator : Validator<ValidatableCommand> {
    protected override void Configure(ValidatorRuleBuilder<ValidatableCommand> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ValidatableCommand.Name))
               .NotEmpty();

        builder.RuleFor(static x => x.Email, nameof(ValidatableCommand.Email))
               .EmailAddress();
    }
}

public sealed class ValidatableCommandExtraValidator : Validator<ValidatableCommand> {
    protected override void Configure(ValidatorRuleBuilder<ValidatableCommand> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ValidatableCommand.Name))
               .NotEmpty();
    }
}

public sealed record NoValidatorCommand(string Name) : IRequest<string>;

public sealed class NoValidatorHandler : IRequestHandler<NoValidatorCommand, string> {
    public Task<string> Handle(NoValidatorCommand request, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(request.Name);
    }
}

[MediatRValidationRuleSets("strict")]
public sealed record StrictCommand(string Name) : IRequest<string>;

public sealed class StrictHandler : IRequestHandler<StrictCommand, string> {
    public Task<string> Handle(StrictCommand request, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(request.Name);
    }
}

public sealed class StrictCommandValidator : Validator<StrictCommand> {
    protected override void Configure(ValidatorRuleBuilder<StrictCommand> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(StrictCommand.Name))
               .NotEmpty();

        builder.RuleSet("strict", strict =>
            strict.RuleFor(static x => x.Name, nameof(StrictCommand.Name))
                  .MaximumLength(10));
    }
}

public sealed record ContinueCommand(string Name) : IRequest<int>;

public sealed class ContinueHandler(ValidationResultAccessor accessor)
    : IRequestHandler<ContinueCommand, int> {
    public Task<int> Handle(ContinueCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(accessor.Result?.Errors.Count ?? 0);
}

public sealed class ContinueCommandValidator : Validator<ContinueCommand> {
    protected override void Configure(ValidatorRuleBuilder<ContinueCommand> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ContinueCommand.Name))
               .NotEmpty();
    }
}

public sealed record TokenCommand : IRequest<string>;

public sealed class TokenHandler : IRequestHandler<TokenCommand, string> {
    public Task<string> Handle(TokenCommand request, CancellationToken cancellationToken) =>
        Task.FromResult("ok");
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class MediatRValidationTests {
    [TestMethod]
    public async Task ContinueStrategy_ClearsAStaleAccessorResultAsync() {
        var accessor = new ValidationResultAccessor {
            Result = ValidationResult.FromErrors([
                new ValidationError("Name", new MessageId("NotEmpty"), "NotEmpty")
            ])
        };
        var behavior = new ValidationBehavior<ContinueCommand, int>(
            validators: [],
            new MediatRValidationOptions { FailureStrategy = MediatRValidationFailureStrategy.Continue },
            accessor);

        var handler = new ContinueHandler(accessor);
        var response = await behavior.Handle(
            new ContinueCommand("valid"),
            cancellationToken => handler.Handle(new ContinueCommand("valid"), cancellationToken),
            CancellationToken.None);

        MSTestAssertions.That(() => response.Should().Be(0));
        MSTestAssertions.That(() => accessor.Result.Should().BeNull());
    }

    [TestMethod]
    public async Task ValidRequest_FlowsToTheHandlerAsync() {
        await using var provider = CreateProvider(services =>
            services.AddScoped<IValidator<ValidatableCommand>, ValidatableCommandValidator>());
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new ValidatableCommand("order-1", "buyer@example.com"), TestCancellation);

        MSTestAssertions.That(() => response.Should().Be("order-1"));
    }

    [TestMethod]
    public async Task InvalidRequest_ThrowsAggregatedValidationExceptionAsync() {
        await using var provider = CreateProvider(services => {
            services.AddScoped<IValidator<ValidatableCommand>, ValidatableCommandValidator>();
            services.AddScoped<IValidator<ValidatableCommand>, ValidatableCommandExtraValidator>();
        });
        var mediator = provider.GetRequiredService<IMediator>();

        ValidationException? caught = null;

        try {
            await mediator.Send(new ValidatableCommand("", "not-an-email"), TestCancellation);
        }
        catch(ValidationException exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().NotBeNull());
        MSTestAssertions.That(() => caught!.Errors.Should().HaveCount(3));
        MSTestAssertions.That(() => caught!.Errors
            .Any(static error => error.MemberName == nameof(ValidatableCommand.Name))
            .Should().BeTrue());
        MSTestAssertions.That(() => caught!.Errors
            .Any(static error => error.MemberName == nameof(ValidatableCommand.Email))
            .Should().BeTrue());
    }

    [TestMethod]
    public async Task MissingValidator_BypassesValidationAsync() {
        await using var provider = CreateProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new NoValidatorCommand(""), TestCancellation);

        MSTestAssertions.That(() => response.Should().BeEmpty());
    }

    [TestMethod]
    public async Task ContinueStrategy_ExposesResultToTheHandlerAsync() {
        await using var provider = CreateProvider(
            services => services.AddScoped<IValidator<ContinueCommand>, ContinueCommandValidator>(),
            options => options.FailureStrategy = MediatRValidationFailureStrategy.Continue);
        var mediator = provider.GetRequiredService<IMediator>();

        var errorCount = await mediator.Send(new ContinueCommand(""), TestCancellation);

        MSTestAssertions.That(() => errorCount.Should().Be(1));
    }

    [TestMethod]
    public async Task RuleSetsAttribute_SelectsOnlyTheNamedRulesAsync() {
        await using var provider = CreateProvider(services =>
            services.AddScoped<IValidator<StrictCommand>, StrictCommandValidator>());
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new StrictCommand(""), TestCancellation);

        MSTestAssertions.That(() => response.Should().BeEmpty());
    }

    [TestMethod]
    public async Task CancellationToken_IsForwardedToValidatorsAsync() {
        var validator = new TokenCapturingValidator();
        await using var provider = CreateProvider(services =>
            services.AddSingleton<IValidator<TokenCommand>>(validator));
        var mediator = provider.GetRequiredService<IMediator>();
        using var cancellation = new CancellationTokenSource();

        await mediator.Send(new TokenCommand(), cancellation.Token);

        MSTestAssertions.That(() => validator.CapturedToken.Should().Be(cancellation.Token));
    }

    private CancellationToken TestCancellation => TestContext?.CancellationToken ?? CancellationToken.None;

    public TestContext? TestContext { get; set; }

    private static ServiceProvider CreateProvider(
        Action<IServiceCollection>? configure = null,
        Action<MediatRValidationOptions>? configureOptions = null) {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<MediatRValidationTests>());
        services.AddValidationPipeline(configureOptions);
        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    private sealed class TokenCapturingValidator : IValidator<TokenCommand> {
        public CancellationToken CapturedToken { get; private set; }

        public ValidationResult Validate(TokenCommand instance) => ValidationResult.Success;

        public ValidationResult Validate(TokenCommand instance, IEnumerable<string> ruleSets) => ValidationResult.Success;

        public ValueTask<ValidationResult> ValidateAsync(
            TokenCommand instance,
            CancellationToken cancellationToken = default) {
            CapturedToken = cancellationToken;
            return ValueTask.FromResult(ValidationResult.Success);
        }

        public ValueTask<ValidationResult> ValidateAsync(
            TokenCommand instance,
            IEnumerable<string> ruleSets,
            CancellationToken cancellationToken = default) {
            CapturedToken = cancellationToken;
            return ValueTask.FromResult(ValidationResult.Success);
        }
    }
}
