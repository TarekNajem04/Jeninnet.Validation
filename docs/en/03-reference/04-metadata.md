# Metadata

`Describe()` returns a frozen description of a validator’s members and rules. It is the source for
JSON Schema, OpenAPI, and client-rule emission.

## Contract

| Type | Purpose |
|---|---|
| `IValidationDescriptorProvider` | `Describe()` returns the descriptor. |
| `IValidationDescriptor` | `ValidatorName`, `ValidatedType`, `Members`, `RulesForMember`. |
| `IValidationMemberDescriptor` | `Name`, `Rules`. |
| `IValidationRuleDescriptor` | `Kind`, `MessageKey`, `Severity`, `ErrorCode`, `DisplayName`, `CustomState`, `HasCondition`, `Parameters`. |

```csharp
var descriptor = new CustomerValidator().Describe();

foreach (var member in descriptor.Members)
{
    foreach (var rule in member.Rules)
    {
        Console.WriteLine($"{member.Name}: {rule.Kind} ({rule.MessageKey})");
    }
}
```

## Fluent validators

`Validator<T>` builds the descriptor once per instance and caches it. `ValidatedType` is `T`.
`RulesForMember` returns an empty list for unknown members.

## Generated validators

The generated validator builds its descriptor from the **actual runtime rule lists** on the first
`Describe()` call and caches it per instance. Rules added through `AddCustomValidators()` are
therefore included; a descriptor is never a stale copy of the attribute declaration.

Rules that expose metadata (all built-in rules and custom rules derived from `SyncValidateRule` or
`AsyncValidateRule`) describe themselves. A rule that cannot describe itself is recorded as a
`Custom` advisory entry instead of being dropped:

| Parameter | Meaning |
|---|---|
| `Reason` | `Rule has no descriptor metadata.` |
| `Type` | The runtime rule type name. |

Use the generated `ConfigureDescriptor` partial hook when a custom rule needs explicit metadata:

```csharp
public sealed partial class CustomerValidator
{
    partial void ConfigureDescriptor(ValidationDescriptorBuilder builder)
    {
        builder.Member("Name").Rule(
            kind: "ReservedName",
            messageKey: "ReservedName",
            severity: ValidationErrorSeverity.Warning,
            parameters: new Dictionary<string, object?> { ["Value"] = "admin" },
            errorCode: "reserved_name",
            displayName: "Customer name");
    }
}
```

The hook adds data only; it never exposes delegates or executes predicates. Calls return a frozen
descriptor with deterministic member and rule order.

## Wrappers

The builder flattens composition wrappers so the descriptor contains the rules that actually run:

- conditional rules keep their inner rule kind and set `HasCondition = true`;
- rule-set groups keep their inner rules and add `Parameters["RuleSets"]`;
- dependent blocks keep the parent rule first, then the dependent rules, and add
  `Parameters["Dependent"] = true`;
- `Nested` and `ForEach` rules retain a `NestedDescriptor` when the child validator exposes one, so
  schema writers can compose `$defs`/`$ref` and `items` recursively.

## Kinds and parameters

`Kind` is the rule type name without the `Rule` suffix, normalized for aliases
(`StringNotEmpty` → `NotEmpty`, `RegexMatch` → `Regex`, min/max length → `Length`). A rule template
reports its template name as the kind instead of the internal rule type name. Parameters use
stable names (`Min`, `Max`, `From`, `To`, `Length`, `ExactLength`, `Pattern`, `Threshold`,
`ComparisonValue`, `ComparisonProperty`, `Other`, `Values`, `Extensions`, `Precision`, `Scale`,
`EnumType`, `Country`, `Policy`, `MinLength`, `RuleSets`, `Dependent`) and retain their runtime types:
integers stay integers, booleans stay booleans, and strings stay strings.

`PropertyName`, `PropertyValue`, `PropertyPath`, and `CollectionIndex` are runtime error parameters,
not descriptor parameters: they depend on the instance or element. Every emitted error includes
`PropertyName`, `PropertyValue`, and `PropertyPath`; element rules add `CollectionIndex`. See the
[rule reference](02-rule-reference.md#message-parameters) for the complete mapping.

Declarative metadata (`ErrorCode`, `DisplayName`, `CustomState`) is copied into the descriptor.
Client rules serialize non-null values as `errorCode`, `displayName`, and `customState`; JSON Schema
advisory entries use the same names, plus `severity` when it is not `Error`.

## Limits

- No runtime reflection: fluent metadata comes from an internal interface; generated metadata is
  built from rule instances without reflecting over delegates.
- Delegates and predicates are not exposed. Template predicates stay hidden; only parameter names
  and static values appear in the descriptor (dynamic values stay `null` until a model fails).
  Static custom state is exposed; object-valued fluent
  state is serialized by the client/schema writers through their normal value conversion.
- The build-time `ClientRulesCatalog` is generated from attributes only. The runtime
  `ClientRulesWriter` and JSON Schema writer use `Describe()` and therefore include custom and
  configured descriptor entries.
- Nested/child validators compose in JSON Schema when the child exposes a descriptor. `ClientRules`
  remains data-only and exports nested rules as advisory entries.

## Related

- [JSON Schema, OpenAPI, and client rules](../02-guides/12-schema-openapi-client.md)
- [Architecture](00-architecture.md)
