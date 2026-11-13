# Contributing to Jeninnet.Validation

Thank you for your interest in Jeninnet.Validation.

The project aims to provide a modular validation framework for .NET with a strong focus on
correctness, deterministic behavior, explicit contracts, trimming/Native AOT compatibility, and
long-term API stability.

## Current Project Status

Jeninnet.Validation is in an API stabilization phase. The current public preview,
`1.0.0-preview.1`, is prepared for the 25 packable packages and adds .NET 11 + C# 15 support. The project remains pre-stable, and
the public surface can still change before `1.0.0`; every change is nevertheless recorded in a
committed public-API baseline.

During this stage, the project's internal architecture, CI/CD pipelines, quality gates, release
process, and public APIs are still being refined and validated.

To maintain consistency and avoid unnecessary breaking changes, **source-code contributions are
temporarily limited to project maintainers**.

## How You Can Contribute Today

Community participation is highly appreciated.

The most valuable contributions at this stage are:

- Bug reports
- Feature requests
- Design discussions
- API design feedback
- Documentation improvements
- Source-generator and analyzer behavior observations
- Security observations
- Real-world usage feedback

Please use GitHub Issues or Discussions to share ideas and suggestions.

## Pull Requests

At the current stage of the project:

- Pull requests containing source-code changes may not be accepted.
- Pull requests that improve documentation may be considered on a case-by-case basis.
- Large design proposals should begin as an Issue or Discussion before implementation.

Once the project reaches a stable baseline, external code contributions will be opened publicly.

## Documentation Contributions

Documentation changes are welcome within the current contribution policy. Read
[Contributing to the Documentation](docs/CONTRIBUTING.md) before opening a documentation pull
request. It covers where a change belongs, chapter structure, code-example verification, package
README rules, the required checks, and the review checklist.

## Project Structure

```text
src/
    Jeninnet.Validation/                  Core engine + fluent API
    Jeninnet.Validation.Abstractions/     Interfaces + value types
    Jeninnet.Validation.Analyzers/        Roslyn source generator + misuse analyzers
    Jeninnet.Validation.Assertions/       Stack-agnostic test assertions
    Jeninnet.Validation.Localization/     Built-in message packs for 40 cultures
    Jeninnet.Validation.Localization.AspNetCore/  IStringLocalizer bridge
    Jeninnet.Validation.AspNetCore/       Minimal API + MVC integration
    Jeninnet.Validation.Extensions.Validation/  Microsoft.Extensions.Validation bridge
    Jeninnet.Validation.JsonSchema/       JSON Schema (draft 2020-12) generation
    Jeninnet.Validation.OpenApi/          OpenAPI schema constraints
    Jeninnet.Validation.ClientRules/      Client-side rule export (JSON/TS/JS)
    Jeninnet.Validation.Rules.Domain/     IBAN, VAT, postal code, national ID, password, UUID, ULID
    Jeninnet.Validation.MediatR/          MediatR validation pipeline
    Jeninnet.Validation.MediatR.ErrorOr/  ErrorOr result adapter
    Jeninnet.Validation.MediatR.FluentResults/  FluentResults adapter
    Jeninnet.Validation.MediatR.CSharpFunctionalExtensions/  CSharpFunctionalExtensions adapter
    Jeninnet.Validation.DataAnnotations/ DataAnnotations bridge
    Jeninnet.Validation.Blazor/           Blazor EditContext adapter
    Jeninnet.Validation.Observable/       INotifyDataErrorInfo adapter
    Jeninnet.Validation.GraphQL/          GraphQL integration
    Jeninnet.Validation.Grpc/             gRPC integration
    Jeninnet.Validation.Diagnostics/      ActivitySource/Meter instrumentation
    Jeninnet.Validation.Benchmarks/       BenchmarkDotNet harness
    shared/                               Shared source units imported via .projitems

tests/
    Jeninnet.Validation.Tests/            Engine, rules, parity, generator snapshots
    Jeninnet.Validation.Analyzers.Tests/  Analyzer + emitter tests
    Jeninnet.Validation.ArchitectureTests/  Layering + dependency-direction guards
    Jeninnet.Validation.AspNetCore.Tests/ ASP.NET Core integration
    Jeninnet.Validation.MediatR.Tests/    MediatR integration
    Jeninnet.Validation.DataAnnotations.Tests/  DataAnnotations bridge
    Jeninnet.Validation.Blazor.Tests/     Blazor adapter

samples/                                  26 sample projects (21 in the solution)
    Jeninnet.Validation.AotSmoke/         NativeAOT publish/run harness
docs/                                     Guides, feature/rule catalogs, migration notes
scripts/                                  Build, format, coverage, Sonar, and docs checks
```

## Design Philosophy

The architecture emphasizes:

- Attribute-driven rule declaration by default; the fluent API (`Validator<T>`) is the supported
  alternative authoring path for dynamic and cross-cutting scenarios
- Both authoring paths feed the same `IValidator<T>` contracts and must not diverge (enforced by
  executable parity tests)
- Compile-time safety: invalid rule configurations produce analyzer errors
- Allocation-free success path, verified by the release allocation test and benchmarks
- Trimming and Native AOT compatibility for every runtime library
- Explicit contracts: cross-assembly contracts live in `Jeninnet.Validation.Abstractions`
- Deterministic behavior: synchronous validation never silently executes or drops asynchronous rules
- Minimal dependencies: BCL and approved `Microsoft.Extensions.*` packages first

When evaluating proposals, maintainability, determinism, and long-term API stability are generally
preferred over short-term convenience.

## Development Standards

Run the following before sending a change:

```powershell
dotnet build Jeninnet.Validation.slnx
dotnet test Jeninnet.Validation.slnx
dotnet format Jeninnet.Validation.slnx --verify-no-changes --severity info
pwsh scripts/docs/check-doc-links.ps1
pwsh scripts/docs/check-readme-claims.ps1
pwsh scripts/docs/check-arabic-text.ps1
pwsh scripts/api/check-public-api-baselines.ps1
```

The build must produce 0 warnings and 0 errors. The Sonar gate requires coverage above 80% and zero
duplicated lines; see `scripts/sonar/README.md` and `docs/en/04-operations/03-coverage-and-quality.md`.

## Public API changes

Every packable project commits `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` next to its
`.csproj`. `Microsoft.CodeAnalysis.PublicApiAnalyzers` treats an unrecorded new API (`RS0016`) or a
stale baseline entry (`RS0017`) as a build error.

- Add new API to `Unshipped` with `pwsh scripts/api/sync-public-api.ps1`; review the diff and
  commit the baseline file with the source change.
- At release time, promote reviewed entries with
  `pwsh scripts/api/sync-public-api.ps1 -Promote`.
- A breaking change is allowed only in a major version. Mark the member
  `[Obsolete("Use X instead.", error: false)]` for at least one minor release, keep the behavior,
  and document the migration. Remove the member and its baseline entry only in the major release.
  Exception: before `1.0.0` nothing has shipped, so delete the member and its baseline entry
  outright instead of marking it `[Obsolete]`.
- Never delete a baseline entry to hide an accidental change; revert the source change instead.
- Razor components are not fixable through `dotnet format`; maintain the Blazor baseline manually
  as described in `scripts/api/README.md`.
- The consumer-facing policy is in
  [Compatibility and versions](docs/en/03-reference/06-compatibility-and-versions.md).

The behavioral gate creates a throwaway project and proves the add/remove transitions without
modifying the repository:

```powershell
pwsh scripts/api/test-public-api-gate.ps1
```

## License

The project is licensed under the MIT License. Any contribution accepted by the maintainers is
licensed under the same terms.
