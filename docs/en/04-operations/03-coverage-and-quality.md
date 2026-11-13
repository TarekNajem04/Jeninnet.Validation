# Coverage and Quality

## Test suite

- **997 tests** across core/validation, analyzers, architecture, ASP.NET Core, MediatR,
  DataAnnotations (`net11.0`), Blazor, Observable UI, GraphQL, gRPC, the ASP.NET Core
  localization bridge, and the 40-culture message catalog, including the 23-case generated/fluent
  conformance matrix.
- Build: 0 warnings / 0 errors.
- Formatting and documentation checks are part of the release gates.

## Coverage

Two scopes are published:

| Scope | Result | Purpose |
|---|---|---|
| ReportGenerator, all assemblies | **82.1% line / 69.4% branch / 81.8% method** (2026-11-12, 25 assemblies) | Broad baseline including analyzer tooling |
| Sonar runtime scope (last measured) | **82.1% coverage** (line 84.3%, branch 77.2%) | Quality gate on runtime code |

Sonar excludes analyzer/code-fix tooling, metadata-only attributes, benchmarks, tests, and
`IRuleDefinition.cs` from coverage; duplication excludes the same tooling. The gate requires
coverage above 80% and zero duplicated lines.

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
