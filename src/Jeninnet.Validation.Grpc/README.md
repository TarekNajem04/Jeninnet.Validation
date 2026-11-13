# Jeninnet.Validation.Grpc

Validates gRPC request messages before the service method runs. Invalid requests return
`StatusCode.InvalidArgument` with `google.rpc.BadRequest` field violations.

## Requirements

- .NET 11.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.Grpc --version 1.0.0-preview.1
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

The status description contains the joined messages. The `grpc-status-details-bin` trailer
contains a `google.rpc.BadRequest` with one `FieldViolation` per error, preserving
`ValidationError.MemberName` as the field. Clients that understand Google RPC status details can
read the structured errors.

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

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
