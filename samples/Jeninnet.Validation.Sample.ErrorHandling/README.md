# Error Handling Cookbook Sample

Demonstrates ValidationResult, ValidateAndThrow, CustomState, Severity, ErrorCode, and multiple errors.

## Features

| Feature | Description |
|---------|-------------|
| **ValidationResult** | Inspect IsValid, iterate Errors collection |
| **ValidateAndThrow** | Throws ValidationException on failure |
| **ValidationException** | Catch and inspect all errors |
| **ErrorCode** | Programmatic error identification via `WithCode()` |
| **Severity** | Info, Warning, Error levels via `WithSeverity()` |
| **CustomState** | Attach arbitrary data to errors via `WithState()` |
| **Multiple errors** | All rule violations collected in one result |

## Demonstrations

1. **ValidationResult** — Inspect errors, counts, severity, codes, custom state.
2. **ValidateAndThrow** — Exception-based error handling.
3. **Severity Levels** — Count errors by severity (Info, Warning, Error).
4. **Successful Validation** — No errors, no exception.

## Output

```
=== ERROR HANDLING COOKBOOK SAMPLE ===
...
```

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.ErrorHandling/Jeninnet.Validation.Sample.ErrorHandling.csproj
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
