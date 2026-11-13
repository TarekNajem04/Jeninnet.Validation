# Changelog

All notable changes to the Jeninnet.Validation solution are documented here.

---


## Versions

| Version | Status |
|---|---|
| `1.0.0-preview.1` | Current, unreleased |
| `Pre-1.0.0` | Historical development (2026-06 – 2026-11) |

---

## 1.0.0-preview.1 — Unreleased

**Status:** Unreleased. The version remains `1.0.0-preview.1` until the first package publication.

### Summary

- .NET 11 (`net11.0`) and C# 15 support, including the .NET 11 validation bridge.
- Complete .NET 10 peer parity, the fluent/generated engine, rules, execution semantics, and
  integrations.
- 40 cultures, JSON Schema/OpenAPI/client-rule emission, migration tooling, and 26 samples.
- 1011 tests across net10.0 and net11.0; 25 packable projects.

### .NET 11 and C# 15

#### Platform support

- The solution now builds with **.NET SDK 11.0.100** (`global.json`) and **C# 15**
  (`LangVersion=15.0`).
- `Jeninnet.Validation.DataAnnotations` and `Jeninnet.Validation.Extensions.Validation` now
  multi-target `net10.0;net11.0` and ship both `lib/net10.0` and `lib/net11.0` assets.
- The .NET 11 `Microsoft.Extensions.Validation` adapter implements
  `IValidatableTypeInfo`/`IValidatableParameterInfo`, so minimal-API parameter validation works on
  .NET 11 as well as .NET 10. The net10 path keeps the existing `IValidatableInfo` contract.
- Added net11 variants for the DataAnnotations and Extensions.Validation test suites; the full
  matrix is **1011/1011 passing**.
- Product version is now `1.0.0-preview.1`.
- Benchmarks run on .NET 11 with `BenchmarkDotNet 0.16.0-preview.2`. Version 0.15.8 does not
  recognize the `net11.0` runtime moniker and failed with
  `GetRuntimeVersion not implemented for NotRecognized` before executing any benchmark; the
  dependency returns to the stable 0.16 line when it ships.
- The Blazor component suppresses `S3881` for its managed-only coordinator, with a written
  justification; no public API change.
- Async DataAnnotations and async Options remain planned for `1.1.0-preview.1`.

---

### Packaging and correctness hardening

#### Analyzer packaging, release gate, and validation fixes

- The analyzer package now places `Jeninnet.Validation.Analyzers.dll` under
  `analyzers/dotnet/cs` with `IncludeBuildOutput=false`, `DevelopmentDependency=true`, and no
  symbols package. A normal `PackageReference` now activates the generator and analyzers.
- The NuGet publish workflow reads the centralized version from MSBuild instead of a
  per-project `<Version>` element that no longer exists.
- The core package README and the Abstractions `About.md` were replaced with consumer-facing
  documentation; package README size and transcript content are now sane.
- The Migration tool no longer ships a .NET Framework binding-redirect `.dll.config`.
- `Microsoft.AspNetCore.OpenApi` was updated from 10.0.1 to 10.0.14; the `NU1903` suppression
  was removed.
- Synchronous `Validate` now detects async-only `RuleSet` and `ChildRules` content through
  recursive rule-content inspection and throws `AsyncValidatorInvokedSynchronouslyException`.
- `[Regex]` generated rules now use a one-second timeout.
- ASP.NET Core ProblemDetails use `MessageKey` as the fallback message.
- OpenAPI schema application preserves existing constraints when the validator has no opinion.
- `ValidationResult.Errors` now exposes a `ReadOnlyCollection<ValidationError>` snapshot, so
  callers cannot cast the result to the internal array and mutate a completed validation result.
- `MemberRuleGroup<T>.SyncRules` and `AsyncRules` now expose `IReadOnlyList<T>` views, and the
  constructor copies its inputs; callers can no longer mutate a built rule group.
- Removed 27 unused public `*Definition` metadata records from
  `Jeninnet.Validation.Abstractions`; the generator emits `IRuleDefinition` implementations
  directly, and no runtime, generated, sample, or test code referenced the records. The
  `IRuleDefinition`/`IPropertyRuleDefinition` contracts remain.
- Validation options are now container-scoped: `AddValidation`/`AddValidationCore` accept a
  configuration transform, `ValidationContext<T>` carries the effective options, and ASP.NET Core
  filters pass the request provider through `ValidationCallOptions`. Display/property resolvers and
  cascade defaults no longer have to share one process-wide instance.
- The core `Jeninnet.Validation` package no longer depends on `Jeninnet.Localization.*`.
  `AddValidationCore`/`AddValidation` register only the validator infrastructure; the new
  `AddValidationLocalization` in `Jeninnet.Validation.Localization` registers the message pipeline,
  and `AddDefaultMessagePacks` calls it automatically. `ValidationOptions.MessageFormatter` moved
  out of the core options.
- Every code-fix provider now has positive and negative coverage in the analyzer test suite;
  the tests uncovered and fixed code actions that were registered even when no applicable syntax
  existed (`SwapInclusiveBetween`, `AddNotNullBeforeNumericRule`, `RemoveConflictingRule`,
  `FixLengthAndNotEmpty`).
- Added 1,000-iteration sync/async concurrency stress tests over shared validators, per-container
  option isolation, and process-wide configuration fallback safety.
- CI now runs package validation on Ubuntu and Windows, includes a reviewed vulnerability gate,
  dependency review on pull requests, a package-version check, and a local-feed publish dry run.
- Standardized every package README with installation, requirements, related packages, and license
  sections; rewrote the console, customization, domain, and analyzer sample READMEs, and added run
  instructions to every runnable sample. Removed internal task identifiers from public documentation.
- Resolved the remaining IDE/Roslynator diagnostics in the maintained source: primary constructors,
  auto-properties, naming rules, blank-line layout, and test cancellation-token usage.
- Added regression tests for async-only wrappers, regex timeouts, ProblemDetails message keys,
  OpenAPI constraint preservation, immutable validation results, member rule groups, isolated
  per-container options, localization-free core registration, code fixes, and concurrency.
  Full suite: 984 tests.

### Localization ownership and dynamic catalog

#### Dynamic messages and explicit host localization

- Added `IValidationMessageCatalog`, registered by `AddDefaultMessagePacks`, for thread-safe runtime additions, overrides, and removals of message templates and whole cultures, layered over the built-in 40 packs. Reads use lock-free copy-on-write snapshots; updates apply to the next lookup.
- `AddValidationCore` no longer inspects `ILocalizationStore` or calls `JeninnetLocalizationExtensions.AddLocalization`. It registers the message pipeline only and never replaces the host’s `IStringLocalizerFactory`; store-backed localization is an explicit `AddLocalization` call (either order).
- Added 7 catalog tests and 4 DI composition tests covering runtime overrides/removals, new cultures, fallback, concurrent updates, no-store startup, custom factory preservation, and explicit store-backed registration.
- Docs: localization guide, package README, DI/ASP.NET Core guides, known limitations, README, and Why page.

### Adoption and differentiation

#### Migration readiness and public parity evidence

- Added a **migration readiness score** to `jeninnet migrate analyze`: Markdown/JSON now report the share of findings the codemod can apply without review, the converted/needs-review counts, and the number of fully convertible files. Added 3 report tests.
- Added the public [Why Jeninnet?](docs/en/01-getting-started/00-why-jeninnet.md) decision page and [Capability parity](docs/en/03-reference/09-capability-parity.md) reference: 93/93 (100%) of the .NET 10 peer-capability union, per-peer coverage, eight advantages, seven unique capabilities, deliberate exclusions, and honest limits.
- Added migration golden tests: the same two-rule model runs through FluentValidation and the migrated Jeninnet validator with matching validity and member names, and the codemod fixtures assert the FluentValidation and DataAnnotations output shape.
- Added `PeerComparisonBenchmarks` (FluentValidation 12.2 and DataAnnotations) plus the [Peer benchmarks](docs/en/04-operations/10-peer-benchmarks.md) methodology. On the repository machine the valid path measured 81.7 ns / 0 B for Jeninnet vs 226.7 ns / 632 B for FluentValidation.
- Added the `jeninnet-validator` `dotnet new` template with a sample model/validator, installation docs, and a local template smoke script.
- Linked the new pages from the README, the book index, and the introduction; added the adoption workstream to the roadmap.
- Corrected the diagnostics reference: 33 diagnostics (`VAL0001`–`VAL0034`, `VAL0026` intentionally unused) and documented `VAL0034`.

### Rules and API parity fixes
#### Wider message packs (26 → 40 cultures)

- Added built-in packs for Greek (`el`), Hebrew (`he`), Hungarian (`hu`), Romanian (`ro`), Slovak (`sk`), Slovenian (`sl`), Croatian (`hr`), Serbian (`sr`), Bulgarian (`bg`), Lithuanian (`lt`), Latvian (`lv`), Estonian (`et`), Catalan (`ca`), and Afrikaans (`af`). The catalog now covers 40 cultures and all 53 message keys.
- Every new pack preserves the English placeholder set and formats multi-parameter templates. Hebrew is RTL; Arabic, Hebrew, Urdu, and Persian are the RTL packs.
- Translations are machine-assisted drafts pending native-speaker review, matching the existing contribution policy.
- Added a Greek regional fallback test (`el-GR` → `el`) and updated the culture-count guards. Docs and package counters now say 40 cultures.
- No package or API changes; resources are embedded by the existing wildcard.

#### Custom validator selector

- Added `IValidatorSelector.CanExecute<T>(descriptor, memberName, context)` and `ValidationCallOptions.Selector`. Selectors run after rule-set and include/exclude filtering and can only narrow the selected rules.
- Added `DefaultValidatorSelector`, `RulesetValidatorSelector`, and `CompositeValidatorSelector`.
- Rule descriptors are cached per rule instance, so selectors do not re-allocate metadata on every call. The default path stays allocation-free when no selector is configured.
- Added 7 tests covering sync/async selection, rule sets, include/exclude interaction, default and composite selectors, and the empty composite.
- Docs: rule sets and cascade guide, core API reference. API additions are staged in `PublicAPI.Unshipped.txt`.

#### Global validator options

- Added `ValidationOptions.PropertyNameResolver`, which rewrites the code-facing `ValidationError.MemberName` for rules without a per-rule override; blank results keep the original name.
- Added `ValidationOptions.ValidatorFactory` and `ValidationConfiguration.CreateValidator<T>(IServiceProvider?)`/`CreateValidator(Type, IServiceProvider?)`. ASP.NET Core MVC, binding, and endpoint filters fall back to the factory when `IValidator<T>` is not registered in DI.
- Added `ValidationOptions.DefaultRuleLevelCascadeMode` (with `DefaultCascadeMode` retained as a forwarding alias) and a virtual `ValidatorDefinition<T>.RuleLevelCascadeMode` override point. Precedence is rule > validator > global; class-level cascade is unchanged.
- Added 10 core tests and 1 ASP.NET Core filter test covering resolver precedence, cascade precedence, factory fallback, and isolation.
- Docs: fluent authoring, rule sets and cascade, core API reference. API additions are staged in `PublicAPI.Unshipped.txt`.

#### Full property-chain API (`PropertyChain`)

- Added `ValidationPath.GetParentChain(string?)`, returning every parent from the immediate parent to the root (`A.B[0].C` → `A.B[0]`, `A.B`, `A`).
- Added the public `PropertyChain` readonly record struct with `FromPath`, `Path`, `IsEmpty`, `Add`, `GetParentChain`, `IsChildChainOf(PropertyChain)`, and `IsChildChainOf(string?)`. Comparisons are ordinal and `A` is not treated as a parent of `AB.C`.
- Added 8 core tests covering roots, indexers, whitespace, add chains, equality, and boundary checks.
- Docs: core API reference and custom-rules guide. API additions are staged in `PublicAPI.Unshipped.txt`.

#### Authored messages take precedence over localization packs

- Added `ValidationError.MessageIsAuthored` and collector overloads that mark an error as authored. A message authored with `WithMessage` (static or dynamic), an attribute message, or an explicit custom message now takes precedence over a matching message-pack template.
- `LocalizeValidationErrorAsync` and the ASP.NET Core `IValidationStringLocalizer` resolver return the authored message immediately; non-authored errors still resolve `MessageKey` through the packs or resx, then fall back to `ValidationError.Message`.
- Descriptors expose `MessageIsAuthored`, and client-rule JSON adds `messageIsAuthored: true` for authored rules. The generated client catalog does the same.
- Tests: 5 fluent/descriptor tests, 2 localization precedence tests, and 1 ASP.NET resolver test; full suite 894 green. Docs: rules and messages, localization, known limitations, client rules.
- No new package; API additions are staged in `PublicAPI.Unshipped.txt`.

#### `[MultipleOf]` numeric divisibility rule

