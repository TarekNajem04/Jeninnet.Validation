//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Maps validation errors to the <c>errors</c> dictionary of a ProblemDetails response.
/// </summary>
public static class ValidationProblemExtensions {
    /// <summary>
    /// Groups a validation result's errors by member name.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <param name="options">Optional detail controls.</param>
    /// <returns>The grouped messages.</returns>
    public static IDictionary<string, string[]> ToProblemErrors(
        this ValidationResult result,
        ValidationProblemOptions? options = null) =>
        result.Errors.ToProblemErrors(options);

    /// <summary>
    /// Groups validation errors by member name, preserving error order within each member.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <param name="options">Optional detail controls.</param>
    /// <returns>The grouped messages.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is null.</exception>
    public static IDictionary<string, string[]> ToProblemErrors(
        this IEnumerable<ValidationError> errors,
        ValidationProblemOptions? options = null) {
        ArgumentNullException.ThrowIfNull(errors);

        var includeErrorCodes = options?.IncludeErrorCodes ?? false;
        var includeSeverities = options?.IncludeSeverities ?? false;

        return errors
            .GroupBy(static error => error.MemberName, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                group => group.Select(error => Format(error, includeErrorCodes, includeSeverities)).ToArray(),
                StringComparer.Ordinal);
    }

    /// <summary>
    /// Groups validation errors by member name and resolves message keys through the registered
    /// message localizer, preserving error order within each member.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <param name="options">Optional detail controls.</param>
    /// <param name="localizer">The optional message localizer.</param>
    /// <param name="culture">The culture used for lookup; defaults to the current UI culture.</param>
    /// <param name="cancellationToken">A token to cancel localization.</param>
    /// <returns>The grouped messages.</returns>
    internal static async ValueTask<IDictionary<string, string[]>> ToProblemErrorsAsync(
        this IEnumerable<ValidationError> errors,
        ValidationProblemOptions? options,
        IMessageLocalizer? localizer,
        CultureInfo? culture,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(errors);

        var includeErrorCodes = options?.IncludeErrorCodes ?? false;
        var includeSeverities = options?.IncludeSeverities ?? false;
        var grouped = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach(var error in errors) {
            var message = await FormatAsync(
                error,
                includeErrorCodes,
                includeSeverities,
                localizer,
                culture,
                cancellationToken).ConfigureAwait(false);

            if(!grouped.TryGetValue(error.MemberName, out var messages)) {
                messages = [];
                grouped[error.MemberName] = messages;
            }

            messages.Add(message);
        }

        return grouped.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.ToArray(),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Builds optional problem extensions such as correlation id, model type, and duration.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="options">Optional detail controls.</param>
    /// <param name="modelType">The validated model type, when known.</param>
    /// <param name="duration">The validation duration, when measured.</param>
    /// <returns>A dictionary of problem extensions; empty when no option is enabled.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="httpContext"/> is null.</exception>
    public static IDictionary<string, object?> BuildProblemExtensions(
        HttpContext httpContext,
        ValidationProblemOptions? options = null,
        string? modelType = null,
        TimeSpan? duration = null) {
        ArgumentNullException.ThrowIfNull(httpContext);

        var extensions = new Dictionary<string, object?>(StringComparer.Ordinal);

        if(options is null) {
            return extensions;
        }

        if(options.IncludeCorrelationId) {
            extensions["correlationId"] = GetCorrelationId(httpContext, options.CorrelationIdHeader);
        }

        if(options.IncludeModelType && !string.IsNullOrWhiteSpace(modelType)) {
            extensions["modelType"] = modelType;
        }

        if(options.IncludeDuration && duration is { } measured) {
            extensions["durationMs"] = Math.Round(measured.TotalMilliseconds, 3);
        }

        return extensions;
    }

    /// <summary>
    /// Adds optional correlation id, model type, and duration extensions to a problem response.
    /// </summary>
    /// <param name="problem">The validation problem.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="options">Optional detail controls.</param>
    /// <param name="modelType">The validated model type, when known.</param>
    /// <param name="duration">The validation duration, when measured.</param>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> or <paramref name="httpContext"/> is null.</exception>
    public static void EnrichProblem(
        ValidationProblemDetails problem,
        HttpContext httpContext,
        ValidationProblemOptions? options = null,
        string? modelType = null,
        TimeSpan? duration = null) {
        ArgumentNullException.ThrowIfNull(problem);

        foreach(var extension in BuildProblemExtensions(httpContext, options, modelType, duration)) {
            problem.Extensions[extension.Key] = extension.Value;
        }
    }

    private static string GetCorrelationId(HttpContext httpContext, string? headerName) {
        if(!string.IsNullOrWhiteSpace(headerName) &&
           httpContext.Request.Headers.TryGetValue(headerName, out var headerValue) &&
           !string.IsNullOrWhiteSpace(headerValue)) {
            return headerValue.ToString();
        }

        return httpContext.TraceIdentifier;
    }

    private static string Format(ValidationError error, bool includeErrorCodes, bool includeSeverities) =>
        Decorate(error.Message ?? error.MessageKey ?? error.ErrorCode ?? "Invalid", error, includeErrorCodes, includeSeverities);

    private static async ValueTask<string> FormatAsync(
        ValidationError error,
        bool includeErrorCodes,
        bool includeSeverities,
        IMessageLocalizer? localizer,
        CultureInfo? culture,
        CancellationToken cancellationToken) {
        var message = error.Message;

        if(message is null &&
           !error.MessageIsAuthored &&
           localizer is not null &&
           error.MessageKey is not null) {
            var parameters = ToLocalizationParameters(error.MessageParameters);

            message = await localizer
                .LocalizeAsync(error.MessageKey, parameters, culture ?? CultureInfo.CurrentUICulture, cancellationToken)
                .ConfigureAwait(false);
        }

        return Decorate(
            message ?? error.MessageKey ?? error.ErrorCode ?? "Invalid",
            error,
            includeErrorCodes,
            includeSeverities);
    }

    private static string Decorate(
        string message,
        ValidationError error,
        bool includeErrorCodes,
        bool includeSeverities) {
        if(includeErrorCodes && error.ErrorCode is not null &&
           !string.Equals(error.ErrorCode, message, StringComparison.Ordinal)) {
            message = $"{message} [{error.ErrorCode}]";
        }

        if(includeSeverities) {
            message = $"{message} ({error.Severity})";
        }

        return message;
    }

    internal static IReadOnlyDictionary<string, object?> ToLocalizationParameters(
        IDictionary<string, object>? parameters) {
        if(parameters is null || parameters.Count == 0) {
            return _emptyParameters;
        }

        var copy = new Dictionary<string, object?>(parameters.Count, StringComparer.Ordinal);

        foreach(var pair in parameters) {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    private static readonly IReadOnlyDictionary<string, object?> _emptyParameters =
        new Dictionary<string, object?>(StringComparer.Ordinal);
}
