//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.MediatR.ResultAdapters.Tests;

public sealed record CSharpCommand(string Name, string Email) : IRequest<Result<CSharpResponse>>;

public sealed record CSharpResponse(string Name);

public sealed class CSharpHandler : IRequestHandler<CSharpCommand, Result<CSharpResponse>> {
    public static int Calls { get; private set; }

    public static void Reset() => Calls = 0;

    public Task<Result<CSharpResponse>> Handle(
        CSharpCommand request,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);
        Calls++;

        return Task.FromResult(global::CSharpFunctionalExtensions.Result.Success(new CSharpResponse(request.Name)));
    }
}

public sealed class CSharpCommandValidator : Validator<CSharpCommand> {
    protected override void Configure(ValidatorRuleBuilder<CSharpCommand> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(CSharpCommand.Name))
               .NotEmpty()
               .WithMessage("Name is required.")
               .WithCode("NAME_REQUIRED");

        builder.RuleFor(static x => x.Email, nameof(CSharpCommand.Email))
               .EmailAddress()
               .WithMessage("Email is invalid.")
               .WithCode("EMAIL_INVALID")
               .WithSeverity(ValidationErrorSeverity.Warning);
    }
}

[TestClass]
public sealed class CSharpFunctionalExtensionsResultAdapterTests {
    [TestMethod]
    public async Task CSharpFunctionalExtensions_Success_FlowsToHandlerAsync() {
        CSharpHandler.Reset();
        await using var provider = MediatRResultTestHost.Create(
            services => services.AddScoped<IValidator<CSharpCommand>, CSharpCommandValidator>(),
            options => options.UseCSharpFunctionalExtensionsAdapter<CSharpResponse>());
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new CSharpCommand("order-1", "buyer@example.com"),
            CancellationToken.None);

        MSTestAssertions.That(() => result.IsSuccess.Should().BeTrue());
        MSTestAssertions.That(() => result.Value.Name.Should().Be("order-1"));
        MSTestAssertions.That(() => CSharpHandler.Calls.Should().Be(1));
    }

    [TestMethod]
    public async Task CSharpFunctionalExtensions_Failure_PreservesDetailsInMessageAsync() {
        CSharpHandler.Reset();
        await using var provider = MediatRResultTestHost.Create(
            services => services.AddScoped<IValidator<CSharpCommand>, CSharpCommandValidator>(),
            options => options.UseCSharpFunctionalExtensionsAdapter<CSharpResponse>());
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new CSharpCommand(string.Empty, "not-an-email"),
            CancellationToken.None);

        MSTestAssertions.That(() => result.IsFailure.Should().BeTrue());
        MSTestAssertions.That(() => CSharpHandler.Calls.Should().Be(0));
        MSTestAssertions.That(() => result.Error.Should().Contain(nameof(CSharpCommand.Name)));
        MSTestAssertions.That(() => result.Error.Should().Contain("NAME_REQUIRED"));
        MSTestAssertions.That(() => result.Error.Should().Contain(nameof(CSharpCommand.Email)));
        MSTestAssertions.That(() => result.Error.Should().Contain("EMAIL_INVALID"));
        MSTestAssertions.That(() => result.Error.Should().Contain("[Warning]"));
    }
}