- Added `MultipleOfRule<T, TValue>` plus `[MultipleOf]` and fluent `.MultipleOf(divisor)` for integer, long, floating-point, decimal, and numeric-string divisors. The divisor must be a positive finite number; zero, negative, non-numeric, and out-of-range divisors throw at configuration time.
- Integral values use decimal modulo; `decimal` values use exact decimal arithmetic (`0.30` is a multiple of `0.10`); `double`/`float` values are converted to `decimal` and fail when they are `NaN`, infinite, or outside the decimal range. Null collections and unsupported values are skipped.
- The generator emits the rule, reports `VAL0034` for invalid divisors, and includes the `Divisor` descriptor parameter. JSON Schema emits `multipleOf`, the three OpenAPI adapters apply it, and client rules mark `MultipleOf` as browser-evaluable.
- Added the `MultipleOf` message key to all 26 message packs (catalog now 53 keys).
- Tests: 6 fluent tests, 3 generator tests (including `VAL0034`), plus schema/client/OpenAPI assertions; full suite 886 green. Docs: rule reference, rules and messages.
- No new package.

#### `[UniqueItems]` collection deduplication rule

- Added `UniqueItemsRule<T, TElement>` plus the `[UniqueItems]` attribute and fluent `.Unique()` / `.Unique(IEqualityComparer<TElement>)` overloads for `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `List<T>`, and arrays. Duplicate elements fail with the new `UniqueItems` message key; null collections and empty collections pass.
- The default comparison uses `EqualityComparer<TElement>.Default`; a custom comparer (for example `StringComparer.OrdinalIgnoreCase`) is supported. Each rule reports once per collection.
- The source generator emits `UniqueItemsRule`, the descriptor exposes `UniqueItems`, JSON Schema emits `uniqueItems: true`, OpenAPI (Microsoft.OpenApi, Swashbuckle, NSwag) applies the same constraint, and client rules mark the rule as browser-evaluable.
- Added a `UniqueItems` entry to all 26 message packs and updated the catalog constant guard.
- Tests: 7 fluent tests (duplicates, async, case sensitivity, custom comparer, null/empty, collection shapes), 3 generated-parity tests, plus schema/client/OpenAPI assertions; full suite 877 green. Docs: rule reference, rules and messages, API reference.
- No new package.

#### Conditions on collection rules keep element paths

- Fixed `.When(...)`/`.Unless(...)` on collection rules that carry both a synchronous and an asynchronous validator: the condition now wraps the rule on both lists instead of leaving the synchronous path unguarded. The same bookkeeping is used by rule sets and dependent rules so dual rules are not left behind on one path.
- Element paths are preserved under collection conditions: child-validator errors report `Lines[0].Name`, `ForEachElement` errors report `Lines[i]`, and `OverrideIndexer` text survives the condition.
- Conditions on `ForEach`/`ForEachElement` are documented as collection-level (evaluated once for the parent) and gate both validation paths; per-element conditions belong inside the child validator.
- Tests: 9 fluent regression tests (sync/async, dual validators, `Where`, original indexes, custom indexer) and 3 generated parity tests; full suite 866 green. Docs: collections guide, rule reference.
- No new package and no public API change.

#### Reject invalid range bounds at configuration

- `InclusiveBetween`, `ExclusiveBetween`, and `Range` now reject impossible bounds when the rule is constructed: reversed bounds throw `ArgumentException`, and equal bounds throw for exclusive ranges. The checks run once at configuration time, never on the validation path.
- `BetweenRule` and `RangeRule` validate their bounds in their constructors; `ExclusiveBetweenRule` additionally requires `min < max`. Nullable bounds keep their previous behavior, and null values still skip validation.
- The generator now reports `VAL0033` (`Error`) when `[ExclusiveBetween]` has equal bounds. Reversed `[InclusiveBetween]`/`[ExclusiveBetween]`/`[ConditionalRange]` bounds continue to use the existing `VAL0010` and its swap code fix.
- Tests: 6 fluent range tests and 4 generator diagnostic tests; full suite 854 green. Docs: rule reference, diagnostics, CHANGELOG.
- No new package and no public API change; `VAL0033` is staged in the analyzer public API baseline.

#### Synchronous options validation

- Added `AddValidatedOptions<T>()` to `Jeninnet.Validation.Extensions.Validation` (service-collection and `OptionsBuilder<T>` overloads). It registers `JeninnetOptionsValidator<T>` as `IValidateOptions<T>` and calls `ValidateOnStart()`, so invalid configuration fails during startup with `OptionsValidationException`.
- Failures use `name: Member [code] message` (named options include the name), and only `ValidationErrorSeverity.Error` fails validation; warnings are ignored. Validators are resolved from a new scope per validation, and duplicate registrations are prevented with `TryAddEnumerable`.
- Missing `IValidator<T>` registrations produce an explicit failure that names the missing validator. Async-only validators surface `AsyncValidatorInvokedSynchronouslyException`; the .NET 11 async path is deferred to `1.1.0-preview.1` .
- Tests: 7 new Extensions.Validation tests (valid, invalid/code, BindConfiguration, missing validator, warning-only, named options, idempotent registration); full suite 844 green. Docs: package README, DI guide, integration-packages reference.
- No new package; additions are staged in `PublicAPI.Unshipped.txt` with AOT annotations.

#### `[SkipValidation]` on handler and controller parameters

- `ValidationEndpointFilter<T>`, `BindingValidationEndpointFilter`, and `JeninnetValidationFilter` now honor `[SkipValidation]` on minimal API handler parameters and MVC action parameters; only that parameter is skipped.
- The MVC filter also tolerates incomplete `ControllerActionDescriptor` instances (null `MethodInfo`/`ControllerTypeInfo`) instead of throwing.
- Tests: 2 new ASP.NET Core tests (generic filter, MVC controller parameter) plus the binding-filter test now covering `[SkipValidation]`; full suite 837 green. Docs: ASP.NET Core guide, attribute authoring, known limitations.
- No new public API and no package change.

#### `[Flags]` enum semantics and AOT safety

- Fixed `EnumRule` for `[Flags]` enums: any combination of declared flags and `0` now passes, while undefined bits fail. Non-flags enums keep the existing `Enum.IsDefined` behavior.
- The rule builds its bit mask once at construction from `Enum.GetValuesAsUnderlyingType` (no `Enum.GetValues` on the validation path), then performs bitwise checks only. This keeps NativeAOT/trimming safe and matches FluentValidation 12.2's AOT fix.
- Added fluent and generated tests for declared combinations, `All`, `0`, undefined bits, and nullable flags; the NativeAOT smoke harness now validates a generated `[Flags]` model.
- Full suite 835 green. Docs: rule reference and AOT/trimming.
- No new public API and no package change.

#### Container validation without a localization store

- Fixed `AddValidationCore()` so it registers the message pipeline (`IMessageLocalizer`, formatter, provider, options) without replacing `IStringLocalizerFactory` when no `ILocalizationStore` is registered. Hosts that call `AddValidation()` with `AddControllers()`/`AddRazorComponents()` no longer fail DI container validation in Development.
- When an `ILocalizationStore` is registered, the previous behavior is preserved: the store-backed `CustomStringLocalizerFactory` and `IStringLocalizer<>` are registered through `JeninnetLocalizationExtensions.AddLocalization`.
- Restored `AddValidation()` in the MVC/static SSR sample instead of the explicit static SSR validator workaround.
- Tests: 2 new tests (validated container without a store and preserved store-backed factory); full suite 824 green. Docs: DI guide, ASP.NET Core guide, known limitations.
- No API change; no new package.

### Integration and metadata parity
#### ASP.NET Core validation interceptors

- Added `IValidationInterceptor` with `BeforeValidationAsync` (returns `ValidationResult?`) and `AfterValidationAsync` (returns `ValidationResult`), plus `ValidationInterceptorContext` (model instance/type, validator name, service provider, `HttpContext`, minimal API `Endpoint`, MVC `ActionDescriptor`).
- Added `AddValidationInterceptor<T>()` (scoped by default) and an overload that takes a `ServiceLifetime`. Interceptors run in registration order for both minimal APIs (generic and binding filters) and MVC.
- A non-null `BeforeValidationAsync` result short-circuits the validator but still passes through every `AfterValidationAsync`; an invalid result produces the usual 400 problem, and a valid result lets the handler run. `AfterValidationAsync` can return a replacement result.
- Interceptors are resolved only when `IValidationInterceptor` is registered (`IServiceProviderIsService`), so hosts without interceptors keep the original path. Interceptor exceptions are wrapped in `ValidationException` with the interceptor type name; `OperationCanceledException` is rethrown unchanged.
- Tests: 13 new ASP.NET Core tests (Before short-circuit for Minimal API/MVC, valid short-circuit, After rewriting errors, registration order, no-interceptor parity, cancellation, exception wrapping, binding filter, registration lifetimes); full suite 822 green. Docs: ASP.NET Core guide, package README, integration packages reference.
- No new package; additions are staged in `PublicAPI.Unshipped.txt`. The existing `JeninnetValidationFilter(MvcValidationOptions)` constructor is preserved, and the `IEnumerable<IValidationInterceptor>` constructor is additive.

#### Advanced fluent extensibility parity

- Added context-aware `WithState` overloads: `Func<T, TProperty, ValidationContext<T>, TState>` and `Func<T, TProperty, TState>`. The factory runs when an error is emitted and populates `ValidationError.CustomState`; the existing static-state overload is unchanged.
- Added `Validator<T>.OnRuleAdded(IRuleDefinition)`, called once per leaf rule after configuration and before the composite validator is built. `IRuleDefinitionOptions` lets the hook change the default message, error code, and severity. Generated validators do not use the hook.
- Added `ValidationPath.GetParent` and `ValidationPath.IsChildOf` for member and indexer chains (`A.B[0].C`, `Items[0]`, roots, and empty paths).
- Tests: 5 new tests (sync/async state factories, rule-added count and default overrides, path helpers, and generator isolation); full suite 809 green. Docs: fluent authoring, custom rules, core API.
- No package change; additions are staged in `PublicAPI.Unshipped.txt`.

#### Model-level `Validate()`/`ValidateAsync()` extensions

- Added `ValidateAttribute.GenerateModelExtensions` (default `true`). The generator now emits a `{Type}ValidationExtensions` class in the model namespace (or `TargetNamespace`) with sync, options, async, and async-options overloads.
- The extensions hold one static validator instance, work for classes, structs, and record structs, and remain a thin convenience layer over `IValidator<T>`; no reflection and no DI is introduced. Instance members still win by normal C# resolution, and `[Validate(GenerateModelExtensions = false)]` disables the output.
- Tests: 3 generator tests (generated source for class/struct/record struct and disabled output, runtime parity for sync/struct/record, options/fail-fast/cancellation and property filtering); full suite 804 green. Docs: attribute authoring and analyzer README.
- No new package; the attribute property is staged in `PublicAPI.Unshipped.txt`.

#### Wider message packs (14 → 26 cultures)

- Added 12 built-in validation message packs: `it`, `pl`, `uk`, `ko`, `vi`, `th`, `id`, `cs`, `sv`, `da`, `fi`, and `no` (Norwegian Bokmål). Every pack carries all 51 `ValidationMessageKeys` constants with the same placeholders and parameters as English.
- The catalog gate now checks 51 constants against 26 packs; new tests verify multi-parameter formatting for every added culture and the `it-CH` → `it` → `en` fallback chain.
- Updated the localization guide, package README, root README, CONTRIBUTING, scripts documentation, ROADMAP, and package map from 14 to 26 cultures. Translations are documented as machine-assisted drafts pending native-speaker review; `no` is Bokmål and Nynorsk is out of scope.
- Docs: localization guide and package README list every culture; the contribution path remains the correction route.
- No API or package changes.

#### Static SSR client validation

- Added `JeninnetStaticSsrValidation` (Blazor package) to render standard `data-val-*` attributes and a `data-jeninnet-rules` payload from a validator descriptor, for `@attributes` on server-rendered inputs and message elements.
- Added the dependency-free `jeninnet-unobtrusive.js` ES module (client package, `./unobtrusive` export). It validates on submit, blocks invalid submissions, renders messages next to fields, clears them on input, and uses the existing client-rules evaluator; rules marked `serverOnly` never block the browser.
- The Blazor package now references ClientRules so attributes and the payload share one source of truth. Rules that cannot run in the browser remain server-only, and without JavaScript the form posts unchanged for server validation.
- Extended `samples/Jeninnet.Validation.Sample.Mvc` with a generated-validator static SSR page at `/static-ssr`, server-side re-validation, and the browser module under `wwwroot/js`.
- Tests: 4 Blazor mapping tests (required/length/range/regex/email, server-only, markers, message resolver) and 8 Node tests (payload parsing, field/form evaluation, messages, server-only, submit blocking); full .NET suite 799 green. Docs: Blazor guide, Blazor/client/sample READMEs, integration packages reference.
- No new package; the helper is staged in `PublicAPI.Unshipped.txt`, and the client module is included in the existing package.

#### Relative date attributes

- Added `[FutureDate]` and `[PastDate]` with `IncludeToday` (default `false`) for `DateTime`, `DateTimeOffset`, and `DateOnly`, including nullable members. They compare against the current UTC time instead of a fixed threshold.
- Added `FutureDateRule<T, TValue>` and `PastDateRule<T, TValue>` (Core) and two message keys (`FutureDate`, `PastDate`) in all 14 localization packs, with English and Arabic included.
- The generator emits the rule directly and resolves `TimeProvider` from `ValidationCallOptions.ServiceProvider`, falling back to `TimeProvider.System`; no reflection and no local-time conversion. Null values pass.
- Added `VAL0032` for unsupported member types (`TimeOnly`, strings, numbers, and so on). Relative-date rules are server-only and remain advisory in JSON Schema.
- Tests: 5 new generator tests (UTC boundaries and `IncludeToday`, nullable `DateTime`/`DateOnly`, injected fixed `TimeProvider`, descriptor/client rules, diagnostic); full suite 795 green. Docs: attribute-authoring, rule reference, diagnostics, and known limitations.
- No new package; the two attributes and rule types are staged in `PublicAPI.Unshipped.txt`.

#### Conditional value attributes

- Added `[RequiredIf(nameof(Other), value)]`, `[RequiredUnless(nameof(Other), value)]`, and `[ConditionalRange(nameof(Other), value, min, max)]` in `Jeninnet.Validation.Attributes`. They compare a sibling member to a compile-time constant and gate a presence or inclusive-range rule on the result, so common conditional forms need no condition method.
- The generator reuses the existing conditional wrapper (no new rule kind): strings and collections use `NotEmpty`, other members use `NotNull`, and `ConditionalRange` uses `InclusiveBetween` with inclusive bounds. String, Boolean, char, numeric, enum, date/time, `Guid`, and `TimeSpan` values are supported; numeric constants compare numerically across widths.
- Added `VAL0029` (invalid target: missing, unreadable, or self-referencing member), `VAL0030` (unsupported value), and `VAL0031` (duplicate attribute targeting the same member). Value conditions combine with method conditions using AND while preserving property-over-class precedence.
- Conditional rules are marked `serverOnly: true` in client-rules output and remain advisory in JSON Schema.
- Tests: 8 new generator tests (runtime behavior for string/bool/enum/DateTime/numeric coercion, inclusive range boundaries, descriptor shape, server-only client rules, generated wrapper, diagnostics); full suite 790 green. Docs: attribute-authoring, rule reference, diagnostics, and known limitations.
- No new package; the three attributes are staged in `PublicAPI.Unshipped.txt`.

#### `[SkipValidation]` attribute

- Added `Jeninnet.Validation.Attributes.SkipValidationAttribute` for properties, classes, structs, and parameters. A skipped property emits no generated rules (even when it carries other validation attributes), and a skipped type is not generated and is not validated through `[Nested]` or `[ForEach]`.
- The DataAnnotations bridge honors the marker on the model type and on validated members: `[JeninnetValidatable]`, `[JeninnetValidateProperty]`, and the direct adapter methods return success instead of running the validator.
- Added `VAL0028` (warning) for `[SkipValidation]` combined with validation attributes on the same property; the generator still drops every rule for that property.
- Tests: 4 new tests (generator property/type exclusion and runtime behavior, bridge class/member bypass); full suite 782 green. Docs: attribute-authoring, DataAnnotations guide, diagnostics reference, and known limitations (handler parameters).
- No new package; `SkipValidationAttribute` is staged in `PublicAPI.Unshipped.txt`.

#### Zero-scan generated validator registration

- Added `GeneratedValidatorCatalog`, a central registry populated by a generated `[ModuleInitializer]` in every assembly that declares `[Validate]` types. Each initializer registers `(model, validator, registration delegate)` pairs using `typeof` only, so registration is AOT-safe and reflection-free.
- Added `AddValidation()` (in `Jeninnet.Validation.Catalog`, one-call registration) that reads the catalog. `AddGeneratedValidators()` now prefers the catalog and falls back to assembly scanning only when it is empty, preserving the legacy path and its `[RequiresUnreferencedCode]` annotation; a counter exposes the fallback for tests.
- Updated the NativeAOT smoke sample to use `AddValidation()`; it still publishes and runs in the AOT workflow.
- Tests: 4 new tests (catalog populated by the module initializer, one-call DI resolution, catalog-preferred path without fallback, explicit fallback when the catalog is empty); full suite 778 green. Docs: installation, DI guide, core/analyzer/aot READMEs, CHANGELOG, and ROADMAP.
- No new package; public surface additions (`GeneratedValidatorCatalog`, `GeneratedValidatorRegistration`, `AddValidation`) are staged in `PublicAPI.Unshipped.txt`.

#### Swashbuckle and NSwag OpenAPI adapters

- Added `Jeninnet.Validation.OpenApi.Swashbuckle` (an `ISchemaFilter` for Swashbuckle) and `Jeninnet.Validation.OpenApi.NSwag` (an `ISchemaProcessor` for NSwag/NJsonSchema), so validator constraints reach the two most common OpenAPI stacks in addition to `Microsoft.AspNetCore.OpenApi`.
- Both adapters reuse the descriptor-to-constraint mapping (`ValidationSchemaMapper`) and the shared `ValidationSchemaRegistry`; only the target serialization is adapter-specific. Existing schema types are preserved (for example `integer` stays `integer`), and `[JsonPropertyName]` plus camel/snake/kebab naming policies are honored.
- Added 11 integration tests over real Swashbuckle `SchemaGenerator` and NJsonSchema `JsonSchemaGenerator` output (required, format, ranges, patterns, collection sizes, IP any-of formats, nested definitions, naming policies, no-validator pass-through, registration); full suite 774 green.
- Packable projects 23 → 25; README, ROADMAP, DocFX, solution, release notes, integration-packages reference, and package READMEs updated. Publication remains deferred.

### Residual engine and integration parity
#### Migration assistant (CLI/codemod)

- Added the optional `Jeninnet.Validation.Migration` dotnet-tool package (`jeninnet` command, 23rd packable package) with `migrate analyze` (Markdown/JSON report, `--check` CI gate) and `migrate apply` (`--dry-run`/`--check`, idempotent), sharing one syntax-only Roslyn engine so analysis and codemod never diverge.
- Safe codemods: `AbstractValidator<T>` to `Validator<T>` with `Configure`, single-argument `RuleFor(x => x.Name)` to `RuleFor(x => x.Name, nameof(T.Name))` (dotted paths become an `"A.B"` literal), `WithErrorCode` to `WithCode`, and `[Required]` to `[NotNull]` with an explicit `// TODO(Jeninnet):` review marker; constructors with parameters, `Custom(`, `Transform(`, and complex selectors are left untouched with `needs-review` report entries. Exit codes: 0 success, 1 pending/error, 2 usage.
- Tests: 8 engine tests (conversions, untouched patterns, line numbers, idempotency) and 6 CLI tests (report, dry-run, apply, `--check`, usage errors); full suite 760 green. Docs: FluentValidation/DataAnnotations migration guides with tool usage, package README, README, ROADMAP, and CHANGELOG.

