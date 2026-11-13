# Validate an MVC Controller

## Problem

You have existing MVC controllers and want automatic validation with `ModelState` and a standard
400 response.

## Solution

```csharp
using Jeninnet.Validation.AspNetCore;

builder.Services.AddControllers();
builder.Services.AddMvcValidation();
builder.Services.AddScoped<IValidator<RegisterUser>, RegisterUserValidator>();
```

```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    [HttpPost]
    public IActionResult Register(RegisterUser request) => Ok(new { request.Email });

    [HttpPost("admin")]
    [JeninnetValidationRuleSets("Admin")]
    public IActionResult RegisterAdmin(RegisterUser request) => Ok();

    [HttpPost("skip")]
    [SkipJeninnetValidation]
    public IActionResult Skip(RegisterUser request) => Ok();
}
```

The filter validates actions whose request model has a registered `IValidator<T>`, writes errors to
`ModelState`, and returns a 400 validation problem.

## Rule sets per action

`[JeninnetValidationRuleSets("Admin")]` selects named sets for that action. Only those sets run; add
`default` to include ungrouped rules. Use `[SkipJeninnetValidation]` to opt out.

## Verify

Send an invalid model and confirm:

- the action does not run;
- the response is a 400 validation problem;
- `ModelState` contains one entry per member.

## Related

- [ASP.NET Core guide](../02-guides/08-aspnet-core.md)
- [Rule sets and cascade](../02-guides/04-rule-sets-and-cascade.md)
