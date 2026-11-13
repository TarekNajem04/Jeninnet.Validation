//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Diagnostics;

/// <summary>
/// Creates OpenTelemetry-compatible activities and metrics for validation calls. The sink only
/// records type, validator, rule-set, outcome, and error-count metadata; messages and values never
/// reach tags.
/// </summary>
public sealed class ValidationDiagnosticsSink : IValidationDiagnosticsSink, IDisposable {
    private readonly ValidationDiagnosticsOptions _options;
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Histogram<double> _duration;
    private readonly Counter<long> _errors;
    private bool _disposed;

    /// <summary>
    /// Initializes a new diagnostics sink.
    /// </summary>
    /// <param name="options">The diagnostics options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">A source or meter name is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The sampling probability is outside <c>[0, 1]</c>.</exception>
    public ValidationDiagnosticsSink(ValidationDiagnosticsOptions options) {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        _options = options;
        _activitySource = new ActivitySource(options.SourceName, options.Version);
        _meter = new Meter(options.MeterName, options.Version);
        _duration = _meter.CreateHistogram<double>(
            "validation.duration",
            unit: "ms",
            description: "Validation duration in milliseconds.");
        _errors = _meter.CreateCounter<long>(
            "validation.errors",
            description: "Number of validation errors emitted.");
    }

    /// <inheritdoc />
    public IValidationDiagnosticsScope? Begin(
        Type modelType,
        string? validatorName,
        IEnumerable<string>? ruleSets,
        bool isAsync) {
        ArgumentNullException.ThrowIfNull(modelType);

        if(_disposed || (!_options.EnableTracing && !_options.EnableMetrics)) {
            return null;
        }

        var ruleSetTag = GetRuleSetTag(ruleSets);
        var activity = CreateActivity(modelType, validatorName, ruleSetTag, isAsync);

        return new Scope(
            this,
            modelType.FullName ?? modelType.Name,
            validatorName,
            ruleSetTag,
            isAsync,
            activity,
            Stopwatch.GetTimestamp());
    }

    /// <summary>
    /// Releases the ActivitySource and Meter and detaches this sink when it is the active one.
    /// </summary>
    public void Dispose() {
        if(_disposed) {
            return;
        }

        _disposed = true;

        if(ReferenceEquals(ValidationDiagnostics.Sink, this)) {
            ValidationDiagnostics.Sink = null;
        }

        _activitySource.Dispose();
        _meter.Dispose();
    }

    private Activity? CreateActivity(
        Type modelType,
        string? validatorName,
        string? ruleSetTag,
        bool isAsync) {
        if(!_options.EnableTracing || !_activitySource.HasListeners()) {
            return null;
        }

        if(_options.SamplingProbability < 1 &&
           Random.Shared.NextDouble() >= _options.SamplingProbability) {
            return null;
        }

        var activity = _activitySource.StartActivity(
            isAsync ? "ValidateAsync" : "Validate",
            ActivityKind.Internal);

        if(activity is null) {
            return null;
        }

        activity.SetTag("validation.model_type", modelType.FullName ?? modelType.Name)
            .SetTag("validation.async", isAsync);

        if(validatorName is not null) {
            activity.SetTag("validation.validator", validatorName);
        }

        if(ruleSetTag is not null) {
            activity.SetTag("validation.rule_sets", ruleSetTag);
        }

        return activity;
    }

    private string? GetRuleSetTag(IEnumerable<string>? ruleSets) {
        if(!_options.IncludeRuleSets || ruleSets is not ICollection<string> collection || collection.Count is 0) {
            return null;
        }

        var joined = string.Join(",", collection);

        return joined.Length is 0 ? null : joined;
    }

    private void Finish(
        Scope scope,
        string outcome,
        int errorCount,
        Exception? exception,
        long startTimestamp) {
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        var elapsedMilliseconds = elapsed.TotalMilliseconds;

        var tags = new TagList {
            { "validation.model_type", scope.ModelTypeName },
            { "validation.async", scope.IsAsync },
            { "validation.outcome", outcome },
            { "validation.error_count", errorCount }
        };

        if(scope.ValidatorName is not null) {
            tags.Add("validation.validator", scope.ValidatorName);
        }

        if(scope.RuleSetTag is not null) {
            tags.Add("validation.rule_sets", scope.RuleSetTag);
        }

        if(scope.Activity is not null) {
            scope.Activity.SetTag("validation.outcome", outcome);
            scope.Activity.SetTag("validation.error_count", errorCount);
            scope.Activity.SetTag("validation.duration_ms", elapsedMilliseconds);

            if(exception is not null) {
                scope.Activity.SetStatus(
                    ActivityStatusCode.Error,
                    $"{exception.GetType().Name}: {exception.Message}");
            }

            scope.Activity.Dispose();
        }

        if(!_options.EnableMetrics) {
            return;
        }

        _duration.Record(elapsedMilliseconds, tags);

        if(errorCount > 0) {
            _errors.Add(errorCount, tags);
        }
    }

    private sealed class Scope(
        ValidationDiagnosticsSink owner,
        string modelTypeName,
        string? validatorName,
        string? ruleSetTag,
        bool isAsync,
        Activity? activity,
        long startTimestamp) : IValidationDiagnosticsScope {
        private bool _completed;

        public string ModelTypeName { get; } = modelTypeName;

        public string? ValidatorName { get; } = validatorName;

        public string? RuleSetTag { get; } = ruleSetTag;

        public bool IsAsync { get; } = isAsync;

        public Activity? Activity { get; } = activity;

        public void Complete(ValidationResult result) {
            if(_completed) {
                return;
            }

            _completed = true;
            owner.Finish(
                this,
                result.IsValid ? "success" : "failure",
                result.Errors.Count,
                exception: null,
                startTimestamp);
        }

        public void Fail(Exception exception) {
            ArgumentNullException.ThrowIfNull(exception);

            if(_completed) {
                return;
            }

            _completed = true;
            owner.Finish(
                this,
                exception is OperationCanceledException ? "canceled" : "faulted",
                errorCount: 0,
                exception,
                startTimestamp);
        }

        public void Dispose() {
            if(_completed) {
                return;
            }

            _completed = true;
            owner.Finish(
                this,
                "faulted",
                errorCount: 0,
                exception: null,
                startTimestamp);
        }
    }
}
