# API Reference

The generated API pages are built by DocFX from the XML documentation of the packable packages. This page
is the map: it tells you which namespace to open and which type to start from.

## Assembly and namespace map

A type’s namespace is not always the same as its assembly. The value types (`ValidationResult`,
`ValidationError`, `MessageId`, `ValidationErrorSeverity`) live in
`Jeninnet.Validation.Abstractions.dll` but use the `Jeninnet.Validation` namespace so consuming code
needs a single `using`.

| Namespace | Key types | Package |
|---|---|---|
| `Jeninnet.Validation` | `ValidationResult`, `ValidationError`, `ValidationException`, `MessageId`, `ValidationErrorSeverity`, `CascadeMode`, `ValidationCallOptions`, `ValidationStrategy`, `ValidationErrorCollector`, `RootContextData`, `ValidationDiagnostics`, `IValidationDiagnosticsSink` | Core / Abstractions |
| `Jeninnet.Validation.Abstractions` | `IValidator<T>`, `ISyncValidator<T>`, `IAsyncValidator<T>`, `IGeneratedValidator`, rule definition contracts | Abstractions |
| `Jeninnet.Validation.Metadata` | `IValidationDescriptor`, `IValidationMemberDescriptor`, `IValidationRuleDescriptor`, `IValidationDescriptorProvider` | Abstractions |
| `Jeninnet.Validation.Fluent` | `Validator<T>`, `ValidatorDefinition<T>`, `ValidatorRuleBuilder<T>`, `IRuleBuilder<T, TProperty>`, `InheritanceValidator<T>`, `InheritanceValidatorMap<T>`, `ApplyConditionTo`, all fluent rule extensions | Core |
| `Jeninnet.Validation.Rules` | Built-in rule classes (`NotNullRule`, `LengthRule`, `MustRule`, `ForEachRule`, …), reusable `RuleTemplate` / `RuleTemplate<TValue>` / `RuleTemplate<T, TValue>`, and optional domain rules (`IbanRule`, `VatNumberRule`, `PasswordStrengthRule`, …) | Core / Rules.Domain |
| `Jeninnet.Validation.Core` | `ValidatorFactory`, `IPropertyAccessor<T, TProperty>`, `ISyncValidationRule<T>`, `IAsyncValidationRule<T>`, `CompositeValidator<T>` | Core |
| `Jeninnet.Validation.Attributes` | `[Validate]`, `[NotEmpty]`, `[Length]`, `[Email]`, `[Regex]`, `[InclusiveBetween]`, `[PropertyEqual]`, `[ForEach]`, `[UniqueItems]`, `[Nested]`, `[RuleSet]`, and optional domain attributes (`[Iban]`, `[VatNumber]`, `[PostalCode]`, `[NationalId]`, `[PasswordStrength]`, `[Uuid]`, `[Ulid]`) | Core / Rules.Domain |
| `Jeninnet.Validation.Attributes.Common` | `[NotNull]`, `[Enum]`, `[In]`, `[NotIn]`, `[ScalePrecision]` | Core |
| `Jeninnet.Validation.Attributes.String` | `[Phone]`, `[Url]`, `[IPAddress]` | Core |
| `Jeninnet.Validation.Configuration` | `ValidationConfiguration`, `ValidationOptions`, `ValidationMessageKeys` | Core |
| `Jeninnet.Validation.Extensions` | `AddGeneratedValidators` | Core |
| `Jeninnet.Validation.Localization` | `AddValidationLocalization`, `AddDefaultMessagePacks`, `ValidationMessagePack`, `ValidationMessagePackOptions`, `IValidationMessageCatalog`, `LocalizeValidationErrorAsync` | Localization |
| `Jeninnet.Validation.AspNetCore` | `AddValidationEndpoints`, `AddRequestValidation<T>`, `AddMvcValidation`, `ToProblemErrors`, `[SkipJeninnetValidation]`, `[JeninnetValidationRuleSets]` | AspNetCore |
| `Jeninnet.Validation.JsonSchema` | `ValidationSchemaWriter`, `ValidationSchemaOptions`, `ValidationSchemaMapper` | JsonSchema |
| `Jeninnet.Validation.OpenApi` | `AddSchemaConstraintsFromValidators`, `ValidationSchemaRegistry`, `ValidationSchemaTransformer` | OpenApi |
| `Jeninnet.Validation.ClientRules` | `ClientRulesWriter`, `ClientRulesOptions` | ClientRules |
| `Jeninnet.Validation.Rules.Domain` | `IbanRule<T>`, `VatNumberRule<T>`, `PostalCodeRule<T>`, `NationalIdRule<T>`, `PasswordStrengthRule<T>`, `UuidRule<T>`, `UlidRule<T>` and the matching attributes | Rules.Domain |
| `Jeninnet.Validation.MediatR` | `AddValidationPipeline`, `ValidationBehavior`, `ValidationResultAccessor`, `[MediatRValidationRuleSets]` | MediatR |
| `Jeninnet.Validation.MediatR.ErrorOr` | `ErrorOrMediatRExtensions`, `ErrorOrValidationResultMapper<TValue>` | MediatR.ErrorOr |
| `Jeninnet.Validation.MediatR.FluentResults` | `FluentResultsMediatRExtensions`, `FluentResultsValidationResultMapper<TValue>` | MediatR.FluentResults |
| `Jeninnet.Validation.MediatR.CSharpFunctionalExtensions` | `CSharpFunctionalExtensionsMediatRExtensions`, `CSharpFunctionalExtensionsValidationResultMapper<TValue>` | MediatR.CSharpFunctionalExtensions |
| `Jeninnet.Validation.Extensions.Validation` | `AddJeninnetValidation`, `JeninnetValidationServiceCollectionExtensions` | Extensions.Validation |
| `Jeninnet.Validation.Observable` | `ObservableValidationAdapter<TModel>`, `ObservableValidationOptions` | Observable |
| `Jeninnet.Validation.GraphQL` | `GraphQLValidationMiddleware`, `GraphQLValidationOptions`, `[GraphQLValidationRuleSets]` | GraphQL |
| `Jeninnet.Validation.Grpc` | `ValidationInterceptor`, `GrpcValidationOptions`, `[GrpcValidationRuleSets]` | Grpc |
| `Jeninnet.Validation.DataAnnotations` | `[JeninnetValidatable]`, `JeninnetDataAnnotationsAdapter` | DataAnnotations |
| `Jeninnet.Validation.Blazor` | `<JeninnetValidator TModel>`, `EditContextValidationCoordinator`, `FieldIdentifierResolver` | Blazor |
| `Jeninnet.Validation.Assertions` | `ShouldHaveValidationErrors`, `TestValidate`, `TestValidateAsync`, `ValidationResultAssertions`, `MemberAssertions` | Assertions |
| `Jeninnet.Validation.Generators` | `ValidatorGenerator` | Analyzers |
| `Jeninnet.Validation.Diagnostics` | `DiagnosticDescriptors` (analyzer IDs, Analyzers package); `AddValidationDiagnostics`, `ValidationDiagnosticsSink`, `ValidationDiagnosticsOptions` (Diagnostics package) | Analyzers / Diagnostics |