#### Optional input sanitization pipeline

- Added opt-in sanitization preprocessing that runs before validation rules: `Sanitize` pure string functions (`Trim`, invariant/cultured casing, `NormalizeWhitespace`, `RemoveControlChars`, `EachElement`), `SanitizeOptions` flags, `ISanitizer<T>`/`ISanitizationStep<T>`, and the `Sanitizers` factory.
- Added `[Sanitize]` (property/class, not a validation attribute) honored by the source generator: per-property or class-wide member sanitizers are emitted into the generated validator and passed to `CompositeValidator`, which runs them at the start of every validate path after the async-content gate.
- Rejected clearly: `[Sanitize]` on members without an accessible non-init setter, on non-string members, or with an unknown culture fails the build with new diagnostic `VAL0027` (Error); fluent `.Sanitize()` without a setter and element-level `.Sanitize()` throw `InvalidOperationException`.
- Sanitizers mutate the model in place, run even when rule-set filtering selects no rules, never appear in descriptors or client/JSON payloads, and cost a single branch when unused; `SanitizeOptions.ToUpper` wins over `ToLower` and the invariant culture is the default.
- Tests: 19 runtime tests (funcs, options, ordering, exemptions, wrapping, exclusion, paths), 8 generator tests (metadata, emission, `VAL0027` cases), 3 end-to-end generated-validator tests (compile, mutate, still-invalid, descriptor exclusion); full suite 746 green. Docs: rules-and-messages guide, rule reference, known limitations, diagnostics reference (table + `VAL0027` section), package/generator docs via README and ROADMAP, and CHANGELOG.

#### Message-key catalog at compile time

- Added a build-time message-key gate: `scripts/localization/check-message-keys.ps1` compares the 49 `ValidationMessageKeys` constants against all 14 packs per culture and fails on missing entries, extra entries, stale allow-list entries, and non-string or empty templates.
- Added `MessageKeyAllowList.json` (initially empty) for pack entries that are deliberate without a constant; custom consumer keys (rule-template names, caller-supplied codes, resx keys) stay outside the catalog by design.
- Added a deterministic `artifacts/message-keys/report.json|md` report and a CI `message-keys` job that runs the check and uploads the report.
- Added 8 `MessageKeyCatalogTests` (repo-catalog cleanliness, allow-list hygiene, missing/extra/allow-list/stale/determinism/uniqueness); the comparison also runs in CI inside the test suite.
- Protected `scripts/localization` in the clean script and solution; no runtime code changes, so no AOT, trimming, or public-API impact.
- Tests: 8 new message-key tests; full suite 716 green. Docs: localization package README, localization guide, docs/CONTRIBUTING, scripts READMEs, ROADMAP, and CHANGELOG.

#### Rule Template API for rule authors

- Added immutable `RuleTemplate`, `RuleTemplate<TValue>`, and `RuleTemplate<T, TValue>` with name, message, message key, severity, error code, and named static plus value-dependent parameters; `RuleFor(...).Rule(template)` applies them.
- Templates execute through an internal `SyncValidateRule` so per-rule `WithMessage`/`WithCode`/`WithSeverity`, `When`/`Unless`, `RuleSet`, and cascade options work without extra code; dynamic parameters evaluate only on failure.
- Descriptors expose the template name as the rule kind with static parameters (dynamic names appear with `null`); client rules export templates as `serverOnly: true` advisory entries.
- Fixed `IRuleMetadataProvider.ErrorCode` to return the rule default when no per-rule override exists, so template codes appear in descriptors.
- Tests: 14 new rule-template tests (valid/invalid, instance-aware predicates, overrides, null handling, immutability, dynamic parameters, `When`/`RuleSet` composition, descriptor, client rules, culture formatting, constructor validation); full suite 708 green. Docs: custom-rules guide, rule reference, metadata, API reference, README, ROADMAP, and CHANGELOG.

#### Public API baselines and API reference

- Added `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` to all 22 packable projects using `Microsoft.CodeAnalysis.PublicApiAnalyzers` 3.3.2; the reviewed current surface is shipped and future additions stage in `Unshipped`.
- Scoped the analyzer to projects whose final `IsPackable` value is `true` by moving the reference to `Directory.Build.targets`; samples are explicitly non-packable.
- Added the `RS0016`/`RS0017` build gate plus `scripts/api/check-public-api-baselines.ps1`, `scripts/api/test-public-api-gate.ps1`, and `scripts/api/sync-public-api.ps1`; the behavior test runs in a dedicated `api-baselines` CI job.
- Documented the obsolete/breaking-change policy across compatibility, contribution, API-reference, and script docs: deprecate in a minor, remove in a major.
- Added the missing `Jeninnet.Validation.Extensions.Validation` project to `docs/docfx.json`; the documentation workflow now generates DocFX metadata, builds the site, and verifies all 22 packable assemblies are represented.
- Added architecture guards for baseline presence, encoding, ordinal sorting, duplicates, analyzer wiring, and DocFX coverage.
- Tests: 4 new architecture guards; full suite 694 green. Docs: CONTRIBUTING, docs/CONTRIBUTING, compatibility, API reference, scripts README, scripts/api README, ROADMAP, and CHANGELOG.

#### Observability and ProblemDetails

- Added the optional `Jeninnet.Validation.Diagnostics` package with an `ActivitySource` and `Meter` named `Jeninnet.Validation`.
- Added `validation.duration` (histogram, ms) and `validation.errors` (counter) with model type, validator, async, rule-set, outcome, and error-count tags; messages and property values never reach tags.
- Added `ValidationDiagnosticsOptions` for tracing/metrics toggles, source names, version, sampling probability, and rule-set labels.
- Added the `IValidationDiagnosticsSink` hook in Abstractions so the core takes one null check when diagnostics are disabled.
- Added opt-in ProblemDetails enrichment (`correlationId`, `modelType`, `durationMs`) in the AspNetCore package for minimal API, MVC, and exception-handler responses.
- Tests: 8 new diagnostics/AspNetCore tests; full suite 690 green. Docs: production checklist, performance, ASP.NET Core guide, API reference, integration packages, package map, README, and ROADMAP.

#### Analyzer family, phase 2

- Added `VAL0023` (`RuleFor` member-name mismatch) with a safe code fix that rewrites the argument to `nameof(Model.Member)`. Only members that exist on the model are reported; custom paths and free-form names stay silent.
- Added `VAL0024` (collection rule on a non-collection) for fluent `Count`/`MinCount`/`MaxCount` and the matching attributes. Strings and scalar types are reported; `object`, `dynamic`, and unconstrained type parameters are ignored to avoid false positives.
- Added `VAL0025` (async-only validator in the synchronous DataAnnotations adapter) for `[JeninnetValidatable]`, `[JeninnetValidateProperty]`, and the generic property attribute where the validator type is statically known.
- Documented `VAL0026` (unregistered generated validator) as deferred: registration may live in another assembly/startup and `AddGeneratedValidators` registers automatically, so no safe static proof exists.
- ID note: `VAL0018`–`VAL0022` were already used by generator diagnostics; the new analyzers use the next free IDs instead of reusing them.
- Tests: 8 new analyzer/code-fix tests; full suite 682 green. Docs: diagnostics reference, analyzer README, AnalyzerReleases, README, and ROADMAP.

