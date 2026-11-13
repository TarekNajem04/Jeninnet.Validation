# Error Handling

Validation produces data, not exceptions, by default. This guide shows when to throw, how to map
errors to HTTP responses, and how to keep messages useful without leaking internals.

## `Validate` vs `ValidateAndThrow`

```csharp
var result = validator.Validate(customer);

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.MemberName}: {error.Message} ({error.ErrorCode})");
    }
}
```

```csharp
try
{
    validator.ValidateAndThrow(customer);
}
catch (ValidationException exception)
{
    // Every error is collected before the exception is thrown.
    Console.WriteLine(exception.Errors.Count);
}
```

`ValidateAndThrow` is convenient at a boundary where an exception is the contract. `Validate` is
better when the caller can branch, aggregate, or translate errors.

## Error model

| Member | Use |
|---|---|
| `MemberName` | Code-facing path; rule-level `WithName` / `OverridePropertyName` can replace it. |
| `Message` | Human-readable text; may be `null` when only a message key is present. |
| `MessageKey` | Stable localization key. |
| `ErrorCode` | Machine-readable code for clients. |
| `Severity` | `Info`, `Warning`, or `Error`. |
| `CustomState` | In-process state; not serialized by the ASP.NET Core adapter. |

A rule can carry all of them:

```csharp
builder.RuleFor(x => x.Email, nameof(Customer.Email))
       .NotEmpty()
       .EmailAddress()
       .WithMessage("Enter a valid email address.")
       .WithCode("EMAIL_INVALID")
       .WithSeverity(ValidationErrorSeverity.Warning)
       .WithState(new { source = "signup" });
```

`WithName` / `OverridePropertyName` replaces the reported `MemberName` (and therefore the default
`{PropertyName}`). `ValidationOptions.DisplayNameResolver` changes only `{PropertyName}`; it keeps
`MemberName` code-facing. A rule-level override wins over the resolver.

## ASP.NET Core minimal APIs

```csharp
builder.Services.AddValidationEndpoints(options =>
{
    options.IncludeErrorCodes = false;
    options.IncludeSeverities = false;
    options.Title = "Request validation failed";
});
```

`AddRequestValidation<T>()` resolves `IValidator<T>`, runs the async path, and returns a 400
ProblemDetails response with errors grouped by member. `AddValidationEndpoints()` also registers an
exception handler, so `ValidateAndThrow` in a handler produces the same 400 shape.

## MVC

```csharp
builder.Services.AddControllers();
builder.Services.AddMvcValidation();
```

The action filter writes errors into `ModelState` and returns a 400 validation problem. Use
`[SkipJeninnetValidation]` to opt out and `[JeninnetValidationRuleSets("Admin")]` to select rule
sets for an action.

## MediatR

```csharp
builder.Services.AddValidationPipeline(); // Throw (default)
```

Invalid requests throw `ValidationException` with every aggregated error. With
`MediatRValidationFailureStrategy.Continue`, the pipeline stores the result in
`ValidationResultAccessor` and calls the handler, which can shape its own response.

## DataAnnotations and Blazor

Both adapters are synchronous. Async-only validators throw
`AsyncValidatorInvokedSynchronouslyException`; keep async rules on the ASP.NET Core or MediatR
paths.

- The DataAnnotations bridge joins multiple errors into one `ValidationResult` message.
- The Blazor component maps member paths to `FieldIdentifier` values and replaces the messages on
  each validation.

## Localized messages

`ValidationError.Message` is the rule’s custom message when one was supplied. The localization
pipeline resolves `MessageKey` first and falls back to `Message` only when no template exists.
Resolve it with:

```csharp
var localizer = provider.GetRequiredService<IMessageLocalizer>();

var message = await localizer.LocalizeValidationErrorAsync(
    error,
    CultureInfo.CurrentUICulture);
```

`LocalizeValidationErrorAsync` injects `{PropertyName}` and the rule parameters. See
[Localization](13-localization.md).

## Logging and exposure

- Log the member path, message key, error code, and correlation ID.
- Do not log rejected values or full request bodies.
- Keep error codes and severities off public responses unless the client needs them.
- Do not put secrets or raw input into custom messages.
- Return a stable error shape; clients should not parse human-readable text.

See [Security and data exposure](../04-operations/09-security.md) for the full threat model.

## Testing error responses

```csharp
var result = new CustomerValidator().Validate(invalid);

result.ShouldHaveValidationErrors()
      .WithErrorCount(4)
      .ForMember(nameof(Customer.Email))
      .ContainsRule(ValidationMessageKeys.EMAIL);
```

Add `.WithErrorCode("EMAIL_INVALID")` when the rule defines a code with `WithCode`. For host adapters,
assert the HTTP status, the `errors` keys, and that the response does not contain error codes when
they are disabled.

## Related

- [Rules and messages](03-rules-and-messages.md)
- [ASP.NET Core](08-aspnet-core.md)
- [MediatR](09-mediatr.md)
- [Testing](14-testing.md)
