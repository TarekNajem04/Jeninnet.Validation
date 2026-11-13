# Best C# validation libraries in 2026: an honest comparison

If you are choosing a validation library for a .NET 10 codebase, the three names that come up most
often are **FluentValidation**, **DataAnnotations**, and **Jeninnet.Validation**. This page compares
them on the criteria that usually decide the choice: authoring model, runtime behavior, host
integrations, metadata reuse, and migration cost. It is written by the Jeninnet.Validation
maintainers, so read it as an informed comparison, not a neutral survey.

## Short answer

- Choose **DataAnnotations** when you need the smallest possible dependency, your rules are simple,
  and ASP.NET Core's built-in model validation is enough.
- Choose **FluentValidation** when you want the largest ecosystem, the most third-party examples,
  and a mature fluent API with no migration.
- Choose **Jeninnet.Validation** when you want one metadata model to drive the validator, JSON
  Schema, OpenAPI, and client rules; compile-time generated validators; AOT/trimming; a
  zero-allocation happy path; and first-class integrations across ASP.NET Core, Blazor, MediatR,
  GraphQL, gRPC, and desktop view models.

## Feature comparison

| Criterion | Jeninnet.Validation | FluentValidation | DataAnnotations |
|---|---|---|---|
| Authoring | Fluent and attribute | Fluent | Attributes |
| Compile-time generated validators | Yes (source generator) | No | No |
| AOT / trimming | Designed for it | Supported with care | Supported |
| Happy-path allocations | 0 B/op (measured) | Allocates | Allocates |
| JSON Schema from the same model | Yes | Separate work | No |
| OpenAPI constraints from the same model | Yes | Separate work | Partial |
| Client-rule export | Yes (JSON/TS/JS) | No | No |
| ASP.NET Core | Minimal API + MVC | Yes | Built-in |
| Blazor | Yes | Yes | Yes |
| MediatR | Yes (+ result adapters) | Yes | No |
| GraphQL / gRPC | Yes | Via custom code | No |
| Desktop `INotifyDataErrorInfo` | Yes | Via custom code | Partial |
| Localization | 40 cultures, multiple providers | Yes | Via resources |
| Diagnostics | 33 `VAL` diagnostics + code fixes | Runtime exceptions | Runtime exceptions |
| Maturity / ecosystem | Newer, smaller | Largest | Built into .NET |
| License | MIT | Apache-2.0 | MIT |

## Performance

On the repository's two-rule benchmark (ShortRun), the valid path measures **81.7 ns and 0 B per
operation**, versus **226.7 ns / 632 B** for FluentValidation and **848.3 ns / 1,408 B** for
DataAnnotations. These numbers are hardware- and model-dependent; re-run the benchmark before
quoting them. Methodology and caveats: [Peer benchmarks](../04-operations/10-peer-benchmarks.md).

## Capability parity, measured

The repository tracks a 93-capability union across FluentValidation 12.2, DataAnnotations/.NET 11,
Sannr 1.6, Validly 1.3, and ValidationModules 1.2. Jeninnet.Validation covers **93/93 (100%)** of the
.NET 10 union; including .NET 11-only async APIs, it covers **93/95 (97.9%)**. The full matrix and
the method are on [Capability parity](../03-reference/09-capability-parity.md).

## When not to choose Jeninnet.Validation

- You need the largest possible hiring pool and the most third-party examples today.
- You cannot take a preview dependency or a migration step.
- You need a capability that is explicitly listed as deferred in
  [Known limitations](../03-reference/07-known-limitations.md).
- You need a library outside the .NET 10 / C# 14 baseline.

## Migration cost

The fluent surface is deliberately familiar: `Validator<T>`, `RuleFor`, `RuleSet`, `When`/`Unless`,
`Cascade`, `WithMessage`, `WithCode`, `WithSeverity`, `SetValidator`, `Include`, and
`ValidateAndThrow` map directly. The optional `jeninnet` tool reports a readiness score, applies
safe codemods, and marks uncertain patterns for review:

- [Migration from FluentValidation](../02-guides/15-migration-from-fluentvalidation.md)
- [Migration from DataAnnotations](../02-guides/16-migration-from-dataannotations.md)

## Try it

```bash
dotnet add package Jeninnet.Validation
```

```csharp
using Jeninnet.Validation;
using Jeninnet.Validation.Fluent;

public sealed class CustomerValidator : Validator<Customer>
{
    protected override void Configure(ValidatorRuleBuilder<Customer> builder)
    {
        builder.RuleFor(x => x.Name, nameof(Customer.Name))
               .NotEmpty()
               .Length(1, 100);

        builder.RuleFor(x => x.Email, nameof(Customer.Email))
               .NotEmpty()
               .EmailAddress();

        builder.RuleFor(x => x.Age, nameof(Customer.Age))
               .InclusiveBetween(18, 120);
    }
}
```

See the [quick start](03-quick-start.md) for the complete example and the
[installation guide](02-installation.md) for package selection.

## Sources and updates

This page is maintained with the repository. The capability matrix, benchmark harness, and
migration guides are the source of truth; if a number here disagrees with them, they win. Last
reviewed: 2026-11-13.
