# Why Jeninnet.Validation?

Jeninnet.Validation is built around one idea: describe a validation rule once, then use the same
metadata for execution, JSON Schema, OpenAPI, client rules, and host integrations.

## The short answer

- **Two authoring styles, one engine.** Declare rules with attributes and the Roslyn source
  generator, or with the fully typed fluent API. Both produce the same `IValidator<T>` contract and
  the same `ValidationResult`.
- **Compile-time safety.** Generated validators are ordinary C# types, checked by the compiler and
  covered by `VAL` diagnostics with code fixes where possible.
- **One descriptor pipeline.** A frozen descriptor powers JSON Schema (draft 2020-12), OpenAPI
  constraints, and client-rule export, so metadata is not hand-maintained a second time.
- **AOT and trimming by design.** Generated validators are instantiated directly; the validation
  hot path does not use runtime reflection.
- **Allocation-free happy path.** The error collector and root context are pooled, and the
  rule-selection fast path returns the rule array without materialization.
- **40 cultures with ICU templates.** JSON, XML, ResX, memory, and database providers support
  parent-culture fallback, plural/select templates, and runtime catalog overrides.
- **First-class integrations.** ASP.NET Core (minimal API and MVC), MediatR with result adapters,
  DataAnnotations, Blazor, GraphQL, gRPC, OpenAPI adapters, and desktop `INotifyDataErrorInfo`.

This preview targets **.NET 10 (LTS)** as the baseline, adds **.NET 11** assets for the
`DataAnnotations`, `Extensions.Validation`, and OpenAPI packages, and uses C# 15. The two .NET 11
asynchronous APIs — async DataAnnotations and async Options validation — are implemented on the
`net11.0` assets.

## What you gain

| Gain | Why it matters |
|---|---|
| **One model → validator + JSON Schema + OpenAPI + client rules** | No duplicated metadata and no hand-written client rules. `Describe()` is the single source. |
| **Compile-time safety** | The source generator emits validators from attributes, and 33 `VAL` diagnostics reject invalid rule configurations with code fixes where possible. |
| **AOT and trimming by design** | Generated validators are instantiated directly; the repository runs Native AOT checks. No runtime reflection in the hot path. |
| **0 B/op hot path** | The error collector is pooled and the happy path is allocation-free; see [Benchmarks](../04-operations/06-benchmarks.md). |
| **40 cultures, dynamic messages, ICU** | JSON/XML/ResX/memory/database providers, parent-culture fallback, plural/select templates, and a runtime `IValidationMessageCatalog` for overrides and new languages. |
| **Integrations as first-class packages** | ASP.NET Core (minimal + MVC), MediatR (plus ErrorOr/FluentResults/CFE adapters), DataAnnotations, Blazor, GraphQL, gRPC, desktop `INotifyDataErrorInfo`, and ProblemDetails. |
| **Two authoring styles, one engine** | Attributes and fluent rules produce the same `IValidator<T>`, the same errors, and the same execution options. |

## One model, several outputs

A validated model is more than a pass/fail check. Jeninnet keeps the rule metadata frozen and
reusable:

- the generated or fluent validator executes the rules;
- the descriptor produces JSON Schema for contracts and documentation;
- the OpenAPI adapters project constraints into the API description;
- the client-rules exporter emits JSON, TypeScript, or JavaScript rule data for browser and desktop
  validation, while the server remains authoritative.

The same pipeline also feeds the DataAnnotations and Blazor bridges, so a host can adopt the library
without replacing its existing validation entry points.

## Performance

On the repository's ShortRun benchmark, the valid path measures **81.7 ns and 0 B per operation**
for a two-rule model. Generated validators are faster than fluent validators on the same engine
because the rule selection and member names are compiled ahead of time. Re-run the suite on your
hardware before quoting numbers:

```bash
dotnet run -c Release --project src/Jeninnet.Validation.Benchmarks \
  -- --filter "*ValidationBenchmarks*"
```

Methodology and caveats: [Benchmarks](../04-operations/06-benchmarks.md).

## Try it in five minutes

```bash
dotnet add package Jeninnet.Validation --version 1.0.0-preview.3
dotnet add package Jeninnet.Validation.Localization --version 1.0.0-preview.3
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

- 40 cultures are included; new translations are machine-assisted drafts pending native-speaker
  review.
- The .NET 11 async DataAnnotations and async Options APIs are implemented on the `net11.0` assets.
  The `net10.0` baseline remains synchronous for those adapters.
- Generated validators require the model to declare a namespace. Models in the global namespace are
  not supported.
- Built-in rules are sealed to keep the hot path devirtualizable and the metadata truthful. Extend
  through `Custom`/`CustomAsync`, `RuleTemplate`, `SyncValidateRule`/`AsyncValidateRule`, or the rule
  interfaces.
- Client rules are data. The server remains authoritative, and C# expressions are not translated to
  JavaScript.
- The project is still a preview. The public surface is baselined, but it can change before `1.0.0`.

## Related

- [Diagnostics](../03-reference/03-diagnostics.md)
- [Performance](../04-operations/01-performance.md)
- [AOT and trimming](../04-operations/02-aot-and-trimming.md)
- [Benchmarks](../04-operations/06-benchmarks.md)
- [Known limitations](../03-reference/07-known-limitations.md)
- [Compatibility and versions](../03-reference/06-compatibility-and-versions.md)
