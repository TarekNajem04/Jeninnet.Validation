# Jeninnet.Validation.OpenApi.Swashbuckle

Swashbuckle schema filter that turns `Jeninnet.Validation` validators into OpenAPI constraints
(`required`, `format`, `pattern`, `minimum`/`maximum`, `minLength`/`maxLength`,
`minItems`/`maxItems`, `enum`, and the `x-enum-type` extension).

## Requirements

- .NET 10.0 or later (the package ships `lib/net10.0` and `lib/net11.0` assets).
- C# 15 when consuming the source contracts.
- Uses Swashbuckle 10.3.1 on net10 and 11.0.1 on net11.

## Installation

```bash
dotnet add package Jeninnet.Validation.OpenApi.Swashbuckle --version 1.0.0-preview.2
```

## Use

```csharp
using Jeninnet.Validation.OpenApi.Swashbuckle;

builder.Services.AddSwaggerGen(options =>
{
    options.AddSchemaConstraintsFromValidators(new CreateOrderValidator());
});
```

The filter matches validator descriptors to generated schemas by member name, honoring
`[JsonPropertyName]` and the application's JSON naming policy (camel, snake, kebab). It only
adds constraints; values already present in the Swashbuckle schema are left untouched.

Nested models are covered when the nested type has its own validator, because Swashbuckle
generates a schema definition for each type and the filter runs once per definition. Rules that
cannot be expressed in OpenAPI (conditional rules, async rules, custom predicates) remain
server-side only.

## Related packages

- [`Jeninnet.Validation.OpenApi`](https://www.nuget.org/packages/Jeninnet.Validation.OpenApi) for
  `Microsoft.AspNetCore.OpenApi`.
- [`Jeninnet.Validation.OpenApi.NSwag`](https://www.nuget.org/packages/Jeninnet.Validation.OpenApi.NSwag)
  for NSwag/NJsonSchema.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
