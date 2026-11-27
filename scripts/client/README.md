# Client support tooling

The client rule support set is single-sourced in [`client/rules-manifest.json`](../../client/rules-manifest.json).
The manifest drives both the C# exporter and the JavaScript evaluator.

## Generate

```powershell
pwsh scripts/client/generate-client-rule-support.ps1
```

The generator writes:

- `src/Jeninnet.Validation.ClientRules/ClientRuleSupport.g.cs`
- `client/src/client-rule-support.generated.js`

Both files are checked in. Do not edit them by hand; edit the manifest and regenerate.

## Check for drift

```powershell
pwsh scripts/client/check-client-rule-support.ps1
```

The check fails if either generated file differs from the manifest. CI and the release gate run
this check.

## Parity fixtures

The shared corpus is [`client/test/fixtures/client-rules-parity.json`](../../client/test/fixtures/client-rules-parity.json).
It contains:

- a payload covering every supported rule with a valid model;
- a representative error payload whose expected errors are asserted by both C# and JavaScript tests;
- a server-only payload with a custom rule and a conditional rule that must set `needsServer`.

The C# tests live in `tests/Jeninnet.Validation.Tests/Metadata/ClientRulesParityTests.cs`; the
JavaScript tests live in `client/test/client-validator.test.mjs`.

## Support matrix

| Classification | Rules | Reason |
|---|---|---|
| Supported | `NotNull`, `NotEmpty`, `StringNotEmpty`, `Length`, `ExactLength`, `Regex`, `Email`, `Url`, `IPAddress`, `Phone`, `CreditCard`, `Base64String`, `FileExtensions`, `Contains`, `StartsWith`, `EndsWith`, `Count`, `MinCount`, `MaxCount`, `UniqueItems`, `MultipleOf`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `InclusiveBetween`, `ExclusiveBetween`, `Range`, `In`, `NotIn`, `ScalePrecision`, `After`, `Before` | Synchronous, expression-friendly, and representable from the exported descriptor parameters. |
| Server-only — conditions | `Conditional` | Conditions are opaque delegates; the server decides applicability. |
| Server-only — custom/async | `Must`, `MustAsync`, `Custom`, `CustomAsync` | Custom code and asynchronous execution cannot be serialized into a synchronous browser evaluator. |
| Server-only — nested/collection | `Nested`, `NestedValidator`, `ChildRules`, `Include`, `ForEach`, `ForEachElement` | Child validators and collection composition require server-side descriptors. |
| Server-only — cross-field/dependent | `PropertyEqual`, `PropertyNotEqual`, `PropertyGreaterThan`, `PropertyGreaterThanOrEqual`, `PropertyLessThan`, `PropertyLessThanOrEqual`, `Dependent` | These rules need the full model and server execution order. |
| Server-only — rule sets/templates | `RuleSet`, `RuleTemplate` | Rule selection and template expansion are host policy. |
| Server-only — domain/reference data | `Iban`, `VatNumber`, `PostalCode`, `NationalId`, `PasswordStrength`, `Uuid`, `Ulid` | Reference data, checksum logic, or policy belongs on the server. |
| Server-only — remaining descriptor rules | `Null`, `NotDefault`, `Empty`, `Enum`, `IsEnumName`, `Equal`, `NotEqual`, `FutureDate`, `PastDate` | Outside the expression-friendly synchronous subset. |
| Server-only — unknown/future | `*` | Fail-safe default: an unknown rule never blocks the browser and requests the server. |

The client remains dependency-free and never evaluates a server-only rule.
