# Known Limitations

These are deliberate boundaries or tracked follow-ups, not surprises.

## Minimal API binding

- `.AddRequestValidation<T>()` validates the first body argument of type `T` for backward
  compatibility. `.AddRequestValidation()` validates every handler parameter with a registered
  validator and prefixes errors with `query.`, `route.`, or `header.` when the binding attribute is
  present.
- Complex `[FromQuery]` models must be bindable by ASP.NET Core (`[AsParameters]` or a parseable
  type); the filter validates the bound object, not the raw query string.
- `IFormFile`, streams, and special framework parameters are ignored by the filter.
- The non-generic filter reads handler parameter metadata and uses cached compiled delegates. Use the
  typed overload when an explicit, trim/AOT-friendly endpoint filter is preferred.
- `[SkipValidation]` on a generated model or member opts out of generated rules; on a minimal API
  handler parameter or an MVC action parameter it skips request validation for that parameter.
  The attribute is also supported on DataAnnotations bridge types and properties.

## Async vs sync adapters

- DataAnnotations is synchronous. Async-only validators throw
  `AsyncValidatorInvokedSynchronouslyException` on that path.
- Blazor validates synchronously by default; enable `UseAsyncValidation` and call
  `JeninnetValidator.ValidateAsync()` from `EditForm.OnSubmit` for async rules.
- Use ASP.NET Core, Minimal API, or MediatR for server-side async rules.
- Sync `Validate` never silently skips async rules.
- `PreValidateAsync` is stricter: synchronous `Validate` throws `InvalidOperationException` because
  the guard cannot run.

## Execution and filtering

- `IncludeProperties` / `ExcludeProperties` work on both fluent member groups and generated rules;
  generated rules expose their member name through `IValidationMemberRule`.
- Property targeting matches ordinal member names; unknown names are a no-op. Nested sub-paths
  (`Address.City`) are still targeted as a unit, not as individual leaf members.
- Nested validation holds one collector while the outer call holds another; the inner call allocates
  one collector for the nesting duration. Bounded by nesting depth; a stack-based pool is future
  work.
- Hooks receive `RuleSets` and `FailFast`, but not `RootData` or `ServiceProvider`.

## Emptiness and equality semantics

- `NotEmpty` does not treat `0`, `false`, or `default(DateTime)` as empty. It treats `null`,
  whitespace-only strings, empty collections, and `Guid.Empty` as empty. Use `NotDefault()` to
  reject value-type defaults.
- `Equal` and `NotEqual` use `EqualityComparer<T>.Default` unless the fluent overload receives an
  explicit `IEqualityComparer<T>` (for example `StringComparer.OrdinalIgnoreCase`). The attribute
  path always uses the default comparer.
- `EmailAddress` has one built-in regex mode; there is no separate simple/regex switch.
- `ScalePrecision` counts decimal digits from the invariant string representation. Set
  `ignoreTrailingZeros` (fluent or `IgnoreTrailingZeros = true` on the attribute) to trim trailing
  fractional zeros before counting.
- `IPAddress` accepts IPv4 and IPv6; JSON Schema represents this as `anyOf` with the `ipv4` and
  `ipv6` formats. OpenAPI receives the same `anyOf` shape.

## Domain rules and localization packs

- `Jeninnet.Validation.Rules.Domain` validates structure only. IBAN mod 97, BSN/btw-id elfproef,
  VAT check digits, and Egyptian national-ID date/governorate checks prove shape, not existence.
- Supported countries are explicit: `VatNumber` (`NL`, `DE`, `FR`, `GB`, `BE`), `PostalCode`
  (`NL`, `US`, `DE`, `FR`, `GB`, `BE`, `EG`), `NationalId` (`NL`, `EG`, `US`), and IBAN registry
  countries. Anything else throws `NotSupportedException` when the rule is constructed.
- `PasswordStrength` is a policy check, not a breach-list service. The built-in common-password list
  is deliberately small; use a custom `PasswordPolicy` or a custom rule for a production breach
  corpus. Null and empty passwords are skipped, so pair the rule with `NotEmpty()`.
- Domain rules are `serverOnly` in ClientRules because the checksum implementations are not
  duplicated in the browser runtime. JSON Schema/OpenAPI still expose patterns, formats, and
  `minLength`.
- The built-in `nl` pack is new; like the other non-English/Arabic packs, native-speaker review is
  welcome before the stable release.

## Rule sets and failure policy

