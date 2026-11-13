# ASP.NET Core

The `Jeninnet.Validation.AspNetCore` package provides a minimal API endpoint filter, a
ProblemDetails mapper, and automatic MVC validation. Core and Abstractions stay free of ASP.NET
Core references.

## Minimal API

```csharp
using Jeninnet.Validation.AspNetCore;
using Jeninnet.Validation.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidationEndpoints(options =>
{
    options.IncludeErrorCodes = false;   // privacy-preserving default
    options.IncludeSeverities = false;
    options.Title = "Request validation failed";
});

builder.Services.AddScoped<CreateUserValidator>();
builder.Services.AddScoped<IValidator<CreateUser>, CreateUserValidator>();

var app = builder.Build();
app.UseExceptionHandler();

app.MapPost("/users", (CreateUser request) => Results.Ok(request))
   .AddRequestValidation<CreateUser>();

app.Run();
```

The filter resolves `IValidator<T>` from DI. If no validator is registered, or the request argument
is null, the filter passes through. Invalid requests return a 400 ProblemDetails response with errors
grouped by member.

Use the non-generic `AddRequestValidation()` overload to validate **all** bound endpoint parameters
with registered validators. It reads the handler parameter metadata and prefixes non-body errors
with their binding source:

```csharp
app.MapPost("/orders/{id}",
        ([FromRoute] string id,
         [FromQuery] int page,
         [FromHeader(Name = "X-Tenant")] string tenant,
         CreateOrder request) => Results.Ok())
   .AddRequestValidation();
```

Errors from this endpoint use keys such as `route.id`, `query.page`, `header.tenant`, while body
model errors keep their member names (for example `Email`). `[SkipJeninnetValidation]` and
`[DisableValidation]` work on endpoints and parameters; `[SkipValidation]` on a parameter skips
validation for that parameter only; `[JeninnetValidationRuleSets]` is passed to the validator. The
non-generic filter reads handler parameter metadata and uses cached compiled delegates for
validator invocation; use the typed overload when you need an explicitly bounded, AOT-friendly
filter.

## MVC

```csharp
using Jeninnet.Validation.AspNetCore;

builder.Services.AddControllers();
builder.Services.AddMvcValidation();
```

The action filter validates actions whose request model has a registered `IValidator<T>`, maps
errors into `ModelState`, and returns a 400 validation problem. Use:

- `[SkipJeninnetValidation]` on a controller or action to opt out.
- `[JeninnetValidationRuleSets("Admin")]` to select rule sets for an action.

## Validation interceptors

Register `IValidationInterceptor` implementations to observe or replace request validation in both
minimal APIs and MVC. Interceptors run in registration order:

```csharp
builder.Services.AddValidationInterceptor<AuditValidationInterceptor>();
// builder.Services.AddValidationInterceptor<CachingInterceptor>(ServiceLifetime.Singleton);

public sealed class AuditValidationInterceptor : IValidationInterceptor
{
    public ValueTask<ValidationResult?> BeforeValidationAsync(
        ValidationInterceptorContext context,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<ValidationResult?>(null); // return a result to skip the validator

    public ValueTask<ValidationResult> AfterValidationAsync(
        ValidationInterceptorContext context,
        ValidationResult result,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(result); // return the original or a replacement
}
```

A non-null `BeforeValidationAsync` result short-circuits the validator, still passes through every
`AfterValidationAsync`, and becomes the response: 400 when invalid, the handler/action when valid.
`AfterValidationAsync` receives the current result and may rewrite it. The context exposes the model
instance and type, validator name, service provider, `HttpContext`, and the minimal API `Endpoint`
or MVC `ActionDescriptor`.

Interceptors are resolved only when at least one is registered; without interceptors the filters
keep their previous path. Exceptions thrown by an interceptor are wrapped in `ValidationException`
with the interceptor type name. `OperationCanceledException` is rethrown unchanged.

## ProblemDetails mapping

`ValidationResult.ToProblemErrors()` groups errors by member and preserves order:

```csharp
var errors = result.ToProblemErrors(new ValidationProblemOptions
{
    IncludeErrorCodes = true,
    IncludeSeverities = true
});
```

`AddValidationEndpoints()` also registers an exception handler that converts `ValidationException`
to a 400 response.

Minimal API and MVC filters can add request diagnostics to the problem extensions. All flags default
to `false`:

```csharp
builder.Services.AddValidationEndpoints(options =>
{
    options.IncludeCorrelationId = true;
    options.CorrelationIdHeader = "X-Correlation-Id"; // falls back to TraceIdentifier
    options.IncludeModelType = true;
    options.IncludeDuration = true;
});
```

The extensions are `correlationId`, `modelType`, and `durationMs`. They contain request metadata
only; messages and property values stay in the `errors` dictionary. The same controls are available
through `MvcValidationOptions.ProblemOptions` for `AddMvcValidation()`.

For OpenTelemetry traces and metrics, add the optional diagnostics package:

```csharp
using Jeninnet.Validation.Diagnostics;

builder.Services.AddValidationDiagnostics();
builder.Services.AddOpenTelemetry()
       .WithTracing(tracing => tracing.AddSource("Jeninnet.Validation"))
       .WithMetrics(metrics => metrics.AddMeter("Jeninnet.Validation"));
```

## Microsoft.Extensions.Validation (`AddValidation`)

For hosts that use the .NET 11 `Microsoft.Extensions.Validation` pipeline, register the bridge after the
validators:

```csharp
using Jeninnet.Validation.Extensions.Validation;

builder.Services.AddScoped<IValidator<CreateOrder>, CreateOrderValidator>();
builder.Services.AddValidation();
builder.Services.AddJeninnetValidation();
```

`AddJeninnetValidation()` discovers model types from existing `IValidator<T>` registrations. Use
`AddJeninnetValidation<CreateOrder>()` for the reflection-free/AOT-safe path. The adapter runs the
asynchronous validator path and writes errors into the standard validation dictionary, so minimal
API `ValidationProblemDetails` keeps the same member paths.

Models annotated with `[JeninnetValidatable]` are skipped by this bridge to avoid double validation
when the DataAnnotations bridge is also active.

Jeninnet's catalog `AddValidation()` (namespace `Jeninnet.Validation.Catalog`) registers the message
pipeline and never replaces `IStringLocalizerFactory`, so it is safe alongside `AddControllers()` in
Development. Store-backed `IStringLocalizer` support is opt-in through
`JeninnetLocalizationExtensions.AddLocalization(services)`. See the
[dependency-injection guide](17-dependency-injection.md#register-with-one-call-generated-catalog).

## Async rules

The filter uses the async validation path, so `MustAsync`, `CustomAsync`, and async conditions work.
The MVC filter is async as well.

## Verify

Send an invalid request and confirm:

- status is 400;
- `errors` is keyed by member path;
- messages contain no error codes unless enabled;
- `ValidationException` also produces a 400.

## Related

- [MediatR](09-mediatr.md)
- [Integration packages](../03-reference/05-integration-packages.md)
