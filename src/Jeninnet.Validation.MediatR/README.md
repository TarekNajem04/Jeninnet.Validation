# Jeninnet.Validation.MediatR

One-line MediatR validation. The package registers a pipeline behavior that
resolves every `IValidator<TRequest>` from DI, validates the request, and either
throws, exposes the result to the handler, or maps it to a result type.

## Requirements

- .NET 11.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.MediatR --version 1.0.0-preview.1
```

## Usage

```csharp
using Jeninnet.Validation.MediatR;

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddValidationPipeline();
builder.Services.AddScoped<IValidator<CreateOrderCommand>, CreateOrderCommandValidator>();
```

The behavior runs before the handler. Requests with no registered validator
bypass validation.

## Failure strategies

```csharp
builder.Services.AddValidationPipeline(options =>
    options.FailureStrategy = MediatRValidationFailureStrategy.Throw);
```

| Strategy | Behavior |
|---|---|
| `Throw` (default) | Throws `ValidationException` with all aggregated errors. |
| `Continue` | Stores the result in `ValidationResultAccessor` and calls the handler. |
| `Result` | Maps the result through `UseResultAdapter` and skips the handler. |

```csharp
public sealed class CreateOrderHandler(ValidationResultAccessor accessor)
    : IRequestHandler<CreateOrderCommand, OrderResponse> {
    public Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken) {
        if(accessor.Result is { IsValid: false } result) {
            return Task.FromResult(OrderResponse.Invalid(result.Errors));
        }

        // ...
    }
}
```

## Result adapters

Optional packages map a failed result to a typed response and skip the handler:

| Package | Response | Registration |
|---|---|---|
| `Jeninnet.Validation.MediatR.ErrorOr` | `ErrorOr<T>` | `options.UseErrorOrResultAdapter<T>()` |
| `Jeninnet.Validation.MediatR.FluentResults` | `FluentResults.Result<T>` | `options.UseFluentResultsAdapter<T>()` |
| `Jeninnet.Validation.MediatR.CSharpFunctionalExtensions` | `CSharpFunctionalExtensions.Result<T>` | `options.UseCSharpFunctionalExtensionsAdapter<T>()` |

```csharp
using Jeninnet.Validation.MediatR.ErrorOr;

builder.Services.AddValidationPipeline(options =>
    options.UseErrorOrResultAdapter<OrderResponse>());
```

Each convenience method calls `options.UseResultAdapter<TResponse>(mapper)` and
switches to `MediatRValidationFailureStrategy.Result`. You can register a custom
mapper directly:

```csharp
options.UseResultAdapter<MyResponse>(new MyValidationResultMapper());
```

`ValidationErrorFormatter` exposes the shared code/message/metadata mapping used
by the adapters. `ErrorOr` and `FluentResults` preserve member name, severity,
code, message key, message parameters, and custom state. CSharpFunctionalExtensions
stores a string error, so details are preserved textually; custom state and
message parameters are not representable.

## Rule sets

Mark a request type to validate only named rule sets:

```csharp
[MediatRValidationRuleSets("strict")]
public sealed record CreateOrderCommand(CreateOrderRequest Request) : IRequest<OrderResponse>;
```

The lookup is cached per request type. Without the attribute, the default rule
sets from `MediatRValidationOptions.RuleSets` apply; an empty list means all
rules.

## Behavior

- **All validators run** for a request type; errors are aggregated, not
  first-match.
- Sync and async validators share the async path, and the cancellation token is
  forwarded.
- MediatR is referenced only by this optional package; `Core` and `Abstractions`
  stay framework-free.
- `Throw` and `Continue` remain available and unchanged.

## Limits

- Result adapters are separate optional packages; the core MediatR package has no
  dependency on ErrorOr, FluentResults, or CSharpFunctionalExtensions.
- The `Result` strategy requires a configured mapper; otherwise the behavior
  throws an actionable `InvalidOperationException`.
- Validators are resolved from DI; register them explicitly or through your
  existing scanning setup.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