- `RuleSets = null` and an empty collection mean **all rules**, including named sets. Use
  `["default"]` to run only ungrouped rules.
- The fluent per-rule `.RuleSet(...)` call is terminal; apply message, condition, and other rule
  modifiers before it. Nested rule sets inside another rule-set callback are not independently
  selectable at the outer call.
- `IncludeAllRuleSets` bypasses include selection but does not bypass `ExcludeRuleSets`.
- `ThrowOnFailures` is delivered after failure hooks run and is independent of `FailFast`;
  `FailFast` controls collection, while `ThrowOnFailures` controls the final delivery.
- Rule-set groups are flattened in descriptors; the original grouped rules appear with a
  `RuleSets` parameter instead of a synthetic `RuleSet` pseudo-rule.

## Inheritance and runtime dispatch

- `SetInheritanceValidator` resolves only the exact runtime type. A runtime type that is not
  registered is skipped; there is no base-type or interface fallback.
- The generator walks base classes for a `[Validate]` type and merges rules from explicitly
  annotated interfaces. It does not generate validators for derived types that omit `[Validate]`.
- Explicit interface implementations are not matched to interface members by the generator; declare
  the interface member publicly or repeat the rule on the class property.
- Async-only entries in an inheritance map throw `InvalidOperationException` on the synchronous
  path. Use `ValidateAsync`.

## Model shapes and date/time values

- Parsed date/time literals use `CultureInfo.InvariantCulture`; invalid literals fail the build with
  `VAL0021`.
- `[FutureDate]`/`[PastDate]` compare against UTC from `TimeProvider` (`ValidationCallOptions.ServiceProvider`,
  then `TimeProvider.System`). The library never converts values to the server's local time zone;
  normalize input at the boundary when the source is local.
- A null date/time value passes relative-date rules; combine with `[NotNull]` when it is required.

- `[Validate]` supports classes, `struct`, and `record struct` models. It does not discover
  validation rules on fields or static properties.
- Models must declare a namespace. The generator emits validators under
  `<model namespace>.Validators`; a model in the global namespace is not supported in this preview.
- Open generic model declarations are rejected with `VAL0022`; use a non-generic model or a fluent
  validator. This avoids ambiguous generated type names and DI registrations.
- `[After]` / `[Before]` parse against the property type (`DateTime`, `DateTimeOffset`, `DateOnly`,
  or `TimeOnly`) with `CultureInfo.InvariantCulture`. `TimeOnly` accepts clock values such as
  `"09:30"`; date-carrying strings are rejected. Invalid values produce `VAL0021`; there is no
  `DateTime.Now` fallback.
- `DateTimeOffset` values without an explicit offset are interpreted as UTC during generation so
  builds are deterministic across machines and cultures.

## Collection element rules and path filtering

- `ForEach` with a child validator still skips null elements. Use `ForEachElement(...)` with
  `NotNull()` when a missing element must be reported.
- `IncludeProperties` / `ExcludeProperties` normalize `[0]`, `[]`, `[*]`, and custom indexers to the
  same collection segment. Index-specific filtering (`Lines[2].Quantity`) is not supported.
- `OverrideIndexer` changes error paths and message text only. The Blazor `FieldIdentifierResolver`
  falls back to the root model and full path when the custom indexer is not an integer; it does not
  throw.
- `CollectionIndex` is added to errors produced by collection element and child-element rules. It is
  not added to ordinary member errors.

## Input sanitization

- Sanitization mutates the model in place before rules run; `Validate` on a model with
  `[Sanitize]` or registered sanitizers is an explicit preprocessing contract, never silent.
  Sanitizers run even when rule-set filtering selects no rules, because they are preprocessing,
  not rules.
- Members without an accessible non-init setter cannot be sanitized; the generator rejects them
  at build time with `VAL0027`, and fluent `.Sanitize()` without a setter throws
  `InvalidOperationException` at configuration time. `record` positional and `init`-only members
  must stay immutable; sanitize a writable copy instead.
- Only `string` and `IList<string>` members are supported; other member types are rejected with
  `VAL0027`. Collection elements cannot be sanitized inside `ForEachElement`; sanitize the
  collection member itself (for example with `Sanitize.EachElement`).
- Sanitization is whitespace/casing/control-character normalization only. It is not HTML or
  markup sanitization, XSS protection, or output encoding; use a dedicated library for untrusted
  markup.
- There are no asynchronous sanitizers; sanitization is synchronous by design.

## Dynamic messages and names