#### Localized domain rule packs

- Added the optional `Jeninnet.Validation.Rules.Domain` package with `Iban`, `VatNumber`, `PostalCode`, `NationalId`, `PasswordStrength`, `Uuid`, and `Ulid` rules, plus matching attributes and fluent extensions.
- Added explicit Dutch support: `Iban("NL")`, `VatNumber("NL")` (btw-id with elfproef), `PostalCode("NL")` (including the `SA`/`SD`/`SS` exclusions), and `NationalId("NL")` (BSN with elfproef).
- Added structural support for VAT (`NL`, `DE`, `FR`, `GB`, `BE`), postal codes (`NL`, `US`, `DE`, `FR`, `GB`, `BE`, `EG`), national IDs (`NL`, `EG`, `US`), and the IBAN registry. Unsupported countries fail explicitly.
- Extended the source generator (`RuleKind`, templates, attribute/rule mappings, emitters, client catalog) so domain attributes and fluent rules share descriptors, JSON Schema/OpenAPI constraints, and ClientRules payloads; checksum rules stay `serverOnly` on the client.
- Added `PasswordPolicy` presets and custom policies with `Policy`, `MinLength`, and `MissingRequirements` message parameters.
- Added `nl` to the built-in localization packs (14 cultures) and the seven new message keys to every pack; the Dutch pack covers all built-in keys.
- Tests: 12 new domain/generator tests (rules, attributes, descriptors, ClientRules, JSON Schema, Dutch localization); full suite 674 green.
- Docs: rule reference, rules-and-messages, localization, metadata, schema/OpenAPI/client guide, known limitations, package map, README, ROADMAP, and CHANGELOG.

#### Unified message placeholder contract

- Added `MessageParameterNames` in Abstractions with stable keys for `PropertyName`, `PropertyValue`, `PropertyPath`, `ComparisonValue`, `ComparisonProperty`, `Min`, `Max`, `From`, `To`, `TotalLength`, `Length`, `ExactLength`, `Pattern`, `Extensions`, `Values`, `EnumType`, `Precision`, `Scale`, `Count`, `CollectionIndex`, and `Value`.
- Every emitted error now carries `PropertyName`, `PropertyValue`, and `PropertyPath`; display-name resolvers still control `PropertyName`, while `PropertyPath` always tracks the full nested/indexed member path.
- Rule families add semantic aliases alongside the legacy positional keys: comparison rules add `ComparisonValue`, property comparisons add `ComparisonProperty`, ranges add `Min`/`Max`/`From`/`To`, length rules add `TotalLength`/`Length`/`ExactLength`, and the remaining families add `Pattern`, `Extensions`, `Values`, `EnumType`, `Count`, `Precision`, and `Scale`.
- `ForEach`/`ForEachElement` and nested validators propagate the full `PropertyPath` and `CollectionIndex` through the mapped errors.
- Client-rule metadata and the generated client catalog export the same semantic parameter names; positional aliases remain for compatibility.
- Tests: 3 contract tests covering rule parameters, localization templates, client payloads, collection indexes, and legacy positional keys; full suite 662 green.
- Docs: rule reference, rules-and-messages guide, localization guide, metadata reference, CHANGELOG, ROADMAP, coverage, and samples.

#### Message packs beyond EN/AR

- Expanded `Jeninnet.Validation.Localization` from 2 to 13 cultures: `en`, `ar`, `fr`, `es`, `de`, `tr`, `ur`, `fa`, `hi`, `zh-Hans`, `ja`, `pt-BR`, and `ru`.
- The provider discovers every embedded `Resources/*.json` pack automatically, checks the exact culture name first, then the two-letter language code, and preserves script/region packs such as `zh-Hans` and `pt-BR` while `fr-FR` resolves through `fr`.
- `ValidationMessagePack` and `ValidationMessagePackOptions` now preserve the normalized culture name instead of collapsing to two letters, so `pt-BR`/`zh-Hans` overrides target the correct pack.
- Catalog tests enforce key completeness, placeholder parity with English, non-empty values, absence of replacement characters, and RTL script presence for `ar`, `ur`, and `fa`.
- Added a contribution path for new languages, including native-speaker review and placeholder rules.
- Tests: 6 new localization tests; full suite 659 green.
- Docs: localization guide and cookbook, package README, integration reference, package map, known limitations, README, ROADMAP, coverage, samples, and CHANGELOG.

#### ASP.NET Core IStringLocalizer bridge

- Added `Jeninnet.Validation.Localization.AspNetCore` with `AddJeninnetStringLocalizer()`, `IValidationMessageResolver`, and `StringLocalizerValidationMessageResolver`.
- Resolution order is `IStringLocalizer` → `IMessageLocalizer` packs → `ValidationError.Message`; missing resources are detected through `LocalizedString.ResourceNotFound`.
- Added `ForceCulture`, `DefaultCulture`, `DisableLocalization`, and `ResourceType` options. Culture control is scoped to the lookup; `CurrentUICulture` is restored immediately and `CurrentCulture` is never changed.
- The bridge is a reader and never registers a second `IInnerMessageProvider`; it also supports async resolution of error collections and cancellation.
- Resx templates use named placeholders (`{PropertyName}`, `{Min}`, ...); the resolver uses a registered culture-aware formatter when present and a small named-placeholder fallback otherwise.
- Tests: 15 new tests covering resx en/ar, formatting, pack fallback, message fallback, missing keys, force/default/disable culture, cancellation, and single-provider registration; full suite 653 green.
- Docs: localization guide and cookbook, package READMEs, integration reference, package map, compatibility, known limitations, README, ROADMAP, coverage, samples, and CHANGELOG.

#### gRPC and HotChocolate GraphQL adapters

- Added `Jeninnet.Validation.Grpc` with `ValidationInterceptor` for unary and server-streaming calls, `InvalidArgument` status, and `google.rpc.BadRequest` details in the `grpc-status-details-bin` trailer.
- Added `Jeninnet.Validation.GraphQL` with HotChocolate field middleware that validates input-object arguments and returns GraphQL errors with `extensions.member`/`extensions.code`.
- Both adapters aggregate multiple validators, use the async validation path, forward cancellation, support missing-validator bypass, and expose rule-set attributes/options.
- Tests: 10 new integration tests (5 gRPC, 5 GraphQL); full suite 638 green.
- Docs: new gRPC and GraphQL guides, package READMEs, integration reference, package map, known limitations, README, ROADMAP, coverage, samples, and CHANGELOG.

#### MediatR structured result adapters

- Added `IValidationResultMapper<TResponse>`, `ValidationErrorFormatter`, and `MediatRValidationFailureStrategy.Result`.
- Added `MediatRValidationOptions.UseResultAdapter<TResponse>()`; the Result strategy maps failures and skips the handler, while Throw and Continue remain unchanged.
- Added separate optional packages `Jeninnet.Validation.MediatR.ErrorOr`, `.FluentResults`, and `.CSharpFunctionalExtensions` with convenience registration extensions.
- ErrorOr and FluentResults preserve member name, severity, code, message key, parameters, and custom state in metadata; CSharpFunctionalExtensions preserves the details textually.
- Tests: 8 new tests; full suite 628 green.
- Docs: MediatR package README, guide, cookbook, integration reference, package map, compatibility, known limitations, README, ROADMAP, coverage, samples, and CHANGELOG.

#### DataAnnotations property-level adapter

- Added `[JeninnetValidateProperty]` (non-generic and generic syntax) for property/field-level validation in DataAnnotations pipelines.
- The attribute reuses `ValidationCallOptions.IncludeProperties` to run only the named member's rules, including nested/indexed paths that match the validator's rule-group name.
- Class-level `[JeninnetValidatable]` wins when both attributes are present, preventing double validation.
- Added `JeninnetDataAnnotationsAdapter.ValidateProperty` and unwrapped reflective invocation failures so async-only validators surface `AsyncValidatorInvokedSynchronouslyException` directly.
- Tests: 8 new tests; full suite 620 green.
- Docs: DataAnnotations package README, guide, integration reference, known limitations, README, ROADMAP, coverage, samples, and CHANGELOG.

#### INotifyDataErrorInfo adapter

- Added `Jeninnet.Validation.Observable` with `ObservableValidationAdapter<TModel>` for WPF, MAUI, and Avalonia view models.
- Maps root, nested, and indexed member paths to `INotifyDataErrorInfo.GetErrors` and raises `ErrorsChanged` for affected paths and their ancestors.
- Added `ObservableValidationOptions` with opt-in property-change validation, async validation, debounce, `SkipIfValid`, and `MinimumSeverity` filtering.
- Async validation cancels in-flight runs and discards superseded results; `PendingValidation` exposes the current property-change run.
- Added `GetValidationErrors` for severity/code/message-key-aware UIs and `ClearErrors` for composed validation flows.
- Tests: 10 new tests; full suite 612 green.
- Docs: new INotifyDataErrorInfo guide and cookbook, package README, integration reference, package map, compatibility, known limitations, README, ROADMAP, coverage, samples, and CHANGELOG.

#### Asynchronous Blazor validation

- Added `EditContextValidationCoordinator<TModel>.ValidateAsync()` and `JeninnetValidator.ValidateAsync()` for async-only validators and `MustAsync`/`CustomAsync` rules.
- Added `EditContextValidationOptions` with `ValidateOnFieldChanged`, `UseAsyncValidation`, `FieldChangedDebounce`, and `SkipIfModelStateValid`.
- Newer validations cancel in-flight ones; superseded results are discarded, and field-change validation can be debounced (opt-in, async only).
- `SkipIfModelStateValid` provides the double-validation toggle by skipping an unchanged, already-valid model.
- Async mode uses the `EditForm.OnSubmit` path; `EditContext.Validate()` throws a guidance exception instead of silently blocking.
- Tests: 8 new tests; full suite 602 green.
- Docs: Blazor package README, guide, cookbook, known limitations, integration reference, README, coverage, samples, and CHANGELOG.

#### Minimal API binding-source validation

- Added non-generic `AddRequestValidation()` for minimal API endpoints. It reads handler parameter metadata and validates body, query, route, and header arguments that have registered validators.
- Non-body errors are keyed by binding source (`query.page`, `route.id`, `header.code`); body members keep their existing names.
- Added `[DisableValidation]` and parameter-level `[SkipJeninnetValidation]`, and passed endpoint `[JeninnetValidationRuleSets]` through to validators.
- Typed `AddRequestValidation<T>()` remains backward-compatible, now also honors endpoint skip metadata and rule sets.
- Added HTTP integration tests through `WebApplication`/`TestHost` for all binding sources, skips, and rule-set propagation.
- Tests: 3 new tests; full suite 594 green.
- Docs: ASP.NET Core guide, minimal API cookbook, package README, known limitations, coverage, samples, and CHANGELOG.

#### Microsoft.Extensions.Validation bridge

- Added `Jeninnet.Validation.Extensions.Validation` with `AddJeninnetValidation()` and `AddJeninnetValidation<TModel>()`.
- The bridge implements `IValidatableInfoResolver` and `IValidatableInfo`, runs validators on the async path, and writes errors into the host `ValidateContext` dictionary.
- Generic registration is reflection-free/AOT-safe; the non-generic overload discovers model types from existing `IValidator<T>` registrations.
- Models using `[JeninnetValidatable]` are skipped to avoid double validation with the DataAnnotations bridge.
- Minimal API integration is covered by a `WebApplication` + `TestHost` test asserting `ValidationProblemDetails` member paths.
- Tests: 5 new tests; full suite 591 green.
- Docs: ASP.NET Core guide, integration package reference, package README, README, coverage, samples, and CHANGELOG.

#### Client runtime evaluator and contract drift gate

- Added the dependency-free `@jeninnet/validation-client` ESM package with `createValidator`, `validate`, TypeScript declarations, and `en`/`ar` message packs.
- The client evaluates the supported synchronous rule subset and reports `needsServer` for conditional, nested, custom, asynchronous, or unsupported rules.
- Client-rules payloads now mark unsupported/conditional rules with `serverOnly: true`; the build-time catalog emits the same marker.
- Added a C# golden-payload snapshot test and a JavaScript test suite using Node's built-in test runner, including a no-`eval`/`Function` guard.
- The analyzer NuGet package now ships a `build` props file that enables `JeninnetEmitClientRules` and `CompilerVisibleProperty` automatically; consumers can opt out with `false`.
- CI gains a Node client-runtime job; `client/**` changes trigger the workflow.
- Tests: 3 new C# tests and 6 client-runtime tests; .NET suite 586 green.
- Docs: ClientRules README, schema/OpenAPI guide, cookbook, known limitations, coverage, samples, and CHANGELOG.

#### OpenAPI naming-policy awareness

