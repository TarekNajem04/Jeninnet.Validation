# Jeninnet.Validation

High-performance, source-generated validation for .NET 10+ and .NET 11.

Jeninnet.Validation combines attribute-driven rule declaration with Roslyn source generation
and a fully typed fluent API. Validators are generated at compile time, so the validation hot
path avoids reflection, boxing, and runtime expression compilation. The runtime is suitable for
high-throughput services, trimming, and Native AOT.

## Installation

```bash
dotnet add package Jeninnet.Validation --version 1.0.0-preview.1
```

Attribute-driven validation also requires the analyzer package:

```bash
dotnet add package Jeninnet.Validation.Analyzers --version 1.0.0-preview.1
```

The analyzer package is a build-time dependency. It is applied to the consuming project
automatically; reference it with `PrivateAssets="all"` when you manage package assets explicitly.

## Quick start: attributes

```csharp
using Jeninnet.Validation.Attributes;

namespace MyApp;

[Validate]
public partial class User
{
    [NotEmpty]
    [Length(2, 50)]
    public string? Name { get; set; }

    [Email]
    public string? Email { get; set; }

    [InclusiveBetween(18, 120)]
    public int Age { get; set; }
}
```

The source generator emits a validator for the model. Register it through DI and resolve the
typed validator:

```csharp
using Jeninnet.Validation.Abstractions;
using Jeninnet.Validation.Catalog;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddValidation();

await using var provider = services.BuildServiceProvider();
var validator = provider.GetRequiredService<IValidator<User>>();

var result = validator.Validate(new User { Name = "", Email = "invalid", Age = 10 });

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.MemberName}: {error.MessageKey ?? error.Message}");
    }
}
```

For attribute authoring without DI, instantiate the generated validator directly. The generated
type lives in the model namespace plus `.Validators` and has the same name as the model with a
`Validator` suffix.

## Quick start: fluent API

```csharp
using Jeninnet.Validation;
using Jeninnet.Validation.Fluent;

public sealed class UserValidator : Validator<User>
{
    protected override void Configure(ValidatorRuleBuilder<User> builder)
    {
        builder.RuleFor(static user => user.Name)
               .NotEmpty()
               .Length(2, 50);

        builder.RuleFor(static user => user.Email)
               .EmailAddress();
    }
}

var result = new UserValidator().Validate(user);
```

Both authoring paths produce the same `IValidator<T>` contract, error model, rule metadata,
JSON Schema, OpenAPI constraints, and client-rule payloads.

## Key capabilities

- Compile-time generated validators with no runtime reflection.
- Handwritten-quality performance and zero allocations on the valid path.
- Rich `ValidationError` model with message keys, severities, error codes, and custom state.
- Rule sets, cascade modes, per-rule conditions, dependent rules, and fail-fast execution.
- Nested objects, collections, polymorphic validators, and element-level filters.
- Localization through the sibling Jeninnet.Localization pipeline, including 40 built-in cultures.
- ASP.NET Core, MVC, Blazor, MediatR, DataAnnotations, GraphQL, gRPC, and OpenAPI integrations.
- JSON Schema, client-rule export, and a dependency-free browser evaluator.

## Packages

The repository ships modular packages. Install only the integrations you need:

| Package | Purpose |
|---|---|
| `Jeninnet.Validation` | Core engine, fluent API, attributes, and rules. |
| `Jeninnet.Validation.Abstractions` | Public contracts and value types. |
| `Jeninnet.Validation.Analyzers` | Source generator and compile-time misuse analyzers. |
| `Jeninnet.Validation.Assertions` | Stack-agnostic test assertions. |
| `Jeninnet.Validation.Localization` | Built-in message packs for 40 cultures. |
| `Jeninnet.Validation.AspNetCore` | Minimal API and MVC validation integration. |
| `Jeninnet.Validation.Blazor` | `EditContext` and static SSR integration. |
| `Jeninnet.Validation.MediatR` | MediatR validation pipeline and result adapters. |
| `Jeninnet.Validation.JsonSchema` | JSON Schema generation from validators. |
| `Jeninnet.Validation.OpenApi` | OpenAPI schema constraints from validators. |
| `Jeninnet.Validation.ClientRules` | JSON, TypeScript, and JavaScript client-rule export. |

See the [package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md)
for the complete list.

## Requirements

- .NET 10.0 or later (including .NET 11).
- C# 15 or later.
- A Roslyn-based toolchain for source generation and analyzers.

## Documentation

- [Getting started](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
- [Why Jeninnet?](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/01-getting-started/00-why-jeninnet.md)
- [Fluent authoring](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/02-guides/01-fluent-authoring.md)
- [Attribute authoring](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/02-guides/02-attribute-authoring.md)
- [Dependency injection](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/02-guides/17-dependency-injection.md)
- [API reference](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/03-reference/08-api-reference.md)

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
