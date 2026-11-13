# Validate a MediatR Command

## Problem

You want every command/query validated before its handler runs, with all errors aggregated.

## Solution

```csharp
using Jeninnet.Validation.MediatR;

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddValidationPipeline();

builder.Services.AddScoped<IValidator<CreateOrderCommand>, CreateOrderCommandValidator>();
```

```csharp
public sealed record CreateOrderCommand(CreateOrderRequest Request)
    : IRequest<OrderResponse>;

public sealed class CreateOrderCommandValidator : Validator<CreateOrderCommand>
{
    protected override void Configure(ValidatorRuleBuilder<CreateOrderCommand> builder)
    {
        builder.RuleFor(x => x.Request.CustomerName, "CustomerName").NotEmpty();
        builder.RuleFor(x => x.Request.Email, "Email").EmailAddress();
    }
}
```

Invalid requests throw `ValidationException` with every error. Use `Continue` when the handler
should shape the response:

```csharp
builder.Services.AddValidationPipeline(options =>
    options.FailureStrategy = MediatRValidationFailureStrategy.Continue);
```

```csharp
public sealed class Handler(ValidationResultAccessor accessor)
    : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    public Task<OrderResponse> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (accessor.Result is { IsValid: false } result)
        {
            return Task.FromResult(OrderResponse.Invalid(result.Errors));
        }

        return Task.FromResult(OrderResponse.Ok());
    }
}
```

## Typed result responses

Use an optional result adapter when the handler returns `ErrorOr<T>`, `FluentResults.Result<T>`, or
`CSharpFunctionalExtensions.Result<T>`:

```csharp
using Jeninnet.Validation.MediatR.ErrorOr;

builder.Services.AddValidationPipeline(options =>
    options.UseErrorOrResultAdapter<OrderResponse>());

public sealed record CreateOrderCommand(CreateOrderRequest Request)
    : IRequest<ErrorOr<OrderResponse>>;
```

The invalid request returns a mapped failure and the handler is not invoked. Use
`UseFluentResultsAdapter<T>()` or `UseCSharpFunctionalExtensionsAdapter<T>()` for the other
libraries. A custom adapter can be registered through
`options.UseResultAdapter<TResponse>(mapper)`.

CSharpFunctionalExtensions stores only a string error, so member names, severities, and codes are
preserved in the message text; custom state is omitted. ErrorOr and FluentResults preserve the
structured details in metadata.

## Rule sets per request

```csharp
[MediatRValidationRuleSets("strict")]
public sealed record CreateOrderCommand(CreateOrderRequest Request)
    : IRequest<OrderResponse>;
```

## Verify

Send an invalid command and confirm the handler does not run and `ValidationException.Errors`
contains every member error. Send a request with no registered validator and confirm the handler
runs.

## Related

- [MediatR guide](../02-guides/09-mediatr.md)
- [Integration packages](../03-reference/05-integration-packages.md)
