# MediatR

The `Jeninnet.Validation.MediatR` package validates every request before its handler runs.

## Setup

```csharp
using Jeninnet.Validation.MediatR;

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddValidationPipeline();

builder.Services.AddScoped<IValidator<CreateOrderCommand>, CreateOrderCommandValidator>();
```

The behavior resolves every registered `IValidator<TRequest>`, aggregates errors, and forwards the
cancellation token. Requests without a validator bypass validation.

## Failure strategies

```csharp
builder.Services.AddValidationPipeline(options =>
    options.FailureStrategy = MediatRValidationFailureStrategy.Throw);
```

| Strategy | Behavior |
|---|---|
| `Throw` (default) | Throws `ValidationException` with all aggregated errors. |
| `Continue` | Stores the result in `ValidationResultAccessor` and calls the handler. |
| `Result` | Maps the result to the response through a result adapter and skips the handler. |

```csharp
public sealed class CreateOrderHandler(ValidationResultAccessor accessor)
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

## Result adapters

The optional adapter packages return typed failures instead of throwing or calling the handler:

| Package | Response | Registration |
|---|---|---|
| `Jeninnet.Validation.MediatR.ErrorOr` | `ErrorOr<T>` | `UseErrorOrResultAdapter<T>()` |
| `Jeninnet.Validation.MediatR.FluentResults` | `FluentResults.Result<T>` | `UseFluentResultsAdapter<T>()` |
| `Jeninnet.Validation.MediatR.CSharpFunctionalExtensions` | `CSharpFunctionalExtensions.Result<T>` | `UseCSharpFunctionalExtensionsAdapter<T>()` |

```csharp
using Jeninnet.Validation.MediatR.ErrorOr;

builder.Services.AddValidationPipeline(options =>
    options.UseErrorOrResultAdapter<OrderResponse>());

public sealed record CreateOrderCommand(string Name, string Email)
    : IRequest<ErrorOr<OrderResponse>>;
```

The convenience methods call `UseResultAdapter<TResponse>(mapper)` and switch to
`MediatRValidationFailureStrategy.Result`. A custom mapper can be registered directly. ErrorOr
and FluentResults preserve member name, severity, code, message key, parameters, and custom state
in their metadata; CSharpFunctionalExtensions stores a string, so those details are preserved
textually and custom state is omitted.

## Rule sets

```csharp
[MediatRValidationRuleSets("strict")]
public sealed record CreateOrderCommand(CreateOrderRequest Request)
    : IRequest<OrderResponse>;
```

The lookup is cached per request type. Without the attribute, `MediatRValidationOptions.RuleSets`
is the fallback; an empty list means all rules. When a set is selected, add `default` to include
ungrouped rules.

## Behavior notes

- All validators run; errors are aggregated, not first-match.
- Sync and async validators share the async path.
- The package is optional; MediatR is never referenced by Core/Abstractions.
- Result adapters are separate optional packages; the core MediatR package has no dependency on
  any result library. `Throw` and `Continue` remain unchanged.

## Verify

Send an invalid command and confirm `ValidationException.Errors` contains every member error, then
send a request with no registered validator and confirm the handler runs.

## Related

- [ASP.NET Core](08-aspnet-core.md)
- [Integration packages](../03-reference/05-integration-packages.md)
