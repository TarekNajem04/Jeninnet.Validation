# Installation

## Requirements

- .NET 11 SDK (`11.0.100`) and C# 15.
- The first release targets .NET 11 (`net11.0`).
- Any host: console, ASP.NET Core, worker, Blazor, MAUI, WPF, or Avalonia.

## Packages

| Package | When to add it |
|---|---|
| `Jeninnet.Validation` | Always; core engine and fluent API. |
| `Jeninnet.Validation.Abstractions` | When a library only needs contracts (`IValidator<T>`, `ValidationResult`). |
| `Jeninnet.Validation.Analyzers` | When you use attribute authoring (`[Validate]`). |
| `Jeninnet.Validation.Localization` | When you want the built-in message packs for 40 cultures and the localization pipeline. `Jeninnet.Validation` itself has no localization dependency. |
| `Jeninnet.Validation.AspNetCore` | Minimal API or MVC integration. |
| `Jeninnet.Validation.JsonSchema` | JSON Schema generation. |
| `Jeninnet.Validation.OpenApi` | OpenAPI schema constraints. |
| `Jeninnet.Validation.ClientRules` | Client-side rule export. |
| `Jeninnet.Validation.MediatR` | MediatR pipeline validation. |
| `Jeninnet.Validation.DataAnnotations` | DataAnnotations pipeline bridge. |
| `Jeninnet.Validation.Blazor` | Blazor `EditContext` adapter. |
| `Jeninnet.Validation.Assertions` | Test helpers (`ShouldHaveValidationErrors`, `TestValidate`). |
| `Jeninnet.Validation.Extensions.Validation` | `Microsoft.Extensions.Validation` bridge for `AddValidation()` hosts. |
| `Jeninnet.Validation.Localization.AspNetCore` | `IStringLocalizer`/resx bridge with explicit culture control. |
| `Jeninnet.Validation.OpenApi.Swashbuckle` | OpenAPI constraints for Swashbuckle. |
| `Jeninnet.Validation.OpenApi.NSwag` | OpenAPI constraints for NSwag/NJsonSchema. |
| `Jeninnet.Validation.Rules.Domain` | IBAN, VAT, postal-code, national-ID, password-strength, UUID, and ULID rules. |
| `Jeninnet.Validation.Diagnostics` | OpenTelemetry-compatible validation tracing and metrics. |
| `Jeninnet.Validation.Observable` | `INotifyDataErrorInfo` adapter for WPF/MAUI/Avalonia. |
| `Jeninnet.Validation.GraphQL` | HotChocolate GraphQL input validation. |
| `Jeninnet.Validation.Grpc` | gRPC request validation with status details. |
| `Jeninnet.Validation.MediatR.ErrorOr` | MediatR failures as `ErrorOr<T>`. |
| `Jeninnet.Validation.MediatR.FluentResults` | MediatR failures as `FluentResults.Result<T>`. |
| `Jeninnet.Validation.MediatR.CSharpFunctionalExtensions` | MediatR failures as `CSharpFunctionalExtensions.Result<T>`. |
| `Jeninnet.Validation.Migration` | `jeninnet` migration assistant (dotnet tool). |

The current preview version is `1.0.0-preview.1`.

```bash
dotnet add package Jeninnet.Validation --version 1.0.0-preview.1
dotnet add package Jeninnet.Validation.Analyzers --version 1.0.0-preview.1
```

## Create a project from the template

The repository ships a `dotnet new` template with a sample model and validator:

```bash
dotnet new install ./templates/jeninnet-validator
dotnet new jeninnet-validator -n MyCompany.Validation -o src/MyCompany.Validation
```

The generated class library targets .NET 10 by default (it runs unchanged on .NET 11) and
references `Jeninnet.Validation` `1.0.0-preview.1`. Replace the sample `Customer` rules with your
own.

## Register validators

```csharp
using Jeninnet.Validation.Catalog;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// One call: the generated module initializer fills the catalog with typeof pairs, so no
// reflection or assembly scanning is needed.
services.AddValidation();
```

`AddValidation()` reads the central generated-validator catalog. An assembly contributes to the
catalog when it loads; if a validator assembly has not loaded yet, reference a type from it first
or register that assembly explicitly. When `Microsoft.Extensions.Validation` is also imported,
qualify the call (`Jeninnet.Validation.Extensions.ValidationServiceCollectionExtensions.AddValidation`)
to avoid the name collision.

Explicit registration remains available for fluent validators:

```csharp
services.AddScoped<CustomerValidator>();
services.AddScoped<IValidator<Customer>, CustomerValidator>();
```

Assembly scanning remains as a fallback:

```csharp
services.AddGeneratedValidators(typeof(CustomerValidator).Assembly);
```

`AddGeneratedValidators()` reads the catalog first and only falls back to reflection when the
catalog is empty; it is annotated `[RequiresUnreferencedCode]`. Trimmed or Native AOT applications
should use `AddValidation()` or explicit registration.

## Enable the source generator

Reference the analyzer project/package:

```xml
<PackageReference Include="Jeninnet.Validation.Analyzers" Version="1.0.0-preview.1"
                  PrivateAssets="all" />
```

Then annotate a model:

```csharp
using Jeninnet.Validation.Attributes;

[Validate]
public sealed class Customer
{
    [NotEmpty]
    public string Name { get; init; } = string.Empty;
}
```

The generator emits `CustomerValidator` in the model’s `Validators` namespace. `[Validate]` accepts an
optional target namespace; without it, the namespace is `<model namespace>.Validators`.

## Verify

```bash
dotnet build
```

The build should produce zero warnings and zero errors. If the generator is active, the generated
validator appears under `obj/.../generated/`.

## Next

- [Quick start](03-quick-start.md)