## Core types at a glance

### `ValidationResult`

```csharp
public readonly struct ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<ValidationError> Errors { get; }
    public static ValidationResult Success { get; }
}
```

`ValidationResult` is a value type; there is no null-result path. `Errors` is empty when the result
is valid.

### `ValidationError`

| Member | Type | Meaning |
|---|---|---|
| `MemberName` | `string` | Code-facing path (`Address.Street`, `Lines[0].Quantity`); `WithName` can override it. |
| `MessageId` | `MessageId` | Message identity; `MessageId.Invalid` for custom failures. |
| `MessageKey` | `string?` | Localization key. |
| `MessageParameters` | `IDictionary<string, object>?` | Formatting values, including `PropertyName`. |
| `Message` | `string?` | Custom message or `null`; localization resolves `MessageKey` separately. |
| `ErrorCode` | `string?` | Machine-readable code. |
| `Severity` | `ValidationErrorSeverity` | `Info`, `Warning`, or `Error`. |
| `CustomState` | `object?` | Arbitrary state attached by the rule. |

### `IValidator<T>`

`IValidator<T>` combines `ISyncValidator<T>` and `IAsyncValidator<T>`:

```csharp
public interface ISyncValidator<T>
{
    ValidationResult Validate(T instance);
    ValidationResult Validate(T instance, IEnumerable<string> ruleSets);
}

public interface IAsyncValidator<T>
{
    ValueTask<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
    ValueTask<ValidationResult> ValidateAsync(T instance, IEnumerable<string> ruleSets, CancellationToken cancellationToken = default);
}
```

Fluent `Validator<T>` adds `Validate(T, ValidationCallOptions)`,
`Validate(T, Action<ValidationStrategy>)`, `ValidateAndThrow`, and `ValidateAndThrowAsync`.

### `ValidatorRuleBuilder<T>`