- The OpenAPI transformer now resolves descriptor member names through the active `JsonSerializerOptions`/`JsonTypeInfo`, covering `camelCase`, `snake_case`, `kebab-case`, `PascalCase`, and `[JsonPropertyName]`.
- Naming maps are cached per `JsonTypeInfo`; unresolved members fall back to the previous case-insensitive matching.
- Nested object and collection item composition uses the same serializer-aware names, including required entries.
- The mapping is limited to schema emission; runtime validation errors keep the CLR `MemberName`.
- Tests: 2 new OpenAPI tests; full suite 583 green.
- Docs: OpenAPI README, schema/OpenAPI guide, known limitations, coverage, samples, and CHANGELOG.

#### Schema composition depth

- JSON Schema now composes `Nested` validators through deterministic `$defs`/`$ref` entries and `ForEach` validators through `items`.
- Added a composition cycle guard so recursive descriptors reuse a definition instead of overflowing.
- Descriptor rules can carry a `NestedDescriptor`; nested and collection rules populate it from child validators or inline `ChildRules`.
- `required` now respects member types: non-nullable value-type members are not marked required merely because of `NotNull`/`NotEmpty`.
- Conditional rules are emitted as advisory entries with `hasCondition: true` because conditions are opaque delegates, not inferable schema predicates.
- `Enum` emits `x-enum-type`; `IPAddress` uses `anyOf` IPv4/IPv6; `FileExtensions` remains the documented vendor extension.
- OpenAPI composes collection item schemas and nested object constraints recursively, and emits the `x-enum-type` extension.
- Tests: 4 new JSON Schema tests and 2 new OpenAPI tests; full suite 581 green.
- Docs: schema/OpenAPI guide, package READMEs, metadata, known limitations, README, coverage, samples, and CHANGELOG.

#### Generator type and determinism coverage

