# Jeninnet.Validation.JsonSchema

Generates deterministic **JSON Schema (draft 2020-12)** from
`Jeninnet.Validation` descriptors. It is standalone (no ASP.NET dependency) and
AOT-safe: it consumes the frozen validation descriptor contract.

## Requirements

- .NET 11.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.JsonSchema --version 1.0.0-preview.1
```

## Usage

```csharp
using Jeninnet.Validation.JsonSchema;

var descriptor = new UserValidator().Describe();
var schema = ValidationSchemaWriter.WriteToString(descriptor);

// or:
var jsonObject = ValidationSchemaWriter.Write(descriptor);
```

## Mapping

| Rules | Schema |
|---|---|
| `NotNull`, `NotEmpty`, `StringNotEmpty` | `required`; `minLength: 1` for strings |
| `Length`, `ExactLength` | `minLength`/`maxLength` |
| `Regex`, `Contains`, `StartsWith`, `EndsWith` | `pattern` |
| `Email`, `Url` | `format` |
| `IPAddress` | `anyOf` `ipv4`/`ipv6` formats |
| `Enum` | `x-enum-type` |
| `Nested` | `type: object` + `$ref` into `$defs` |
| `ForEach` | `type: array` + `items` `$ref` to the element schema |
| `Count`, `MinCount`, `MaxCount` | `type: array`, `minItems`/`maxItems` |
| `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual` | `exclusiveMinimum`/`minimum`/`exclusiveMaximum`/`maximum` |
| `InclusiveBetween`, `ExclusiveBetween`, `Range` | numeric bounds |
| `In` | `enum` |
| `Base64String` | `contentEncoding: base64` |
| `ScalePrecision` | `multipleOf` |
| `Iban`, `VatNumber`, `PostalCode`, `NationalId` | `pattern` (checksums stay server-side) |
| `PasswordStrength` | `minLength` |
| `Uuid` | `format: uuid` |
| `Ulid` | `pattern` |
| `Must`, `Custom`, `Dependent`, conditional rules | `x-validation-rules` advisory entries |

Nested composition uses deterministic `$defs` names and emits `$ref` for recursive descriptors
without recursing forever. Array item schemas use the same composition.

Nullability and requiredness follow the validation rules: `NotNull`/`NotEmpty`
mark a member required; other rules do not invent requiredness. `required` is
omitted for non-nullable value-type members because presence is implied by
serialization, not by a non-default value.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
