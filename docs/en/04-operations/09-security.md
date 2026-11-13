# Security and Data Exposure

Validation is a correctness boundary, not an authorization boundary. A valid request can still be
forbidden, and an invalid request must never reach privileged code.

## Server authority

Client rules and JSON Schema are **advisory data**. They tell a client what the server expects, but
the server must run the validator again. Never treat a client-side pass as authorization or as proof
that the payload is safe.

## Error responses

`ValidationError` can carry a message, a message key, an error code, parameters, and custom state.
Treat all of it as data that may reach an untrusted caller.

- Keep `ValidationProblemOptions.IncludeErrorCodes` and `IncludeSeverities` at their defaults
  (`false`) unless the client needs them.
- Do not interpolate secrets, connection strings, internal identifiers, SQL, or raw user input into
  custom messages.
- Prefer stable message keys over free-form messages when the response is public.
- Remember that `ValidationError.CustomState` is not serialized by the ASP.NET Core adapter, but it
  is visible to in-process consumers.
- Return the same shape for “not found” and “not authorized” where the distinction would leak
  information.

```csharp
builder.Services.AddValidationEndpoints(options =>
{
    options.IncludeErrorCodes = false;
    options.IncludeSeverities = false;
    options.Title = "Request validation failed";
});
```

## Client rules

`ClientRulesWriter` exports validator names, member names, rule kinds, message keys, and parameters.
That is useful for forms and dangerous for internal models.

- Use `ClientRulesOptions.IncludedValidators` as an allow-list; export only public request models.
- Do not export validators for admin, billing, or internal-only endpoints.
- Conditions are exported as `hasCondition`, not as predicates; do not assume the client can
  reproduce server logic.
- If you use `MessageResolver` to pre-resolve messages, confirm the text contains no internal data.
- Version the payload with `SchemaVersion` and reject unknown versions on the client.

```csharp
var options = new ClientRulesOptions
{
    SchemaVersion = "1.0"
};

options.IncludedValidators.Add(typeof(CreateOrderValidator).FullName!);

var json = ClientRulesWriter.WriteToString([new CreateOrderValidator()], options);
```

## Schema and OpenAPI

JSON Schema and OpenAPI expose the shape and constraints of your contracts.

- Generate schemas only for public DTOs.
- Do not publish internal endpoints or admin-only models in the public OpenAPI document.
- Treat `x-validation-rules` and pattern constraints as public information.
- Keep custom naming policies aligned with the transformer; a mismatch can silently omit
  constraints.

## Regular expressions and custom predicates

- `Matches(pattern)` compiles the pattern and applies a **one-second timeout**. A timeout
  propagates as `RegexMatchTimeoutException`; keep patterns bounded and handle it at the boundary if
  the input is untrusted.
- `Matches(Regex)` uses the `Regex` instance you supply. If you pass a custom instance, supply a
  timeout and avoid catastrophic backtracking.
- `Must`, `MustAsync`, `Custom`, and `CustomAsync` run arbitrary code. Keep them deterministic,
  bounded, and cancellation-aware. A custom delegate exception becomes a validation failure whose
  message names the exception type; override the message if that type name is sensitive.
- Expensive async rules (database or network calls) should be rate-limited and protected by the
  request cancellation token.

## Multi-tenancy

Validators are stateless after configuration and are safe to reuse across requests. Tenant-specific
state must come from the instance or from `ValidationContext`:

- Use `context.Root` for the root instance.
- Use `context.RootData` for a per-walk cache; it is pooled and not shared across calls.
- Use `context.ServiceProvider` for scoped services supplied by the caller.
- Never capture a tenant identifier, `HttpContext`, or a scoped service in a singleton validator.

## Logging

- Log the member path, message key, error code, and a correlation ID.
- Do not log the rejected value, the full request body, or message parameters that may contain PII.
- Separate 400 (validation) from 500 (server fault) metrics.
- Alert on `AsyncValidatorInvokedSynchronouslyException`; it means an async validator reached a sync
  adapter.

## Supply chain

- Pin package versions and review preview upgrades.
- Keep all Jeninnet.Validation packages on the same version.
- Restore from the official NuGet feed; verify the package owner and signature if your policy
  requires it.
- Run the AOT/trimming publish in CI for deployments that depend on it.

## Related

- [Production checklist](08-production-checklist.md)
- [ASP.NET Core](../02-guides/08-aspnet-core.md)
- [JSON Schema, OpenAPI, and client rules](../02-guides/12-schema-openapi-client.md)
- [Known limitations](../03-reference/07-known-limitations.md)
