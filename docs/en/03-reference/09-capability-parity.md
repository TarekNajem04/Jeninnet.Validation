# Capability parity

This page summarizes how the built-in capability set compares with the current peer set:

- FluentValidation 12.2
- System.ComponentModel.DataAnnotations + `Microsoft.Extensions.Validation` on .NET 11
- Sannr 1.6
- Validly 1.3
- ValidationModules 1.2

The comparison is a union: a capability counts when at least one peer ships it. It is not a
per-library clone checklist. The full matrix has 95 rows: 81 baseline capabilities and 14
capabilities added by the 2026 peer re-baseline.

## Headline

| Scope | Score | Result |
|---|---|---|
| .NET 10 capability union (93 rows) | **93/93** | **100%** |
| Full matrix including .NET 11-only async APIs (95 rows) | **93/95** | **97.9%** |
| Deferred to `1.1.0-preview.1` | 2 capabilities | N1 async DataAnnotations, N3 async Options |

Scoring is `✅ = 1`, `⚠️ = 0.5`, `❌ = 0`. The two deferred rows are the only capabilities in the
matrix that a peer ships and Jeninnet does not cover on .NET 10. This preview targets .NET 11
(`net11.0`) and includes the .NET 11 validation adapter; the two async rows remain deferred.

## Per-peer coverage

“Full” and “partial” describe the peer. “Covered at full strength” means Jeninnet has a `✅`
implementation on every row where that peer has `✅` or `⚠️` on .NET 10.

| Peer | Peer full | Peer partial | Covered at full strength |
|---|---:|---:|---:|
| FluentValidation 12.2 | 49 | 13 | **62 / 62** |
| DataAnnotations / .NET 11 | 28 | 27 | **55 / 55** |
| Sannr 1.6 | 32 | 26 | **58 / 58** |
| Validly 1.3 | 15 | 23 | **38 / 38** |
| ValidationModules 1.2 | 38 | 25 | **63 / 63** |

On the full 95-row matrix, DataAnnotations rises to 30 full and 27 partial rows; Jeninnet covers
55 of 57 (the two gaps are N1/N3). ValidationModules adds one partial row; Jeninnet covers 63 of
64 (the gap is N3).

## Where the matrix shows an advantage

Eight capabilities are implemented at full strength where the best peer is partial:

1. Reusable rule templates.
2. Executable parity tests between fluent and generated validators.
3. Multi-source messages (JSON, XML, ResX, memory, database).
4. `INotifyDataErrorInfo` adapter for desktop/MAUI view models.
5. Rule descriptors as a tool/UI SPI.
6. Naming-policy-aware OpenAPI schema mapping.
7. Snapshot tests for generated code.
8. A measured 0 B/op hot path.

Seven capabilities have no peer equivalent in the matrix:

1. NRT-aware null policies inferred from nullable annotations.
2. Domain rule packs (IBAN, VAT, postal code, national ID, password strength, UUID, ULID).
3. ICU plural/select messages.
4. A MediatR validation pipeline.
5. HotChocolate GraphQL validation.
6. gRPC validation with status details.
7. Repository honesty gates: documentation claims are checked against the code and packaging.

## Deliberate exclusions

The matrix excludes eight adjacent or intentionally different capabilities: compile-time rule
classes, jQuery MVC client validation, multi-targeting, nested dictionaries, advanced
`TestValidate`, Shadow Types/PII/Deep Clone, Guard Clauses, and dynamic JSON/YAML rule engines.
These are tracked in the peer-comparison notes rather than scored as parity gaps.

## What this page does not claim

- It does not prove semantic equivalence on every edge case or every API signature.
- It does not replace migration tests. Run the migration tool, keep the existing test suite, and
  compare results before deleting the old dependency.
- It is not a performance benchmark. The repository’s benchmark and allocation-audit pages cover
  that separately.
- Peer marks are based on their public documentation as of 2026-11-08, not on executing every
  library on this machine.

## Related

- [Why Jeninnet](../01-getting-started/00-why-jeninnet.md)
- [Migration from FluentValidation](../02-guides/15-migration-from-fluentvalidation.md)
- [Migration from DataAnnotations](../02-guides/16-migration-from-dataannotations.md)
- [Performance](../04-operations/01-performance.md)
- [Benchmarks](../04-operations/06-benchmarks.md)
- [Diagnostics](03-diagnostics.md)