- Message/name factories are configuration callbacks. They should be pure and cheap; they run when
  an error is emitted, not during rule configuration.
- A `null` or blank factory result falls back to the static override/default message or the
  code-facing member name.
- Dynamic `WithMessage` factories mark the error as authored, like static `WithMessage`; authored
  messages take precedence over a message-pack template for the same `MessageKey`. Dynamic messages
  are exported as `messageIsAuthored: true` in client-rule descriptors.
- `WithName(Func<T, string>)` changes only `{PropertyName}`. Use
  `OverridePropertyName(Func<T, string>)` when `MemberName` itself must change.
- `DisplayNameResolverForInstance` is process-wide when validators are created without a service
  provider. Register `ValidationOptions` through `AddValidation` and pass the request provider
  through `ValidationCallOptions` (ASP.NET Core filters do this automatically) to keep it
  container-scoped; otherwise prefer rule-level dynamic names per request.

## Declarative rule metadata

- Attribute `CustomState` is a string because attribute arguments must be compile-time constants; use
  fluent `WithState` for arbitrary objects.
- The `[Display]` bridge reads only `DisplayAttribute.Name` and `DisplayNameAttribute`; resource
  types, localization, and other `[Display]` properties are not bridged.
- `IValidationRuleDescriptor` gained `ErrorCode`, `DisplayName`, and `CustomState`. This is a
  source-breaking contract change for external implementations; the preview package has no
  compatibility shim.
- Metadata on generated nested/collection rules is applied to the wrapper and therefore to every
  child error; it does not describe the child validator's internal rules separately.

## Declarative conditions and hooks

- Synchronous attribute conditions resolve to methods on the model type. They cannot reference
  external services or arbitrary expressions; use fluent `When` for closures over captured state.
- A property-level `[WhenCondition]`/`[UnlessCondition]` overrides the class-level attribute for that
  property. Multiple conditions on the same scope are not combined.
- `[RequiredIf]`, `[RequiredUnless]`, and `[ConditionalRange]` compare one simple sibling member to a
  compile-time constant. Nested paths, multiple values in one attribute, and arbitrary expressions are
  not supported; use `[WhenCondition]` or fluent `When` for those.
- A value shortcut combined with `[WhenCondition]`/`[UnlessCondition]` requires both conditions to
  pass. The effective method condition still follows property-over-class precedence.
- Declarative `PreValidate`/`OnFailure` hooks are static and have no async variants. Use fluent
  `PreValidateAsync`/`OnFailureAsync` when the hook must perform I/O.
- Hook errors added through `ValidationErrorCollector` are merged with rule errors, but a `false`
  pre-validation result always stops rule evaluation.

## Declarative custom validators

- `[CustomValidator]` methods must be static, non-generic, and declared on the type passed to
  `typeof(...)`. Only `ValueTask<T>` async returns are supported; `Task<T>` is not.
- The `IServiceProvider` parameter receives the caller-supplied provider and can be `null` when no
  provider was configured. Methods that require services should accept a nullable parameter or guard
  before use.
- `ValidationResult` errors with an empty member name are attributed to the property or model path;
  explicit member paths are preserved unchanged.
- Class-level custom rules execute during validation but are not listed in the generated static
  descriptor. Descriptor completeness for model-level rules remains tracked as future work.

## OpenAPI naming policies

The transformer resolves descriptor member names through the active `JsonSerializerOptions` and
`JsonTypeInfo`, so `camelCase`, `snake_case`, `kebab-case`, `PascalCase`, and
`[JsonPropertyName]` are supported. The mapping is cached per `JsonTypeInfo`; a descriptor member
that cannot be mapped falls back to case-insensitive matching. This conversion applies only to the
OpenAPI schema and never changes `ValidationError.MemberName`.

## Schema mapping

- `required` is a presence constraint. Non-nullable value-type members never receive `required`,
  even when `NotNull`/`NotEmpty` is present; a comparison rule expresses non-default values.
- Fluent `NotEmpty` on a string infers `type: string` and `minLength: 1` when the descriptor carries
  the member type; generated `StringNotEmpty` rules always do.
- `IPAddress` maps to `anyOf` `format: "ipv4"` / `format: "ipv6"`.
- `Enum` maps to `x-enum-type` (the CLR enum name); the actual value list comes from the generated
  model schema. It is not expanded into a literal `enum` array.
