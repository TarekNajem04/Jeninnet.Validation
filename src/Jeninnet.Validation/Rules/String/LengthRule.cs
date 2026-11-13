//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A specialized, type-specific rule for ensuring that a string member's length is within a specified range.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="min">The minimum length.</param>
/// <param name="max">The maximum length.</param>
/// <param name="message"   >The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class LengthRule<T>(IPropertyAccessor<T, string?> accessor, int min, int max, string? message, string? messageKey = ValidationMessageKeys.LENGTH)
    : LengthRule<T, string?>(accessor, min, max, message, messageKey);

/// <summary>
/// A specialized, type-specific rule for ensuring that a string member's length is within a specified range.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type (string or nullable string).</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="min">The minimum length.</param>
/// <param name="max">The maximum length.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public class LengthRule<T, TProperty>(IPropertyAccessor<T, TProperty> accessor, int min, int max, string? message, string? messageKey = ValidationMessageKeys.LENGTH)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <summary>
    /// Gets the minimum length.
    /// </summary>
    public int Min { get; } = min;

    /// <summary>
    /// Gets the maximum length.
    /// </summary>
    public int Max { get; } = max;

    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        if(value is not string s) {
            return;
        }

        if(s.Length < Min || s.Length > Max) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) {
        var value = Accessor.GetValue(context.Instance);
        var totalLength = value is string text ? text.Length : 0;

        return new Dictionary<string, object> {
            { "0", Min },
            { "1", Max },
            { MessageParameterNames.Min, Min },
            { MessageParameterNames.Max, Max },
            { MessageParameterNames.TotalLength, totalLength },
            { MessageParameterNames.PropertyValue, value! }
        };
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> {
            { MessageParameterNames.Min, Min },
            { MessageParameterNames.Max, Max }
        };
}
