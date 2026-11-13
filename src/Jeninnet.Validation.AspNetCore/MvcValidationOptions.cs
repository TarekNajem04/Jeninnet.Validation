//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Configures automatic MVC validation.
/// </summary>
public sealed class MvcValidationOptions {
    /// <summary>
    /// Gets or sets a function that maps a validation error to a <c>ModelState</c> key. When
    /// <see langword="null"/>, <see cref="ValidationError.MemberName"/> is used.
    /// </summary>
    public Func<ValidationError, string>? ModelStateKeySelector { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether error codes are appended to model-state messages.
    /// </summary>
    public bool IncludeErrorCodes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether severities are appended to model-state messages.
    /// </summary>
    public bool IncludeSeverities { get; set; }

    /// <summary>
    /// Gets or sets the optional ProblemDetails enrichment controls. Defaults to a new instance
    /// with all enrichment disabled.
    /// </summary>
    public ValidationProblemOptions ProblemOptions { get; set; } = new();
}
