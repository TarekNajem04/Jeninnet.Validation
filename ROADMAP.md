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
| `1.0.0-preview.1` | **Current — unreleased** | .NET 11 + C# 15 platform, .NET 10 peer parity, stable-release gates |
| `1.0.0-preview.2` … `preview.N` | **Conditional** | Post-preview fixes and API feedback; no new scope |
| `1.0.0-rc.1` … `rc.N` | **Conditional** | Release-candidate hardening; release-blocking fixes only |
| `1.0.0` | **Planned** | First stable release |
| `1.1.0-preview.1` … `1.1.0` | **Planned** | Additive .NET 11 async bridges |
| Post-1.0 candidates | **Deferred / demand-driven** | Optional features that are not committed |
| Explicit exclusions | **Not planned** | Out-of-scope capabilities |

---

# 1.0.0-preview.1 — Current, unreleased

**Goal:** ship the first public preview with .NET 11 + C# 15 support, complete .NET 10 peer parity,
and the stable-release evidence in place. The version remains `1.0.0-preview.1` until the first
package publication.

## Version snapshot

| Dimension | Status |
|---|---|
| Version | `1.0.0-preview.1` (central, `Directory.Build.props`) |
| Target frameworks | `net10.0` core; `net10.0;net11.0` for `Jeninnet.Validation.DataAnnotations` and `Jeninnet.Validation.Extensions.Validation`; analyzer `netstandard2.0` |
| Language | C# 15 (`LangVersion=15.0`); SDK pinned to .NET `11.0.100` |
| Build | **Verified:** 0 errors, 0 warnings |
| Tests | **Verified:** 1011/1011 passing (net10 + net11 matrices) |
| Analyzers | **Verified:** clean; diagnostics `VAL0001`–`VAL0034` (`VAL0026` unused) |
| AOT/trimming | **Verified:** shipping libraries `IsAotCompatible`; NativeAOT smoke publishes and runs in CI |
| Packages | **Verified:** 25 packable packages; package contents and consumer smoke verified |
| Public API | **Verified:** 25 package baselines with the `RS0016`/`RS0017` gate |
| Coverage | **Verified 2026-11-12:** ReportGenerator 82.1% line / 69.4% branch / 81.8% method across 25 assemblies; Sonar last measured 82.1% coverage / 0.0% duplication. Re-run Sonar on the release commit ([details](docs/en/04-operations/03-coverage-and-quality.md)). |
| Localization | **Available:** 40 cultures; sibling `Jeninnet.Localization.*` packages remain on `1.0.0-preview.1` |

## Completed in this version

### Platform and build

- [x] **MUST — .NET 11 SDK and C# 15.** `global.json` pins `11.0.100`; all projects compile with
  `LangVersion=15.0`.
- [x] **MUST — .NET 11 assets.** `Jeninnet.Validation.DataAnnotations` and
  `Jeninnet.Validation.Extensions.Validation` ship `lib/net10.0` and `lib/net11.0`.
- [x] **MUST — .NET 11 validation bridge.** `IValidatableTypeInfo`/`IValidatableParameterInfo`,
  including minimal-API parameter validation, on net11; the net10 `IValidatableInfo` path remains.
- [x] **MUST — Central versioning and package set.** 25 packable packages with no per-project
  versions.

### Engine, rules, and execution

- [x] Attribute-driven generation and fluent authoring share one engine and executable parity tests.
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

- [x] Built-in packs for 40 cultures with key/placeholder parity and RTL support.
- [x] Runtime `IValidationMessageCatalog` for culture/message additions, overrides, and removals.
- [x] Unified message placeholders, authored-message precedence, and ICU plural/select opt-in.
- [x] Localized domain rule packs (IBAN, VAT, postal code, national ID, password strength, UUID,
  ULID, Dutch BSN/btw/postcode).

### Quality, tooling, and documentation

- [x] 33 diagnostics and code fixes; analyzer family phase 1 + 2.
- [x] `Jeninnet.Validation.Assertions` test helpers.
- [x] `jeninnet migrate` CLI with readiness score and golden migration tests.
- [x] Public API baselines, DocFX API reference, and compatibility policy.
- [x] AOT/trimming evidence, allocation audit, benchmark evidence, and peer benchmarks.
- [x] Architecture tests, coverage gate, vulnerability gate, package content checks, and consumer
  smoke tests.
- [x] 26 sample projects, the `jeninnet-validator` template, and the documentation book.
- [x] README, package READMEs, ROADMAP, CHANGELOG, release notes, and migration guides.

## Open release gates

- [ ] **MUST — NuGet publication.** Push the preview packages and create the matching `v*` tag.
  **Deferred:** publication starts only after a separate go/no-go review.
- [ ] **MUST — Release checklist.** Record the build/test matrix, docs, AOT, package validation,
  consumer validation, vulnerability scan, and installation smoke test.
- [ ] **SHOULD — Consumer validation.** Validate the published packages from a clean consumer feed
  before the preview announcement.
- [ ] **SHOULD — Template package.** Publish `Jeninnet.Validation.Templates` once the preview feed is
  live; the repo-local `dotnet new install` path is tested.

## Moved out of this version

- **`1.1.0-preview.1` — Async DataAnnotations bridge.** `AsyncValidationAttribute`,
  `IAsyncValidatableObject`, and `Validator.ValidateObjectAsync` support.
- **`1.1.0-preview.1` — Async Options validation.** `IAsyncStartupValidator`/
  `IAsyncValidateOptions<T>` support.
- **Post-1.0 candidates (deferred, demand-driven):** ReactiveUI/ReactiveValidation integration,
  custom-attribute source generation, and client message packs.

---

# 1.0.0-preview.2 … 1.0.0-preview.N — Conditional

**Goal:** absorb preview feedback without expanding scope.

- [ ] **MUST — Fix preview blockers.** Correct release-blocking bugs and API feedback.
- [ ] **MUST — Re-run every release gate.** Build/test/format/docs/AOT/package/consumer/Sonar.
- [ ] **MUST — No new features.** A feature is added only by an explicit roadmap re-cut.
- [ ] **SHOULD — Refresh release notes and migration notes** with the preview delta.

---

# 1.0.0-rc.1 … 1.0.0-rc.N — Conditional

**Goal:** harden the release candidate; no new features.

- [ ] **MUST — API freeze.** Freeze the public surface and refresh every baseline.
- [ ] **MUST — Release-blocking fixes only.**
- [ ] **MUST — Final release notes, migration guide, and supported-runtime matrix.**
- [ ] **MUST — Full quality gate on the release commit.**
- [ ] **SHOULD — Community preview soak.** Resolve reported issues before the stable decision.

---

# 1.0.0 — Stable

**Goal:** ship the first stable release with `net10.0` assets and native `net11.0` bridge assets.

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

# 1.1.0-preview.1 … 1.1.0 — Additive .NET 11

**Goal:** add the .NET 11 asynchronous capabilities without breaking the 1.0 API. `net10.0` consumers
keep the `1.0.x` behavior.

- [ ] **MUST — Async DataAnnotations bridge.** `AsyncValidationAttribute`,
  `IAsyncValidatableObject`, `Validator.ValidateObjectAsync`, and cancellation support.
- [ ] **MUST — Async Options validation.** `IAsyncStartupValidator`/
  `IAsyncValidateOptions<T>`, named options, and `CancellationToken`.
- [ ] **SHOULD — Additive API review.** Stage any new surface in `PublicAPI.Unshipped.txt`.
- [ ] **SHOULD — Release notes and migration notes** for the async paths.

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
