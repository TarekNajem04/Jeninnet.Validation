# Why Jeninnet.Validation?

Most teams already have a validation library. The real question is not “which library has one more
rule?” but “what will switching cost, and what do we gain?” This page answers both.

## The short answer

Jeninnet.Validation covers the complete .NET 10 peer-capability union: **93/93 (100%)**. Every
capability that FluentValidation, DataAnnotations, Sannr, Validly, or ValidationModules ships
fully is also fully implemented here, and every partial capability they ship is full here.
This preview targets .NET 11 (`net11.0`) and C# 15; the two .NET 11-only async APIs are
deferred to `1.1.0-preview.1`.

What you gain in exchange for a mechanical migration is one metadata pipeline, compile-time
generated validators, AOT/trimming, a measured 0 B/op hot path, 40 message packs, and host
integrations that old libraries leave to separate packages.

## Parity, measured

| Scope | Result |
|---|---|
| .NET 10 capability union | **93 / 93 = 100%** |
| Full matrix including .NET 11-only async APIs | **93 / 95 = 97.9%** |
| Deferred to `1.1.0-preview.1` | N1 async DataAnnotations, N3 async Options |

| Peer | Peer full | Peer partial | Covered at full strength on .NET 10 |
|---|---:|---:|---:|
| FluentValidation 12.2 | 49 | 13 | **62 / 62** |
| DataAnnotations / .NET 11 | 28 | 27 | **55 / 55** |
| Sannr 1.6 | 32 | 26 | **58 / 58** |
| Validly 1.3 | 15 | 23 | **38 / 38** |
| ValidationModules 1.2 | 38 | 25 | **63 / 63** |

“Covered at full strength” means Jeninnet has a full implementation on every row where that peer
has a full or partial implementation on .NET 10. The methodology, the eight capabilities where we
go beyond the best peer, and the seven with no peer equivalent are on
[Capability parity](../03-reference/09-capability-parity.md).

## Performance proof

On the same two-rule model and the repository’s ShortRun benchmark, the valid path measures
**81.7 ns and 0 B per operation**, versus 226.7 ns / 632 B for FluentValidation and 848.3 ns /
1,408 B for DataAnnotations. The invalid path is also faster than FluentValidation, though it
allocates for error collection. Re-run it on your hardware before quoting it:

```bash
dotnet run -c Release --project src/Jeninnet.Validation.Benchmarks \
  -- --filter "*PeerComparisonBenchmarks*"
```

Methodology and caveats: [Peer benchmarks](../04-operations/10-peer-benchmarks.md).

## What you gain

| Gain | Why it matters |
|---|---|
| **One model → validator + JSON Schema + OpenAPI + client rules** | No duplicated metadata and no hand-written client rules. `Describe()` is the single source. |
| **Compile-time safety** | The source generator emits validators from attributes, and 33 `VAL` diagnostics reject invalid rule configurations with code fixes where possible. |
| **AOT and trimming by design** | Generated validators are instantiated directly; the repository runs Native AOT checks. No runtime reflection in the hot path. |
| **0 B/op hot path** | The error collector is pooled and the happy path is allocation-free; see [Benchmarks](../04-operations/06-benchmarks.md). |
| **40 cultures, dynamic messages, ICU** | JSON/XML/ResX/memory/database providers, parent-culture fallback, plural/select templates, and a runtime `IValidationMessageCatalog` for overrides and new languages. |
| **Integrations as first-class packages** | ASP.NET Core (minimal + MVC), MediatR (plus ErrorOr/FluentResults/CFE adapters), DataAnnotations, Blazor, GraphQL, gRPC, desktop `INotifyDataErrorInfo`, and ProblemDetails. |
| **Authoring parity** | Fluent and attribute authoring produce the same `IValidator<T>`, the same errors, and the same execution options. |

## If you use FluentValidation

The fluent surface is deliberately familiar: `Validator<T>`, `RuleFor`, `RuleSet`, `When`/`Unless`,
`Cascade`, `WithMessage`, `WithCode`, `WithSeverity`, `SetValidator`, `Include`, and
`ValidateAndThrow` map directly. The differences are small and mechanical:

