# Benchmarks

Measured evidence for the performance claims in this book.
These are **local ShortRun results**, not a CI gate; hardware variance is real
and the error columns are wide. Treat them as directional evidence, not as
absolute numbers.

## Machine profile

| Item | Value |
|---|---|
| OS | Windows 11 (10.0.26300.9974) |
| CPU | Intel Core i7-8850H 2.60 GHz (Coffee Lake), 1 CPU, 12 logical / 6 physical cores |
| Runtime | .NET 10.0.15, X64 RyuJIT x86-64-v3 |
| SDK | .NET SDK 11.0.100 |
| Tool | BenchmarkDotNet 0.16.0-preview.2 |
| Job | `ShortRun`: 3 warmup, 3 measured iterations, 1 launch |
| Configuration | Release, no debugger, pre-JIT |

> **Measurement date.** The validation table below was re-measured on 2026-11-24 on the pinned
> toolchain. The `net11.0` run measured identical allocations (2,496 / 4,936 / 14,456 B and 456 B);
> its means are comparable within `ShortRun` noise. Means are directional; allocation counters are
> deterministic. The localization and Base64 tables remain from an earlier run and are still
> directional.
>
> **Tooling.** In BenchmarkDotNet 0.16 an `Allocated` value of `-` means zero managed allocations;
> the table shows `0 B` for readability.

Reproduce with:

```powershell
dotnet run -c Release -f net10.0 --project src/Jeninnet.Validation.Benchmarks -- --filter '*ValidationBenchmarks*'
dotnet run -c Release -f net11.0 --project src/Jeninnet.Validation.Benchmarks -- --filter '*ValidationBenchmarks*'
dotnet run -c Release -f net10.0 --project src/Jeninnet.Validation.Benchmarks -- --filter '*LocalizationBenchmarks*'
dotnet run -c Release -f net10.0 --project src/Jeninnet.Validation.Benchmarks -- --filter '*Base64ValidationBenchmarks*'
```

## Validation engine

Models have 5, 10, and 30 validated properties. `Generated` uses real
source-generated validators (the benchmark project references the analyzer);
`Fluent` uses `Validator<T>`. `Invalid` models fail every rule; `FailFast`
stops at the first error.

| Method | Mean | Error | StdDev | Ratio | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|
| Fluent_Small_Valid | 223.7 ns | 319.6 ns | 17.52 ns | 1.00 | 0 B | NA |
| Generated_Small_Valid | 172.9 ns | 186.6 ns | 10.23 ns | 0.78 | 0 B | NA |
| Fluent_Small_Invalid | 2,091.8 ns | 608.1 ns | 33.33 ns | 9.39 | 2,496 B | NA |
| Generated_Small_Invalid | 1,850.1 ns | 3,105.9 ns | 170.25 ns | 8.30 | 2,496 B | NA |
| Fluent_Medium_Valid | 219.3 ns | 303.7 ns | 16.65 ns | 0.98 | 0 B | NA |
| Generated_Medium_Valid | 135.1 ns | 201.7 ns | 11.06 ns | 0.61 | 0 B | NA |
| Fluent_Medium_Invalid | 3,258.9 ns | 2,330.1 ns | 127.72 ns | 14.63 | 4,936 B | NA |
| Generated_Medium_Invalid | 3,586.1 ns | 7,595.1 ns | 416.31 ns | 16.09 | 4,936 B | NA |
| Fluent_Large_Valid | 576.8 ns | 151.7 ns | 8.32 ns | 2.59 | 0 B | NA |
| Generated_Large_Valid | 387.2 ns | 718.5 ns | 39.39 ns | 1.74 | 0 B | NA |
| Fluent_Large_Invalid | 10,656.6 ns | 25,027.5 ns | 1,371.84 ns | 47.82 | 14,456 B | NA |
| Generated_Large_Invalid | 11,003.5 ns | 21,957.6 ns | 1,203.57 ns | 49.38 | 14,456 B | NA |
| Fluent_Small_Invalid_FailFast | 389.1 ns | 715.5 ns | 39.22 ns | 1.75 | 456 B | NA |

### Reading the numbers

- **Generated vs fluent:** generated validators are faster on the valid path
  (173 vs 224 ns for 5 properties, 135 vs 219 ns for 10, 387 vs 577 ns for 30).
  Invalid paths are within run-to-run noise; error construction dominates.
- **Happy-path allocation:** **0 B per operation**. The
  `ValidationErrorCollector` and `RootContextData` are pooled and reused, and
  the rule-selection fast path returns the rule array without materialization.
  This confirms the zero-allocation happy-path contract.
- **Error path:** allocations scale with the number of errors
  (2,496 B for 5 errors, 4,936 B for 10, 14,456 B for 30). Fail-fast cuts both
  time and allocation (389 ns / 456 B vs 2,092 ns / 2,496 B). Each error carries
  its rule parameters plus `PropertyName`, `PropertyPath`, and `PropertyValue`
  in one pre-sized dictionary.
- **Benchmark-driven change:** `CompositeValidator` uses an indexed array
  fast path and a no-filter `SelectRules` return instead of `foreach` over
  `IEnumerable` plus materialization. Together these removed the residual
  32 B/op measured during early benchmarking; the happy path is now zero. A
  later allocation audit removed the second error dictionary and cut
  invalid-path allocation by 46–48% without changing the public error shape.
  The allocation contract and stress evidence live in
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

## CI decision

Continuous performance-regression gating is **not enabled**. BenchmarkDotNet
needs a quiet, pinned machine; hosted CI runners are too noisy for stable
thresholds. The suite remains a manual evidence tool, run before releases and
after engine changes that could shift numbers. A future self-hosted runner could
add a budget gate; that is a separate decision.
