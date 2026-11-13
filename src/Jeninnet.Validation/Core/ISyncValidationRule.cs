//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Core;

/// <summary>
/// Represents a validation rule for a specific type.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public interface ISyncValidationRule<T> {
    /// <summary>
    /// Executes the validation rule.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="errors">
    /// The collection to which any generated <see cref="ValidationError"/> instances should be added.
    /// </param>
    void Validate(in ValidationContext<T> context, ValidationErrorCollector errors);
}
