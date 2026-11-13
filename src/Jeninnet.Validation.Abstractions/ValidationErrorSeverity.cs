//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// Represents the severity of a validation error.
/// </summary>
public enum ValidationErrorSeverity {
    /// <summary>
    /// Indicates that the validation issue is informational and does not block execution.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Indicates a non-critical issue that may be ignored in some contexts.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Indicates a critical validation failure that should prevent further processing.
    /// </summary>
    Error = 2
}
