# Jeninnet.Validation.OpenApi

Applies `Jeninnet.Validation` descriptor constraints to ASP.NET Core OpenAPI
schemas.

## Requirements

- .NET 11.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.OpenApi --version 1.0.0-preview.1
```

## Usage

```csharp
builder.Services.AddSchemaConstraintsFromValidators(new UserValidator());
builder.Services.AddOpenApi(options => options.AddSchemaConstraintsFromValidators());
```

`AddSchemaConstraintsFromValidators(IServiceCollection, params IValidationDescriptorProvider[])`
registers the descriptor registry and the schema transformer for the default
`v1` document. Use the `OpenApiOptions` overload to attach it to a custom
document.

## Mapping

The transformer applies the same mapping as `Jeninnet.Validation.JsonSchema`:
required members, string length/pattern/format, numeric bounds, array sizes,
`enum`, `multipleOf`, and nested collection item constraints. Nested objects
are composed from their child descriptors when those validators are registered
in the same `ValidationSchemaRegistry`. `Enum` rules add an `x-enum-type`
extension.

Member names are resolved through the active `JsonSerializerOptions` and
`JsonTypeInfo`, including `camelCase`, `snake_case`, `kebab-case`, `PascalCase`,
and `[JsonPropertyName]`. The mapping is cached per type metadata. Rules without
a direct OpenAPI equivalent remain available through the descriptor and are not
silently dropped from validation.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
