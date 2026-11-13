//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// Represents a strongly-typed identifier for validation messages.
/// </summary>
/// <param name="Id">The identifier for the validation message.</param>
public readonly record struct MessageId(string Id) {
    /// <summary>
    /// Represents an absent or unset message identifier.
    /// </summary>
    public static MessageId None => new(string.Empty);

    /// <summary>
    /// Represents an invalid or unrecognized message identifier.
    /// </summary>
    public static MessageId Invalid => new("Invalid");
}
