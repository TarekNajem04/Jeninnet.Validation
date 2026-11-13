# Core API

## Contracts

| Type | Purpose |
|---|---|
| `IValidator<T>` | Combined sync + async validator. |
| `ISyncValidator<T>` | `Validate(instance)` and `Validate(instance, ruleSets)`. |
| `IAsyncValidator<T>` | `ValidateAsync(instance, cancellationToken)` and the rule-set overload. |
| `IValidationOptionsValidator<T>` | Executes a validator with `ValidationCallOptions`; implemented by fluent and generated validators. |
| `IValidatorSelector` | Per-call top-level rule selection; `DefaultValidatorSelector`, `RulesetValidatorSelector`, and `CompositeValidatorSelector` are built in. |
| `IValidationContextValidator<T>` | Executes a validator with an inherited `ValidationContext<T>`; used by nested, collection, and included validators. |
| `InheritanceValidator<T>` | Dispatches validation to the validator registered for the actual runtime type. |
| `IValidationDescriptorProvider` | `Describe()` metadata. |
| `IValidationDescriptor` | `ValidatorName`, `ValidatedType`, `Members`, `RulesForMember`. |
| `IValidationMemberDescriptor` | `Name`, `Rules`. |
| `IValidationRuleDescriptor` | `Kind`, `MessageKey`, `Severity`, `ErrorCode`, `DisplayName`, `CustomState`, `HasCondition`, `Parameters`. |

## Value types

`ValidationResult` is a readonly struct:

```csharp
public readonly struct ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<ValidationError> Errors { get; }
    public static ValidationResult Success { get; }
    public static ValidationResult FromErrors(ValidationErrorCollector errors);
}
```

`ValidationError`:

| Member | Type |
|---|---|
| `MemberName` | `string` |
| `MessageId` | `MessageId` |
| `MessageKey` | `string?` |
| `MessageParameters` | `IDictionary<string, object>?` |
| `Message` | `string?` |
| `ErrorCode` | `string?` |
| `Severity` | `ValidationErrorSeverity` |
| `CustomState` | `object?` |

## Fluent API

```csharp
public abstract class Validator<T> : ValidatorDefinition<T>, IValidator<T>
{
    protected abstract void Configure(ValidatorRuleBuilder<T> builder);
    public ValidationResult Validate(T instance);
    public ValidationResult Validate(T instance, IEnumerable<string> ruleSets);
    public ValidationResult Validate(T instance, ValidationCallOptions options);
    public ValueTask<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
    public void ValidateAndThrow(T instance);
    public ValueTask ValidateAndThrowAsync(T instance, CancellationToken cancellationToken = default);
}
```

`ValidatorRuleBuilder<T>` exposes `RuleFor`, `RuleSet`, `Include`, `When`/`Unless`, and async group
variants. `IRuleBuilder<T, TProperty>` exposes `WithState(object?)` and context-aware
`WithState(Func<T, TProperty, ValidationContext<T>, TState>)` overloads. `Validator<T>` exposes a
protected `OnRuleAdded(IRuleDefinition)` hook, and `IRuleDefinitionOptions` lets the hook change the
default message, error code, and severity. `ValidationPath.GetParent`, `IsChildOf`, and
`GetParentChain` handle member and indexer chains. The `PropertyChain` readonly record struct wraps a
path with `FromPath`, `Add`, `GetParentChain`, and `IsChildChainOf` for advanced rule authors.

## Execution options

```csharp
public sealed record ValidationCallOptions
{
    public IEnumerable<string>? RuleSets { get; init; }
    public bool IncludeAllRuleSets { get; init; }
    public bool ThrowOnFailures { get; init; }
    public CascadeMode? CascadeMode { get; init; }
    public bool FailFast { get; init; }
    public IReadOnlyCollection<string>? IncludeProperties { get; init; }
    public IReadOnlyCollection<string>? ExcludeProperties { get; init; }
    public IReadOnlyCollection<string>? ExcludeRuleSets { get; init; }
    public IServiceProvider? ServiceProvider { get; init; }
}
```

`RuleSets = null` or an empty collection runs all rules, including named sets. Use `["default"]` for
ungrouped rules only, or `IncludeAllRuleSets = true` to select every named set explicitly.
`ThrowOnFailures = true` throws `ValidationException` with all collected errors.

`ValidationStrategy` is the chained builder equivalent and adds `IncludeAllRuleSets()`,
`IncludeRulesNotInRuleSet()`, and `ThrowOnFailures()`.

Both fluent (`Validator<T>`) and generated (`[Validate]`) validators implement
`IValidationOptionsValidator<T>`, so options, fail-fast, property/rule-set filtering, and
`ClassLevelCascadeMode` behave identically on both paths. `ValidationOptionsExtensions` exposes
the same overloads on `IValidator<T>` plus `ValidateAndThrow`/`ValidateAndThrowAsync`. A custom
validator that does not implement `IValidationOptionsValidator<T>` throws `NotSupportedException`
when options are supplied; the basic overloads continue to work.

Both paths also implement `IValidationContextValidator<T>`. Nested (`SetValidator`), collection
(`ForEach`), and included (`Include`) validators receive the parent context: `Root`,
`RootData`, `ServiceProvider`, `RuleSets`, `FailFast`, and the current `MemberPath`. Error fields
(`MessageKey`, `MessageParameters`, `ErrorCode`, `Severity`, `CustomState`) survive the boundary.

## Exceptions

| Exception | When |
|---|---|
| `ValidationException` | `ValidateAndThrow` / `ValidateAndThrowAsync`; carries `Errors`. |
| `AsyncValidatorInvokedSynchronouslyException` | Sync `Validate` on an async-only validator. |
| `ValidationAssertionException` | Assertion helpers. |

## Configuration

`services.AddValidation(options => options with { ... })` registers container-scoped options:
`DefaultCascadeMode`/`DefaultRuleLevelCascadeMode`, `DefaultClassLevelCascadeMode`,
`DisplayNameResolver`, `DisplayNameResolverForInstance`, `PropertyNameResolver`,
`ValidatorFactory`. ASP.NET Core filters and any call that supplies
`ValidationCallOptions.ServiceProvider` use the container instance, so multiple containers in one
process stay isolated. The instance-aware resolver receives `(Type, memberName, instance)` and
takes precedence over the legacy resolver when it returns a non-empty name.
`PropertyNameResolver` changes the code-facing `ValidationError.MemberName` for rules without an
explicit override. `ValidationConfiguration.CreateValidator<T>(services)` asks `ValidatorFactory`
for a validator when DI has no `IValidator<T>` registration.

`ValidationConfiguration.Configure(options => options with { ... })` remains the process-wide
fallback used when no service provider is available (for example direct `new` construction in a
unit test).

## Related

- [Rule reference](02-rule-reference.md)
- [Metadata](04-metadata.md)
