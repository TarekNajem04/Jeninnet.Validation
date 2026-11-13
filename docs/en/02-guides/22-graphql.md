# HotChocolate GraphQL

The `Jeninnet.Validation.GraphQL` package validates GraphQL input objects before resolvers run.
It uses HotChocolate field middleware, so it works without changing individual input types.

## Setup

```csharp
using Jeninnet.Validation.GraphQL;

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddJeninnetGraphQLValidation();

builder.Services.AddScoped<IValidator<CreateOrderInput>, CreateOrderInputValidator>();
```

The middleware inspects every provided input-object argument, resolves the registered
`IValidator<T>` for the runtime type, runs validation on the asynchronous path with
`RequestAborted`, and aggregates errors.

## Input and resolver

```csharp
public sealed class CreateOrderInput
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}

public sealed class Mutation
{
    public string CreateOrder(CreateOrderInput input) => input.Name ?? string.Empty;
}
```

## Error shape

Each validation error becomes a GraphQL error:

```json
{
  "errors": [
    {
      "message": "Name is required.",
      "extensions": {
        "code": "NAME_REQUIRED",
        "member": "Name",
        "severity": "Error",
        "errorCode": "NAME_REQUIRED"
      }
    }
  ]
}
```

The resolver is skipped when validation fails. `extensions.member` preserves
`ValidationError.MemberName`; `extensions.code` uses the error code, then the message key, then the
message identifier.

## Rule sets

```csharp
[GraphQLValidationRuleSets("strict")]
public sealed class CreateOrderInput { /* ... */ }
```

Without the attribute, `GraphQLValidationOptions.RuleSets` applies. An empty list means all rules.
The validator must implement `IValidationOptionsValidator<T>` when rule sets are selected; all
built-in and generated validators do.

## Limits

- Field middleware adds a small per-field overhead; input validation is opt-in per registered
  validator.
- Subscriptions are not specially handled; the same field middleware applies when they run.
- The package is optional; Core and Abstractions have no HotChocolate dependency.

## Verify

Send a mutation with invalid input and confirm the response contains member/code extensions and the
resolver did not run. Send valid input and confirm the resolver result is returned without errors.

## Related

- [ASP.NET Core](08-aspnet-core.md)
- [Integration packages](../03-reference/05-integration-packages.md)
