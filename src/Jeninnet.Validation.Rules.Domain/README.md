# Jeninnet.Validation.Rules.Domain

Optional domain rule packs for `Jeninnet.Validation`: IBAN, VAT numbers, postal codes, national
identifiers, password strength, UUID, and ULID. The package is structural and offline: it never
calls an external registry or tax service.

## Requirements

- .NET 10.0 or later (including .NET 11).
- C# 15 when consuming the source contracts.

## Installation

```xml
<PackageReference Include="Jeninnet.Validation.Rules.Domain" Version="1.0.0-preview.1" />
```

## Fluent usage

```csharp
using Jeninnet.Validation.Rules;

builder.RuleFor(x => x.Iban, nameof(Account.Iban))
       .Iban("NL");

builder.RuleFor(x => x.VatNumber, nameof(Company.VatNumber))
       .VatNumber("NL");

builder.RuleFor(x => x.PostalCode, nameof(Address.PostalCode))
       .PostalCode("NL");

builder.RuleFor(x => x.Bsn, nameof(Person.Bsn))
       .NationalId("NL");

builder.RuleFor(x => x.Password, nameof(Account.Password))
       .PasswordStrength(PasswordPolicy.Strict);

builder.RuleFor(x => x.RequestId, nameof(Request.RequestId))
       .Uuid();

builder.RuleFor(x => x.EventId, nameof(Event.EventId))
       .Ulid();
```

## Attribute usage

```csharp
using Jeninnet.Validation.Attributes;

[Iban("NL")]
public string? Iban { get; set; }

[VatNumber("NL")]
public string? VatNumber { get; set; }

[PostalCode("NL")]
public string? PostalCode { get; set; }

[NationalId("NL")]
public string? Bsn { get; set; }

[PasswordStrength(PasswordStrengthPreset.Strict)]
public string? Password { get; set; }

[Uuid]
public string? RequestId { get; set; }

[Ulid]
public string? EventId { get; set; }
```

## Supported countries

| Rule | Countries |
|---|---|
| `VatNumber` | `NL`, `DE`, `FR`, `GB`, `BE` |
| `PostalCode` | `NL`, `US`, `DE`, `FR`, `GB`, `BE`, `EG` |
| `NationalId` | `NL` (BSN), `EG`, `US` (SSN) |
| `Iban` | SEPA and the wider IBAN registry; pass a country to pin the prefix and length |

Unsupported countries fail explicitly with `NotSupportedException` when the rule is constructed.
`Iban` without a country validates the checksum and the country-specific length.

## Scope

- Structural validation only: checksum/pattern and date structure. The package does not prove that
  an account, company, or person exists.
- Checksums (IBAN mod 97, BSN elfproef, VAT check digits) stay on the server. JSON Schema/OpenAPI and
  ClientRules carry patterns and lengths; the client runtime marks these rules `serverOnly`.
- Password policies are configurable through `PasswordPolicy`; attributes expose the
  `Basic`/`Strong`/`Strict` presets.
- The Dutch rules cover IBAN, btw-id, postcode, and BSN. BSN and btw-id use the eleven-test.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
