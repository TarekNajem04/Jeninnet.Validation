# Performance

The engine is designed for a zero-allocation happy path and predictable error costs.

## Measured result

| Scenario | Allocation |
|---|---|
| Valid model, 5/10/30 properties | **0 B/op** |
| Invalid model, 5 errors | 2,496 B |
| Invalid model, 10 errors | 4,936 B |
| Invalid model, 30 errors | 14,456 B |
| Fail-fast, 5-property model | 456 B |
| Base64 rule | 0 B |
| Localization lookup | 0 B |

Generated validators are consistently at least as fast as fluent ones on valid paths. Invalid
paths are dominated by error construction and are effectively equal.

## Diagnostics overhead

`Jeninnet.Validation.Diagnostics` is opt-in. When `ValidationDiagnostics.Sink` is null the hot path
adds one volatile null check per `CompositeValidator` call and allocates nothing. When enabled, it
creates one short-lived diagnostics scope plus an Activity only when a listener is attached; the
histogram/counter use `TagList` and type/rule-set labels, never messages or values.

Per-rule tracing is a second opt-in layer (`EnableRuleTrace`, default `false`). With a sink installed
but tracing off, the engine checks one boolean per validation operation and allocates nothing extra.
When enabled, each executed or skipped rule pays for a timestamp, a member path, and an
`ActivityEvent`; that cost is bounded by the number of configured rules and is never paid on the
default path.

## Why it is fast

- `ValidationErrorCollector` and `RootContextData` are pooled per thread.
- Rule selection returns the original rule array when no filters apply.
- Rules iterate arrays by index; no enumerator allocation.
- Generated validators emit literal rule construction.
- Descriptors are frozen and never allocate on repeated `Describe()`.

## Measuring your own validators

```csharp
var validator = new CustomerValidator();
var model = new Customer { /* valid */ };

for (var i = 0; i < 1_000; i++) validator.Validate(model);

var before = GC.GetAllocatedBytesForCurrentThread();
for (var i = 0; i < 1_000; i++) validator.Validate(model);
var after = GC.GetAllocatedBytesForCurrentThread();

Console.WriteLine((after - before) / 1_000.0); // expect 0 in Release
```

Debug builds can allocate the pooled collector differently; measure in Release.

## Benchmarks

The repository publishes machine profile, methodology, and tables in
[Benchmarks](06-benchmarks.md) and the allocation contract in
[Allocation audit](07-allocation-audit.md).

## Related

- [Coverage and quality](03-coverage-and-quality.md)
- [AOT and trimming](02-aot-and-trimming.md)
