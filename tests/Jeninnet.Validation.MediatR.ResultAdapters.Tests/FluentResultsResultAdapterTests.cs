//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.MediatR.ResultAdapters.Tests;

public sealed record FluentResultsCommand(string Name, string Email)
    : IRequest<FluentResult>;

public sealed record FluentResultsResponse(string Name);

public sealed class FluentResultsHandler : IRequestHandler<FluentResultsCommand, FluentResult> {
    public static int Calls { get; private set; }

    public static void Reset() => Calls = 0;

    public Task<FluentResult> Handle(
        FluentResultsCommand request,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);
        Calls++;

        return Task.FromResult(global::FluentResults.Result.Ok(new FluentResultsResponse(request.Name)));
    }
}

public sealed class FluentResultsCommandValidator : Validator<FluentResultsCommand> {
    protected override void Configure(ValidatorRuleBuilder<FluentResultsCommand> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(FluentResultsCommand.Name))
               .NotEmpty()
               .WithMessage("Name is required.")
               .WithCode("NAME_REQUIRED");

        builder.RuleFor(static x => x.Email, nameof(FluentResultsCommand.Email))
               .EmailAddress()
               .WithMessage("Email is invalid.")
               .WithCode("EMAIL_INVALID")
               .WithSeverity(ValidationErrorSeverity.Warning);
    }
}

[TestClass]
public sealed class FluentResultsResultAdapterTests {
    [TestMethod]
    public async Task FluentResults_Success_FlowsToHandlerAsync() {
        FluentResultsHandler.Reset();
        await using var provider = MediatRResultTestHost.Create(
            services => services.AddScoped<IValidator<FluentResultsCommand>, FluentResultsCommandValidator>(),
            options => options.UseFluentResultsAdapter<FluentResultsResponse>());
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new FluentResultsCommand("order-1", "buyer@example.com"),
            CancellationToken.None);

        MSTestAssertions.That(() => result.IsSuccess.Should().BeTrue());
        MSTestAssertions.That(() => result.Value.Name.Should().Be("order-1"));
        MSTestAssertions.That(() => FluentResultsHandler.Calls.Should().Be(1));
    }

    [TestMethod]
    public async Task FluentResults_Failure_MapsMetadataAndSkipsHandlerAsync() {
        FluentResultsHandler.Reset();
        await using var provider = MediatRResultTestHost.Create(
            services => services.AddScoped<IValidator<FluentResultsCommand>, FluentResultsCommandValidator>(),
            options => options.UseFluentResultsAdapter<FluentResultsResponse>());
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new FluentResultsCommand(string.Empty, "not-an-email"),
            CancellationToken.None);

        MSTestAssertions.That(() => result.IsFailed.Should().BeTrue());
        MSTestAssertions.That(() => result.Errors.Should().HaveCount(2));
        MSTestAssertions.That(() => FluentResultsHandler.Calls.Should().Be(0));

        var nameError = result.Errors.Single(error => Equals(error.Metadata["errorCode"], "NAME_REQUIRED"));
        var emailError = result.Errors.Single(error => Equals(error.Metadata["errorCode"], "EMAIL_INVALID"));

        MSTestAssertions.That(() => nameError.Message.Should().Be("Name is required."));
        MSTestAssertions.That(() => nameError.Metadata["memberName"].Should().Be(nameof(FluentResultsCommand.Name)));
        MSTestAssertions.That(() => nameError.Metadata["severity"].Should().Be("Error"));

        MSTestAssertions.That(() => emailError.Metadata["memberName"].Should().Be(nameof(FluentResultsCommand.Email)));
        MSTestAssertions.That(() => emailError.Metadata["severity"].Should().Be("Warning"));
    }
}
