# Troubleshooting

## Quick map

| Symptom | Likely cause |
|---|---|
| `AsyncValidatorInvokedSynchronouslyException` | Sync `Validate` on an async-only validator. |
| `InvalidOperationException` mentioning `PreValidateAsync` | Async hook configured; call `ValidateAsync`. |
| Validation silently does nothing | Validator not registered as `IValidator<T>`. |
| Rule set ignored | Name mismatch or rules outside the set. |
| Property filter has no effect | Generated rules are flat; property filters target fluent member groups. |
| OpenAPI schema has no constraints | Transformer/document mismatch or naming policy. |
| Blazor message on the wrong field | Unsupported collection shape or null model graph. |
| Localization returns the fallback | Missing pack/key or a second provider. |
| `ArgumentException` from `In`/`NotIn` | Empty allowed/disallowed set. |
| `RegexMatchTimeoutException` | Pattern exceeded the one-second timeout. |

## `AsyncValidatorInvokedSynchronouslyException`

The validator has async rules but no sync rules. Use `ValidateAsync` or add synchronous rules.
`VAL0016` warns at compile time for `[AsyncOnlyValidator]` receivers.

## `InvalidOperationException` from an async pre-validation hook

`PreValidateAsync` makes the synchronous path throw:

```text
An asynchronous PreValidate hook is configured on this validator; call ValidateAsync instead.
```

Call `ValidateAsync`, or move the check into a custom rule that runs on the async path.

## Validation silently does nothing

- No validator is registered for the request type.
- The validator is registered as a concrete type but the host resolves `IValidator<T>`.
- The request type differs from the validator’s `T`.
- The host adapter is not attached (`AddRequestValidation<T>`, `AddMvcValidation`,
  `AddValidationPipeline`).

Register `IValidator<T>` explicitly.

## Rule sets are ignored

- Set names are case-insensitive but must match exactly otherwise.
- When rule sets are named, ungrouped rules do not run unless `default` is included.
- `ruleSets: null` means “all rules”, not “default rules only”.
- `ValidationCallOptions.RuleSets` selects sets for that call; `null` or empty means all rules.
- `ExcludeRuleSets("default")` removes ungrouped rules.

## Property filters have no effect

`IncludeProperties` / `ExcludeProperties` work on fluent and generated validators. Check that the
member name matches exactly (ordinal), that nested members are targeted as a unit, and that the
validator implements `IValidationOptionsValidator<T>`; a validator without that contract throws
`NotSupportedException` instead of silently ignoring the options.

## OpenAPI schema has no constraints

- The transformer is attached to a different document name.
- The request type is not registered in `AddSchemaConstraintsFromValidators`.
- A custom JSON naming policy hides the member name.
- The validator was never described (generated validators need the generator to run).

Register the validator and use the default `v1` document or attach the transformer to the custom
document. See [JSON Schema, OpenAPI, and client rules](../02-guides/12-schema-openapi-client.md).

## Blazor messages appear on the wrong field

Nested paths resolve through the model graph. Confirm the model is not null and the collection is an
array or `IList`; other collection shapes fall back to the root identifier.

## Localization returns the fallback message

- The culture has no pack and no parent/invariant fallback.
- The message key is not present in the pack.
- A second provider was registered; the platform enforces exactly one provider.
- `ValidationError.Message` is null and the caller did not resolve `MessageKey`.

Use `AddDefaultMessagePacks(options => ...)` for overrides instead of a second provider, and resolve
`MessageKey` through `LocalizeValidationErrorAsync`.

## `ArgumentException` from `In` or `NotIn`

Both rules reject an empty value set at construction time. Supply at least one value, or use a
different rule when “anything” is allowed.

## `RegexMatchTimeoutException`

`Matches(pattern)` compiles the regex with a one-second timeout. A timeout propagates to the caller.
Keep patterns bounded, avoid nested quantifiers on untrusted input, and handle the exception at the
boundary if the input is adversarial.

## `ValidationException` with an inner exception

A lifecycle hook threw. `PreValidate`, `PreValidateAsync`, `OnFailure`, and `OnFailureAsync` wrap
non-cancellation exceptions in `ValidationException`. Inspect `InnerException` and fix the hook.
`OperationCanceledException` is not wrapped.

## Trim or AOT warnings from `AddGeneratedValidators`

The method is annotated `[RequiresUnreferencedCode]`. Register validators explicitly in trimmed or
Native AOT applications:

```csharp
services.AddScoped<CustomerValidator>();
services.AddScoped<IValidator<Customer>, CustomerValidator>();
```

## Duplicate validation

Both the host filter and manual code run the validator. Validate once at the boundary. If you need a
manual check inside a handler, remove the endpoint/action filter or opt out with
`[SkipJeninnetValidation]`.

## Client rules are missing a validator

- `ClientRulesOptions.IncludedValidators` is non-empty and does not contain the validator name.
- The validator was not passed to `ClientRulesWriter`.
- The build-time catalog is disabled; set `JeninnetEmitClientRules=true` and add
  `<CompilerVisibleProperty Include="JeninnetEmitClientRules" />`.
- The client cached an old payload; check `schemaVersion`.

## `NU1903` on build

The OpenApi package and the AOT smoke harness suppress vendor-gated advisories for build-time
dependencies. See the package README and [AOT and trimming](02-aot-and-trimming.md).

## Getting help

Open an issue with the validator definition, the input, the expected result, and the actual result.
For analyzer issues, include the diagnostic ID and the smallest reproducer.
