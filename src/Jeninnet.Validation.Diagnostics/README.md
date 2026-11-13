# Jeninnet.Validation.Diagnostics

Optional OpenTelemetry-compatible tracing and metrics for `Jeninnet.Validation`. The core engine
takes one null-check when no sink is installed; the diagnostics package only pays for activity and
measurement work when enabled.

## Requirements

- .NET 10.0 or later (including .NET 11).
- C# 15 when consuming the source contracts.

## Installation

```xml
<PackageReference Include="Jeninnet.Validation.Diagnostics" Version="1.0.0-preview.1" />
```

## Register

```csharp
using Jeninnet.Validation.Diagnostics;

services.AddValidationDiagnostics(options =>
{
    options.EnableTracing = true;
    options.EnableMetrics = true;
    options.SamplingProbability = 0.25;
});
```

The package installs an `ActivitySource` and `Meter` named `Jeninnet.Validation` by default. Point
your OpenTelemetry SDK at those names:

```csharp
builder.Services.AddOpenTelemetry()
       .WithTracing(tracing => tracing.AddSource("Jeninnet.Validation"))
       .WithMetrics(metrics => metrics.AddMeter("Jeninnet.Validation"));
```

## Signals

| Signal | Type | Tags |
|---|---|---|
| `Validate` / `ValidateAsync` | Activity | `validation.model_type`, `validation.validator`, `validation.async`, `validation.rule_sets`, `validation.outcome`, `validation.error_count`, `validation.duration_ms` |
| `validation.duration` | Histogram (ms) | model type, validator, async, rule sets, outcome, error count |
| `validation.errors` | Counter | same tags; records the error count of failed validations |

Rule-set names and type names are configuration labels. Messages, property values, and custom state
never reach tags, so the signals stay free of user data.

## Options

| Option | Default | Meaning |
|---|---|---|
| `EnableTracing` | `true` | Creates activities when a listener is attached. |
| `EnableMetrics` | `true` | Records the histogram and error counter. |
| `SourceName` | `Jeninnet.Validation` | ActivitySource name. |
| `MeterName` | `Jeninnet.Validation` | Meter name. |
| `Version` | `null` | Optional instrumentation version. |
| `SamplingProbability` | `1.0` | Trace sampling probability; metrics are never sampled. |
| `IncludeRuleSets` | `true` | Adds the rule-set label when one is requested. |

`AddValidationDiagnostics` is explicit. With no call, `ValidationDiagnostics.Sink` is null and the
core allocates nothing. Diagnostics are process-wide; call `ValidationDiagnostics.Sink = null` (or
dispose the sink) when a host shuts down and the process keeps running.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