| Member | Purpose |
|---|---|
| `RuleFor(expression, memberName)` | Starts a member group and returns `IRuleBuilder<T, TProperty>`. |
| `RuleSet(name, configure)` | Adds a named rule set. |
| `Include(validator)` | Composes another validator’s rules at the root. |
| `When` / `Unless` | Gates a group of rules. |
| `WhenAsyncRule` / `UnlessAsyncRule` | Async group gates. |

### `ValidationCallOptions`

```csharp
public sealed record ValidationCallOptions
{
    public IEnumerable<string>? RuleSets { get; init; }
    public CascadeMode? CascadeMode { get; init; }
    public bool FailFast { get; init; }
    public IReadOnlyCollection<string>? IncludeProperties { get; init; }
    public IReadOnlyCollection<string>? ExcludeProperties { get; init; }
    public IReadOnlyCollection<string>? ExcludeRuleSets { get; init; }
    public IServiceProvider? ServiceProvider { get; init; }
}
```

`ValidationStrategy` is the chained equivalent and supports `IncludeProperties`,
`ExcludeProperties`, `ExcludeRuleSets`, `WithCascadeMode`, and `FailFast`.

## Registration APIs

| API | Package | Notes |
|---|---|---|
| `services.AddScoped<IValidator<T>, TValidator>()` | Core | Explicit, AOT-safe. |
| `services.AddGeneratedValidators(assembly)` | Core | Reflection-based discovery; annotated `[RequiresUnreferencedCode]`. |
| `services.AddGeneratedValidators(types)` | Core | Registers the given types; still inspects interfaces at runtime. |
| `services.AddValidationEndpoints()` | AspNetCore | ProblemDetails + exception handler for minimal APIs. |
| `services.AddValidationDiagnostics()` | Diagnostics | Optional `ActivitySource`/`Meter` instrumentation. |
| `services.AddMvcValidation()` | AspNetCore | MVC action filter. |
| `services.AddValidationPipeline()` | MediatR | Pipeline behavior + result accessor. |
| `services.AddDefaultMessagePacks()` | Localization | Message packs for 40 cultures and the mutable `IValidationMessageCatalog`. |
| `services.AddSchemaConstraintsFromValidators(...)` | OpenApi | Registers descriptors and the `v1` transformer. |

## Metadata APIs

`Describe()` is available on fluent and generated validators through
`IValidationDescriptorProvider`:

```csharp
var descriptor = new CustomerValidator().Describe();

foreach (var member in descriptor.Members)
{
    foreach (var rule in member.Rules)
    {
        Console.WriteLine($"{member.Name}: {rule.Kind} ({rule.MessageKey})");
    }
}
```

See [Metadata](04-metadata.md) for the descriptor contracts and their limits.

## Public API baselines

The generated pages are informative; the compatibility contract is the pair of committed baseline
files in every packable project:

| File | Purpose |
|---|---|
| `PublicAPI.Shipped.txt` | The reviewed surface that has shipped. |
| `PublicAPI.Unshipped.txt` | Additive APIs staged for the next release. |

`Microsoft.CodeAnalysis.PublicApiAnalyzers` compares the compiled surface with these files at build
time. An unrecorded API (`RS0016`) or a stale entry (`RS0017`) fails the build because the repository
treats warnings as errors. The same check runs in the `api-baselines` CI job.

- Add API: run `pwsh scripts/api/sync-public-api.ps1`, review `Unshipped`, and commit it with the
  source change.
- Remove API: follow the obsolete/major policy in
  [Compatibility and versions](06-compatibility-and-versions.md); remove the entry only when the
  removal is intentional.
- The detailed workflow, including the Razor/Blazor exception, is in
  [scripts/api/README.md](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/scripts/api/README.md).

## Reading the generated pages

- Types are grouped by namespace.
- Each member page lists parameters, return types, exceptions, and remarks.
- `inheritdoc` members point to the contract documentation.
- XML documentation is enforced by CS1591 for shipping projects.
- The analyzer and generator namespaces (`Jeninnet.Validation.Analyzers.*`,
  `Jeninnet.Validation.Generators.*`) are tooling internals; use the diagnostics reference instead.

Generate the site locally:

```powershell
dotnet tool restore
dotnet docfx metadata docs/docfx.json --warningsAsErrors
dotnet docfx build docs/docfx.json --warningsAsErrors
pwsh scripts/docs/check-api-reference.ps1
```

`docs/api` and `docs/_site` are generated and git-ignored. The API-reference check fails when a
packable project is missing from the DocFX metadata or from `docs/docfx.json`.

## Related

- [Architecture](00-architecture.md)
- [Core API](01-core-api.md)
- [Rule reference](02-rule-reference.md)
- [Metadata](04-metadata.md)
