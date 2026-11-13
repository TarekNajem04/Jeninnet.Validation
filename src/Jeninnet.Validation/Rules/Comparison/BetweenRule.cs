//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a numeric member is between the specified range
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
public abstract class BetweenRule<T, TValue> : SyncValidateRule<T, TValue> {
    /// <summary>
    /// Initializes a new instance of the <see cref="BetweenRule{T, TValue}"/> class.
    /// </summary>
    /// <param name="accessor">The property accessor to extract the value.</param>
    /// <param name="min">The minimum inclusive bound.</param>
    /// <param name="max">The maximum inclusive bound.</param>
    /// <param name="message">The optional error message.</param>
    /// <param name="messageKey">The key for the validation message.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="min"/> is greater than <paramref name="max"/>.
    /// </exception>
    protected BetweenRule(
        IPropertyAccessor<T, TValue> accessor,
        TValue min,
        TValue max,
        string? message,
        string? messageKey = ValidationMessageKeys.BETWEEN
    ) : base(accessor, message, messageKey) {
        RangeBounds.EnsureOrdered(min, max, exclusive: false);
        Min = min;
        Max = max;
    }

    /// <summary>
    /// The minimum inclusive bound. Cannot be null.
    /// </summary>
    public TValue Min { get; }

    /// <summary>
    /// The maximum inclusive bound. Cannot be null.
    /// </summary>
    public TValue Max { get; }

    /// <inheritdoc />
    protected sealed override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        if(value is null) {
            return;
        }

        if(!IsValid(ValueComparison.Compare(value, Min), ValueComparison.Compare(value, Max))) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected abstract bool IsValid(int minComparisonResult, int maxComparisonResult);

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context)
        => new Dictionary<string, object> {
            { "0", Min! },
            { "1", Max! },
            { MessageParameterNames.Min, Min! },
            { MessageParameterNames.Max, Max! },
            { MessageParameterNames.From, Min! },
            { MessageParameterNames.To, Max! }
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> {
            { MessageParameterNames.Min, Min },
            { MessageParameterNames.Max, Max },
            { MessageParameterNames.From, Min },
            { MessageParameterNames.To, Max }
        };
}
