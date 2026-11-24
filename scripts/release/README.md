# Release gate runner

`run-release-gates.ps1` is the single local entry point for the preview/stable release gate. It runs
the repository's existing checks in dependency order, writes a Markdown and JSON report tied to the
current commit and product version, and exits non-zero when a required gate fails.

The runner deliberately calls the existing scripts instead of reimplementing them. CI uses the same
script from the publish workflow, so local and CI evidence have the same shape.

## Prerequisites

- PowerShell 7 or later.
- The .NET SDK pinned by `global.json`.
- Node.js/npm for the client runtime gate.
- NativeAOT prerequisites for the current runtime identifier when `aot-smoke` runs.

## Usage

Run every required gate and write the report under `artifacts/release/<version>/`:

```powershell
pwsh scripts/release/run-release-gates.ps1 -ExpectedVersion 1.0.0-preview.3
```

Run a fast local iteration while keeping the report shape:

```powershell
pwsh scripts/release/run-release-gates.ps1 -SkipPackages -SkipAot
```

Skip individual gates:

```powershell
pwsh scripts/release/run-release-gates.ps1 `
  -SkipRestore -SkipBuild -SkipTests -SkipFormat `
  -SkipSourceHeaders -SkipApiBaselines -SkipApiGateSelfTest `
  -SkipApiDiff -SkipMessageKeys -SkipDocs -SkipClientSupport -SkipClient -SkipVulnerability `
  -SkipPackages -SkipConsumer -SkipAotSupport -SkipAot -SkipCoverage -SkipSonar
```

Run the external coverage/Sonar steps when their credentials and baselines exist:

```powershell
pwsh scripts/release/run-release-gates.ps1 -IncludeCoverage -IncludeSonar
```

## Gates

| Gate | Switch to skip | Depends on | Evidence |
|---|---|---|---|
| `version` | — | — | Evaluated MSBuild version, optionally compared with `-ExpectedVersion` |
| `restore` | `-SkipRestore` | — | NuGet restore |
| `build` | `-SkipBuild` | `restore` | Release build, 0 warnings / 0 errors |
| `tests` | `-SkipTests` | `build` | Full solution test run |
| `format` | `-SkipFormat` | `build` | `dotnet format --verify-no-changes` |
| `source-headers` | `-SkipSourceHeaders` | — | Standard attribution header |
| `api-baselines` | `-SkipApiBaselines` | `build` | Public API baseline files |
| `api-gate-self-test` | `-SkipApiGateSelfTest` | — | RS0016/RS0017 fail-closed fixture |
| `api-diff` | `-SkipApiDiff` | — | Preview.2 public API diff with explicit approvals |
| `message-keys` | `-SkipMessageKeys` | — | Message-key catalog vs resource packs |
| `docs-links` | `-SkipDocs` | — | Local Markdown links |
| `readme-claims` | `-SkipDocs` | — | README project and packable-count claims |
| `arabic-text` | `-SkipDocs` | — | Arabic text and hidden-control check |
| `client-support` | `-SkipClientSupport` | — | C#/JS support lists match the manifest |
| `client` | `-SkipClient` | — | JavaScript client runtime tests |
| `vulnerability` | `-SkipVulnerability` | `restore` | NuGet vulnerability audit |
| `package-pack` | `-SkipPackages` | `build` | Pack all 25 packages with package validation |
| `package-contents` | — | `package-pack` | Package asset/content check |
| `package-budget` | — | `package-pack` | Dependencies, assets, metadata, and reviewed size budgets |
| `package-versions` | — | `package-pack` | Package file names match the product version |
| `consumer-smoke` | `-SkipConsumer` | `package-pack` | Packed-package consumer smoke on `net10.0`/`net11.0` |
| `aot-support` | `-SkipAotSupport` | — | Every package has an explicit AOT/trim position |
| `aot-smoke` | `-SkipAot` | `build` | NativeAOT publish and run |
| `coverage` | `-SkipCoverage` | `build` | Reported-only unless `-IncludeCoverage` |
| `sonar` | `-SkipSonar` | `build` | Reported-only unless `-IncludeSonar` |

A skipped dependency causes its dependants to be skipped with a reason. The final command succeeds
only when no required gate failed; reported-only gates do not fail the command but are counted
separately. The `api-diff` gate writes its own report under `artifacts/api-diff/<version>/`.

## Report

Each run replaces the same version directory:

```text
artifacts/release/<version>/
  release-gates.md
  release-gates.json
  logs/
    00-version.log
    01-restore.log
    ...
  test-results/
  packages/
  aot/<rid>/
```

The Markdown report contains the product version, commit SHA, branch, exact command line, duration,
per-gate status, evidence, and a tail of the failing log. The JSON report contains the same data in
machine-readable form.

`artifacts/` is ignored by Git. In CI, upload the report files and logs as the
`release-gates-<version>` artifact; the publish workflow does this before NuGet publication.

## Exit codes

| Code | Meaning |
|---:|---|
| `0` | Every required gate passed (reported-only gates are allowed) |
| `1` | At least one required gate failed, setup failed, or the report could not be written |

## Failure self-test

The runner has been exercised with a deliberately broken documentation link. The run failed with
exit code `1`, named `docs-links` as the failing gate, and recorded the broken link in the log and
report. The temporary fixture is not part of the repository.
