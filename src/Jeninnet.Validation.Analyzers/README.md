# Jeninnet.Validation.Analyzers

Roslyn incremental source generator and diagnostic analyzers for the Jeninnet validation framework. Targets `netstandard2.0` and ships as a NuGet analyzer.

## Requirements

- .NET 10.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.Analyzers --version 1.0.0-preview.3
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

Ships 17 code-fix providers covering 11 diagnostics: converting attribute parameters to type-safe
literals (`ConvertToEnumLiteralCodeFix`, `ConvertToDateTimeLiteralCodeFix`, etc.), resolving rule
conflicts, rewriting mismatched `RuleFor` member names to `nameof(...)`
(`UseNameofForRuleForMemberCodeFix`), rewriting sync calls to `ValidateAsync()`, and removal fixes
for redundant `[NotNull]` (`VAL0014`) and tautological rules (`VAL0017`).

The other 22 diagnostics are intentional omissions: the correct change depends on the author's
intent (the rule, the member type, the member name, or the missing method). The public
[diagnostics matrix](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/03-reference/03-diagnostics.md)
records the reason for every row, and `scripts/quality/check-analyzer-fix-coverage.ps1` verifies
that the table, the providers, and `AnalyzerReleases.Shipped.md`/`Unshipped.md` agree.

## Dependencies

- `Microsoft.CodeAnalysis.Analyzers`
- `Microsoft.CodeAnalysis.CSharp`
- `Microsoft.CodeAnalysis.CSharp.Workspaces`
- `Microsoft.CodeAnalysis.Workspaces.Common`

## AOT position

**Unsupported (build-time only).** See the [25-package AOT matrix](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/04-operations/02-aot-and-trimming.md#package-matrix).

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
