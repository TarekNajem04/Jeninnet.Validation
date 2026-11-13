# JSON Schema, OpenAPI, and Client Rules

These packages consume the frozen validator descriptor; they never execute validation.

## Descriptors

```csharp
var descriptor = new CreateOrderValidator().Describe();
```

`Describe()` is available on fluent validators and generated validators. It exposes members, rule
kinds, message keys, severities, conditions, and parameters. See [Metadata](../03-reference/04-metadata.md).

## JSON Schema

```csharp
using Jeninnet.Validation.JsonSchema;

var schema = ValidationSchemaWriter.WriteToString(descriptor);
```

The writer emits deterministic JSON Schema (draft 2020-12): `required`, string length/pattern/format,
numeric bounds, array sizes, `enum`, `multipleOf`, and advisory `x-validation-rules` for rules
without a direct mapping.

## Rule-to-schema mapping

| Rule | JSON Schema output |
|---|---|
| `NotNull`, `NotEmpty` | `required`; `NotEmpty` also sets `minLength: 1` or `minItems: 1` when the type is known. |
| `Length(min, max)` | `minLength` / `maxLength`. |
| `ExactLength(n)` | `minLength = maxLength = n`. |
| `Matches(pattern)` | `pattern`. |
| `Contains`, `StartsWith`, `EndsWith` | Escaped `pattern` with the appropriate anchor. |
| `EmailAddress` | `format: "email"`. |
| `Url` | `format: "uri"`. |
| `IPAddress` | `anyOf` with `format: "ipv4"` and `format: "ipv6"`. |
| `Base64String` | `contentEncoding: "base64"`. |
| `FileExtensions` | `x-file-extensions` (advisory, not a JSON Schema keyword). |
| `Iban`, `VatNumber`, `PostalCode`, `NationalId` | `pattern` from the explicit country; checksums stay server-side. |
| `PasswordStrength` | `minLength` from the policy. |
| `Uuid` | `format: "uuid"`. |
| `Ulid` | `pattern` for 26 Crockford Base32 characters. |
| `Enum` | `x-enum-type` carrying the CLR enum name; values come from the generated model schema. |
| `Nested` | `type: object` plus `$ref` into `$defs`. |
| `ForEach` | `type: array` plus `items` and a `$ref` to the element schema. |
| `Count`, `MinCount`, `MaxCount` | `minItems` / `maxItems`. |
| `GreaterThan`, `GreaterThanOrEqual` | `exclusiveMinimum` / `minimum`. |
| `LessThan`, `LessThanOrEqual` | `exclusiveMaximum` / `maximum`. |
| `InclusiveBetween`, `Range` | `minimum` / `maximum`. |
| `ExclusiveBetween` | `exclusiveMinimum` / `exclusiveMaximum`. |
| `In` | `enum`. |
| `ScalePrecision` | `multipleOf: 10^-scale`. |
| `After`, `Before` | `format: "date-time"`. |
| `Must`, `Custom`, conditional | `x-validation-rules` advisory entries; conditional entries carry `hasCondition: true`. |

Nested validators produce `$defs` and `$ref`; collection child validators use `items` with the same
composition. Recursive descriptors are guarded and reuse a single definition.

`required` is a presence constraint: it does not encode “non-null” for value types. A nullable
reference type with `[NotNull]` is required, but a non-nullable `int` cannot express “not zero” as
`required`; use a comparison rule for that. The writer therefore omits `required` for non-nullable
value-type members even when `NotNull`/`NotEmpty` is present.

```csharp
var options = new ValidationSchemaOptions
{
    IncludeAdvisoryRules = false
};
```

The package is standalone: no ASP.NET dependency, AOT-safe, and deterministic ordering.

## OpenAPI

```csharp
using Jeninnet.Validation.OpenApi;

builder.Services.AddSchemaConstraintsFromValidators(new CreateOrderValidator());
builder.Services.AddOpenApi(options =>
    options.AddSchemaConstraintsFromValidators());
```

The transformer applies member constraints and required members to OpenAPI schemas. It resolves
descriptor member names through the active `JsonSerializerOptions` and `JsonTypeInfo`, so
`camelCase`, `snake_case`, `kebab-case`, `PascalCase`, and `[JsonPropertyName]` overrides are
supported. A member that cannot be mapped falls back to case-insensitive matching. This name
translation is limited to schema emission; runtime validation errors keep the CLR `MemberName`.

## Client rules

```csharp
using Jeninnet.Validation.ClientRules;

var json = ClientRulesWriter.WriteToString([new CreateOrderValidator()]);
var typescript = ClientRulesWriter.WriteTypeScript([new CreateOrderValidator()]);
var javascript = ClientRulesWriter.WriteJavaScript([new CreateOrderValidator()]);
```

Payload shape:

```json
{
  "schemaVersion": "1.0",
  "validators": [
    {
      "name": "MyApp.Validators.CreateOrderValidator",
      "type": "MyApp.CreateOrder",
      "members": [
        {
          "name": "Email",
          "rules": [
            {
              "rule": "Email",
              "messageKey": "Email",
              "severity": "Error",
              "hasCondition": false,
              "params": {}
            }
          ]
        }
      ]
    }
  ]
}
```

- Default export carries `messageKey` and parameters; set `ClientRulesOptions.MessageResolver` to
  add a resolved `message`, and authored messages also carry `messageIsAuthored: true`.
- `IncludedValidators` allow-lists validators.
- TypeScript output declares interfaces and `export const clientRules`; JavaScript output is an ES
  module.
- The analyzer package enables the build-time catalog automatically through its packaged props
  file and makes `JeninnetEmitClientRules` compiler-visible. Opt out with
  `<JeninnetEmitClientRules>false</JeninnetEmitClientRules>`. The generator emits
  `Jeninnet.Validation.Generated.ClientRulesCatalog` (source file `ClientRulesCatalog.g.cs`) with
  `Json` and `SchemaVersion` constants.
- The dependency-free `@jeninnet/validation-client` ESM runtime evaluates the supported
  synchronous rule subset. Unsupported or conditional rules carry `serverOnly: true`, and the
  result exposes `needsServer` for an explicit server round trip.

## Serving the payload

```csharp
app.MapGet("/client-rules", (CreateOrderValidator validator) =>
    Results.Text(ClientRulesWriter.WriteToString([validator]), "application/json"));
```

## Limits

- The client receives data, not an engine; the server remains authoritative.
- Conditions are opaque delegates, so they are exported as `hasCondition` advisory entries rather
  than inferred `if/then` schemas. Add explicit descriptor metadata if a machine-readable condition
  is required.
- Custom rules remain advisory; nested and collection child validators are composed when the child
  validator exposes a descriptor.
- OpenAPI applies nested constraints through the registered child descriptors. Register the nested
  validator (or use the generated registry) so its component schema is transformed too.
- The server’s validation remains the source of truth.

## Related

- [Metadata](../03-reference/04-metadata.md)
- [Integration packages](../03-reference/05-integration-packages.md)
