# Roadmap

This is the public delivery plan for Jeninnet.Validation. It is organized by **release**, not by
feature. Each release lists what shipped, what remains open, and what moved to a later version.

Legend: **MUST** ships with the release; **SHOULD** is planned but can move at the scope gate;
**LATER** is intentionally deferred. Status values are `Shipped`, `Open`, `Planned`, `Deferred`,
and `Not planned`.

## Release principles

- **SemVer.** Previews use `1.0.0-preview.N`, release candidates use `1.0.0-rc.N`, stable uses
  `1.0.0`; additive features use minor versions; breaking changes require a major version.
- **One product version.** `Directory.Build.props` owns the single solution-wide `<Version>`;
  package projects must not declare their own version. `AssemblyVersion`/`FileVersion` stay numeric.
- **One public API baseline per TFM and package.** A surface change is never silent.
- **Packages, consumers, docs, AOT, and security are release gates**, not afterthoughts.
- **No dependency without a written reason.** BCL and approved `Microsoft.Extensions.*` packages
  come first.
- **No public artifact links to an internal, unpublished document.**
- **Preview and RC counts are not fixed.** Additional `preview.N` or `rc.N` releases can be cut
  when feedback or release-gate evidence requires another pass.

## Status at a glance

| Release line | Status | Scope |
|---|---|---|
| `1.0.0-preview.1` | **Released 2026-11-13** | .NET 11 (STS) first preview; .NET 11 + C# 15 platform and authoring features |
| `1.0.0-preview.2` | **Released 2026-11-17** | .NET 10 (LTS) baseline restored; .NET 11 bridge packages and OpenAPI keep net11 assets |
| `1.0.0-preview.3` | **Current — implemented on `release/1.0.0-preview.3`; not yet published** | Async DataAnnotations and async Options promoted from 1.1.0; release gates and Roslynator setup |
| `1.0.0-preview.4` … `preview.N` | **Conditional** | Post-preview fixes and API feedback; no unrelated scope |
| `1.0.0-rc.1` … `rc.N` | **Conditional** | Release-candidate hardening; release-blocking fixes only |
| `1.0.0` | **Planned** | First stable release |
| `1.1.0-preview.1` … `1.1.0` | **Planned** | Follow-up .NET 11 feedback and additive API review (async bridges promoted to preview.3) |
| Post-1.0 candidates | **Deferred / demand-driven** | Optional features that are not committed |
| Explicit exclusions | **Not planned** | Out-of-scope capabilities |

---

# 1.0.0-preview.3 — Current, implemented on branch; not yet published

**Goal:** ship the two .NET 11 async bridges promoted from `1.1.0`, keep the `net10.0` baseline
unchanged, and close the preview release gates.

**Scope re-cut (2026-11-23):** a maintainer value review promoted N1 (async DataAnnotations) and N3
(async Options) into this release. The post-1.0 candidates remain deferred. No other feature is
added without another explicit re-cut.

## Completed in this version

- [x] **Async DataAnnotations bridge.** `JeninnetValidatableAttribute` and
  `JeninnetValidatePropertyAttribute` use `AsyncValidationAttribute` on `net11.0`;
  `Validator.ValidateObjectAsync`/`TryValidateObjectAsync` run async-only validators, including
  scoped resolution and cancellation. `net10.0` remains synchronous.
- [x] **Async Options validation.** `AddValidatedOptions<T>()` registers the async options validator
  on `net11.0`; asynchronous startup runs async-only validators, including named options and
  cancellation. `net10.0` remains synchronous.
- [x] **Central Roslynator setup.** Roslynator 5.0.1 is versioned once and referenced once for every
  C# project; nine RCS diagnostics were fixed without suppressions.

## Version snapshot

| Dimension | Status |
|---|---|
| Version | `1.0.0-preview.3` (central, `Directory.Build.props`) |
| Target frameworks | `net10.0` baseline; `net11.0` additive for `DataAnnotations`, `Extensions.Validation`, and the three OpenAPI adapters |
| Build | **Verified:** 0 warnings / 0 errors on a clean Release build |
| Tests | **Verified:** 1,106/1,106 across 18 test assemblies on `net10.0` and `net11.0` |
| Format/docs | **Verified:** `dotnet format`, doc links, README claims, Arabic text, source headers, docfx metadata/build, and API reference |
| AOT/trimming | **Verified:** 10 AOT-compatible packages; 25-package matrix; NativeAOT smoke covers core, JSON Schema, client rules, and the Microsoft OpenAPI applier; CI verifies `linux-x64` |
| Packages | **Verified:** 25 packable packages; contents, central-version alignment, dual-TFM consumer smoke, and local-feed dry run |
| Public API | **Verified:** 25 package baselines, including the per-TFM net11 DataAnnotations override |
| Release gates | **Verified locally:** `scripts/release/run-release-gates.ps1`; report under `artifacts/release/1.0.0-preview.3/` |
| API freeze | **Verified:** diff against `v1.0.0-preview.2`; 1 approved additive API, 0 removals, 0 signature changes |
| Coverage | Pending refresh on the preview.3 commit |

