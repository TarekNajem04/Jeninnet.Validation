//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Diagnostics;

internal sealed class DiagnosticsModel {
    public string? Name { get; set; }
}

file sealed class DiagnosticsValidator : Validator<DiagnosticsModel> {
    protected override void Configure(ValidatorRuleBuilder<DiagnosticsModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(DiagnosticsModel.Name)).NotEmpty();
    }
}

[TestClass]
// Installs and restores the process-wide validation diagnostics sink.
[DoNotParallelize]
public sealed class ValidationDiagnosticsTests {
    [TestMethod]
    public void Activity_IsCreatedForValidation() {
        var options = new ValidationDiagnosticsOptions {
            EnableMetrics = false,
            SamplingProbability = 1
        };

        using var sink = new ValidationDiagnosticsSink(options);
        ValidationDiagnostics.Sink = sink;

        Activity? captured = null;
        using var listener = new ActivityListener {
            ShouldListenTo = source => string.Equals(source.Name, options.SourceName, StringComparison.Ordinal),
            Sample = static (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => captured = activity
        };

        ActivitySource.AddActivityListener(listener);

        try {
            var result = new DiagnosticsValidator().Validate(new DiagnosticsModel { Name = string.Empty });

            MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
            MSTestAssertions.That(() => captured.Should().NotBeNull());
            MSTestAssertions.That(() => captured!.DisplayName.Should().Be("Validate"));
            MSTestAssertions.That(() => captured!.GetTagItem("validation.outcome").Should().Be("failure"));
            MSTestAssertions.That(() => captured!.GetTagItem("validation.error_count").Should().Be(1));
            MSTestAssertions.That(() => captured!.GetTagItem("validation.model_type").Should().Be(typeof(DiagnosticsModel).FullName));
        }
        finally {
            ValidationDiagnostics.Sink = null;
        }
    }

    [TestMethod]
    public void Metrics_RecordDurationAndErrors() {
        var options = new ValidationDiagnosticsOptions {
            EnableTracing = false,
            EnableMetrics = true
        };

        using var sink = new ValidationDiagnosticsSink(options);
        ValidationDiagnostics.Sink = sink;

        var measurements = new List<(string Instrument, double Value, string? Type, string? Outcome)>();
        using var listener = new MeterListener {
            InstrumentPublished = static (instrument, meterListener) => {
                if(string.Equals(instrument.Meter.Name, "Jeninnet.Validation", StringComparison.Ordinal)) {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => {
            var modelType = ReadTag(tags, "validation.model_type");
            var outcome = ReadTag(tags, "validation.outcome");
            measurements.Add((instrument.Name, value, modelType, outcome));
        });

        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => {
            var modelType = ReadTag(tags, "validation.model_type");
            var outcome = ReadTag(tags, "validation.outcome");
            measurements.Add((instrument.Name, value, modelType, outcome));
        });

        listener.Start();

        try {
            _ = new DiagnosticsValidator().Validate(new DiagnosticsModel { Name = string.Empty });

            MSTestAssertions.That(() => measurements.Should().Contain(item =>
                item.Instrument == "validation.duration" &&
                item.Value >= 0 &&
                item.Type == typeof(DiagnosticsModel).FullName &&
                item.Outcome == "failure"));
            MSTestAssertions.That(() => measurements.Should().Contain(item =>
                item.Instrument == "validation.errors" &&
                Math.Abs(item.Value - 1) < 0.0001 &&
                item.Outcome == "failure"));
        }
        finally {
            listener.Dispose();
            ValidationDiagnostics.Sink = null;
        }
    }

    [TestMethod]
    public void DisabledDiagnostics_ReturnNoScope() {
        var options = new ValidationDiagnosticsOptions {
            EnableTracing = false,
            EnableMetrics = false
        };

        using var sink = new ValidationDiagnosticsSink(options);

        MSTestAssertions.That(() => sink.Begin(typeof(DiagnosticsModel), null, null, isAsync: false).Should().BeNull());
    }

    [TestMethod]
    public void DisposedSink_Begin_ReturnsNull() {
        using var sink = new ValidationDiagnosticsSink(new ValidationDiagnosticsOptions());
        sink.Dispose();

        MSTestAssertions.That(() => sink.Begin(typeof(DiagnosticsModel), null, null, isAsync: false).Should().BeNull());
    }

    [TestMethod]
    public void AddValidationDiagnostics_InstallsAndDetachesTheSink() {
        ValidationDiagnostics.Sink = null;

        var services = new ServiceCollection();
        services.AddValidationDiagnostics(options => options.EnableMetrics = false);
        var provider = services.BuildServiceProvider();

        try {
            MSTestAssertions.That(() => ValidationDiagnostics.IsEnabled.Should().BeTrue());
            MSTestAssertions.That(() => provider.GetRequiredService<ValidationDiagnosticsOptions>().EnableMetrics.Should().BeFalse());
        }
        finally {
            provider.Dispose();
            ValidationDiagnostics.Sink = null;
        }

        MSTestAssertions.That(() => ValidationDiagnostics.IsEnabled.Should().BeFalse());
    }

    private static string? ReadTag(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach(var tag in tags) {
            if(string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }
}
