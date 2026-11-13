# Benchmarks

Measured evidence for the performance claims in this book.
These are **local ShortRun results**, not a CI gate; hardware variance is real
and the error columns are wide. Treat them as directional evidence, not as
absolute numbers. For the same-model comparison against FluentValidation and
DataAnnotations, see [Peer benchmarks](10-peer-benchmarks.md).

## Machine profile

| Item | Value |
|---|---|
| OS | Windows 11 (10.0.26300.9818) |
| CPU | Intel Core i7-8850H 2.60 GHz (Coffee Lake), 1 CPU, 12 logical / 6 physical cores |
| Runtime | .NET 10.0.14, X64 RyuJIT x86-64-v3 |
| SDK | .NET SDK 10.0.204 |
| Tool | BenchmarkDotNet 0.15.8 (historical) |
| Job | `ShortRun`: 3 warmup, 3 measured iterations, 1 launch |
| Configuration | Release, no debugger, pre-JIT |

> **Historical measurement.** These numbers were captured with the .NET 10 SDK. The repository now
> pins .NET 11 SDK `11.0.100` and C# 15; re-run the suite before treating the numbers as a
> current performance claim.
>
> **Tooling.** BenchmarkDotNet 0.15.8 does not recognize the `net11.0` runtime moniker and throws
> `GetRuntimeVersion not implemented for NotRecognized` before running any benchmark. The
> repository now uses **BenchmarkDotNet 0.16.0-preview.2**, the first release with .NET 11
> support. In 0.16 an `Allocated` value of `-` means zero managed allocations; older versions
> printed `0 B` for the same result.

Reproduce with:

```powershell
dotnet run -c Release --project src/Jeninnet.Validation.Benchmarks -- --filter '*ValidationBenchmarks*'
dotnet run -c Release --project src/Jeninnet.Validation.Benchmarks -- --filter '*LocalizationBenchmarks*'
dotnet run -c Release --project src/Jeninnet.Validation.Benchmarks -- --filter '*Base64ValidationBenchmarks*'
```

## Validation engine

Models have 5, 10, and 30 validated properties. `Generated` uses real
source-generated validators (the benchmark project references the analyzer);
`Fluent` uses `Validator<T>`. `Invalid` models fail every rule; `FailFast`
stops at the first error.

| Method | Mean | Error | StdDev | Ratio | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|
| Fluent_Small_Valid | 165.24 ns | 152.51 ns | 8.360 ns | 1.00 | 0 B | NA |
| Generated_Small_Valid | 134.63 ns | 186.36 ns | 10.215 ns | 0.82 | 0 B | NA |
| Fluent_Small_Invalid | 712.51 ns | 716.75 ns | 39.288 ns | 4.32 | 968 B | NA |
| Generated_Small_Invalid | 676.21 ns | 312.56 ns | 17.132 ns | 4.10 | 968 B | NA |
| Fluent_Medium_Valid | 147.78 ns | 235.06 ns | 12.884 ns | 0.90 | 0 B | NA |
| Generated_Medium_Valid | 95.76 ns | 32.42 ns | 1.777 ns | 0.58 | 0 B | NA |
| Fluent_Medium_Invalid | 1,358.55 ns | 2,213.67 ns | 121.339 ns | 8.24 | 2,104 B | NA |
| Generated_Medium_Invalid | 1,397.88 ns | 303.95 ns | 16.660 ns | 8.47 | 2,104 B | NA |
| Fluent_Large_Valid | 401.29 ns | 230.56 ns | 12.638 ns | 2.43 | 0 B | NA |
| Generated_Large_Valid | 257.71 ns | 227.69 ns | 12.481 ns | 1.56 | 0 B | NA |
| Fluent_Large_Invalid | 4,087.47 ns | 4,375.20 ns | 239.820 ns | 24.78 | 6,024 B | NA |
| Generated_Large_Invalid | 3,871.51 ns | 1,529.07 ns | 83.813 ns | 23.47 | 6,024 B | NA |
| Fluent_Small_Invalid_FailFast | 178.67 ns | 88.25 ns | 4.837 ns | 1.08 | 208 B | NA |

### Reading the numbers

- **Generated vs fluent:** generated validators are faster on the valid path
  (135 vs 165 ns for 5 properties, 96 vs 148 ns for 10, 258 vs 401 ns for 30).
  Invalid paths are within run-to-run noise; error construction dominates.
- **Happy-path allocation:** **0 B per operation**. The
  `ValidationErrorCollector` and `RootContextData` are pooled and reused, and
  the rule-selection fast path returns the rule array without materialization.
  This confirms the zero-allocation happy-path contract.
- **Error path:** allocations scale with the number of errors
  (968 B for 5 errors, 2,104 B for 10, 6,024 B for 30). Fail-fast cuts both
  time and allocation (179 ns / 208 B vs 713 ns / 968 B).
- **Benchmark-driven change:** `CompositeValidator` now uses an indexed array
  fast path and a no-filter `SelectRules` return instead of `foreach` over
  `IEnumerable` plus materialization. Together these removed the residual
  32 B/op measured during early benchmarking; the happy path is now zero. The
  allocation contract and stress evidence live in
  [`07-allocation-audit.md`](07-allocation-audit.md).

## Localization

Single built-in provider (`DefaultValidationMessageProvider`) with the built-in
packs; the measurements below use English and Arabic, and the provider is the
exactly-one-provider model used by the localization pipeline.

| Method | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|
| EnglishHit | 26.24 ns | 28.031 ns | 1.536 ns | 0 B |
| ArabicHit | 28.13 ns | 31.973 ns | 1.753 ns | 0 B |
| MissingKey | 25.78 ns | 4.394 ns | 0.241 ns | 0 B |
| MissingCulture | 43.36 ns | 16.249 ns | 0.891 ns | 0 B |

## Base64 rule

| Method | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|
| ValidBase64 | 80.70 ns | 59.50 ns | 3.262 ns | 0 B |

The Base64 rule and the engine's happy path are both allocation-free.

## Peer comparison

A direct peer run is published in [Peer benchmarks](10-peer-benchmarks.md):
on the repository machine the valid path measured 81.7 ns / 0 B for Jeninnet
versus 226.7 ns / 632 B for FluentValidation. Treat those numbers as a
machine-specific baseline, not a universal guarantee; run the benchmark harness
on your own hardware before making capacity decisions. Generated and fluent
validators share the same engine and allocation profile.

## CI decision

Continuous performance-regression gating is **not enabled**. BenchmarkDotNet
needs a quiet, pinned machine; hosted CI runners are too noisy for stable
thresholds. The suite remains a manual evidence tool, run before releases and
after engine changes that could shift numbers. A future self-hosted runner could
add a budget gate; that is a separate decision.