## Open gates

- [x] **MUST — Build, test, format, headers, docs, AOT, package, and consumer evidence.** Verified on
  the preview.3 branch.
- [x] **MUST — Release gate runner.** Local run green with 19 passed and 2 reported-only external
  gates; the publish workflow uploads `release-gates-1.0.0-preview.3` from
  `scripts/release/run-release-gates.ps1`.
- [ ] **MUST — Coverage/Sonar refresh** on the preview.3 commit.
- [ ] **MUST — Version alignment.** Central and package versions agree; the tag and published
  packages are created at publication.
- [ ] **MUST — Preview.3 publication.** Push to NuGet and tag only after the gates are green.
- [x] **SHOULD — Release notes** with the preview delta.

---

# 1.0.0-preview.2 — Released 2026-11-17

**Goal:** restore .NET 10 (LTS) as the baseline target while keeping .NET 11 (STS) as an additive
target for the packages that expose .NET 11-only APIs. The first preview shipped `net11.0` only;
this preview makes the same feature set consumable from .NET 10 without changing the public API.
It was published to NuGet on 2026-11-17.

## Version snapshot

| Dimension | Status |
|---|---|
| Version | `1.0.0-preview.2` (central, `Directory.Build.props`) |
| Target frameworks | `net10.0` baseline for every shipping library; `net11.0` added for `DataAnnotations`, `Extensions.Validation`, and the three OpenAPI adapters; analyzer `netstandard2.0` |
| Language | C# 15 (`LangVersion=15.0`); SDK pinned to .NET `11.0.100` |
| Build | **Verified:** 0 errors, 0 warnings |
| Tests | **Verified:** 1,038/1,038 passing across `net10.0` and `net11.0` (997 on the net10 baseline) |
| Analyzers | **Verified:** clean; diagnostics `VAL0001`–`VAL0034` (`VAL0026` unused) |
| AOT/trimming | **Verified:** shipping libraries `IsAotCompatible`; NativeAOT smoke publishes and runs in CI |
| Packages | **Verified:** 25 packable packages; package contents and dual-TFM consumer smoke verified |
| Public API | **Verified:** 25 package baselines with the `RS0016`/`RS0017` gate |
| Coverage | **Pending:** re-run SonarCloud and ReportGenerator on the preview.2 release commit |
| Localization | **Available:** 40 cultures; sibling `Jeninnet.Localization.*` packages remain on `1.0.0-preview.1` |

## Completed in this version

### Platform and build

- [x] **MUST — .NET 10 baseline.** Every shipping library targets `net10.0`; the analyzer stays on
  `netstandard2.0`.
- [x] **MUST — .NET 11 assets.** `Jeninnet.Validation.DataAnnotations`,
  `Jeninnet.Validation.Extensions.Validation`, and the three OpenAPI adapters ship both
  `lib/net10.0` and `lib/net11.0`.
- [x] **MUST — Per-target package versions.** OpenAPI uses Microsoft.AspNetCore.OpenApi 10.0.14 +
  Microsoft.OpenApi 2.x on net10, and 11.0.0 + 3.x on net11; Swashbuckle uses 10.3.1 / 11.0.1.
- [x] **MUST — Per-target output paths.** `AppendTargetFrameworkToOutputPath=true` prevents net10
  and net11 dependency assets from mixing in one output folder.
- [x] **MUST — Dual-TFM tests.** Bridge tests and OpenAPI adapter tests run on both targets; the
  rest of the suite runs on the net10 baseline.
- [x] **MUST — Central versioning and package set.** 25 packable packages with no per-project
  versions.

### Engine, rules, and execution

- [x] Attribute-driven generation and fluent authoring share one engine and executable conformance tests.
- [x] Rule sets, rule/property include-exclude, class- and rule-level cascade, fail-fast, and
  `ValidationCallOptions`.
