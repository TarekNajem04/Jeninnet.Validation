//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Controls the detail exposed in validation problem responses.
/// </summary>
public sealed class ValidationProblemOptions {
    /// <summary>
    /// Gets or sets a value indicating whether error codes are appended to problem messages.
    /// Defaults to <see langword="false"/> (privacy-preserving).
    /// </summary>
    public bool IncludeErrorCodes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether severities are appended to problem messages.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool IncludeSeverities { get; set; }

    /// <summary>
    /// Gets or sets an optional problem title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the trace correlation identifier is added to the
    /// problem extensions. Defaults to <see langword="false"/>.
    /// </summary>
    public bool IncludeCorrelationId { get; set; }

    /// <summary>
    /// Gets or sets an optional request-header name used as the correlation identifier. When unset
    /// or absent, <see cref="HttpContext.TraceIdentifier"/> is used.
    /// </summary>
    public string? CorrelationIdHeader { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the validated model type is added to the problem
    /// extensions. Defaults to <see langword="false"/>.
    /// </summary>
    public bool IncludeModelType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the validation duration is added to the problem
    /// extensions. Defaults to <see langword="false"/>.
    /// </summary>
    public bool IncludeDuration { get; set; }
}
