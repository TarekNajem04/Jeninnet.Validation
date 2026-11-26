# Coverage and Quality

## Test suite

- **1,142 tests** across 18 test assemblies on `net10.0` and `net11.0`, covering core/validation, analyzers, architecture, ASP.NET Core, MediatR,
  DataAnnotations (`net10.0` and `net11.0`), Blazor, Observable UI, GraphQL, gRPC, the ASP.NET Core
  localization bridge, and the 40-culture message catalog, including the 23-case generated/fluent
  conformance matrix.
- Build: 0 warnings / 0 errors.
- Formatting and documentation checks are part of the release gates.

## Coverage

> Preview.3 note: the local SonarQube result below is the current release-candidate check. The CI
> SonarCloud/ReportGenerator run remains part of the publication gate; replace the local row when
> the CI run completes.

Two scopes are published:

| Scope | Result | Purpose |
|---|---|---|
| ReportGenerator, all assemblies | **79.1% line / 65.2% branch / 84.6% method** (2026-11-12, 25 assemblies) | Broad historical baseline including analyzer tooling |
| SonarQube local preview.3 (2026-11-26, `2ba52b5`) | **81.5% coverage** (line 85.2%, branch 73.5%), **0.0% duplication**, 0 issues | Release-candidate runtime and client scope |
| SonarCloud runtime scope (2026-11-13) | **80.2% coverage** (line 83.4%, branch 73.6%) | Previous published-preview quality gate |

Sonar excludes analyzer/code-fix tooling, metadata-only attributes, benchmarks, tests, and
`IRuleDefinition.cs` from coverage; duplication excludes the same tooling. The local preview also
imports JavaScript client coverage through LCOV. The gate requires coverage above 80% and zero
duplicated lines.

## Duplication

Duplicated lines density is **0.0%** on the Sonar runtime scope. Shared runtime code lives in
`src/shared` and is compiled into each consumer through `.projitems`.

## Running locally

```powershell
pwsh scripts/coverage/coverage.ps1 -Execute -Clean
pwsh scripts/sonar/preview.ps1 -Local -WithCoverage -ExportIssues
```

The Sonar preview requires a local SonarQube server and `SONAR_TOKEN_LOCAL`; see
`scripts/sonar/README.md`.

## Quality gates

- Build 0/0.
- Full suite green.
- `dotnet format --verify-no-changes`.
- Docs links, README claims, and Arabic text checks.
- Sonar coverage > 80%, duplication 0%, open issues 0.

## Related

- [Performance](01-performance.md)
- [AOT and trimming](02-aot-and-trimming.md)
