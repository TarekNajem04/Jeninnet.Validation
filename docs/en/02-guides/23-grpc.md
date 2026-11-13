# gRPC

The `Jeninnet.Validation.Grpc` package validates gRPC request messages before service methods run.
Invalid requests return `StatusCode.InvalidArgument` with `google.rpc.BadRequest` details.

## Setup

```csharp
using Jeninnet.Validation.Grpc;

builder.Services.AddJeninnetGrpcValidation();
builder.Services.AddGrpc(options =>
    options.Interceptors.Add<ValidationInterceptor>());

builder.Services.AddScoped<IValidator<CreateRequest>, CreateRequestValidator>();
```

The interceptor resolves every `IValidator<TRequest>` from the request service provider, runs them
on the asynchronous path with the call cancellation token, and aggregates errors. Requests with no
registered validator bypass validation.

## Failure details

The status description contains the joined messages. The `grpc-status-details-bin` trailer contains
a `google.rpc.BadRequest` with one `FieldViolation` per error. `ValidationError.MemberName` becomes
the violation field, so clients that understand Google RPC status details can map errors to message
fields.

```csharp
var details = ex.Trailers.Single(entry => entry.Key == "grpc-status-details-bin");
var status = Google.Rpc.Status.Parser.ParseFrom(details.ValueBytes);
var badRequest = status.Details.Select(detail => detail.Unpack<Google.Rpc.BadRequest>()).Single();
```

## Rule sets

```csharp
[GrpcValidationRuleSets("strict")]
public sealed partial class CreateRequest { /* generated protobuf partial */ }
```

Without the attribute, `GrpcValidationOptions.RuleSets` applies. An empty list means all rules.

## Limits

- Unary and server-streaming calls are validated.
- Client-streaming and duplex-streaming calls are not intercepted in this version.
- The package is optional; Core and Abstractions have no gRPC dependency.

## Verify

Call an invalid unary method and confirm the client receives `InvalidArgument` with field
violations. Call a method with no registered validator and confirm the service runs.

## Related

- [ASP.NET Core](08-aspnet-core.md)
- [Integration packages](../03-reference/05-integration-packages.md)
