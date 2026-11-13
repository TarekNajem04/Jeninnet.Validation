//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Diagnostics;

/// <summary>
/// Registration helpers for optional validation diagnostics.
/// </summary>
public static class ValidationDiagnosticsServiceCollectionExtensions {
    /// <summary>
    /// Registers the validation ActivitySource and Meter and installs the process-wide diagnostics
    /// sink. Calling this method replaces any previously installed sink.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional diagnostics configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <exception cref="ArgumentException">A source or meter name is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The sampling probability is outside <c>[0, 1]</c>.</exception>
    public static IServiceCollection AddValidationDiagnostics(
        this IServiceCollection services,
        Action<ValidationDiagnosticsOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ValidationDiagnosticsOptions();
        configure?.Invoke(options);
        options.Validate();

        services.AddSingleton(options);

        if(!options.EnableTracing && !options.EnableMetrics) {
            var previousSink = ValidationDiagnostics.Sink;
            ValidationDiagnostics.Sink = null;

            if(previousSink is IDisposable previousDisposable) {
                previousDisposable.Dispose();
            }

            return services;
        }

        var sink = new ValidationDiagnosticsSink(options);
        var oldSink = ValidationDiagnostics.Sink;
        ValidationDiagnostics.Sink = sink;

        if(oldSink is IDisposable oldDisposable) {
            oldDisposable.Dispose();
        }

        // Factory registrations are owned by the container, so disposing the provider disposes
        // the sink and detaches the static validation hook.
        services.AddSingleton<IValidationDiagnosticsSink>(_ => sink);

        return services;
    }
}
