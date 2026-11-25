# Jeninnet.Validation.Grpc

Validates gRPC request messages before the service method runs. Invalid requests return
`StatusCode.InvalidArgument` with `google.rpc.BadRequest` field violations.

## Requirements

- .NET 10.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.Grpc --version 1.0.0-preview.3
```

## Usage

```csharp
using Jeninnet.Validation.Grpc;

builder.Services.AddJeninnetGrpcValidation();
builder.Services.AddGrpc(options =>
    options.Interceptors.Add<ValidationInterceptor>());

builder.Services.AddScoped<IValidator<CreateRequest>, CreateRequestValidator>();
```

The interceptor resolves every `IValidator<TRequest>` from the request service provider, runs
them on the asynchronous path with the call cancellation token, and aggregates errors. Requests
with no registered validator bypass validation.

## Failure details

The status description contains the joined human-readable messages. The
`grpc-status-details-bin` trailer contains two kinds of details:

- **`google.rpc.BadRequest`** — one `FieldViolation` per error. `Field` is the member path, or
  `$` for a model-level error; `Description` is the same message used in the status description.
- **`google.rpc.ErrorInfo`** — one detail per error. `Reason` is the error code (or
  `VALIDATION_ERROR` when no code is present), `Domain` is `jeninnet.validation/v1`, and
  `Metadata` contains `field`, `severity`, and `code` when an error code exists.

Codes and severities are not appended to the human-readable description. Old clients that only
read `google.rpc.BadRequest` keep working; clients that need the structured metadata can read
`ErrorInfo` and ignore it when absent.

```csharp
var details = ex.Trailers.Single(entry => entry.Key == "grpc-status-details-bin");
var status = Google.Rpc.Status.Parser.ParseFrom(details.ValueBytes);
var badRequest = status.Details
    .Where(detail => detail.Is(Google.Rpc.BadRequest.Descriptor))
    .Select(detail => detail.Unpack<Google.Rpc.BadRequest>())
    .Single();
var errorInfos = status.Details
    .Where(detail => detail.Is(Google.Rpc.ErrorInfo.Descriptor))
    .Select(detail => detail.Unpack<Google.Rpc.ErrorInfo>())
    .ToArray();
```

The same projection feeds HTTP ProblemDetails; model-level errors use the `$` key there too.

## Rule sets

```csharp
[GrpcValidationRuleSets("strict")]
public sealed class CreateRequest { /* generated protobuf message */ }
```

Without the attribute, `GrpcValidationOptions.RuleSets` applies. An empty list means all rules.

## Scope

- Unary and server-streaming calls are validated.
- Client-streaming and duplex-streaming calls are not intercepted in this version.
- The package is optional; Core and Abstractions have no gRPC dependency.

## AOT position

**Trim-compatible only.** See the [25-package AOT matrix](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/04-operations/02-aot-and-trimming.md#package-matrix).

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
