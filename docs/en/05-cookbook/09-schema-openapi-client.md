# Emit JSON Schema, OpenAPI, and Client Rules

## Problem

You want the API contract and client-side rule data to stay in sync with the validators.

## Solution: descriptor

```csharp
var descriptor = new CreateOrderValidator().Describe();
```

## JSON Schema

```csharp
using Jeninnet.Validation.JsonSchema;

var schema = ValidationSchemaWriter.WriteToString(descriptor);
```

Serve it as a static asset or return it from an endpoint:

```csharp
app.MapGet("/schemas/order", (CreateOrderValidator validator) =>
    Results.Text(ValidationSchemaWriter.WriteToString(validator.Describe()),
        "application/schema+json"));
```

## OpenAPI

```csharp
using Jeninnet.Validation.OpenApi;

builder.Services.AddSchemaConstraintsFromValidators(new CreateOrderValidator());
builder.Services.AddOpenApi(options =>
    options.AddSchemaConstraintsFromValidators());
```

The transformer applies `required`, length, pattern, format, numeric bounds, array sizes, `enum`,
and `multipleOf` to the OpenAPI schemas.

## Client rules

```csharp
using Jeninnet.Validation.ClientRules;

app.MapGet("/client-rules", (CreateOrderValidator validator) =>
    Results.Text(ClientRulesWriter.WriteToString([validator]), "application/json"));
```

TypeScript and JavaScript modules:

```csharp
var typescript = ClientRulesWriter.WriteTypeScript([validator]);
var javascript = ClientRulesWriter.WriteJavaScript([validator]);
```

Build-time catalog:

The analyzer package enables `JeninnetEmitClientRules` and makes it compiler-visible through its
packaged props file. Opt out with `<JeninnetEmitClientRules>false</JeninnetEmitClientRules>`.

Then serve `Jeninnet.Validation.Generated.ClientRulesCatalog.Json` (the `Json` constant on the
generated `ClientRulesCatalog` type).

Client runtime:

```js
import { createValidator } from "@jeninnet/validation-client";

const result = createValidator(await fetch("/client-rules").then(r => r.json()))
    .validate(request);

// result.errors contains client errors; result.needsServer requests server validation.
```

## Verify

- `/schemas/order` contains `required` and the rule constraints.
- `/openapi/v1.json` shows the same constraints on the request schema.
- `/client-rules` contains the validator, members, and rules.
- The TypeScript module compiles with `tsc`.

## Limits

- The client runtime covers the supported synchronous subset; the server remains authoritative.
- Conditions export `hasCondition` and are server-only because they are opaque delegates.
- Nested/custom/async rules are marked `serverOnly: true` and require a server round trip.
- OpenAPI member names follow the active serializer naming policy and `[JsonPropertyName]`.

## Related

- [Schema, OpenAPI, and client rules guide](../02-guides/12-schema-openapi-client.md)
- [Metadata](../03-reference/04-metadata.md)
