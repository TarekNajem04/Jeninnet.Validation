# Jeninnet.Validation.AspNetCore

ASP.NET Core integration for `Jeninnet.Validation`:

- **Minimal APIs:** `.AddRequestValidation<T>()` body filter,
  `.AddRequestValidation()` for body/query/route/header parameters, and
  `ValidationResult.ToProblemErrors()`.
- **MVC / Razor Pages:** `AddMvcValidation()` auto-validation filter with
  `[SkipJeninnetValidation]` and `[JeninnetValidationRuleSets(...)]`.
- **Interceptors:** `AddValidationInterceptor<T>()` observes or replaces validation in both
  pipelines (`BeforeValidationAsync`/`AfterValidationAsync`).
- **ProblemDetails:** `ValidationException` → 400 handler and privacy-preserving
  error-code/severity options.

## Requirements

- .NET 11.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.AspNetCore --version 1.0.0-preview.1
```

## Minimal API

```csharp
builder.Services.AddValidationEndpoints();

app.MapPost("/users", (CreateUserRequest request) => ...)
   .AddRequestValidation<CreateUserRequest>();
```

The non-generic `.AddRequestValidation()` inspects the handler parameters and validates body,
query, route, and header arguments that have registered validators. Error keys are prefixed with
`query.`, `route.`, or `header.`; body errors keep their member names. Use
`[SkipJeninnetValidation]`/`[DisableValidation]` on an endpoint or parameter and
`[JeninnetValidationRuleSets]` to select rule sets.

## Validation interceptors

`IValidationInterceptor` hooks into request validation for both minimal APIs and MVC:

```csharp
builder.Services.AddValidationInterceptor<AuditValidationInterceptor>();

public sealed class AuditValidationInterceptor : IValidationInterceptor
{
    public ValueTask<ValidationResult?> BeforeValidationAsync(
        ValidationInterceptorContext context,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<ValidationResult?>(null);

    public ValueTask<ValidationResult> AfterValidationAsync(
        ValidationInterceptorContext context,
        ValidationResult result,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(result);
}
```

Interceptors run in registration order. A non-null `BeforeValidationAsync` result skips the
validator, still runs every `AfterValidationAsync`, and becomes the response (400 when invalid,
the handler/action when valid). `AfterValidationAsync` may return a replacement result. The default
registration lifetime is scoped; pass `ServiceLifetime` to override. With no interceptor registered
the filters take their original path. Interceptor exceptions become `ValidationException` with the
interceptor name; `OperationCanceledException` is rethrown.

## MVC

```csharp
builder.Services.AddMvcValidation();

[SkipJeninnetValidation]
public IActionResult Health() => Ok();
```

The package is an integration shell; `Jeninnet.Validation` and
`Jeninnet.Validation.Abstractions` remain free of ASP.NET Core references.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
