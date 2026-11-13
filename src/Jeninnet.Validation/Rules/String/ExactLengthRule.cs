//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A specialized, type-specific rule for ensuring that a string member has an exact length.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="length">The required length.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class ExactLengthRule<T>(
    IPropertyAccessor<T, string?> accessor,
    int length,
    string? message,
    string? messageKey = ValidationMessageKeys.EXACT_LENGTH)
    : ExactLengthRule<T, string?>(accessor, length, message, messageKey);

/// <summary>
/// A rule for ensuring that a string member has an exact length.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type (string or nullable string).</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="length">The required length.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public class ExactLengthRule<T, TProperty>(
    IPropertyAccessor<T, TProperty> accessor,
    int length,
    string? message,
    string? messageKey = ValidationMessageKeys.EXACT_LENGTH)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <summary>
    /// Gets the required length.
    /// </summary>
    public int Length { get; } = length;

    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(value is not string text) {
            return;
        }

        if(text.Length != Length) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) =>
        new Dictionary<string, object> {
            { "0", Length },
            { MessageParameterNames.Length, Length },
            { MessageParameterNames.ExactLength, Length }
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> {
            { MessageParameterNames.Length, Length },
            { MessageParameterNames.ExactLength, Length }
        };
}
