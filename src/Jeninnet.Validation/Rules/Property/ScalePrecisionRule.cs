//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a decimal member has a precision and scale within the specified limits.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property type (decimal or decimal?).</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="precision">The precision.</param>
/// <param name="scale">The scale.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The message key for localization.</param>
/// <param name="ignoreTrailingZeros">Whether trailing fractional zeros are ignored when counting scale.</param>
public sealed class ScalePrecisionRule<T, TValue>(
    IPropertyAccessor<T, TValue> accessor,
    int precision,
    int scale,
    string? message,
    string? messageKey = ValidationMessageKeys.PRECISION_SCALE,
    bool ignoreTrailingZeros = false
) : SyncValidateRule<T, TValue>(accessor, message, messageKey) {
    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) =>
        new Dictionary<string, object> {
            { "0", precision },
            { "1", scale },
            { MessageParameterNames.Precision, precision },
            { MessageParameterNames.Scale, scale }
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> {
            { MessageParameterNames.Precision, precision },
            { MessageParameterNames.Scale, scale },
            { "IgnoreTrailingZeros", ignoreTrailingZeros }
        };

    /// <inheritdoc />
    protected override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        if(value is not decimal d) {
            return;
        }

        var text = d.ToString(CultureInfo.InvariantCulture).Replace("-", "");
        var parts = text.Split('.');

        var integerPart = parts[0].TrimStart('0');
        var fractionalPart = parts.Length > 1 ? parts[1] : string.Empty;

        if(ignoreTrailingZeros) {
            fractionalPart = fractionalPart.TrimEnd('0');
        }

        var actualPrecision = integerPart.Length + fractionalPart.Length;
        var actualScale = fractionalPart.Length;

        if(actualPrecision > precision || actualScale > scale) {
            AddError(errors, context);
        }
    }
}
