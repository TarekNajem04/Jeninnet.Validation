# Allocation Audit

Evidence for the "zero-allocation beyond errors" contract in
`CompositeValidator<T>`. The benchmark numbers live in
[`06-benchmarks.md`](06-benchmarks.md); this note records the lifecycle contract and
the stress evidence behind it.

## Contract

- `ValidationErrorCollector` is pooled per thread (`[ThreadStatic]`). A
  validation rents one collector, returns it in `finally`, and the collector is
  cleared before caching.
- `RootContextData` is pooled the same way. Its dictionaries are allocated
  lazily on the first write; a read-only walk does not allocate.
- `ValidationResult.FromErrors` copies the collected errors into a new array
  before the collector is returned, so a result never aliases a pooled
  collector.
- Rule selection returns the original rule array when no rule sets or filters
  apply; only filtered paths materialize a new array.
- The happy path performs no heap allocation after warmup. Error paths allocate
  the errors themselves (plus the result snapshot), which is expected.

## Evidence

| Check | Result |
|---|---|
| Release test, 1,000 warmup + 1,000 validations | **0 B** allocated |
| BenchmarkDotNet, 5/10/30-property valid models | **0 B** per operation |
| Invalid 5/10/30-property models | 968 / 2,104 / 6,024 B (error objects + snapshot) |
| Fail-fast invalid 5-property model | 208 B |
| Base64 rule | 0 B |
| Localization lookups | 0 B |
| Parallel invalid validation, 10,000 iterations | Stable error count, no cross-thread leakage |
| Nested validators, 1,000 iterations | Stable valid/invalid results |
| Async validator with continuation, 1,000 iterations | Stable valid/invalid results |

The tests are in
`tests/Jeninnet.Validation.Tests/Core/AllocationAndConcurrencyTests.cs`. Debug
builds allow a small per-call budget because debug codegen does not reuse the
pooled collector the same way; the exact **0 B** assertion runs in Release.

## Fixes found by the audit

1. `CompositeValidator.ValidateRules`/`ValidateRulesAsync` iterate the rule
   array by index instead of `foreach` over `IEnumerable`, removing the array
   enumerator allocation.
2. `SelectRules` returns the original array immediately when no rule sets or
   filters apply. The previous `as`/materialization path allocated 32 B per
   call.

Together these removed the residual 32 B/op measured during early benchmarking; the happy
path is now zero.

## Threading and async behavior

- The caches are per thread. A thread pool thread pays one collector and one
  root-data allocation the first time it validates, then reuses them.
- Nested validation runs while the outer call holds a collector, so the inner
  call allocates one collector for the duration of the nesting. This is
  bounded by nesting depth and does not leak; a future stack-based pool could
  remove it.
- `ValidateAsync` may resume on a different thread after an `await`. The
  `finally` returns the collector to the continuation thread's cache. This is
  safe (no shared mutable state), but reuse is per thread rather than per
  logical call.
- `ValidationResult` snapshots errors before the collector returns; there is no
  aliasing bug.

## Rejected alternative

Ambient/`AsyncLocal` pooling was rejected: it hides ownership, adds ambient
state to the hot path, and conflicts with the project's explicit-over-ambient
direction. The thread-static pool is smaller and easier to reason about.
