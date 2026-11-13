# Minimal API Sample

Demonstrates Jeninnet.Validation integration with ASP.NET Core Minimal APIs.

## Highlights

- Request validation using `Validator<T>` via DI
- `Results.ValidationProblem()` for error responses
- `ChildRules` for nested address validation
- `ValidateAndThrow` for exception-based flow
- Self-test endpoint for demonstration
- `/client-rules` exposes the deterministic client-side rule payload

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.MinimalApi
```

Then visit `http://localhost:5000/self-test` to see the validation demo, and
`http://localhost:5000/client-rules` for the client-side rule payload.

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
