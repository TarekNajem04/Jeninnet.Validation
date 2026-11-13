//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// Receives validation lifecycle events so an optional diagnostics package can create traces and
/// metrics without coupling the core engine to a telemetry implementation.
/// </summary>
public interface IValidationDiagnosticsSink {
    /// <summary>
    /// Starts one validation operation.
    /// </summary>
    /// <param name="modelType">The validated model type.</param>
    /// <param name="validatorName">The validator display name, when known.</param>
    /// <param name="ruleSets">The requested rule sets, when any.</param>
    /// <param name="isAsync">Whether the operation uses the asynchronous path.</param>
    /// <returns>A scope that completes the operation, or <see langword="null"/> when disabled.</returns>
    IValidationDiagnosticsScope? Begin(
        Type modelType,
        string? validatorName,
        IEnumerable<string>? ruleSets,
        bool isAsync);
}

/// <summary>
/// Completes one validation diagnostics operation. Implementations must be idempotent.
/// </summary>
public interface IValidationDiagnosticsScope : IDisposable {
    /// <summary>
    /// Completes the operation with a validation result.
    /// </summary>
    /// <param name="result">The completed validation result.</param>
    void Complete(ValidationResult result);

    /// <summary>
    /// Completes the operation with an exception.
    /// </summary>
    /// <param name="exception">The exception that escaped the validation pipeline.</param>
    void Fail(Exception exception);
}

/// <summary>
/// Holds the optional validation diagnostics sink. When no sink is registered, the core engine
/// takes a single null check and allocates nothing.
/// </summary>
public static class ValidationDiagnostics {
    private static IValidationDiagnosticsSink? _sink;

    /// <summary>
    /// Gets or sets the diagnostics sink. Set by the optional diagnostics package or by tests.
    /// </summary>
    public static IValidationDiagnosticsSink? Sink {
        get => System.Threading.Volatile.Read(ref _sink);
        set => System.Threading.Volatile.Write(ref _sink, value);
    }

    /// <summary>
    /// Gets a value indicating whether a diagnostics sink is registered.
    /// </summary>
    public static bool IsEnabled => System.Threading.Volatile.Read(ref _sink) is not null;
}
