# Jeninnet.Validation

[![CI](https://img.shields.io/github/actions/workflow/status/TarekNajem04/Jeninnet.Validation/ci.yml?branch=main&label=CI)](https://github.com/TarekNajem04/Jeninnet.Validation/actions/workflows/ci.yml)
[![Documentation](https://img.shields.io/github/actions/workflow/status/TarekNajem04/Jeninnet.Validation/docs-build.yml?branch=main&label=docs)](https://tareknajem04.github.io/Jeninnet.Validation/)
[![License: MIT](https://img.shields.io/github/license/TarekNajem04/Jeninnet.Validation)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/TarekNajem04/Jeninnet.Validation)](https://github.com/TarekNajem04/Jeninnet.Validation/stargazers)
[![GitHub contributors](https://img.shields.io/github/contributors/TarekNajem04/Jeninnet.Validation)](https://github.com/TarekNajem04/Jeninnet.Validation/graphs/contributors)

<!--
NuGet version/downloads and GitHub release badges are enabled after the first package
publication and the first tagged release; until then they would resolve to "not found".
[![NuGet version](https://img.shields.io/nuget/v/Jeninnet.Validation)](https://www.nuget.org/packages/Jeninnet.Validation/)
[![NuGet downloads](https://img.shields.io/nuget/dt/Jeninnet.Validation)](https://www.nuget.org/packages/Jeninnet.Validation/)
[![GitHub release](https://img.shields.io/github/v/release/TarekNajem04/Jeninnet.Validation)](https://github.com/TarekNajem04/Jeninnet.Validation/releases)
-->


![api stability: baselines ok](badges/api-stability.svg) ![architecture: invariants ok](badges/architecture.svg) ![target framework: net11.0](badges/frameworks.svg) ![obsolete members: 0](badges/obsolete.svg) ![public surface: frozen ok](badges/public-surface.svg)

- **Status:** `1.0.0-preview.1` — current preview, not yet published. The version stays
  `1.0.0-preview.1` until the first package publication.
- **Documentation:** [Jeninnet.Validation reference book](docs/en/README.md) and the
  [Why Jeninnet?](docs/en/01-getting-started/00-why-jeninnet.md) decision page.
- **Target frameworks:** `net11.0` for every shipping library (STS); analyzer `netstandard2.0`.
- **Language:** C# 15 (`LangVersion=15.0`), built with the .NET 11 SDK.
- **Security:** report privately through [GitHub Security Advisories](.github/SECURITY.md); see
  [SECURITY.md](.github/SECURITY.md).
- **Support:** see [SUPPORT.md](.github/SUPPORT.md) and the [documentation](docs/en/README.md).

Jeninnet.Validation is a modular validation framework for .NET. It combines **attribute-driven rule
declaration** with a **Roslyn source generator** to produce efficient, type-safe validators at
compile time. A fully typed fluent API is available for dynamic rules, dependency injection, and
cross-cutting scenarios. Both authoring paths feed one engine and are pinned by executable parity
tests.

## Why Jeninnet?

- **Compare first.** [Best C# validation libraries in 2026](docs/en/01-getting-started/00b-best-csharp-validation-libraries.md) — an honest comparison with FluentValidation and DataAnnotations, including when not to choose Jeninnet.
- **Capability parity, measured.** Covers **93/93 (100%)** of the .NET 10 peer-capability union;
  [per-peer coverage and honest limits](docs/en/03-reference/09-capability-parity.md).
- **One model → validator + JSON Schema + OpenAPI + client rules.** One compile-time metadata
  pipeline, no hand-maintained second source.
- **Compile-time safety.** 33 `VAL` diagnostics and code fixes reject invalid rule configurations
  before the code runs.
- **AOT + 0 B/op.** No runtime reflection on the validation path; the happy path is allocation-free
  ([benchmarks](docs/en/04-operations/06-benchmarks.md), [audit](docs/en/04-operations/07-allocation-audit.md)).
- **40 cultures and real integrations.** Multi-source localization, ICU plural/select, ASP.NET Core,
  MediatR, DataAnnotations, Blazor, GraphQL, gRPC, and desktop `INotifyDataErrorInfo`.
- **Migration in minutes.** The optional `jeninnet` tool reports a readiness score, applies safe
  codemods, and marks every uncertain pattern for review.

## Conceptual model

- **Declarative authoring.** `[Validate]` on a model plus rule attributes; the source generator emits
  a readable partial validator.
- **Fluent authoring.** `Validator<T>` and `ValidatorRuleBuilder<T>` for dynamic rules, conditions,
  and injected services.
- **One execution engine.** Cascade modes, fail-fast, rule sets, include/exclude strategy, lifecycle
  hooks, shared context, and a strict asynchronous contract.
- **Metadata pipeline.** Frozen descriptors feed JSON Schema, OpenAPI constraints, client-rule
  export, and a client evaluator.
- **DI-first, AOT-safe.** `AddValidation()` reads the generated catalog without reflection; explicit
  registration remains available for fluent validators.
- **Localization and integrations.** Message packs and providers are owned by the sibling
  [Jeninnet.Localization](https://github.com/TarekNajem04/Jeninnet.Localization) platform.

## Repository layout

| Area | Contents |
|---|---|
| Runtime (`src/`) | `Jeninnet.Validation`, `Jeninnet.Validation.Abstractions` |
| Build-time (`src/`) | `Jeninnet.Validation.Analyzers` (source generator + misuse analyzers) |
| Integrations (`src/`) | `AspNetCore`, `Extensions.Validation`, `DataAnnotations`, `Blazor`, `Observable`, `MediatR` + result adapters, `GraphQL`, `Grpc` |
| Metadata and clients (`src/`) | `JsonSchema`, `OpenApi`, `OpenApi.Swashbuckle`, `OpenApi.NSwag`, `ClientRules` |
| Localization and domain (`src/`) | `Localization`, `Localization.AspNetCore`, `Rules.Domain`, `Diagnostics`, `Assertions` |
| Tests (`tests/`) | 15 MSTest projects covering the engine, analyzers, integrations, architecture, packaging, and the `net11.0` matrix |
| Samples (`samples/`) | 26 sample projects, including the Clean Architecture and XML-localization samples |
| Templates (`templates/`) | `jeninnet-validator` `dotnet new` template |
| Tooling (`scripts/`) | Build, coverage, Sonar, package, API, localization, badge, template, and cleanup tools |
| Client (`client/`) | Dependency-free JavaScript/TypeScript client evaluator |
| Documentation (`docs/`) | The reference book, DocFX site, and release evidence |

The repository builds **25 packable projects** (25 packages). See the
[package map](docs/en/appendix/02-package-map.md) for the complete list and when to use each package. Generated folders such as `bin/`, `obj/`,
`artifacts/`, `out/`, `BenchmarkDotNet.Artifacts/`, and `TestResults/` are ignored; see
[`scripts/clean/README.md`](scripts/clean/README.md) before deleting them.

## Quick start

```bash
dotnet add package Jeninnet.Validation --version 1.0.0-preview.1
dotnet add package Jeninnet.Validation.Analyzers --version 1.0.0-preview.1
```

```csharp
using Jeninnet.Validation.Attributes;

[Validate]
public partial class User
{
    [NotEmpty]
    public string? Name { get; set; }

    [NotEmpty, Email]
    public string? Email { get; set; }

    [InclusiveBetween(18, 120)]
    public int Age { get; set; }
}
```

```csharp
using Jeninnet.Validation.Abstractions;
using Jeninnet.Validation.Catalog;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddValidation();

using var provider = services.BuildServiceProvider();
var validator = provider.GetRequiredService<IValidator<User>>();
var result = validator.Validate(new User { Name = "", Email = "bad", Age = 10 });

foreach (var error in result.Errors)
{
    Console.WriteLine($"{error.MemberName}: {error.Message ?? error.MessageKey}");
}
```

Synchronous `Validate` is strict: async-only validators throw
`AsyncValidatorInvokedSynchronouslyException`; use `ValidateAsync` for `MustAsync`, `CustomAsync`,
and async conditions.

For the complete walkthrough, see [Quick start](docs/en/01-getting-started/03-quick-start.md),
[Fluent authoring](docs/en/02-guides/01-fluent-authoring.md), and the
[rule reference](docs/en/03-reference/02-rule-reference.md).

## Building and testing

```bash
dotnet build Jeninnet.Validation.slnx -c Release
dotnet test  Jeninnet.Validation.slnx -c Release
dotnet format Jeninnet.Validation.slnx --verify-no-changes --no-restore
```

The current suite runs **997 tests** on `net11.0`. Package validation uses the packed
local feed:

```bash
dotnet pack Jeninnet.Validation.slnx -c Release -o artifacts/packages
pwsh scripts/package/check-package-contents.ps1 -PackagesPath artifacts/packages
pwsh scripts/package/check-consumer-smoke.ps1 -PackagesPath artifacts/packages
```

## Documentation

| Start here | What it covers |
|---|---|
| [Documentation book](docs/en/README.md) | Full table of contents and reading paths |
| [Introduction](docs/en/01-getting-started/01-introduction.md) | Problem space, authoring models, and design goals |
| [Package map](docs/en/appendix/02-package-map.md) | All 25 packages and when to add each one |
| [Core API](docs/en/03-reference/01-core-api.md) | `IValidator<T>`, options, results, and context |
| [Rules](docs/en/03-reference/02-rule-reference.md) | Built-in rules, attributes, and fluent methods |
| [Diagnostics](docs/en/03-reference/03-diagnostics.md) | `VAL` diagnostics and code fixes |
| [Metadata](docs/en/03-reference/04-metadata.md) | Descriptors, JSON Schema, OpenAPI, and client rules |
| [Integrations](docs/en/03-reference/05-integration-packages.md) | ASP.NET Core, MediatR, Blazor, DataAnnotations, GraphQL, gRPC, and more |
| [Compatibility](docs/en/03-reference/06-compatibility-and-versions.md) | Supported runtimes, versioning, and API policy |
| [AOT and trimming](docs/en/04-operations/02-aot-and-trimming.md) | AOT posture and verification |
| [Benchmarks](docs/en/04-operations/06-benchmarks.md) | Performance evidence and methodology |
| [Known limitations](docs/en/03-reference/07-known-limitations.md) | Boundaries to read before adopting |
| [Roadmap](ROADMAP.md) | Release plan by version |
| [Changelog](CHANGELOG.md) | Work grouped by release |

Release notes: [.github/release-notes/v1.0.0-preview.1.md](.github/release-notes/v1.0.0-preview.1.md).
Migration guides: [FluentValidation](docs/en/02-guides/15-migration-from-fluentvalidation.md) and
[DataAnnotations](docs/en/02-guides/16-migration-from-dataannotations.md).

## Contributing

The project is in an API stabilization phase; source contributions are limited to maintainers.
Issues, design feedback, documentation fixes, and migration reports are welcome. Read
[CONTRIBUTING.md](CONTRIBUTING.md), [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md), and
[SECURITY.md](.github/SECURITY.md) before opening an issue or pull request.

## License

MIT — see [LICENSE](LICENSE). Package dependencies and their licenses are listed in the packaged
package metadata and the repository's dependency manifests.
