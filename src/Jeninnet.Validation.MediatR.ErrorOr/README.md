# Jeninnet.Validation.MediatR.ErrorOr

Maps failed Jeninnet.Validation results to `ErrorOr<T>` responses in the MediatR validation
pipeline. The handler is skipped for invalid requests.

## Requirements

- .NET 11.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.MediatR.ErrorOr --version 1.0.0-preview.1
```

## Usage

```csharp
using Jeninnet.Validation.MediatR;
using Jeninnet.Validation.MediatR.ErrorOr;

builder.Services.AddValidationPipeline(options =>
    options.UseErrorOrResultAdapter<OrderResponse>());

public sealed record CreateOrderCommand(string Name, string Email)
    : IRequest<ErrorOr<OrderResponse>>;
```

Every validation error becomes `Error.Validation` with:

- `Code`: `ErrorCode`, then `MessageKey`, then `MessageId`, then `Validation`.
- `Description`: the validation message.
- `Metadata`: `memberName`, `severity`, `messageKey`, `errorCode`, `messageId`,
  `messageParameters`, and `customState` when present.

`Throw` and `Continue` remain available and unchanged. Without a registered validator the request
bypasses validation and reaches the handler.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