- [x] Per-rule `When`/`WhenAsync`/`Unless`/`UnlessAsync`, `ApplyConditionTo`, and dependent rules.
- [x] Lifecycle hooks (`PreValidate`, `PreValidateAsync`, `OnFailure`, `OnFailureAsync`).
- [x] Shared `ValidationContext<T>` with `Root`, pooled `RootData`, and `ServiceProvider`.
- [x] Custom/custom-async rules with context and multi-error emission.
- [x] Collection counts, `ForEach`/`Where`/`WhereAsync`, and collection element rules.
- [x] Dedicated format rules (`ExactLength`, `Base64String`, `FileExtensions`), `UniqueItems`, and
  `MultipleOf`.
- [x] Inheritance/polymorphism, rule templates, input sanitization, `[Flags]` enums, and full
  property-chain helpers.
- [x] Strict-async guard and `VAL0016`; synchronous validation never silently drops async rules.

### Integrations

- [x] ASP.NET Core minimal API, MVC auto-validation, endpoint filters, binding sources,
  interceptors, and ProblemDetails mapping.
- [x] MediatR pipeline and `ErrorOr`/`FluentResults`/`CSharpFunctionalExtensions` result adapters.
- [x] DataAnnotations class/property bridges.
- [x] Blazor `EditContext` adapter with async submit, debounce, and static SSR client validation.
- [x] `ObservableValidationAdapter<TModel>` (`INotifyDataErrorInfo`) for WPF/MAUI/Avalonia.
- [x] Microsoft.Extensions.Validation bridge with net10 + net11 assets.
- [x] HotChocolate GraphQL middleware and gRPC server interceptor.
- [x] ASP.NET Core `IStringLocalizer`/resx bridge with explicit culture control.
- [x] Optional observability (`ActivitySource`/`Meter`) and opt-in ProblemDetails enrichment.

### Metadata, schemas, and clients

- [x] Frozen validator descriptors and `Describe()` for fluent and generated validators.
- [x] Deterministic JSON Schema (draft 2020-12) generation with nested `$defs`/`$ref`.
- [x] OpenAPI constraints, naming-policy awareness, Swashbuckle adapter, and NSwag adapter.
- [x] Client-rule export (JSON/TypeScript/JavaScript) and a dependency-free client evaluator.
- [x] Generated client catalog with server fallback.

### Localization

- [x] Built-in packs for 40 cultures with key/placeholder consistency and RTL support.
- [x] Runtime `IValidationMessageCatalog` for culture/message additions, overrides, and removals.
- [x] Unified message placeholders, authored-message precedence, and ICU plural/select opt-in.
- [x] Localized domain rule packs (IBAN, VAT, postal code, national ID, password strength, UUID,
  ULID, Dutch BSN/btw/postcode).

### Quality, tooling, and documentation

- [x] 33 diagnostics and code fixes; analyzer family phase 1 + 2.
- [x] `Jeninnet.Validation.Assertions` test helpers.
- [x] `jeninnet migrate` CLI with readiness score and golden migration tests.
- [x] Public API baselines, DocFX API reference, and compatibility policy.
- [x] AOT/trimming evidence, allocation audit, and benchmark evidence.
- [x] Architecture tests, coverage gate, vulnerability gate, package content checks, and consumer
  smoke tests.
- [x] 26 sample projects, the `jeninnet-validator` template, and the documentation book.
- [x] README, package READMEs, ROADMAP, CHANGELOG, release notes, and migration tooling.

## Open release gates

- [x] **MUST — Preview.1 publication.** `1.0.0-preview.1` was pushed to NuGet and tagged on 2026-11-13.
- [x] **MUST — Preview.2 publication.** `1.0.0-preview.2` was pushed to NuGet and tagged on 2026-11-17.
- [ ] **MUST — Release checklist.** Record the build/test matrix, docs, AOT, package validation and
  dependency budget, consumer validation, vulnerability scan, and installation smoke test.
- [ ] **SHOULD — Consumer validation.** Validate the published packages from a clean consumer feed
  on both `net10.0` and `net11.0` before the preview announcement.
- [ ] **SHOULD — Template package.** Publish `Jeninnet.Validation.Templates` once the preview feed is
  live; the repo-local `dotnet new install` path is tested.

## Moved out of this version (historical; async bridges promoted on 2026-11-23)

- **Promoted to `1.0.0-preview.3` (2026-11-23) — Async DataAnnotations bridge.**
  `AsyncValidationAttribute`, `IAsyncValidatableObject`, and `Validator.ValidateObjectAsync` support.
