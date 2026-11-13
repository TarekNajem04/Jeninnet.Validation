# Jeninnet.Validation.Analyzers

Roslyn incremental source generator and diagnostic analyzers for the Jeninnet validation framework. Targets `netstandard2.0` and ships as a NuGet analyzer.

## Requirements

- .NET 11.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.Analyzers --version 1.0.0-preview.1
```

## Source Generation

The `ValidatorGenerator` (`IIncrementalGenerator`) scans for types decorated with `[Validate]` and:

- Inspects validation attributes on each property (e.g. `[NotEmpty]`, `[Email]`, `[InclusiveBetween]`, `[Length]`, `[Regex]`, `[CreditCard]`, `[GreaterThan]`, etc.)
- Emits partial `*Validator` classes that implement `IValidator<T>` with all rules wired up
- Emits `{Type}ValidationExtensions` with `Validate()`/`ValidateAsync()` model shortcuts; disable
  per model with `[Validate(GenerateModelExtensions = false)]`
- Emits a module initializer that registers every generated validator in the central catalog, so
  `AddValidation()` (in `Jeninnet.Validation.Catalog`) needs no assembly scanning, plus a per-assembly
  `AddGeneratedValidatorsFromThisAssembly()` entry point
- Handles string, numeric, date/time, comparison, property-comparison, collection (`[ForEach]`), nested, and conditional rules

## Diagnostic Analyzers

| Analyzer | Purpose |
|----------|---------|
| `ValidationRuleAnalyzer` | Detects conflicting or redundant rule combinations |
| `RuleSuggestionAnalyzer` | Suggests missing validation rules (e.g. `[NotNull]` before numeric rules) |
| `AttributeMisuseAnalyzer` | Validates correct usage of validation attributes across different data types |
| `RuleForMemberNameAnalyzer` | Detects `RuleFor` member names that point at a different model member (`VAL0023`) |
| `CollectionRuleAnalyzer` | Detects `Count`/`MinCount`/`MaxCount` on non-collections (`VAL0024`) |
| `SyncAdapterAsyncValidatorAnalyzer` | Detects async-only validators in the synchronous DataAnnotations adapter (`VAL0025`) |

## Code Fixes

Includes code fix providers for converting attribute parameters to type-safe literals (`ConvertToEnumLiteralCodeFix`, `ConvertToDateTimeLiteralCodeFix`, etc.), resolving rule conflicts, and rewriting mismatched `RuleFor` member names to `nameof(...)` (`UseNameofForRuleForMemberCodeFix`).

## Dependencies

- `Microsoft.CodeAnalysis.Analyzers`
- `Microsoft.CodeAnalysis.CSharp`
- `Microsoft.CodeAnalysis.CSharp.Workspaces`
- `Microsoft.CodeAnalysis.Workspaces.Common`

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
