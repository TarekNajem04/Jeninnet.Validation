# Jeninnet.Validation.GraphQL

Validates HotChocolate GraphQL input objects before resolvers run. Invalid input adds GraphQL
errors with `extensions.member` and `extensions.code` and skips the resolver.

## Requirements

- .NET 10.0 or later (including .NET 11).
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.GraphQL --version 1.0.0-preview.1
```

## Usage

```csharp
using Jeninnet.Validation.GraphQL;

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddJeninnetGraphQLValidation();

builder.Services.AddScoped<IValidator<CreateOrderInput>, CreateOrderInputValidator>();
```

The field middleware inspects every provided input-object argument, resolves the registered
`IValidator<T>` for its runtime type, runs validators on the asynchronous path with
`RequestAborted`, and aggregates errors.

## Error shape

Each `ValidationError` becomes a GraphQL error:

```json
{
  "message": "Name is required.",
  "extensions": {
    "code": "NAME_REQUIRED",
    "member": "Name",
    "severity": "Error",
    "errorCode": "NAME_REQUIRED"
  }
}
```

`extensions.member` preserves `ValidationError.MemberName`; `extensions.code` uses the error code,
then the message key, then the message identifier.

## Rule sets

```csharp
[GraphQLValidationRuleSets("strict")]
public sealed class CreateOrderInput { /* ... */ }
```

Without the attribute, `GraphQLValidationOptions.RuleSets` applies. An empty list means all rules.
The validator must implement `IValidationOptionsValidator<T>` when rule sets are selected; all
built-in and generated validators do.

## Scope

- Works with HotChocolate field middleware and input-object arguments.
- Subscriptions are not specially handled; the same field middleware applies to subscription
  fields when they run.
- The package is optional; Core and Abstractions have no HotChocolate dependency.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
