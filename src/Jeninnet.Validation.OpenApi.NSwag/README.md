# Jeninnet.Validation.OpenApi.NSwag

NSwag/NJsonSchema schema processor that turns `Jeninnet.Validation` validators into OpenAPI
constraints (`required`, `format`, `pattern`, `minimum`/`maximum`, `minLength`/`maxLength`,
`minItems`/`maxItems`, `enum`, and the `x-enum-type` extension).

## Requirements

- .NET 10.0 or later (the package ships `lib/net10.0` and `lib/net11.0` assets).
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.OpenApi.NSwag --version 1.0.0-preview.3
```

## Use

```csharp
using Jeninnet.Validation.OpenApi.NSwag;

builder.Services.AddOpenApiDocument(settings =>
{
    settings.AddSchemaConstraintsFromValidators(new CreateOrderValidator());
});
```

The processor matches validator descriptors to generated schemas by member name, honoring
`[JsonPropertyName]` and the application's JSON naming policy (camel, snake, kebab). It only
adds constraints; values already present in the NJsonSchema schema are left untouched.

Nested models are covered when the nested type has its own validator, because NJsonSchema
generates a schema definition for each type and the processor runs once per definition. Rules
that cannot be expressed in OpenAPI (conditional rules, async rules, custom predicates) remain
server-side only.

## AOT position

**Reflection-dependent.** See the [25-package AOT matrix](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/04-operations/02-aot-and-trimming.md#package-matrix).

## Related packages

- [`Jeninnet.Validation.OpenApi`](https://www.nuget.org/packages/Jeninnet.Validation.OpenApi) for
  `Microsoft.AspNetCore.OpenApi`.
- [`Jeninnet.Validation.OpenApi.Swashbuckle`](https://www.nuget.org/packages/Jeninnet.Validation.OpenApi.Swashbuckle)
  for Swashbuckle.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
