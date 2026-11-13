# Jeninnet.Validation.MediatR.CSharpFunctionalExtensions

Maps failed Jeninnet.Validation results to a failed `CSharpFunctionalExtensions.Result<T>`. The
handler is skipped for invalid requests.

## Requirements

- .NET 10.0 or later (including .NET 11).
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.MediatR.CSharpFunctionalExtensions --version 1.0.0-preview.1
```

## Usage

```csharp
using CSharpFunctionalExtensions;
using Jeninnet.Validation.MediatR;
using Jeninnet.Validation.MediatR.CSharpFunctionalExtensions;

builder.Services.AddValidationPipeline(options =>
    options.UseCSharpFunctionalExtensionsAdapter<OrderResponse>());

public sealed record CreateOrderCommand(string Name, string Email)
    : IRequest<Result<OrderResponse>>;
```

CSharpFunctionalExtensions stores only a string error, so member names, severities, and codes are
preserved textually in the failure message (`Member: [Severity] message (code)`). Custom state and
message parameters are not representable and are omitted.

`Throw` and `Continue` remain available and unchanged. Without a registered validator the request
bypasses validation and reaches the handler.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
