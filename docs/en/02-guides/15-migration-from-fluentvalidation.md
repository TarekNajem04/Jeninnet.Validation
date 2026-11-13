# Migration from FluentValidation

Jeninnet.Validation deliberately keeps a familiar fluent surface, so most validators migrate with
small mechanical changes.

## Mapping

| FluentValidation | Jeninnet.Validation |
|---|---|
| `AbstractValidator<T>` | `Validator<T>` |
| `RuleFor(x => x.Name)` | `RuleFor(x => x.Name, nameof(T.Name))` |
| `NotNull()` / `NotEmpty()` | `NotNull()` / `NotEmpty()` |
| `Length(min, max)` | `Length(min, max)` |
| `EmailAddress()` | `EmailAddress()` |
| `Matches(pattern)` | `Matches(pattern)` |
| `InclusiveBetween(min, max)` | `InclusiveBetween(min, max)` |
| `GreaterThan(value)` | `GreaterThan(value)` |
| `Must(predicate)` | `Must(predicate)` |
| `MustAsync(predicate)` | `MustAsync(predicate)` |
| `RuleForEach(x => x.Lines)` | `RuleFor(x => x.Lines).ForEach(...)` |
| `SetValidator(...)` | `SetValidator(...)` |
| `ChildRules(...)` | `ChildRules(...)` |
| `Include(...)` | `Include(...)` |
| `RuleSet("name", ...)` | `RuleSet("name", ...)` |
| `Cascade(CascadeMode.Stop)` | `Cascade(CascadeMode.Stop)` |
| `WithMessage` / `WithErrorCode` / `WithSeverity` | `WithMessage` / `WithCode` / `WithSeverity` |
| `OverridePropertyName` | `OverridePropertyName` / `WithName` |
| `ValidateAndThrow` | `ValidateAndThrow` |

## Step-by-step

1. Replace the package reference with `Jeninnet.Validation`.
2. Change the base class to `Validator<T>` and the builder type to `ValidatorRuleBuilder<T>`.
3. Add the member-name argument to `RuleFor` (it is used for paths and localization).
4. Rename `WithErrorCode` to `WithCode`; `Custom` delegates use the Jeninnet error collector.
5. Replace `RuleForEach` with `RuleFor(...).ForEach(...)`.
6. Replace `Transform` with a `Must` predicate over the projected value.
7. Move async validators to `ValidateAsync`; the strict-async guard throws on the sync path for
   async-only validators.
8. Register validators as `IValidator<T>` and use the integration package for your host.

## Behavioral differences

- **Strict async contract.** Sync `Validate` never silently skips async rules; async-only validators
  throw `AsyncValidatorInvokedSynchronouslyException`.
- **Error aggregation.** `ValidateAndThrow` collects all errors before throwing.
- **Descriptors.** `Describe()` exposes frozen metadata; schema/client packages consume it.
- **Integration packages.** ASP.NET Core, MediatR, DataAnnotations, and Blazor adapters ship as
  optional packages.
- **Generated validators.** Attribute authoring emits the same contract as fluent authoring.

## Custom rules

FluentValidation’s `Custom` context exposes a `AddFailure` method. In Jeninnet.Validation the
delegate receives `(value, context, errors)` and adds errors through
`ValidationErrorCollector`. See [Custom rules](07-custom-rules.md).

## Migration assistant (dotnet tool)

The optional `jeninnet` tool scans C# sources and applies the safe rewrites above. It
never guesses semantics: anything it does not understand stays untouched with an explicit
`// TODO(Jeninnet):` marker and a report entry.

```bash
dotnet tool install --global Jeninnet.Validation.Migration --version 1.0.0-preview.1

# Report only; exits 0. Add --format json for machine-readable output.
jeninnet migrate analyze ./src --output migration-report.md

# Preview the codemod without writing files.
jeninnet migrate apply ./src --dry-run

# Rewrite files in place (idempotent: a second run changes nothing).
jeninnet migrate apply ./src
```

Gate it in CI so new FluentValidation code cannot sneak back in:

```bash
jeninnet migrate analyze ./src --check
```

The report prints a **migration readiness score** (the share of findings the codemod can apply
without human review) and the number of files that are fully convertible. Use it to estimate the
remaining manual work before deleting FluentValidation.

What the tool converts (all from FluentValidation, with the reason Jeninnet differs):

- `AbstractValidator<T>` to `Validator<T>` with `Configure`: Jeninnet builds rules in the
  overridden `Configure` method instead of the constructor.
- Single-argument `RuleFor(x => x.Name)` to `RuleFor(x => x.Name, nameof(T.Name))`
  (dotted paths become a `"A.B"` literal): Jeninnet requires the explicit member name for
  paths and localized messages.
- `WithErrorCode` to `WithCode`: error codes travel on `WithCode` in Jeninnet.

Constructors with parameters, `Custom(`, and `Transform(` are left in place with review
items: `Custom`/`Transform` are FluentValidation escape hatches with no direct equivalent
in Jeninnet's error-collector model. Complex selectors get `builder.RuleFor(...)` plus a
review item for the missing member name. Matching is name-based and syntactic (no project
references required); the report states this assumption. Exit codes: `0` success,
`1` changes pending or error, `2` usage error.

## Verify

Run the existing validator tests against the migrated validator. The assertions package can replace
FluentValidation’s test helpers:

```csharp
result.ShouldHaveValidationErrors()
      .ForMember(nameof(Customer.Email))
      .ContainsRule(ValidationMessageKeys.EMAIL);
```

## Related

- [Fluent authoring](01-fluent-authoring.md)
- [Custom rules](07-custom-rules.md)
