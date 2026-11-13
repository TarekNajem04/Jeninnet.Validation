//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Provides a base class for comparable type comparison rules.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
/// <param name="accessor">The property accessor to extract the value.</param>
/// <param name="threshold">The threshold to compare against.</param>
/// <param name="message">The custom validation message, if any.</param>
/// <param name="messageKey">The message key for localization.</param>
public abstract class ComparisonRuleBase<T, TValue>(IPropertyAccessor<T, TValue> accessor, TValue threshold, string? message, string? messageKey = ValidationMessageKeys.COMPARABLE)
    : SyncValidateRule<T, TValue>(accessor, message, messageKey) {
    /// <summary>
    /// Gets the threshold value for the comparison.
    /// </summary>
    public TValue Threshold { get; } = threshold;

    /// <inheritdoc />
    protected sealed override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        if(value is null) {
            return;
        }

        if(!Compare(value, Threshold)) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context)
        => new Dictionary<string, object> {
            { "0", Threshold! },
            { MessageParameterNames.ComparisonValue, Threshold! }
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> {
            { "Threshold", Threshold },
            { MessageParameterNames.ComparisonValue, Threshold }
        };

    /// <inheritdoc />
    protected abstract bool Compare(TValue value, TValue threshold);
}
