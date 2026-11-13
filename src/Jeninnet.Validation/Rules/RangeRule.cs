//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A specialized, type-specific rule for ensuring that a member is within a specified range.
/// This rule is constrained by <see cref="IComparable{T}"/> to avoid runtime reflection and boxing.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public sealed class RangeRule<T, TProperty> : SyncValidateRule<T, TProperty> where TProperty : struct, IComparable<TProperty> {
    private readonly TProperty _min;
    private readonly TProperty _max;

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeRule{T, TProperty}"/> class.
    /// </summary>
    /// <param name="accessor">The property accessor to extract the value.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="message">The optional error message.</param>
    /// <param name="messageKey">The key for the validation message.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="min"/> is greater than <paramref name="max"/>.
    /// </exception>
    public RangeRule(
        IPropertyAccessor<T, TProperty> accessor,
        TProperty min,
        TProperty max,
        string? message,
        string? messageKey = ValidationMessageKeys.RANGE
    ) : base(accessor, message, messageKey) {
        RangeBounds.EnsureOrdered(min, max, exclusive: false);
        _min = min;
        _max = max;
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context)
        => new Dictionary<string, object> {
            { "0", _min },
            { "1", _max },
            { MessageParameterNames.Min, _min },
            { MessageParameterNames.Max, _max },
            { MessageParameterNames.From, _min },
            { MessageParameterNames.To, _max }
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> {
            { MessageParameterNames.Min, _min },
            { MessageParameterNames.Max, _max },
            { MessageParameterNames.From, _min },
            { MessageParameterNames.To, _max }
        };

    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        if(value.CompareTo(_min) < 0 || value.CompareTo(_max) > 0) {
            AddError(errors, context);
        }
    }
}
