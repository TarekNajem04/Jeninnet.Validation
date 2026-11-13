# Jeninnet.Validation.MediatR.FluentResults

Maps failed Jeninnet.Validation results to a failed `FluentResults.Result<T>`. The handler is
skipped for invalid requests.

## Requirements

- .NET 11.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.MediatR.FluentResults --version 1.0.0-preview.1
```

## Usage

```csharp
using FluentResults;
using Jeninnet.Validation.MediatR;
using Jeninnet.Validation.MediatR.FluentResults;

builder.Services.AddValidationPipeline(options =>
    options.UseFluentResultsAdapter<OrderResponse>());

public sealed record CreateOrderCommand(string Name, string Email)
    : IRequest<Result<OrderResponse>>;
```

The adapter creates one `Error` per validation error with the message and metadata containing
`memberName`, `severity`, `messageKey`, `errorCode`, `messageId`, `messageParameters`, and
`customState` when present.

`Throw` and `Continue` remain available and unchanged. Without a registered validator the request
bypasses validation and reaches the handler.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
