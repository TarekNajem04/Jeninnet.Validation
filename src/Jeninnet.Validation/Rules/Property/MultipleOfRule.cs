//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a numeric member is a multiple of the specified divisor.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="divisor">The positive divisor.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class MultipleOfRule<T, TValue>(
    IPropertyAccessor<T, TValue> accessor,
    decimal divisor,
    string? message,
    string? messageKey = ValidationMessageKeys.MULTIPLE_OF
) : SyncValidateRule<T, TValue>(accessor, message, messageKey) {
    private readonly decimal _divisor = divisor > 0m
        ? divisor
        : throw new ArgumentOutOfRangeException(nameof(divisor), divisor, "The divisor must be greater than zero.");

    /// <inheritdoc />
    protected override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(value is null) {
            return;
        }

        if(!NumericValueConversion.TryConvert(value, out var decimalValue, out var isNumericCandidate)) {
            if(isNumericCandidate) {
                AddError(errors, context);
            }

            return;
        }

        if(decimalValue % _divisor != 0m) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) =>
        new Dictionary<string, object> {
            { "0", _divisor },
            { MessageParameterNames.Divisor, _divisor }
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> {
            { MessageParameterNames.Divisor, _divisor }
        };
}
