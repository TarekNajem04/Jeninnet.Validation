//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.MediatR.ResultAdapters.Tests;

public sealed record ErrorOrCommand(string Name, string Email) : IRequest<ErrorOr<ErrorOrResponse>>;

public sealed record ErrorOrResponse(string Name);

public sealed class ErrorOrHandler : IRequestHandler<ErrorOrCommand, ErrorOr<ErrorOrResponse>> {
    public static int Calls { get; private set; }

    public static void Reset() => Calls = 0;

    public Task<ErrorOr<ErrorOrResponse>> Handle(
        ErrorOrCommand request,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);
        Calls++;

        ErrorOr<ErrorOrResponse> response = new ErrorOrResponse(request.Name);
        return Task.FromResult(response);
    }
}

public sealed class ErrorOrCommandValidator : Validator<ErrorOrCommand> {
    protected override void Configure(ValidatorRuleBuilder<ErrorOrCommand> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ErrorOrCommand.Name))
               .NotEmpty()
               .WithMessage("Name is required.")
               .WithCode("NAME_REQUIRED");

        builder.RuleFor(static x => x.Email, nameof(ErrorOrCommand.Email))
               .EmailAddress()
               .WithMessage("Email is invalid.")
               .WithCode("EMAIL_INVALID")
               .WithSeverity(ValidationErrorSeverity.Warning);
    }
}

[TestClass]
public sealed class ErrorOrResultAdapterTests {
    [TestMethod]
    public async Task ErrorOr_Success_FlowsToHandlerAsync() {
        ErrorOrHandler.Reset();
        await using var provider = MediatRResultTestHost.Create(
            services => services.AddScoped<IValidator<ErrorOrCommand>, ErrorOrCommandValidator>(),
            options => options.UseErrorOrResultAdapter<ErrorOrResponse>());
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new ErrorOrCommand("order-1", "buyer@example.com"), CancellationToken.None);

        MSTestAssertions.That(() => result.IsError.Should().BeFalse());
        MSTestAssertions.That(() => result.Value.Name.Should().Be("order-1"));
        MSTestAssertions.That(() => ErrorOrHandler.Calls.Should().Be(1));
    }

    [TestMethod]
    public async Task ErrorOr_Failure_MapsValidationErrorsAndSkipsHandlerAsync() {
        ErrorOrHandler.Reset();
        await using var provider = MediatRResultTestHost.Create(
            services => services.AddScoped<IValidator<ErrorOrCommand>, ErrorOrCommandValidator>(),
            options => options.UseErrorOrResultAdapter<ErrorOrResponse>());
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new ErrorOrCommand(string.Empty, "not-an-email"), CancellationToken.None);

        MSTestAssertions.That(() => result.IsError.Should().BeTrue());
        MSTestAssertions.That(() => result.Errors.Should().HaveCount(2));
        MSTestAssertions.That(() => ErrorOrHandler.Calls.Should().Be(0));

        var nameError = result.Errors.Single(error => error.Code == "NAME_REQUIRED");
        var emailError = result.Errors.Single(error => error.Code == "EMAIL_INVALID");

        MSTestAssertions.That(() => nameError.Type.Should().Be(ErrorType.Validation));
        MSTestAssertions.That(() => nameError.Description.Should().Be("Name is required."));
        MSTestAssertions.That(() => nameError.Metadata!["memberName"].Should().Be(nameof(ErrorOrCommand.Name)));
        MSTestAssertions.That(() => nameError.Metadata!["severity"].Should().Be("Error"));
        MSTestAssertions.That(() => nameError.Metadata!["errorCode"].Should().Be("NAME_REQUIRED"));

        MSTestAssertions.That(() => emailError.Type.Should().Be(ErrorType.Validation));
        MSTestAssertions.That(() => emailError.Metadata!["memberName"].Should().Be(nameof(ErrorOrCommand.Email)));
        MSTestAssertions.That(() => emailError.Metadata!["severity"].Should().Be("Warning"));
        MSTestAssertions.That(() => emailError.Metadata!["errorCode"].Should().Be("EMAIL_INVALID"));
    }

    [TestMethod]
    public async Task ErrorOr_MissingValidator_BypassesValidationAsync() {
        ErrorOrHandler.Reset();
        await using var provider = MediatRResultTestHost.Create(
            configureOptions: options => options.UseErrorOrResultAdapter<ErrorOrResponse>());
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new ErrorOrCommand(string.Empty, "not-an-email"), CancellationToken.None);

        MSTestAssertions.That(() => result.IsError.Should().BeFalse());
        MSTestAssertions.That(() => ErrorOrHandler.Calls.Should().Be(1));
    }
}
