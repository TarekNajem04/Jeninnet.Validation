//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Diagnostics;

/// <summary>
/// Configures the optional validation ActivitySource and Meter.
/// </summary>
public sealed class ValidationDiagnosticsOptions {
    /// <summary>
    /// Gets or sets a value indicating whether tracing is enabled. Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether metrics are enabled. Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the ActivitySource name. Defaults to <c>Jeninnet.Validation</c>.
    /// </summary>
    public string SourceName { get; set; } = "Jeninnet.Validation";

    /// <summary>
    /// Gets or sets the Meter name. Defaults to <c>Jeninnet.Validation</c>.
    /// </summary>
    public string MeterName { get; set; } = "Jeninnet.Validation";

    /// <summary>
    /// Gets or sets an optional instrumentation version stamped into the ActivitySource and Meter.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the trace sampling probability in the range <c>[0, 1]</c>. Metrics are never
    /// sampled. Defaults to <c>1.0</c>.
    /// </summary>
    public double SamplingProbability { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets a value indicating whether rule-set names are added to tags. Defaults to
    /// <see langword="true"/>. Rule-set names are configuration labels, not user data.
    /// </summary>
    public bool IncludeRuleSets { get; set; } = true;

    internal void Validate() {
        if(string.IsNullOrWhiteSpace(SourceName)) {
            throw new ArgumentException("An ActivitySource name is required.");
        }

        if(string.IsNullOrWhiteSpace(MeterName)) {
            throw new ArgumentException("A Meter name is required.");
        }

        if(double.IsNaN(SamplingProbability) || SamplingProbability is < 0 or > 1) {
            throw new ArgumentException("SamplingProbability must be between 0 and 1.");
        }
    }
}
