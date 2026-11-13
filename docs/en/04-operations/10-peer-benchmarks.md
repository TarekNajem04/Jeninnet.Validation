# Peer benchmarks

This page compares the same two-rule model across Jeninnet.Validation, FluentValidation 12.2,
and DataAnnotations. It is a focused microbenchmark, not a full application benchmark.

## Model and rules

The model has one string and one integer member:

```csharp
public sealed class PeerModel
{
    [Required]
    public string? Name { get; set; }

    [Range(18, 120)]
    public int Age { get; set; }
}
```

All three paths apply the same rules: `Name` is required and at most 10 characters; `Age` is
between 18 and 120. The Jeninnet and FluentValidation validators use their fluent APIs;
DataAnnotations uses `Validator.TryValidateObject(..., validateAllProperties: true)`.

## Results

ShortRun job on the repository’s reference machine (Windows 11, Intel Core i7-8850H, .NET 10.0.14,
x64 RyuJIT). Lower is better; allocations are managed bytes per operation. This is a historical
measurement; current builds use .NET 11 SDK `11.0.100`, so re-run before quoting it as a current
performance claim.

| Method | Mean | Allocated |
|---|---:|---:|
| **Jeninnet — valid** | **81.7 ns** | **0 B** |
| FluentValidation — valid | 226.7 ns | 632 B |
| DataAnnotations — valid | 848.3 ns | 1,408 B |
| **Jeninnet — invalid** | **1,048.5 ns** | **1,976 B** |
| FluentValidation — invalid | 2,456.4 ns | 4,880 B |
| DataAnnotations — invalid | 1,488.9 ns | 1,752 B |

On the valid path, Jeninnet is about **2.8× faster than FluentValidation** and allocates nothing.
The invalid path is dominated by error collection and message formatting; it is still about 2.3×
faster than FluentValidation. DataAnnotations allocates slightly less than Jeninnet on this invalid
path because it stores `ValidationResult` objects without Jeninnet’s parameter dictionary; that is
a payload difference, not a throughput win.

## Reproduce

```bash
dotnet run -c Release --project src/Jeninnet.Validation.Benchmarks \
  -- --filter "*PeerComparisonBenchmarks*"
```

The benchmark class is `PeerComparisonBenchmarks` in
`src/Jeninnet.Validation.Benchmarks/PeerComparisonBenchmarks.cs`.

## Caveats

- `ShortRunJob` is intentionally quick and noisy; use a full job for release claims.
- Results are machine-, runtime-, and model-specific. Re-run on your hardware.
- The benchmark isolates validation execution. It excludes model binding, HTTP, database calls,
  localization lookup, and logging.
- DataAnnotations defaults may short-circuit differently from a custom `ValidationContext`; this
  benchmark keeps the default `TryValidateObject` behavior.

## Related

- [Performance](01-performance.md)
- [Benchmarks](06-benchmarks.md)
- [Allocation audit](07-allocation-audit.md)
- [Why Jeninnet?](../01-getting-started/00-why-jeninnet.md)
