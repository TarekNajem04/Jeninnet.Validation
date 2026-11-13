//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR;

/// <summary>
/// Carries the validation result for the current MediatR request when the
/// <see cref="MediatRValidationFailureStrategy.Continue"/> strategy is used.
/// </summary>
public sealed class ValidationResultAccessor {
    /// <summary>
    /// Gets or sets the validation result, or <see langword="null"/> when validation passed
    /// or did not run.
    /// </summary>
    public ValidationResult? Result { get; set; }
}