- `[Validate]` now supports `struct` and `record struct` models in addition to classes and interfaces.
- Open generic model declarations report `VAL0022` and emit no invalid validator source.
- `[After]` / `[Before]` now parse against the actual property type (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`) with `CultureInfo.InvariantCulture`.
- `DateTimeOffset` values without an explicit offset are normalized to UTC so generated output is independent of build-machine time zones.
- Invalid date/time literals report `VAL0021` instead of silently falling back to `DateTime.Now`.
- Generated date/time literals use invariant parsing and explicit `global::System` types (`DateOnly`, `TimeOnly`, `DateTimeOffset`).
- Attribute constants are formatted invariantly, so generated output is identical under different build cultures.
- Tests: 6 new tests; full suite 575 green.
- Docs: attribute authoring, rule reference, diagnostics, known limitations, README, coverage, samples, and CHANGELOG.

#### Generated descriptor completeness

- Generated validators now build `Describe()` from the actual runtime rule lists on first use and cache it per instance. Rules added through `AddCustomValidators()` are included instead of being missed by a static snapshot.
- Added `ValidationDescriptorBuilder` and `ValidationMemberDescriptorBuilder`. Generated validators expose a `ConfigureDescriptor(ValidationDescriptorBuilder)` partial hook for explicit metadata on custom rules.
- Rules with no metadata provider are recorded as `Custom` advisories with `Reason` and `Type`, never dropped silently.
- Conditional, rule-set, and dependent wrappers are flattened to the rules that actually run: `HasCondition`, `RuleSets`, and `Dependent` parameters are propagated.
- Descriptors now expose typed parameters (`Min`/`Max` as integers, flags as booleans), `ErrorCode`, `DisplayName`, `Severity`, and `CustomState`, with deterministic member/rule ordering.
- The build-time client-rules catalog now emits typed values and mirrors runtime parameter names/message keys for length, file extensions, membership, enum, scale/precision, date, and property comparison rules.
- Property comparison rules expose the other member through `Parameters["Other"]`.
- The NativeAOT smoke harness now calls `Describe()` and verifies a custom rule appears.
- Tests: 9 new tests; full suite 569 green.
- Docs: metadata, attribute authoring, known limitations, README, and CHANGELOG.

#### Generated DI registry

- The source generator now emits a per-assembly `GeneratedValidatorRegistry` plus an `AddGeneratedValidatorsFromThisAssembly()` extension in `<assembly name>.GeneratedValidation`. Registration uses `typeof` references, not `Assembly.GetTypes()` or interface reflection.
- Added `AddValidationCore()` for reflection-free validator infrastructure (factory, service-provider forwarding, localization).
- Generated registrations are scoped by default, idempotent through `TryAdd`, cover `IValidator<T>`, `ISyncValidator<T>`, and `IAsyncValidator<T>`, and resolve the same validator instance through all interfaces.
- Reflection-based `AddGeneratedValidators(...)` overloads remain for compatibility and keep their `[RequiresUnreferencedCode]` annotations.
- The NativeAOT smoke harness now registers through the generated registry.
- Tests: 1 new generated-registry test; full suite 560 green.
- Docs: AOT/trimming, dependency injection, known limitations, sample README, README, and CHANGELOG.

#### Rule semantics parity

- Added `NotDefault()` for value types, `Guid`, `bool`, and `DateTime` defaults. `NotEmpty` keeps its existing semantics and continues to treat `0`, `false`, and `default(DateTime)` as non-empty.
- Added `Equal`/`NotEqual` overloads accepting `IEqualityComparer<T>` and `StringComparer`; the default comparer remains unchanged.
- Added `ScalePrecision` `ignoreTrailingZeros` support to the fluent API, `[ScalePrecision]`, generated rules, and metadata.
- Corrected `IPAddress` schema output from `format: ipv4` to `anyOf` `ipv4`/`ipv6` in JSON Schema and OpenAPI.
- Documented the single built-in email validation mode and the clarified emptiness/equality semantics.
- Tests: 8 new tests; full suite 559 green.
- Docs: rule reference, rules/messages, known limitations, coverage, samples, and CHANGELOG.

#### Rule-set selection parity

- Added `ValidationStrategy.IncludeAllRuleSets()`, `IncludeRulesNotInRuleSet()`, and `ThrowOnFailures()`; the equivalent `ValidationCallOptions` flags are `IncludeAllRuleSets` and `ThrowOnFailures`.
- `RuleSets = null` and empty collections now have one documented meaning: all rules run, including named sets. `["default"]` selects ungrouped rules only.
- Added a `RuleSet` named property to `ValidationAttribute` for per-rule grouping; it overrides property-level and class-level `[RuleSet]`. Multiple `[RuleSet]` attributes are now allowed per scope.
- Added the fluent per-rule `.RuleSet("name")` extension for wrapping the immediately preceding rule.
- `ThrowOnFailures` throws `ValidationException` with every collected error after hooks run; it remains independent of `FailFast`.
- Descriptors and client rules now expose rule-set names through `Parameters["RuleSets"]`; fluent rule-set groups are flattened to their real rules.
- Tests: 11 new tests; full suite 551 green.
- Docs: rule sets/cascade, attribute authoring, core API, known limitations, coverage, samples, and CHANGELOG.

#### Custom and async attribute rules

- Added `[CustomValidator(typeof(Rules), nameof(Rules.Method), IsAsync = ...)]` for property and class-level custom rules. Methods must be static and return `bool`/`ValidationResult` (sync) or `ValueTask<bool>`/`ValueTask<ValidationResult>` (async).
- Generated rules call the method directly without reflection, pass the caller-supplied `IServiceProvider` when requested, and preserve explicit error paths from `ValidationResult`.
- Async custom rules run only through `ValidateAsync`; all-async validators emit `[AsyncOnlyValidator]` and throw on synchronous `Validate`.
- Added `VAL0020` for missing or invalid custom validator methods.
- Fixed the generated `[AsyncOnlyValidator]` marker to reference the public `Jeninnet.Validation` namespace.
- Tests: 7 new tests; full suite 540 green.
- Docs: attribute authoring, custom rules, diagnostics, known limitations, coverage, samples, and CHANGELOG.

#### Attribute conditions and lifecycle hooks

- Added `[WhenCondition]`/`[UnlessCondition]` for synchronous attribute conditions. Methods may be instance or static, return `bool`, and accept `(T)` or `(T, ValidationContext<T>)`; property-level conditions override class-level conditions on both sync and async paths.
- Added `[PreValidate]` and `[OnFailure]` static lifecycle hooks for generated validators. `PreValidate` supports `(T, ValidationContext<T>)` and `(T, ValidationContext<T>, ValidationErrorCollector)`; pre-validation errors merge with rule errors, and returning `false` stops rule evaluation.
- Generated validators call hooks directly without reflection; `OnFailure` runs once after the merged failure result is known.
- Added `ConditionalRule<T>` context-aware condition support and `VAL0018`/`VAL0019` diagnostics for invalid condition methods and hooks.
- Tests: 8 new tests; full suite 533 green.
- Docs: attribute authoring, conditions, lifecycle hooks, diagnostics, known limitations, coverage, samples, and CHANGELOG.

#### Declarative rule metadata

- Added named properties to `ValidationAttribute`: `Severity`, `ErrorCode`, `DisplayName`, `OverridePropertyName`, and static string `CustomState`.
- Generated rules now receive the metadata through `ValidationRuleMetadata.Apply`; no reflection is involved.
- Added the `[Display(Name = ...)]`/`[DisplayName]` bridge to `{PropertyName}`; a rule-level name override or `DisplayNameResolver` takes precedence.
- Extended `IValidationRuleDescriptor` and `ValidationRuleDescriptor` with `ErrorCode`, `DisplayName`, and `CustomState`.
- Client rules now emit `errorCode`, `displayName`, and `customState`; JSON Schema advisory rules emit the same fields plus non-default `severity`.
- Tests: 4 new tests; full suite 525 green.
- Docs: attribute authoring, metadata, core API, known limitations, and CHANGELOG.

#### Dynamic messages and display names

- Added `WithMessage(Func<T, string>)` and `WithMessage(Func<T, ValidationContext<T>, string>)`; factories run only when a rule fails and blank results fall back to the static/default message.
- Added `WithName(Func<T, string>)` for display-name-only overrides and `OverridePropertyName(Func<T, string>)` for member-name overrides. Dynamic `WithName` leaves `MemberName` unchanged.
- Added `ValidationOptions.DisplayNameResolverForInstance` receiving `(Type, memberName, instance)`; it takes precedence over the legacy resolver and preserves the old signature.
- Dynamic options apply to leaf and composite rules (`SetValidator`, `ForEach`, `ChildRules`, and `ForEachElement`); factories receive the parent instance/context.
- Localization precedence is unchanged: a message-pack template for `MessageKey` wins over static and dynamic messages.
- Tests: 11 new tests; full suite 521 green.
- Docs: rules/messages, localization, core API, cookbook, known limitations, and CHANGELOG.

#### Collection element rules and path filtering

- Added `ForEachElement` for direct value rules on array, `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, and `List<T>` members. Element rules validate `null` elements instead of silently skipping them.
- Added `Where`/`WhereAsync` and `OverrideIndexer` support for element rules; original indexes are preserved and `CollectionIndex` is attached to element errors.
- Added wildcard sub-path matching for `IncludeProperties`/`ExcludeProperties`: `Orders[].Cost`, `Orders[*].Cost`, and nested paths flow through `SetValidator`, `ForEach`, and `ChildRules`. Numeric indexes and custom indexers normalize to the same collection segment.
- `ForEach` child-validator errors now carry `CollectionIndex` as well.
- The unfiltered `SelectRules` path remains allocation-free (covered by the allocation test).
- Tests: 11 new tests; full suite 510 green.
- Docs: collections guide, rule reference, known limitations, and CHANGELOG.

#### Inheritance and runtime-type dispatch

- Generated validators for `[Validate]` derived classes now include base-class property rules in deterministic base-first order; overridden properties contribute their base rules once.
- `[Validate]` is now allowed on interfaces. An annotated interface contributes matching property rules to annotated implementing classes; matching is by public member name and type.
- Added `IncludeBase` for composing a base-type or interface validator inside a derived fluent validator.
- Added `InheritanceValidator<T>` and `InheritanceValidatorMap<T>` for explicit runtime-type dispatch: exact-type lookup, unknown types skipped, no reflection or activation on the validation path.
- Added `SetInheritanceValidator` and polymorphic `ForEach` overloads for arrays, `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, and `List<T>`.
- Fixed `SetValidator`, `Include`, `IncludeBase`, and `ChildRules` so async-capable nested validators are registered on the asynchronous path; descriptor generation now deduplicates rules registered on both paths.
- Tests: 19 new tests; full suite 499 green.
- Docs: new inheritance guide; updated fluent, attribute, collections, core API, known limitations, and navigation.

#### Dead null policies and orphan rules removed

- Deleted the unused `Jeninnet.Validation.Policies` namespace: `INullPolicy<T>`, `NullPolicy`, and the four built-in policy implementations. No production path ever resolved them, so the advertised nullability-aware policy feature did not exist.
- Deleted the orphan `AfterOffsetRule`, `BeforeOffsetRule`, and `ComparableRule` types and their dedicated tests; no public overloads emitted them and `After`/`Before` already cover the supported date types.
- Corrected the API reference and the rules guide so they no longer list the removed namespace or claim offset rule variants.
- Recorded the stale historical comparison matrix row 21 claim in the implementation record; the historical matrix file is intentionally left unchanged.
- Full suite: 480 green (11 files removed).

#### Executable conformance suite

- Replaced the simulated `ParityTests` harness with `GeneratedConformanceTests`: a real source-generated validator and a hand-written fluent validator for the same model are compiled together and compared across a 23-case matrix.
- The matrix covers presence, comparison, string shape/format, date, enum, property comparison, collection count, null handling, multi-error, and all-invalid cases; it compares validity, error count, member names, message keys, error codes, severities, and custom state.
- Fixed the generator's `[Contains]`, `[StartsWith]`, and `[EndsWith]` attribute emission (`RuleNames`/`RuleKindExtensions`/`RuleExpressionEmitter`).
- Full suite: 482 green.

#### Rule option coverage across rule kinds

- `WithMessage`, `WithCode`, `WithSeverity`, `WithState`, and `WithName`/`OverridePropertyName` now apply to leaf rules, `SetValidator`, `ForEach`, and `ChildRules` composite rules.
- Composite options are applied to every emitted error; `WithName` replaces the member name for each error.
- Options applied after `When`/`Unless` or `DependentRules` throw `InvalidOperationException` instead of silently doing nothing.
- Tests: 4 composite-option tests; full suite 486 green.
- Docs: rules/messages guide, README, and CHANGELOG.

#### Nested context propagation and error fidelity

- Added `IValidationContextValidator<T>`; `CompositeValidator<T>`, `Validator<T>`, and generated validators implement it.
- Nested (`SetValidator`), collection (`ForEach`), and included (`Include`) validators now inherit `Root`, `RootData`, `ServiceProvider`, `RuleSets`, `FailFast`, `MemberPath`, and the parent class-level cascade mode.
- `NestedValidatorRule` and `ForEachRule` preserve `CustomState`, `ErrorCode`, `Severity`, and `MessageParameters` when copying legacy errors.
- Context-aware error paths are normalized: full paths are kept; relative paths from custom rules are prefixed exactly once.
- Tests: 3 nested/collection context tests + 1 generated-validator context-path test; full suite 482 green.
- Docs: core API, collections/nesting, dependency injection, README shared-context row, and CHANGELOG.

#### Generated validator execution parity

- Generated `[Validate]` validators now implement `IValidationOptionsValidator<T>` and expose the same per-call execution surface as `Validator<T>`: `ValidationCallOptions`, `ValidationStrategy`, `ClassLevelCascadeMode`, fail-fast, property/rule-set filtering, `ServiceProvider`, and `ValidateAndThrow*`/`ValidateAndThrowAsync*`.
- `CompositeValidator<T>` implements the new contract; `IValidationMemberRule` lets generated flat rules participate in `IncludeProperties`/`ExcludeProperties`.
- `ValidationOptionsExtensions` provides options/strategy/throw overloads for any `IValidator<T>` and throws `NotSupportedException` when a validator does not implement the options contract instead of silently ignoring options.
- Fixed `[RuleSet]` and async-condition attribute recognition in the generator (`GetAttributeTypeName` normalization).
- Tests: 10 real source-generated validator execution tests (options, strategy, rule sets, filtering, fail-fast, class cascade, service provider, async, throw) plus the existing 468-test baseline.
- Docs: README feature table, core API, rule sets/cascade guide, known limitations, and troubleshooting.

### Execution correctness and repository truthfulness

#### Strict-async guard

- Strict-async guard: `CompositeValidator.Validate` now throws `AsyncValidatorInvokedSynchronouslyException` when a validator carries asynchronous rules but no synchronous content, instead of returning a silent `ValidationResult.Success`. Truly empty validators still return `Success`; mixed sync/async validators are unchanged.
- New public API in `Jeninnet.Validation.Abstractions`: `AsyncValidatorInvokedSynchronouslyException` (validator type name + async rule count) and `[AsyncOnlyValidator]` marker attribute (emitted by the source generator for async-only generated validators).
- New analyzer diagnostic `VAL0016` (Warning): synchronous `Validate` / `Validate(instance, ruleSets)` on an `[AsyncOnlyValidator]`-marked type warns at the call site; no code fix (the analyzer backlog).
- `CompositeValidator<T>` constructor accepts an optional `validatorName` used in the exception message; fluent and generated validators pass their concrete type name.
- Migration note: [strict async contract](docs/en/02-guides/15-migration-from-fluentvalidation.md).
- Tests: 8 core guard tests + 4 analyzer tests; full suite 246 + 16 green.

#### Generated custom-rule wiring

- Fixed generated-validator custom-rule wiring: the emitted constructor now invokes `AddCustomValidators()` before constructing `CompositeValidator`, so rules added in the partial hook execute. Previously the composite snapshotted its rule lists first and every custom rule was silently ignored.
- Regression coverage: emitted-order assertion plus two execution tests (a sync custom rule executes; an async custom rule runs on the async path and its sync path throws `AsyncValidatorInvokedSynchronouslyException`).
- Docs: migration note boundary updated; the analyzer sample now reports the custom password rules.

#### Repository truthfulness sweep

- Repository truthfulness sweep: `README.md` now states the real package set (3 packable projects: `Jeninnet.Validation`, `.Abstractions`, `.Analyzers`), the real sample counts (25 sample projects across 22 directories; 20 in the solution), and delegates localization to the sibling `Jeninnet.Localization` packages.
- `ROADMAP.md` and the internal phase-4 backlog recomputed against the tree: 265 tests dated 2026-11-04, engine-only remaining work, and cross-repo localization items redirected to the sibling repository.
- `CONTRIBUTING.md` rewritten (it was a copy of the sibling localization guide); `docs/CONTRIBUTING.md` added (the missing target of two documentation links).
- [Sample index](docs/en/appendix/03-samples.md): real counts plus the two missing sample entries.
- `scripts/docs/check-readme-claims.ps1` now verifies the packable count against `src/` projects with `IsPackable=true`; `scripts/README.md` documents the check rules.
- Docs checks green: links, README claims, and Arabic text.

### Quality, AOT, and analyzer expansion

#### API surface hygiene

- API surface hygiene (binary-breaking cleanup; source-compatible in documented shapes):
  - `ValidatorFactoryImplementation` is now `internal`; the public contract is `IValidatorFactory`.
  - `ValidationErrorCollector` is sealed.
  - Removed the dead duplicate `IPropertyAccessor<,>` from the analyzer assembly.
  - `AddGeneratedValidators`: added an explicit parameterless overload (all loaded assemblies), replaced the public single-`Type` overload with a private registration path, unified factory/localization bootstrap across the assembly and type entry points (`TryAdd*`, consumer overrides preserved), and made assembly scanning skip dynamic assemblies and tolerate `ReflectionTypeLoadException`.
  - `InternalsVisibleTo`: kept only the analyzer test assembly; removed the duplicate attribute and unused grants.
- Test hygiene: the nine generator snapshot tests in `Jeninnet.Validation.Tests` now load the generator directly (previously they returned early because the analyzer assembly was never referenced). Full suite: 246 + 19 green.

#### AOT and trimming evidence

- AOT/trimming evidence: `Jeninnet.Validation` and `Jeninnet.Validation.Abstractions` now declare `IsAotCompatible` and build with 0 trim/AOT analyzer warnings.
- Reflection-based `AddGeneratedValidators` scanning overloads are annotated `[RequiresUnreferencedCode]`; explicit registration is documented as the AOT-safe pattern.
- New NativeAOT smoke harness (`samples/Jeninnet.Validation.AotSmoke`) covering generated and fluent rules, custom rules, rule sets, the strict-async guard, localization, and a source-generated JSON round-trip; CI workflow `.github/workflows/aot-smoke.yml` publishes and runs it on `linux-x64`.
- Removed the dead `NullPolicyResolver` (`MakeGenericType` reflection path).
- Docs: [AOT and trimming](docs/en/04-operations/02-aot-and-trimming.md) (guarantee, exclusions, verification).

#### Executable architecture tests

- Executable architecture tests: new `tests/Jeninnet.Validation.ArchitectureTests` project (MSTest, no third-party framework) pins layering and dependency directions — Abstractions has no repository dependencies; Validation references only Abstractions plus approved `Microsoft.Extensions`/`Jeninnet.Localization`; Analyzers has no runtime references; no shipping assembly references `System.ComponentModel.DataAnnotations`; assembly/namespace identity is guarded.
- Four meta-tests feed synthetic violations to the predicates, proving the guards can fail.
- Full suite: 276 tests (246 + 19 + 11); the architecture suite runs in ~100 ms.

#### Coverage baseline

- Coverage baseline: the existing `scripts/coverage/coverage.ps1` pipeline runs the 276-test suite with coverlet and ReportGenerator; assembly filters updated for the current layout (three packable assemblies only).
- [Coverage and quality](docs/en/04-operations/03-coverage-and-quality.md) records the 2026-11-04 baseline: 54.3% line / 47.1% branch / 54.1% method; per assembly `Jeninnet.Validation` 55.9%, `Abstractions` 62.2%, `Analyzers` 52.2%; cold spots mapped to the remaining parity backlog.
- CI threshold enforcement and mutation testing remain deferred; README, ROADMAP, and the backlog were updated.

#### Test assertions package

- New packable package `Jeninnet.Validation.Assertions`: stack-agnostic test assertions (no MSTest/xUnit/FluentAssertions dependency) — `ShouldHaveValidationErrors`/`ShouldNotHaveValidationErrors`, result and member matchers (count, message fragment, rule message key, error code, severity, custom state), `TestValidate`/`TestValidateAsync`, and a sealed `ValidationAssertionException` with actionable failure messages.
- The assertions package builds with `IsAotCompatible`, joins the architecture guards, and has 15 tests; documentation lives in the [testing guide](docs/en/02-guides/14-testing.md) and the package README.
- Repository counts updated to four packable projects; the coverage baseline was regenerated (55.3% line / 47.6% branch over four assemblies; `Assertions` 84.2%).

#### Analyzer family expansion

- Analyzer family expansion: new `VAL0017` (Info) reports tautological rules whose bounds cover the whole value range (`MinimumLength(0)`, `MaximumLength(int.MaxValue)`, `Length(0, int.MaxValue)`, and inclusive/comparison bounds at numeric type extremes); the vetting table records the excluded cases.
- `VAL0016` now has a code fix (`UseValidateAsyncCodeFix`): it rewrites a synchronous `Validate` call to `await ValidateAsync(...)` when the enclosing method or local function is already `async`.
- Message-key drift and redundant-null-policy families were vetted out (custom keys are legitimate; `VAL0014` already covers redundant `[NotNull]`).
- Docs: [diagnostics reference](docs/en/03-reference/03-diagnostics.md) (17 diagnostics, 13 code fixes); tests: 6 new (analyzer suite 25); coverage refreshed (55.9% line / 47.9% branch).

### Rule surface, execution semantics, and integrations

#### Lifecycle hooks

- Lifecycle hooks for fluent validators: `PreValidate`/`PreValidateAsync` guards (returning `false` short-circuits with `Success`; rules and failure hooks are skipped) and `OnFailure`/`OnFailureAsync` callbacks (once per `Validate*` call, all errors, CT-aware on the async path).
- The synchronous path with an async pre-hook throws `InvalidOperationException` (fail-fast, no blocking); hook exceptions are wrapped in `ValidationException` naming the hook; `OperationCanceledException` propagates.
- 11 lifecycle tests; docs contract table in the [lifecycle hooks guide](docs/en/02-guides/19-lifecycle-hooks.md); coverage refreshed (56.6% line / 48.4% branch).

#### Class-level cascade and fail-fast

- Class-level cascade: `ValidatorDefinition<T>.ClassLevelCascadeMode` (default from `ValidationOptions.DefaultClassLevelCascadeMode`) stops after the first member that adds errors.
- `ValidationCallOptions` (`RuleSets`, `CascadeMode?`, `FailFast`) plus `Validate(instance, options)` and `ValidateAsync(instance, options, ct)` overloads; `FailFast` yields exactly one error on both paths and skips the async phase after a sync failure under stop/fail-fast.
- `ValidationContext<T>` carries `FailFast` into child/element contexts; `MemberRuleGroup` stops at the first error under fail-fast.
- Precedence documented and tested (rule-level > call options > type-level > global); 11 new tests; coverage refreshed (57.0% line / 49.1% branch).

#### Validation strategy

- Validation strategy: `ValidationCallOptions` gained `IncludeProperties`, `ExcludeProperties`, and `ExcludeRuleSets`; new `ValidationStrategy` chained builder plus `Validate(instance, strategy => ...)` and `ValidateAsync(instance, strategy => ..., ct)` overloads.
- Semantics: include is restrictive (only fluent member groups; flat generated rules are excluded when an include set is present), exclude wins over include, rule-set names are case-insensitive with `"default"` covering ungrouped rules, unknown names are a no-op, and member matching is ordinal; default validation (no filters) is unchanged.
- 13 new strategy tests; docs section in the [rule sets and cascade guide](docs/en/02-guides/04-rule-sets-and-cascade.md); coverage refreshed (57.4% line / 49.5% branch).

#### Per-rule conditions

- Per-rule conditions: `When`, `WhenAsync`, `Unless`, and `UnlessAsync` extension methods on `IRuleBuilder<T, TProperty>`, with `ApplyConditionTo.CurrentValidator` (default) and `ApplyConditionTo.AllValidators` scopes.
- Async conditions run on the async path only; synchronous validation skips the gated rule. Pure sync/async members evaluate the condition once per validate; mixed members evaluate an `AllValidators` condition once per phase.
- 8 new condition tests; docs section in the [conditions guide](docs/en/02-guides/05-conditions-and-dependent-rules.md); coverage refreshed (58.5% line / 50.5% branch).

#### Shared validation context

- Shared validation context: `ValidationContext<T>` now carries `Root`, a pooled `RootData` bag (`GetOrAdd`, indexer, `TryGetValue`), and an optional `ServiceProvider`. `ForChild`/`ForElement` preserve all three.
- `RootContextData` allocates its dictionary lazily and is pooled per thread; the engine rents one holder per `Validate*` walk and returns it in a `finally`, so the bag-less path allocates nothing after warm-up.
- `ValidationCallOptions.ServiceProvider` and `ValidationStrategy.WithServiceProvider` expose the integration hook; the engine never resolves services.
- 7 new context tests; docs section in the [custom rules guide](docs/en/02-guides/07-custom-rules.md); coverage refreshed (59.1% line / 50.9% branch).

#### Custom rules with context

- Custom validation with context: `CustomValidation<T, TValue>` / `CustomValidationAsync<T, TValue>` delegates receive the member value, `ValidationContext<T>`, and the error collector (plus a cancellation token for the async variant), so one rule can emit multiple errors.
- New `CustomRule<T, TValue>` / `CustomAsyncRule<T, TValue>` plus fluent `Custom(...)` / `CustomAsync(...)` extensions; the async rule runs on the asynchronous path only.
- Context access covers `Root`, `RootData`, `ServiceProvider`, and `GetFullPath(...)`; a delegate exception becomes a single `MessageId.Invalid` failure with code `CustomValidationException`, and cancellation propagates.
- No attribute equivalent (fluent-only, matching peer behavior).
- 8 new custom-rule tests; rule catalog and advanced guide sections; coverage refreshed (59.2% line / 50.7% branch).

#### Dependent rules

- Dependent rules: `DependentRules(Action<ValidatorRuleBuilder<T>>)` attaches a block to the rule immediately before it; the block runs only when that rule produced no errors.
- New `DependentRule<T>` wrapper; parent runs first, dependents follow in declaration order, nested blocks compose, and errors are emitted parent-first.
- Synchronous dependents run on `Validate`; asynchronous dependents run on `ValidateAsync` only. A synchronous parent's gate is recorded in the per-walk `RootContextData` for the async phase; an asynchronous parent's block never runs on the synchronous path.
- `CascadeMode.Stop` on the parent member group (and `FailFast`) stops the dependent block after the first failing dependent.
- Fluent-only; no attribute equivalent. 12 new dependent-rule tests; rule catalog and advanced guide sections; coverage refreshed (60.2% line / 52.0% branch).

#### Collection count rules and element filters

- Collection cardinality: `Count(min, max)`, `MinCount(min)`, and `MaxCount(max)` runtime rules with fluent extensions for every collection shape (`IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `List<T>`, arrays); null collections are a no-op.
- New `[Count]`, `[MinCount]`, and `[MaxCount]` attributes plus new `RuleKind` values, metadata builders, and emitter paths so generated validators use the same runtime rules.
- `ForEach` element filters: `Where(predicate)` (both paths) and `WhereAsync(predicate)` (async path only; the ForEach rule becomes async-only); original indices are preserved for null and filtered-out elements, including arrays.
- New message keys `Count`, `MinCount`, `MaxCount` with `{Min}`, `{Max}`, `{Count}`, and `{PropertyValue.Count}` parameters.
- 9 new core tests + 2 analyzer/emitter tests; rule catalog and advanced guide sections; coverage refreshed (64.2% line / 55.7% branch).