- Conditional rules (`[RequiredIf]`, `[RequiredUnless]`, `[ConditionalRange]`, and any rule gated by a
  condition) are not emitted as unconditional schema constraints. JSON Schema keeps them as advisory,
  and the client-rules export marks them `serverOnly: true`; enforce them on the server.
- `FileExtensions` uses the vendor extension `x-file-extensions`, not a JSON Schema keyword.
- `Nested` rules compose through `$defs`/`$ref`; `ForEach` composes through `items`. The child
  validator must expose a descriptor.
- `Must`, `Custom`, and conditional rules remain advisory (`x-validation-rules`). Conditions are
  opaque delegates, so they are not translated into `if/then`.

## Client rules runtime

`ClientRulesWriter` exports rule data. The companion `@jeninnet/validation-client` package evaluates
the supported synchronous subset and returns `needsServer` for conditional, nested, custom,
asynchronous, or otherwise unsupported rules. Those rules are marked `serverOnly: true` in the
payload. The server remains authoritative, and client messages are early feedback only.

## Message precedence

`WithMessage` sets `ValidationError.Message` and marks `MessageIsAuthored`; authored messages win
over a message-pack template for the same `MessageKey` when the error is localized. Use a pack
override to change localized default text; the pack remains the fallback for non-authored errors.

## DataAnnotations bridge

- `[JeninnetValidateProperty]` runs only the named member's rules through
  `ValidationCallOptions.IncludeProperties`; it does not reimplement filtering.
- When `[JeninnetValidatable]` is also present, the class-level attribute wins and property-level
  attributes are skipped.
- Unknown member names follow `IncludeProperties` semantics (a no-op), and a custom validator
  without `IValidationOptionsValidator<T>` throws a clear `InvalidOperationException`.
- Nested and indexed paths must match the validator's rule-group member name; a child group is
  targeted as a unit.
- DataAnnotations may skip property-level attributes on null reference values; initialize the
  property or use class-level validation when the member can be null.
- `[SkipValidation]` on the model type or on the validated member makes the bridge return success
  without running the validator; combining it with validation attributes on the same property is
  reported as `VAL0028`.
- Multiple errors are joined into one DataAnnotations message.
- Reflection is used to invoke the generic validator; the bridge is not an AOT target.
- Async rules throw `AsyncValidatorInvokedSynchronouslyException`; reflective invocation unwraps
  `TargetInvocationException` so callers see the original exception.

## GraphQL and gRPC

- The GraphQL middleware validates provided input-object arguments on every field; scalar
  arguments are only validated when a validator is registered for their runtime type.
- GraphQL subscriptions are not specially handled; the same field middleware applies when they run.
- The gRPC interceptor validates unary and server-streaming calls. Client-streaming and
  duplex-streaming calls are not intercepted.
- gRPC status details use the `grpc-status-details-bin` trailer; clients must unpack
  `google.rpc.BadRequest` to read field violations.
- Both packages are optional; Core and Abstractions have no GraphQL or gRPC dependency.

## Localization packs

- The package ships 40 cultures; additional languages follow the contribution path and require a
  native-speaker review before merge.
- Culture packs preserve script and region subtags (`zh-Hans`, `pt-BR`), while the pipeline still
  resolves regional requests such as `fr-FR` through the neutral `fr` pack.
- `ar`, `he`, `ur`, and `fa` contain RTL text; the package does not set UI direction.
- The catalog test enforces key completeness and placeholder parity with English. A missing key or
  changed placeholder fails the test rather than falling back silently.
- `AddDefaultMessagePacks` exposes a mutable `IValidationMessageCatalog` for runtime additions,
  overrides, and removals. It is process-local and in-memory; changes do not survive a restart or
  propagate to other instances. Use the sibling `ILocalizationStore` for persistent or shared
  messages.
- `AddValidation` never replaces `IStringLocalizerFactory`. Register an `ILocalizationStore` and call
  `JeninnetLocalizationExtensions.AddLocalization(services)` explicitly when you want store-backed
  `IStringLocalizer` support.

## Localization bridge

- The ASP.NET Core bridge resolves `IStringLocalizer[error.MessageKey]` first, then the registered
  message packs, then `ValidationError.Message`. A missing resource is detected through
  `LocalizedString.ResourceNotFound`, so a localizer that echoes the key does not hide the pack.
- `IStringLocalizer` has no culture parameter. The bridge scopes `CultureInfo.CurrentUICulture` to
  the lookup and restores it immediately; `CultureInfo.CurrentCulture` is never changed.
- `ForceCulture` wins over an explicitly passed culture. `DefaultCulture` applies only when no
  culture is supplied and no force culture is configured.
