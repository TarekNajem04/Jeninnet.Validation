# Package Map

| Package | Add it when |
|---|---|
| `Jeninnet.Validation` | You need the engine and fluent API. |
| `Jeninnet.Validation.Abstractions` | A library only needs contracts and value types. |
| `Jeninnet.Validation.Analyzers` | You use attribute authoring. |
| `Jeninnet.Validation.Assertions` | You write tests for validators. |
| `Jeninnet.Validation.Localization` | You want the built-in message packs for 40 cultures. |
| `Jeninnet.Validation.Diagnostics` | You want OpenTelemetry-compatible validation traces/metrics; ProblemDetails enrichment is configured by AspNetCore. |
| `Jeninnet.Validation.Rules.Domain` | You need optional IBAN, VAT, postal-code, national-ID, password-strength, UUID, or ULID rules. |
| `Jeninnet.Validation.Localization.AspNetCore` | You want resx/`IStringLocalizer` messages or explicit culture control in ASP.NET Core. |
| `Jeninnet.Validation.AspNetCore` | Minimal API or MVC validation. |
| `Jeninnet.Validation.Extensions.Validation` | Microsoft.Extensions.Validation bridge for `AddValidation()` hosts. |
| `Jeninnet.Validation.JsonSchema` | JSON Schema generation. |
| `Jeninnet.Validation.OpenApi` | OpenAPI schema constraints. |
| `Jeninnet.Validation.OpenApi.Swashbuckle` | OpenAPI constraints for Swashbuckle. |
| `Jeninnet.Validation.OpenApi.NSwag` | OpenAPI constraints for NSwag/NJsonSchema. |
| `Jeninnet.Validation.ClientRules` | Client-side rule export. |
| `Jeninnet.Validation.Migration` | `jeninnet` migration assistant (dotnet tool). |
| `Jeninnet.Validation.MediatR` | MediatR request validation. |
| `Jeninnet.Validation.MediatR.ErrorOr` | MediatR failures as `ErrorOr<T>`. |
| `Jeninnet.Validation.MediatR.FluentResults` | MediatR failures as `FluentResults.Result<T>`. |
| `Jeninnet.Validation.MediatR.CSharpFunctionalExtensions` | MediatR failures as `CSharpFunctionalExtensions.Result<T>`. |
| `Jeninnet.Validation.DataAnnotations` | Existing DataAnnotations pipelines. |
| `Jeninnet.Validation.Blazor` | Blazor `EditContext` forms. |
| `Jeninnet.Validation.Observable` | WPF/MAUI/Avalonia `INotifyDataErrorInfo` view models. |
| `Jeninnet.Validation.GraphQL` | HotChocolate GraphQL input validation. |
| `Jeninnet.Validation.Grpc` | gRPC request validation with status details. |

## Typical combinations

**Web API**

```text
Jeninnet.Validation
Jeninnet.Validation.AspNetCore
Jeninnet.Validation.Localization
Jeninnet.Validation.Localization.AspNetCore   # when messages live in resx
```

**CQRS service**

```text
Jeninnet.Validation
Jeninnet.Validation.MediatR
Jeninnet.Validation.MediatR.ErrorOr       # or .FluentResults / .CSharpFunctionalExtensions
Jeninnet.Validation.Localization
```

**API plus generated clients**

```text
Jeninnet.Validation
Jeninnet.Validation.AspNetCore
Jeninnet.Validation.JsonSchema
Jeninnet.Validation.OpenApi
Jeninnet.Validation.ClientRules
```

**GraphQL or gRPC**

```text
Jeninnet.Validation
Jeninnet.Validation.GraphQL    # HotChocolate
Jeninnet.Validation.Grpc       # gRPC server
Jeninnet.Validation.Localization
```

**Desktop/Blazor**

```text
Jeninnet.Validation
Jeninnet.Validation.Blazor        # Blazor EditContext forms
Jeninnet.Validation.Observable    # WPF/MAUI/Avalonia view models
Jeninnet.Validation.Localization
```

## Target frameworks

- `net11.0`: the core runtime and all integration packages.
- `netstandard2.0`: `Jeninnet.Validation.Analyzers`, so it can run in Roslyn hosts.

## Version

The current preview is `1.0.0-preview.1`; all packages share the central version.