#### Dedicated format rules

- Dedicated format rules: `ExactLength(n)` with its own `ExactLength` message/parameters (no longer composing `Length(n, n)`), `Base64String()` with allocation-free whitespace-tolerant validation, and `FileExtensions(...)` with dot normalization, ordinal/ordinal-ignore-case matching, `AllowNonStandard`, and `{Extensions}` message parameters.
- New `[ExactLength]`, `[Base64String]`, and `[FileExtensions]` attributes plus new `RuleKind` values, metadata builders, and emitter paths; generated validators use the same runtime rules.
- New message keys `ExactLength`, `Base64String`, and `FileExtensions`.
- Base64 benchmark smoke (`Base64ValidationBenchmarks`) documents the zero-allocation happy path.
- 8 new tests (5 runtime + 1 generator + 2 analyzer/emitter); rule catalog and advanced guide sections; coverage refreshed (65.4% line / 57.6% branch).

#### Built-in message packs

- New fifth packable project `Jeninnet.Validation.Localization` with embedded **English** and **Arabic** packs covering every `ValidationMessageKeys` constant.
- `AddDefaultMessagePacks()` registers the pack as the single `IInnerMessageProvider` required by the sibling localization platform; the options overload adds custom packs and per-key overrides inside that provider.
- `LocalizeValidationErrorAsync` bridges `ValidationError` into the localization pipeline, supplying `{PropertyName}` and merging rule parameters.
- `scripts/docs/check-arabic-text.ps1` now scans the Arabic message resource; the cookbook and the [localization guide](docs/en/02-guides/13-localization.md) document registration, precedence, and the full key catalog.
- 10 new tests (catalog completeness, placeholders, en/ar rendering, overrides, custom packs, fallback, parameters, exactly-one-provider guard); coverage refreshed (65.8% line / 57.6% branch; 5 assemblies, Localization 96.8%).
- Audit follow-up: compared the pre-split localization classes, documented the class-to-platform mapping in the [localization guide](docs/en/02-guides/13-localization.md), and restored the threshold parameter in the `Comparable` message.

#### ICU plural messages

