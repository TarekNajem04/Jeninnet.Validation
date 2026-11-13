# Validate a Minimal API Request

## Problem

You want every request to a minimal API endpoint validated before the handler runs, with a standard
400 ProblemDetails response.

## Solution

Use `Jeninnet.Validation.AspNetCore`.

```csharp
using Jeninnet.Validation.AspNetCore;
using Jeninnet.Validation.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidationEndpoints(options =>
{
    options.IncludeErrorCodes = false;
    options.IncludeSeverities = false;
    options.Title = "Request validation failed";
});

builder.Services.AddScoped<CreateUserValidator>();
builder.Services.AddScoped<IValidator<CreateUser>, CreateUserValidator>();

var app = builder.Build();
app.UseExceptionHandler();

app.MapPost("/users", (CreateUser request) => Results.Ok(new { request.Email }))
   .AddRequestValidation<CreateUser>();

app.Run();

public sealed record CreateUser(string Name, string Email, int Age);

public sealed class CreateUserValidator : Validator<CreateUser>
{
    protected override void Configure(ValidatorRuleBuilder<CreateUser> builder)
    {
        builder.RuleFor(x => x.Name, nameof(CreateUser.Name))
               .NotEmpty()
               .Length(2, 100);

        builder.RuleFor(x => x.Email, nameof(CreateUser.Email))
               .NotEmpty()
               .EmailAddress();

        builder.RuleFor(x => x.Age, nameof(CreateUser.Age))
               .InclusiveBetween(18, 120);
    }
}
```

The filter resolves `IValidator<CreateUser>` from DI. If no validator is registered, the request
passes through.

## Verify

```bash
curl -i -X POST http://localhost:5000/users \
  -H "Content-Type: application/json" \
  -d '{"name":"","email":"bad","age":12}'
```

Expect:

- `400 Bad Request`;
- a ProblemDetails body with `errors` keyed by `Name`, `Email`, `Age`.

## Query, route, and header parameters

Use `.AddRequestValidation()` without a generic argument to validate every bound parameter that has
a registered validator:

```csharp
app.MapPost("/orders/{id}",
        ([FromRoute] string id,
         [FromQuery] int page,
         [FromHeader(Name = "X-Tenant")] string tenant,
         CreateOrder request) => Results.Ok())
   .AddRequestValidation();
```

Errors are keyed by binding source and parameter/member: `route.id`, `query.page`,
`header.tenant`, and body members such as `Email`.

## Variants

- `AddRequestValidation<T>()` is also available on `RouteGroupBuilder`.
- `[SkipJeninnetValidation]` / `[DisableValidation]` skip an endpoint or a parameter;
  `[JeninnetValidationRuleSets]` selects rule sets.
- `AddValidationEndpoints()` registers a `ValidationException` handler, so `ValidateAndThrow` in a
  handler also produces a 400.
- To expose error codes/severities, enable them in `ValidationProblemOptions`.

## Related

- [ASP.NET Core guide](../02-guides/08-aspnet-core.md)
- [Integration packages](../03-reference/05-integration-packages.md)
