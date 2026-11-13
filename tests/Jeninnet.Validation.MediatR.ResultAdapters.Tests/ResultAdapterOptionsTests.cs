//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR.ResultAdapters.Tests;

public sealed record ResultStrategyCommand(string Name) : IRequest<string>;

public sealed class ResultStrategyHandler : IRequestHandler<ResultStrategyCommand, string> {
    public Task<string> Handle(ResultStrategyCommand request, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(request.Name);
    }
}

public sealed class ResultStrategyCommandValidator : Validator<ResultStrategyCommand> {
    protected override void Configure(ValidatorRuleBuilder<ResultStrategyCommand> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ResultStrategyCommand.Name))
               .NotEmpty();
    }
}

[TestClass]
public sealed class ResultAdapterOptionsTests {
    [TestMethod]
    public async Task ResultStrategy_WithoutMapper_ThrowsClearExceptionAsync() {
        await using var provider = MediatRResultTestHost.Create(
            services => services.AddScoped<IValidator<ResultStrategyCommand>, ResultStrategyCommandValidator>(),
            options => options.FailureStrategy = MediatRValidationFailureStrategy.Result);
        var mediator = provider.GetRequiredService<IMediator>();
        InvalidOperationException? caught = null;

        try {
            await mediator.Send(new ResultStrategyCommand(string.Empty), CancellationToken.None);
        }
        catch(InvalidOperationException exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().NotBeNull());
        MSTestAssertions.That(() => caught!.Message.Should().Contain("UseResultAdapter"));
    }

    [TestMethod]
    public async Task MultipleResponseTypes_UseTheirOwnResultAdapterAsync() {
        await using var provider = MediatRResultTestHost.Create(
            services => {
                services.AddScoped<IValidator<ErrorOrCommand>, ErrorOrCommandValidator>();
                services.AddScoped<IValidator<FluentResultsCommand>, FluentResultsCommandValidator>();
            },
            options => {
                options.UseErrorOrResultAdapter<ErrorOrResponse>();
                options.UseFluentResultsAdapter<FluentResultsResponse>();
            });

        var mediator = provider.GetRequiredService<IMediator>();

        var errorOrResponse = await mediator.Send(
            new ErrorOrCommand(string.Empty, "not-an-email"),
            CancellationToken.None);
        var fluentResultsResponse = await mediator.Send(
            new FluentResultsCommand(string.Empty, "not-an-email"),
            CancellationToken.None);

        MSTestAssertions.That(() => errorOrResponse.IsError.Should().BeTrue());
        MSTestAssertions.That(() => fluentResultsResponse.IsFailed.Should().BeTrue());
    }
}