- Documented and tested the opt-in ICU plural/select path through the sibling `IcuMessageFormatter`: register it before `AddLocalization` (the pipeline's `TryAdd` keeps it), and use ICU templates in pack overrides.
- [Localization](docs/en/02-guides/13-localization.md) covers ICU wiring, `Count` examples in English and Arabic, parameter types (numeric vs string), and the supported ICU subset; the cookbook and default-pack docs link to it.
- 2 integration tests: direct `IcuMessageFormatter` plural rendering (`one`/`other` in English, `one`/`few` in Arabic) and a `Count` rule localized through the full pack + pipeline.
- No engine changes; the default formatter and non-ICU templates remain byte-identical. Coverage unchanged (65.8% line / 57.6% branch).

#### Validator descriptors

- New metadata contracts in `Jeninnet.Validation.Abstractions` (`Jeninnet.Validation.Metadata`): `IValidationDescriptorProvider`, `IValidationDescriptor`, `IValidationMemberDescriptor`, `IValidationRuleDescriptor`, and immutable implementations with a precomputed `RulesForMember` lookup.
- Fluent validators implement `Describe()` (cached once per instance) through an internal, reflection-free rule metadata bridge; key rules expose named parameters (`Min`/`Max`, `Length`, `Pattern`, `Threshold`, `Extensions`, `Precision`/`Scale`), and conditional/dependent wrappers surface `HasCondition`.
- The source generator now emits `IValidationDescriptorProvider` plus a static frozen `ValidationDescriptor` and `Describe()` on every generated validator.
- 4 new tests (fluent members/parameters/caching/conditions + a compile-and-run generated descriptor test); the [metadata reference](docs/en/03-reference/04-metadata.md) documents the contract and limits.
- Coverage refreshed (66.5% line / 58.6% branch; 5 assemblies).

#### Display-name resolver

- New `ValidationOptions.DisplayNameResolver` (`Func<Type, string, string>`) for global human-friendly `{PropertyName}` values; precedence is rule override → resolver → member name.
- `AddError` injects the resolved `PropertyName` only when a resolver is configured, so the no-resolver path allocates nothing extra; `ValidationError.MemberName` remains the code-facing key.
- `LocalizeValidationErrorAsync` now preserves an existing `PropertyName` instead of overwriting it with `MemberName`.
- 4 new tests (resolver, rule-override precedence, no-resolver allocation path, localization integration); advanced guide section; coverage refreshed (66.6% line / 58.6% branch).

#### Minimal API and MVC validation filters

- New sixth packable project `Jeninnet.Validation.AspNetCore` (ASP.NET Core integration shell; Core/Abstractions stay framework-free).
- Minimal APIs: `AddRequestValidation<T>()` endpoint filter (DI-resolved validator, validator-missing/null pass-through), `ValidationResult.ToProblemErrors()`, privacy-preserving error-code/severity options, `ValidationException` → 400 handler, and `AddValidationEndpoints()`.
- MVC/Razor Pages: `AddMvcValidation()` async action filter, `ModelState` mapping, `[SkipJeninnetValidation]`, `[JeninnetValidationRuleSets]`, cached validator/invoker resolution (no per-request reflection).
- 11 integration tests; package README and the [integration packages reference](docs/en/03-reference/05-integration-packages.md); coverage refreshed (66.8% line / 58.7% branch; 6 assemblies, AspNetCore 72.0%).

#### JSON Schema and OpenAPI emission

- New seventh packable project `Jeninnet.Validation.JsonSchema`: deterministic JSON Schema (draft 2020-12) generation from validator descriptors, including `required`, string length/pattern/format, numeric bounds, array sizes, `enum`, `multipleOf`, and advisory `x-validation-rules` for rules without a direct schema mapping.
- New eighth packable project `Jeninnet.Validation.OpenApi`: `AddSchemaConstraintsFromValidators(...)`, `ValidationSchemaRegistry` (validated type → descriptor), and an `IOpenApiSchemaTransformer` that applies member constraints and required members to OpenAPI schemas for the default `v1` document or a custom document through `OpenApiOptions`.
- `IValidationDescriptor` now exposes `ValidatedType` (nullable for hand-built descriptors); fluent `Describe()` and generated descriptors populate it so emission can map model types to validators.
- 5 new tests (2 JSON Schema, 3 OpenAPI registry/applier/transformer); both packages are documented and the coverage pipeline gained `classFilters` to exclude generated OpenAPI/compiler classes.
- Coverage refreshed (67.2% line / 58.5% branch; 8 assemblies, JsonSchema 67.9%, OpenApi 77.0%).

#### Client rules export

- New ninth packable project `Jeninnet.Validation.ClientRules`: deterministic JSON, TypeScript, and JavaScript export from validator descriptors, with a schema-version stamp, validator allow-list, optional message resolver, and stable member/rule ordering.
- The source generator can emit a build-time catalog: set `JeninnetEmitClientRules=true` (plus `CompilerVisibleProperty`) and `Jeninnet.Validation.Generated.ClientRulesCatalog` exposes `SchemaVersion` and `Json` constants — no reflection, AOT-safe, and opt-in.
- The MinimalApi sample now exposes `/client-rules` through the runtime writer.
- The [schema, OpenAPI, and client rules guide](docs/en/02-guides/12-schema-openapi-client.md) documents the payload contract, message strategy, build-time and runtime delivery, and limits.
- 5 new tests (deterministic payload, allow-list + resolver, TypeScript/JavaScript modules, generator catalog parity, default-off); coverage refreshed (68.0% line / 58.8% branch; 9 assemblies, ClientRules 88.2%).

#### MediatR pipeline

- New tenth packable project `Jeninnet.Validation.MediatR`: `AddValidationPipeline()`, `ValidationBehavior<TRequest, TResponse>`, and `MediatRValidationOptions`.
- The behavior runs **all** registered `IValidator<TRequest>` implementations, aggregates errors, forwards the cancellation token, and bypasses validation when no validator is registered.
- Failure strategies: `Throw` (default, `ValidationException`) and `Continue` (stores the result in the scoped `ValidationResultAccessor` for the handler).
- `[MediatRValidationRuleSets]` selects named rule sets per request type; the lookup is cached, and `MediatRValidationOptions.RuleSets` supplies a default.
- The MediatR sample now uses the package and its local behavior class was removed; MediatR remains isolated from Core/Abstractions.
- 6 new tests (valid flow, aggregated throw, missing-validator bypass, continue strategy, rule-set selection, cancellation-token forwarding); coverage refreshed (68.4% line / 59.2% branch; 10 assemblies, MediatR 100%).

#### DataAnnotations bridge

- New eleventh packable project `Jeninnet.Validation.DataAnnotations`: `[JeninnetValidatable(typeof(TValidator))]` runs a Jeninnet validator as a class-level DataAnnotations attribute, so MVC model binding, Blazor's `DataAnnotationsValidator`, and `Validator.TryValidateObject` execute the same engine.
- Validator resolution: the validation context's service provider first (`IValidator<T>` or the concrete type), then a parameterless constructor; unresolved validators fail with an actionable `InvalidOperationException`.
- `JeninnetDataAnnotationsAdapter` converts results to DataAnnotations (`BuildMessage`, `GetMemberNames`, `ToValidationResults`) and supports the `IValidatableObject` alternative surface; severity prefixes and error codes are configurable.
- Sync-only behavior is documented; async-only validators throw `AsyncValidatorInvokedSynchronouslyException` on the DataAnnotations path.
- 5 new tests (DI resolution, parameterless fallback, `ErrorMessage` override, severity/error-code conversion, missing-validator diagnostics, `IValidatableObject` helper); coverage refreshed (68.7% line / 59.3% branch; 11 assemblies, DataAnnotations 91%).

#### Blazor adapter

- New twelfth packable project `Jeninnet.Validation.Blazor`: `<JeninnetValidator TModel="..." />` subscribes to `EditContext`, runs `IValidator<TModel>` on `EditContext.Validate()` (and optionally on field changes), and replaces `ValidationMessageStore` messages.
- `FieldIdentifierResolver` maps nested (`Address.Street`) and indexed (`Lines[0].ProductName`) member paths to the owning object and leaf field; unresolved paths fall back to the root model and full path.
- `EditContextValidationCoordinator<TModel>` is the renderer-independent core (subscribe, validate, clear, dispose); the component resolves the validator from DI or accepts an explicit parameter.
- 5 new tests (root/nested/collection mapping, resolver paths and fallback, field-change validation, clear/dispose); coverage refreshed (68.9% line / 59.3% branch; 12 assemblies, Blazor 74.7%).

#### Benchmark evidence

- New [benchmarks](docs/en/04-operations/06-benchmarks.md): machine profile, ShortRun methodology, validation tables for 5/10/30-property models (fluent vs real source-generated validators, valid/invalid/fail-fast), localization lookup, and Base64.
- Benchmark suite completed: the stale composite-provider benchmark (invalid under the exactly-one-provider rule) was replaced by single-provider localization benchmarks; the benchmark project now references the analyzer and measures real generated validators.
- Benchmark-driven engine fix: `CompositeValidator.ValidateRules`/`ValidateRulesAsync` use an indexed array fast path for the default rule set, removing 32 B/op from the 10/30-property fluent paths and error paths.
- Honest correction: the happy path measures **32 B/op**, not zero. the historical comparison matrix row 59 was corrected from 1.0 to 0.5; row 55 (published evidence) moved from 0.5 to 1.0, leaving the headline unchanged.
- No continuous perf-regression CI gate; the decision and hardware-variance rationale are recorded in the [benchmarks](docs/en/04-operations/06-benchmarks.md).

#### Allocation audit

- Allocation audit closed: `CompositeValidator` now iterates rule arrays by index and `SelectRules` returns the original array when no rule sets or filters apply. The residual 32 B/op from the allocation audit is gone; the Release test and BenchmarkDotNet both measure **0 B/op** on the happy path.
- New [allocation audit](docs/en/04-operations/07-allocation-audit.md) records the collector/root-data lifecycle, the result snapshot guarantee (no aliasing), thread-pool/async-continuation behavior, nested-validation cost, and the rejected `AsyncLocal` alternative.
- 4 new stress/allocation tests: zero-allocation happy path (Release-exact, Debug budget), 10,000 parallel invalid validations with stable error counts, 1,000 nested validations, and 1,000 async validations with continuations.
- Benchmark refresh: error paths 968/2,104/6,024 B (5/10/30 errors), fail-fast 208 B, Base64 and localization 0 B. the historical comparison matrix row 59 restored to 1.0; headline **59.0/60 ≈ 98.3%**.

#### Sonar quality gate

- Fixed `scripts/sonar/preview.ps1` to match CI: runs the **whole solution** with `XPlat Code Coverage;Format=opencover` (previously one test project, a missing `.runsettings`, and cobertura vs. opencover mismatch), and aligned the local/CI exclusions.
- Sonar scope now focuses on runtime code: analyzer/code-fix tooling, metadata-only attributes, benchmarks, tests, and `IRuleDefinition.cs` are excluded from coverage/duplication; the gate requires coverage > 80%, duplication 0%, and zero new violations.
- Added 18 tests covering JSON Schema mapping, DI registration, legacy conditional helpers, property comparison, collection shapes/filters, single-generic string rules, date offsets, null policies, and Blazor resolver edge cases; refactored the shared rule base, emptiness helper, and JSON value factory to remove duplication.
- Local Sonar result: **coverage 82.1%** (line 84.3%, branch 77.2%), **new coverage 80.2%**, **duplicated lines density 0.0%**, **0 open issues**, **Quality Gate OK**; ReportGenerator all-assembly baseline is 72.7% line / 63.0% branch.

#### Versioning and repository shape

- Centralized the product version in `Directory.Build.props` as `1.0.0-preview.1` (matching the sibling projects); removed per-project `<Version>` declarations from all 32 `.csproj` files and kept `AssemblyVersion`/`FileVersion` numeric.
- Moved the shared JSON value factory to `src/shared/JsonValueFactory` with a `.projitems` import, matching the sibling `Jeninnet.Localization` shared-source pattern; the shared files are listed in the solution under `/src/Shared/`.
- Moved the NativeAOT smoke harness from `tests/` to `samples/Jeninnet.Validation.AotSmoke` and added its README, solution folder, CI path, sample index, and documentation links.
- Rewrote `CONTRIBUTING.md` to the maintainers-only stabilization policy used by the sibling projects (source contributions limited until stable; issues, design feedback, and documentation remain welcome).
- Restructured `ROADMAP.md` as a release-based public delivery plan (SemVer, `1.0.0-preview.N`, MUST/SHOULD/LATER, release gates) with the demand-driven post-1.0 features section.


---

## Pre-1.0.0 — Historical development (2026-06 – 2026-11)

The sections below record the pre-preview development batches. They are kept for traceability;
the current release scope is the `1.0.0-preview.1` section above.

### Phase 3-D — Release Stabilization (Batch 2)

#### Release audit

- Release audit completed. No critical blockers identified.
- Integration tests remain the only genuine gap before v1.0.
- Service locator pattern documented as benign (ValidatorFactoryStatic — stateless, no captive dependency).
- ROADMAP.md and the internal release tracking documents were refreshed.

#### Service locator audit

- Service locator audit across all `src/` projects completed.
- Only one file (`ValidatorFactoryStatic.cs`) uses `IServiceProvider` / `GetService`.
- Zero `GetRequiredService`, `ActivatorUtilities`, or `BuildServiceProvider` usage in `src/`.

#### Composite provider benchmark design

- Composite provider benchmark proposal completed.
- 9 scenarios across 1/2/3 providers with hit/miss/fallback configurations.
- Design only — no implementation.

#### Testcontainers analysis

- Testcontainers design analysis completed.
- Supports SQL Server (`Testcontainers.MsSql`), PostgreSQL (`Testcontainers.PostgreSql`), MySQL (`Testcontainers.MySql`).
- SQLite remains in-memory — no container needed.
- Estimated effort: ~13-15 hours.

#### Validator factory lifetime review

- `ValidatorFactoryStatic` investigation completed.
- Root `IServiceProvider` captured via `AddSingleton` factory — confirmed.
- Scoped `IValidatorFactory` lifetime bypassed via root-resolved `GetService` — confirmed.
- Impact: **none** — `ValidatorFactoryImplementation` and `PropertyAccessor` are stateless.
- Verdict: Bug confirmed but currently harmless.

#### Composite provider tests

- Added 5 deep composite provider tests:
  - Priority (Memory hit prevents Json)
  - Fallback (Memory miss → Json hit)
  - Missing everywhere (all null → null)
  - Registration order (first winner reversed)
  - Multiple providers (3-provider chain, first success stops traversal)

#### Dead code removal

- Dead code removal — eliminated 5 unused interfaces/classes:
  - `ICustomRuleDefinition` — dead interface, no implementations
  - `IValidationMessageRenderer` — dead interface, no consumers
  - `IDatabaseMessageProvider` — dead marker interface
  - `ILocalizationRepository` — planned but never implemented (providers use ADO.NET directly)
  - `RuleBuilderExtensions.cs` — empty partial class, no members

#### Dependency registration audit

- Registration audit completed across all `src/` projects.
- 14 DI registrations reviewed: 11 `TryAddSingleton`, 1 `AddSingleton`, 3 `AddScoped`.
- No `AddTransient`, `TryAddScoped`, or `TryAddTransient` in any src project.
- All localization providers correctly use Singleton. Validators correctly use Scoped.
- Only concern: ValidatorFactoryStatic's static `IServiceProvider` capture (deferred to the validator factory lifetime review).

#### DI API unification

- DI API unification — all 8 provider DI extensions standardized.
- Each provider now exposes `AddXxx()` (parameterless) and `AddXxx(Action<XxxOptions> configure)`.
- Existing overloads preserved; parameterless delegates to `_ => { }`.
- Projects: Memory, Json, Xml, ResX, MsSql, MySql, PostgreSql, Sqlite.

---

### Phase 3-C — Release Stabilization (Batch 1)

#### Composite message provider registration

- Fixed CompositeValidationMessageProvider registration design.
- Root cause: all 8 provider DI extensions registered as `IValidationMessageProvider`, but composite depends on `IEnumerable<IInnerValidationMessageProvider>` — collection was always empty.
- Fix: changed all 8 `TryAddSingleton<IValidationMessageProvider>(...)` to `TryAddSingleton<IInnerValidationMessageProvider>(...)`.
- No provider class modifications needed (all already implemented `IInnerValidationMessageProvider`).
- Added 5 contract tests: 4 composite behavior + 1 DI resolution count.
- Build: 0/0, Tests: 262/262, Analyzers: clean.

---

### Phase 3-B — Archive Translation Providers

- Archived `Jeninnet.Validation.Localization.Translation.Google` and `Jeninnet.Validation.Localization.Translation.Microsoft` to `archive/`.
- Both projects were placeholders with no provider class, empty DI extensions, no tests, and no consumers.
- Removed from solution; git history preserved.
- Decision: archived for v1.0. May return in a future release.

---

### Phase 3-A1 — Source Documentation Enforcement

- CS1591 enforced for all 19 `src/` projects; conditionally suppressed for tests, samples, and benchmarks.
- Fixed `IsUnitTestProject` detection (`.Test` -> `.Tests`) in `Directory.Build.props`.
- Added `IsSampleProject` and `IsNonShippingProject` heuristics via project-name patterns.

---

### XML Documentation Phase

#### XML-FINAL

- Documented the final 9 remaining public API members across 6 files:
  - `RuleBuilderExtensions.cs` — class summary
  - `RuleSetRule.cs` — constructor, `Validate()`, `ValidateAsync()`
  - `DatabaseValidationMessageProviderBase.cs` — `Options` property, constructor
  - `MemoryValidationMessageProvider.cs` — constructor
  - `ResXValidationMessageProvider.cs` — constructor
  - `JsonValidationMessageProvider.cs` — constructor
- Verified 0 CS1591 from `src/` projects.

#### XML documentation: analyzers and code fixes

- Completed public API documentation for `Jeninnet.Validation.Analyzers`.
- `<inheritdoc/>` on all DiagnosticAnalyzer (3 files) and CodeFixProvider (13 files) overrides.
- Class summaries for 2 analyzer + 13 code-fix classes.
- Documented 15 DiagnosticDescriptors (VAL0001–VAL0015), GeneratorDiagnostics, and ValidatorGenerator.Initialize.

#### XML documentation cleanup

- Normalized hybrid XML documentation artifact.
- Migrated 4 regex `<remarks>` blocks into `StringRegexes.cs` source comments.
- Added `src/**/*.xml` to `.gitignore`. Removed 19 build-generated XML files from git tracking.

#### XML documentation: fluent API and rule types

- Fluent API extensions — 6 files, 41 methods documented.
- Rule base types — 5 files, 11 properties documented.
- Null policy types — 4 files, class/typeparam/Instance docs.
- Attribute types — 31 files across Common, Comparison, Conditional, DateTime, Property, and String categories.
- Core and dependency injection — 8 files including IValidationRule, ValidationContext, ValidatorFactoryStatic, DI extensions.

#### XML documentation: abstractions

- Added XML documentation to `Jeninnet.Validation.Abstractions`.
- Files: MessageId.cs, ValidationError.cs, ValidationErrorCollector.cs, IRuleDefinition.cs, ValidationMessageKeys.cs.

---

### Architectural Reconciliation (Batches 1–5)

#### Database provider contract tests

- Added `DatabaseProviderContractTests.cs` — 4 contract tests via SQLite in-memory covering exact lookup, missing key, missing culture, and no fallback.

#### Database localization option hardening

- Added `TableName` validation to `DatabaseLocalizationOptions` to prevent SQL injection. Centralized fix protecting all 4 database providers.

#### Message provider nullability

- Made `IValidationMessageProvider` non-nullable in `DefaultValidationMessageLocalizer` constructor. Removed runtime null guard and null-forgiving operators.

#### Duplicate formatter removal

- Eliminated duplicate `IMessageFormatter` from `Jeninnet.Validation.Configuration`. Deleted dead code — zero call sites.

#### Translation provider evaluation

- Translation Provider Evaluation completed. Both Google and Microsoft translation providers are placeholder implementations (no provider class, empty DI extensions, no tests). Archived in Phase 3-B.

#### Localization test expansion

- Expanded localization test suite from 8 to 58 tests. Added DatabaseExactLookup, FileProviderConvention, and enhanced Architecture/Composite/DI tests.

#### Documentation audit and composite provider

- Completed documentation audit, database guidelines, and implemented `CompositeValidationMessageProvider`.

#### Centralized fallback strategy

- Refactored to Centralized Fallback Strategy. Verified provider alignment.

#### SQLite localization provider

- Implemented Sqlite localization provider.

#### PostgreSQL localization provider

- Implemented PostgreSql localization provider.
