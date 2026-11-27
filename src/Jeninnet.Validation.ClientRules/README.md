# Jeninnet.Validation.ClientRules

Exports **client-side validation rule data** from `Jeninnet.Validation`
descriptors. The payload is deterministic, reflection-free, and AOT-safe. A
companion dependency-free ESM runtime (`client/`) can evaluate the supported
synchronous rule subset; the server remains authoritative, and no C# expression
is translated to JavaScript.

## Requirements

- .NET 10.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.ClientRules --version 1.0.0-preview.3
```

## Usage

```csharp
using Jeninnet.Validation.ClientRules;

var json = ClientRulesWriter.WriteToString([new UserValidator()]);
var typescript = ClientRulesWriter.WriteTypeScript([new UserValidator()]);
var javascript = ClientRulesWriter.WriteJavaScript([new UserValidator()]);
```

The payload:

```json
{
  "schemaVersion": "1.0",
  "validators": [
    {
      "name": "MyApp.Validators.UserValidator",
      "type": "MyApp.User",
      "members": [
        {
          "name": "Email",
          "rules": [
            {
              "rule": "NotEmpty",
              "messageKey": "NotEmpty",
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

## Message strategy

By default the payload carries `messageKey` and rule parameters, **not** a
localized message. The client resolves the key for its own culture at runtime.
Set `ClientRulesOptions.MessageResolver` when a pre-resolved message is required:

```csharp
var options = new ClientRulesOptions {
    MessageResolver = rule => localizer[rule.MessageKey ?? "Invalid"]
};
```

## Allow-listing

```csharp
var options = new ClientRulesOptions();
options.IncludedValidators.Add(typeof(UserValidator).FullName!);
```

An empty allow-list exports every supplied validator. Validators are ordered by
name, members by name, and rules in their configured order, so repeated runs
produce byte-identical payloads.

## Build-time emission

Generated validators can also emit a catalog at compile time. The analyzer
package ships `build/Jeninnet.Validation.Analyzers.props`, which enables the
property and `CompilerVisibleProperty` automatically. Opt out with:

```xml
<PropertyGroup>
  <JeninnetEmitClientRules>false</JeninnetEmitClientRules>
</PropertyGroup>
```

The generator then emits `Jeninnet.Validation.Generated.ClientRulesCatalog` with
`SchemaVersion` and `Json` constants. Serve it directly:

```csharp
app.MapGet("/client-rules", () =>
    Results.Text(ClientRulesCatalog.Json, "application/json"));
```

The build-time catalog covers generated (`[Validate]`) validators. Use the
writer at startup for fluent validators or for a runtime allow-list.

## Client runtime

The optional `client/` package (`@jeninnet/validation-client`) exposes
`createValidator(payload).validate(model)` and `validate(payload, model)`. It
evaluates supported synchronous rules and returns `needsServer` for
conditional, nested, custom, asynchronous, or otherwise unsupported rules.
See [`client/README.md`](../../client/README.md).

## Client/server support matrix

The support list is single-sourced in
[`client/rules-manifest.json`](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/client/rules-manifest.json)
and generated into the C# and JavaScript implementations. Supported synchronous rules:
`NotNull`, `NotEmpty`, `StringNotEmpty`, `Length`, `ExactLength`, `Regex`, `Email`, `Url`,
`IPAddress`, `Phone`, `CreditCard`, `Base64String`, `FileExtensions`, `Contains`, `StartsWith`,
`EndsWith`, `Count`, `MinCount`, `MaxCount`, `UniqueItems`, `MultipleOf`, `GreaterThan`,
`GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `InclusiveBetween`, `ExclusiveBetween`,
`Range`, `In`, `NotIn`, `ScalePrecision`, `After`, `Before`.

Everything else is marked `serverOnly: true` and requires the server. The main groups are:
conditions (`Conditional`); custom/async (`Must`, `MustAsync`, `Custom`, `CustomAsync`);
nested/collection (`Nested`, `NestedValidator`, `ChildRules`, `Include`, `ForEach`,
`ForEachElement`); cross-field/dependent (`PropertyEqual` and related rules, `Dependent`); rule
sets/templates (`RuleSet`, `RuleTemplate`); domain/reference-data rules (`Iban`, `VatNumber`,
`PostalCode`, `NationalId`, `PasswordStrength`, `Uuid`, `Ulid`); and unknown/future rules. The full
reasoned matrix and parity fixtures are under
[`scripts/client/README.md`](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/scripts/client/README.md).

## Limits

- Nested/child validators and custom/async rules are marked `serverOnly: true`;
  the client runtime does not execute them.
- Rules added through `AddCustomValidators()` are not part of the generated
  catalog.
- The server remains the source of truth; client messages are early feedback.

## AOT position

**AOT-compatible.** See the [25-package AOT matrix](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/04-operations/02-aot-and-trimming.md#package-matrix).

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
