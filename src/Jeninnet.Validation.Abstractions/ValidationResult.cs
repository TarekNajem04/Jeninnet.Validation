//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
using System.Collections.ObjectModel;

namespace Jeninnet.Validation;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public readonly struct ValidationResult {
    private readonly IReadOnlyList<ValidationError>? _errors;

    internal ValidationResult(IReadOnlyList<ValidationError> errors) => _errors = errors;

    /// <summary>
    /// Gets a singleton instance representing a successful validation with no errors.
    /// </summary>
    public static ValidationResult Success => default;

    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid => _errors is null || _errors.Count == 0;

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => _errors ?? [];

    /// <summary>
    /// Creates a new <see cref="ValidationResult"/> from the specified errors.
    /// </summary>
    /// <param name="errors">The errors that occurred during validation.</param>
    /// <returns>A <see cref="ValidationResult"/> instance representing the outcome.</returns>
    public static ValidationResult FromErrors(ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        return errors.HasErrors()
            ? new ValidationResult(new ReadOnlyCollection<ValidationError>([.. errors]))
            : Success;
    }
}