- `AbstractValidator<T>` becomes `Validator<T>`, and rules move into `Configure`.
- `RuleFor(x => x.Name)` gains a member name: `RuleFor(x => x.Name, nameof(T.Name))`.
- `WithErrorCode` becomes `WithCode`.
- `RuleForEach` becomes `RuleFor(...).ForEach(...)`.
- Async-only validators must be called through `ValidateAsync`; the sync path throws instead of
  silently skipping them.

The optional `jeninnet` tool performs the safe rewrites, leaves unknown patterns untouched with an
explicit `// TODO(Jeninnet):` marker, and writes a deterministic report. See
[Migration from FluentValidation](../02-guides/15-migration-from-fluentvalidation.md).

**Stay with FluentValidation if** you need its 59 language packs today, depend on its exact
extension ecosystem, or cannot accept a preview package. Those are real reasons; parity is not the
only buying criterion.

## If you use DataAnnotations

DataAnnotations is built into .NET, simple, and often the right choice for small CRUD models. The
`Jeninnet.Validation.DataAnnotations` bridge lets existing MVC, Blazor, and `IValidatableObject`
pipelines run Jeninnet validators without rewriting the host.

You gain conditional and cross-property rules, async rules, reusable validators, descriptors,
client/schema output, and the same error model as the fluent path. You keep the existing
DataAnnotations pipeline while you migrate at your own pace.

**Stay with DataAnnotations if** your model only needs presence/range/format checks, you have no
async or cross-property rules, and the built-in surface is enough.

## Migration readiness score

The migration tool now reports how much of a codebase can move without human review:

```text
- Migration readiness: 82.6% (19 auto-converted of 23 findings; 4 need review)
- Files fully convertible: 8 of 11
```

Start with a report, not a rewrite:

```bash
dotnet tool install --global Jeninnet.Validation.Migration --version 1.0.0-preview.1
jeninnet migrate analyze ./src --output migration-report.md
jeninnet migrate apply ./src --dry-run
```

Apply when the report is acceptable, then keep the old dependency until the migrated tests and the
new validators agree. The tool is idempotent: a second `apply` changes nothing.

## Try it in five minutes

```bash
dotnet add package Jeninnet.Validation --version 1.0.0-preview.1
dotnet add package Jeninnet.Validation.Localization --version 1.0.0-preview.1
```

From a clone of this repository, you can also generate a starting project:

```bash
dotnet new install ./templates/jeninnet-validator
dotnet new jeninnet-validator -n MyCompany.Validation -o src/MyCompany.Validation
```

```csharp
using Jeninnet.Validation;
using Jeninnet.Validation.Attributes;

[Validate]
public partial class Customer
{
    [NotEmpty]
    [EmailAddress]
    public string? Email { get; set; }
}

var validator = new CustomerValidator();
var result = validator.Validate(new Customer { Email = "" });
```

Next: [Installation](02-installation.md), [Quick start](03-quick-start.md), or
[Diagnostics](../03-reference/03-diagnostics.md).

## Honest limits

- The matrix compares capability presence, not every edge-case semantic. Keep your tests.
- 40 cultures vs FluentValidation's 59; new translations are machine-assisted drafts pending
  native-speaker review.
- N1/N3 are .NET 11-only async APIs and land in `1.1.0-preview.1`; this preview's platform
  support (the `net11.0` target, C# 15, and the .NET 11 validation adapter) does not include them.
- Eight adjacent categories are deliberately excluded (compile-time rule classes, jQuery MVC,
  multi-targeting, nested dictionaries, advanced `TestValidate`, Shadow Types, Guard Clauses,
  dynamic JSON rules).
- Client rules are data; the server remains authoritative and expressions are not translated to
  JavaScript.

## Related

- [Capability parity](../03-reference/09-capability-parity.md)
- [Migration from FluentValidation](../02-guides/15-migration-from-fluentvalidation.md)
- [Migration from DataAnnotations](../02-guides/16-migration-from-dataannotations.md)
- [Diagnostics](../03-reference/03-diagnostics.md)
- [Performance](../04-operations/01-performance.md)
- [AOT and trimming](../04-operations/02-aot-and-trimming.md)