- `DisableLocalization` skips only the resx source; the pack and message fallback remain.
- The store-backed factory from `Jeninnet.Localization.Core` requires an `ILocalizationStore`;
  register a store provider when that factory is active.
- `AddValidation()` registers the message pipeline but never activates or replaces a store-backed
  `IStringLocalizerFactory`; register the store and call `AddLocalization` explicitly when you want
  that bridge. This keeps container validation working when no localization provider is configured.
- Resx templates use the named placeholders from the packs (`{PropertyName}`, `{Min}`, ...).
  Positional `IStringLocalizer[key, args]` formatting is not used.
- The package is not an AOT target; `IStringLocalizer`/resource-manager lookups are reflection-based.

## MediatR result adapters

- Adapters are separate optional packages; `Jeninnet.Validation.MediatR` has no dependency on
  ErrorOr, FluentResults, or CSharpFunctionalExtensions.
- `MediatRValidationFailureStrategy.Result` requires a configured mapper; otherwise the behavior
  throws an actionable `InvalidOperationException`.
- ErrorOr and FluentResults preserve member name, severity, code, message key, parameters, and
  custom state in metadata. CSharpFunctionalExtensions stores only a string, so custom state and
  message parameters are omitted.
- `Throw` and `Continue` remain available and unchanged.

## Blazor

- `EditContext.OnValidationRequested` is synchronous. Async validation uses the explicit
  `JeninnetValidator.ValidateAsync()` path from `EditForm.OnSubmit`; `EditContext.Validate()` in
  async mode throws a guidance exception rather than blocking.
- `EditContext.ValidateAsync` exists on .NET 11; the package targets `net11.0` and also exposes its
  own async submit path.
- `FieldChangedDebounce` requires async mode; no delay is applied by default.
- `SkipIfModelStateValid` is the double-validation toggle: it skips an unchanged, already-valid
  model.
- The path resolver supports arrays and `IList`; other collection shapes fall back to the root.

## Observable UI (WPF, MAUI, Avalonia)

- `ObservableValidationAdapter<TModel>` tracks `PropertyChanged` on the root model. For nested
  object changes, call `Validate()`/`ValidateAsync()` or forward the nested event.
- `ValidateOnPropertyChanged` and `SkipIfValid` require the model to implement
  `INotifyPropertyChanged`.
- `MinimumSeverity` filters the errors exposed through `GetErrors`/`HasErrors`; `Validate()`
  still returns the model's unfiltered validity.
- The adapter is a decorator and does not reference a UI framework; it is not an AOT target.

## Metadata and emission

- Generated descriptors are built from the runtime rule lists on first `Describe()` and cached per
  instance. Rules added by `AddCustomValidators()` are included, but every validator instance pays
  the one-time descriptor build; this is the trade-off that keeps custom rules visible.
- Rules with no metadata provider appear as `Custom` advisories with a `Reason` and `Type`; they are
  never discarded silently. Use `ConfigureDescriptor` to add explicit metadata.
- The build-time `ClientRulesCatalog` covers attribute-driven rules only. Runtime `ClientRulesWriter`
  and JSON Schema use the descriptor and include custom/configured entries.
- Nested/child validators are advisory in schema/client output.
- Descriptors expose static custom state, never delegates or predicates.
- Conditional wrappers are flattened to their inner rules with `HasCondition = true`; rule-set and
  dependent wrappers add `RuleSets` and `Dependent` parameters.

## Registration and AOT

- `AddGeneratedValidators` (assembly and `Type[]` overloads) uses reflection and is annotated
  `[RequiresUnreferencedCode]`. Use the generated registry
  (`AddGeneratedValidatorsFromThisAssembly`) or explicit registration for trimming and Native AOT.
- The generated registry is per assembly. A host that references validators from several assemblies
  must call each assembly's generated extension (or register those validators explicitly); there is
  no cross-assembly discovery.
- The generated registry is emitted only when the compilation references
  `Microsoft.Extensions.DependencyInjection`.
- Integration packages follow their host’s AOT posture; DataAnnotations, Blazor, and Observable UI
  are not AOT targets.
- Benchmarks and samples are outside the runtime AOT claim; the assertions package is
  AOT-compatible but test-only.

## Related

- [Compatibility and versions](06-compatibility-and-versions.md)
- [Troubleshooting](../04-operations/04-troubleshooting.md)
- [Production checklist](../04-operations/08-production-checklist.md)
