# Jeninnet.Validation.Abstractions

Core value types and interfaces for the Jeninnet validation framework. This library contains the fundamental building blocks consumed by all other validation components.

## Requirements

- .NET 10.0 or later (including .NET 11).
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.Abstractions --version 1.0.0-preview.1
```

## Types

| Type | Description |
|------|-------------|
| `ValidationResult` | Immutable struct representing the outcome of a validation operation; exposes `IsValid` and `Errors`. |
| `ValidationError` | Record struct describing a single validation failure — member name, `MessageId`, severity, message parameters, error code, and custom state. |
| `ValidationErrorCollector` | Mutable collector implementing `IReadOnlyList<ValidationError>`; the central accumulation point for errors during rule evaluation. |
| `ValidationException` | Exception thrown when validation fails; carries the full list of `ValidationError`s. |
| `MessageId` | Strongly typed identifier for validation messages. |
| `ValidationErrorSeverity` | Enum: `Info`, `Warning`, `Error`. |
| `CascadeMode` | Enum: `Continue` (evaluate all rules) or `Stop` (halt on first failure). |
| `ValidationMessageKeys` | Static class providing stable message-key constants for all built-in rule types. |

## Interfaces

| Interface | Description |
|-----------|-------------|
| `IValidator<T>` | Combined synchronous and asynchronous validator contract (inherits both below). |
| `ISyncValidator<T>` | `Validate(T)` and `Validate(T, ruleSets)`. |
| `IAsyncValidator<T>` | `ValidateAsync(T, CancellationToken)` and `ValidateAsync(T, ruleSets, CancellationToken)`. |
| `IGeneratedValidator` | Marker interface emitted by the source generator on auto-generated validators. |
| `IRuleDefinition` / `IPropertyRuleDefinition` | Stable metadata contracts used by the source generator and custom rule implementations. |

## Dependencies

Zero external dependencies.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
