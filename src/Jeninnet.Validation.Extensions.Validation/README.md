# Jeninnet.Validation.Extensions.Validation

Bridges `Jeninnet.Validation` validators into the `Microsoft.Extensions.Validation`
pipeline used by ASP.NET Core 10/11+ minimal APIs and compatible hosts. On .NET 11 the bridge
implements `IValidatableTypeInfo`/`IValidatableParameterInfo`, including minimal-API parameter
validation.

## Requirements

- .NET 10.0 or later (the package ships `lib/net10.0` and `lib/net11.0` assets).
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.Extensions.Validation --version 1.0.0-preview.1
```

## Usage

Register generated or fluent validators first, then add the bridge:

```csharp
builder.Services.AddScoped<CreateOrderValidator>();
builder.Services.AddScoped<IValidator<CreateOrder>, CreateOrderValidator>();

builder.Services.AddValidation();
builder.Services.AddJeninnetValidation();
```

`AddJeninnetValidation()` scans the service collection for `IValidator<T>` registrations and adds
an `IValidatableInfoResolver` for each model. For Native AOT, use the reflection-free generic
overload instead:

```csharp
builder.Services.AddJeninnetValidation<CreateOrder>();
```

The bridge runs the validator's asynchronous path, so `MustAsync`, `CustomAsync`, and async
conditions are supported. Errors are added to the host's validation dictionary using the same
member paths and messages as the validator.

## Coexistence with `[JeninnetValidatable]`

Models already annotated with `Jeninnet.Validation.DataAnnotations.JeninnetValidatableAttribute` are
skipped by this bridge, avoiding double validation when the DataAnnotations bridge is also active.

## Options validation

The same package integrates Jeninnet validators with `Microsoft.Extensions.Options`. Register the
validator first, then call `AddValidatedOptions<T>()` on the options builder (or on the service
collection):

```csharp
builder.Services.AddScoped<IValidator<WorkerOptions>, WorkerOptionsValidator>();

builder.Services.AddOptions<WorkerOptions>()
                .BindConfiguration("Worker")
                .AddValidatedOptions();
```

`AddValidatedOptions<T>()` registers `IValidateOptions<T>` and calls `ValidateOnStart()`, so an
invalid configuration fails at startup with an `OptionsValidationException`. Each failure uses the
form `name: Member [code] message` (the `name:` prefix appears for named options); only errors with
`ValidationErrorSeverity.Error` fail validation, while warnings are ignored. Validators are resolved
from a new scope per validation, so scoped dependencies work.

If no `IValidator<T>` is registered, startup fails with a message that names the missing validator.
The integration is synchronous: validators that only expose asynchronous rules throw
`AsyncValidatorInvokedSynchronouslyException`, which is surfaced as a configuration failure. Async
options validation requires the .NET 11 `IAsyncStartupValidator` path planned for `1.1.0-preview.1`.

## Limits

- The non-generic overload discovers model types from `IValidator<T>` registrations and uses
  runtime generic construction; the generic overload is trim/AOT-safe.
- MVC is outside `Microsoft.Extensions.Validation`; use `Jeninnet.Validation.AspNetCore` there.
- The server remains authoritative; the bridge only adapts results into the host pipeline.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