- **Promoted to `1.0.0-preview.3` (2026-11-23) — Async Options validation.**
  `IAsyncStartupValidator`/`IAsyncValidateOptions<T>` support.
- **Post-1.0 candidates (deferred, demand-driven):** ReactiveUI/ReactiveValidation integration,
  custom-attribute source generation, and client message packs.

---

# 1.0.0-preview.4 … 1.0.0-preview.N — Conditional

**Goal:** absorb preview feedback without expanding scope.

- [ ] **MUST — Fix preview blockers.** Correct release-blocking bugs and API feedback.
- [ ] **MUST — Re-run every release gate.** Build/test/format/docs/AOT/package/consumer/Sonar.
- [ ] **MUST — No new features.** A feature is added only by an explicit roadmap re-cut.
- [ ] **SHOULD — Refresh release notes** with the preview delta.

---

# 1.0.0-rc.1 … 1.0.0-rc.N — Conditional

**Goal:** harden the release candidate; no new features.

- [ ] **MUST — API freeze.** Freeze the public surface and refresh every baseline.
- [ ] **MUST — Release-blocking fixes only.**
- [ ] **MUST — Final release notes and supported-runtime matrix.**
- [ ] **MUST — Full quality gate on the release commit.**
- [ ] **SHOULD — Community preview soak.** Resolve reported issues before the stable decision.

---

# 1.0.0 — Stable

**Goal:** ship the first stable release with `net10.0` baseline assets and `net11.0` bridge assets.

- [ ] **MUST — Preview/RC soak and feedback.** Resolve release-blocking bugs and API feedback.
- [ ] **MUST — API freeze and baselines.** Refresh every baseline and publish the compatibility
  policy.
- [ ] **MUST — Final release notes and migration package.**
- [ ] **MUST — Final quality gate.** Build/test/format/docs/AOT/package/consumer/Sonar green on the
  release commit.
- [ ] **MUST — Long-run benchmark baseline.** Re-run the suite on a quiet machine before the stable
  release; decide whether a pinned CI budget is justified.
- [ ] **SHOULD — SBOM, checksums, and signing decision.**
- [ ] **SHOULD — Additional consumer reference app** for the stable announcement.
- [ ] **SHOULD — Mutation testing evaluation** for the rule engine and evaluator.

---

# 1.1.0-preview.1 … 1.1.0 — Follow-up additive .NET 11

**Goal:** absorb follow-up .NET 11 async feedback without breaking the 1.0 API. The two original
async bridges were promoted to `1.0.0-preview.3` on 2026-11-23.

- [x] **Moved — Async DataAnnotations bridge.** Promoted to preview.3; see the preview.3 section.
- [x] **Moved — Async Options validation.** Promoted to preview.3; see the preview.3 section.
- [ ] **SHOULD — Additive API review.** Stage any new surface in `PublicAPI.Unshipped.txt`.
- [ ] **SHOULD — Release notes** for follow-up async changes.

---

# Post-1.0 — Demand-driven candidates

These are **not commitments**. Each starts only after a user request and a maintainer decision.

| Feature | Status | Target | Notes |
|---|---|---|---|
| ReactiveUI/ReactiveValidation integration | `Deferred` | Post-1.0 | The `Observable` adapter already covers `INotifyDataErrorInfo`; an Rx-specific wrapper is demand-driven. |
| Custom-attribute source generation | `Deferred` | Post-1.0 | Lets user-defined attributes emit rules through the generator. |
| Client message packs | `Deferred` | Post-1.0 | Localized message templates for client-rules consumers. |

## Explicitly not planned

- Porting the validation engine to JavaScript.
- A WYSIWYG form builder or schema designer.
- Localization provider composition (the exactly-one-provider rule is deliberate).

---

## Release gates (every release)

- Build: 0 warnings / 0 errors; `dotnet format` clean.
- Tests: the full suite green on every supported TFM.
- Sonar: coverage above 80% on the runtime scope, zero duplicated lines, zero open issues.
- AOT: shipping libraries trim/AOT clean; the smoke harness publishes and runs.
- Packages: every packable project produces a valid package with README, license, and SourceLink.
- Documentation: links, README claims, Arabic text, and package READMEs verified.
- Version: tag, central version, and package version agree; previews use `1.0.0-preview.N` and
  release candidates use `1.0.0-rc.N`.

---

*Localization providers, translation services, and localization storage backends are owned by the
sibling Jeninnet.Localization repository and are intentionally out of scope here.*
