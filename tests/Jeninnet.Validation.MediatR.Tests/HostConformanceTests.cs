//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR.Tests;

public sealed record HostConformanceCommand(string Name, string Email) : IRequest<string>;

public sealed class HostConformanceHandler : IRequestHandler<HostConformanceCommand, string> {
    public static int Calls { get; set; }

    public Task<string> Handle(HostConformanceCommand request, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);
        Calls++;
        return Task.FromResult(request.Name);
    }
}

public sealed class HostConformanceCommandValidator : Validator<HostConformanceCommand> {
    protected override void Configure(ValidatorRuleBuilder<HostConformanceCommand> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(HostConformanceCommand.Name))
               .NotEmpty();

        builder.RuleFor(static x => x.Email, nameof(HostConformanceCommand.Email))
               .EmailAddress();
    }
}

public sealed class HostConformanceAsyncOnlyValidator : IValidator<HostConformanceCommand> {
    public int SyncCalls { get; private set; }

    public int AsyncCalls { get; private set; }

    public ValidationResult Validate(HostConformanceCommand instance) {
        SyncCalls++;
        throw new AsyncValidatorInvokedSynchronouslyException(nameof(HostConformanceAsyncOnlyValidator), 2);
    }

    public ValidationResult Validate(HostConformanceCommand instance, IEnumerable<string> ruleSets) =>
        Validate(instance);

    public ValueTask<ValidationResult> ValidateAsync(
        HostConformanceCommand instance,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(instance, [], cancellationToken);

    public ValueTask<ValidationResult> ValidateAsync(
        HostConformanceCommand instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) {
        AsyncCalls++;
        var errors = new ValidationErrorCollector {
            { nameof(HostConformanceCommand.Name), "Name is required." },
            { nameof(HostConformanceCommand.Email), "Email is invalid." }
        };

        return ValueTask.FromResult(ValidationResult.FromErrors(errors));
    }
}

public sealed class HostConformanceCancelingValidator : IValidator<HostConformanceCommand> {
    public ValidationResult Validate(HostConformanceCommand instance) =>
        throw new NotSupportedException("Use the asynchronous path.");

    public ValidationResult Validate(HostConformanceCommand instance, IEnumerable<string> ruleSets) =>
        throw new NotSupportedException("Use the asynchronous path.");

    public ValueTask<ValidationResult> ValidateAsync(
        HostConformanceCommand instance,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(instance, [], cancellationToken);

    public ValueTask<ValidationResult> ValidateAsync(
        HostConformanceCommand instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ValidationResult.Success);
    }
}

public sealed record HostConformanceMissingCommand(string Name) : IRequest<string>;

public sealed class HostConformanceMissingHandler : IRequestHandler<HostConformanceMissingCommand, string> {
    public static int Calls { get; set; }

    public Task<string> Handle(HostConformanceMissingCommand request, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);
        Calls++;
        return Task.FromResult(request.Name);
    }
}

[TestClass]
[DoNotParallelize]
public sealed class HostConformanceTests {
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_MediatR_InvalidRequest_DoesNotCallHandlerAsync() {
        HostConformanceHandler.Calls = 0;
        await using var provider = CreateProvider(services =>
            services.AddScoped<IValidator<HostConformanceCommand>, HostConformanceCommandValidator>());
        var mediator = provider.GetRequiredService<IMediator>();
        ValidationException? caught = null;

        try {
            await mediator.Send(
                new HostConformanceCommand(string.Empty, "not-an-email"),
                TestContext.CancellationToken);
        }
        catch(ValidationException exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().NotBeNull());
        MSTestAssertions.That(() => caught!.Errors.Should().HaveCount(2));
        MSTestAssertions.That(() => HostConformanceHandler.Calls.Should().Be(0));
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_MediatR_AsyncOnlyInvalidRequest_DoesNotCallHandlerAsync() {
        HostConformanceHandler.Calls = 0;
        var validator = new HostConformanceAsyncOnlyValidator();
        await using var provider = CreateProvider(services =>
            services.AddSingleton<IValidator<HostConformanceCommand>>(validator));
        var mediator = provider.GetRequiredService<IMediator>();
        ValidationException? caught = null;

        try {
            await mediator.Send(
                new HostConformanceCommand(string.Empty, "bad"),
                TestContext.CancellationToken);
        }
        catch(ValidationException exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().NotBeNull());
        MSTestAssertions.That(() => caught!.Errors.Should().HaveCount(2));
        MSTestAssertions.That(() => validator.SyncCalls.Should().Be(0));
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(1));
        MSTestAssertions.That(() => HostConformanceHandler.Calls.Should().Be(0));
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_MediatR_MissingValidator_PassesThroughAsync() {
        HostConformanceMissingHandler.Calls = 0;
        await using var provider = CreateProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(
            new HostConformanceMissingCommand(string.Empty),
            TestContext.CancellationToken);

        MSTestAssertions.That(() => response.Should().BeEmpty());
        MSTestAssertions.That(() => HostConformanceMissingHandler.Calls.Should().Be(1));
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_MediatR_CancelledRequest_DoesNotCallHandlerAsync() {
        HostConformanceHandler.Calls = 0;
        await using var provider = CreateProvider(services =>
            services.AddSingleton<IValidator<HostConformanceCommand>, HostConformanceCancelingValidator>());
        var mediator = provider.GetRequiredService<IMediator>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        Exception? caught = null;

        try {
            await mediator.Send(
                new HostConformanceCommand("valid", "buyer@example.com"),
                cancellation.Token);
        }
        catch(Exception exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().BeOfType<OperationCanceledException>());
        MSTestAssertions.That(() => HostConformanceHandler.Calls.Should().Be(0));
    }

    private static ServiceProvider CreateProvider(Action<IServiceCollection>? configure = null) {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<HostConformanceTests>());
        services.AddValidationPipeline();
        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
