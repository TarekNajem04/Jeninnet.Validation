//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Provides common functionality for comparing two properties of the same object.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <param name="left">The left property accessor.</param>
/// <param name="right">The right property accessor.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The optional message key.</param>
public abstract class PropertyComparisonRuleBase<T, TProperty>(
    IPropertyAccessor<T, TProperty> left,
    IPropertyAccessor<T, TProperty> right,
    string? message,
    string? messageKey = ValidationMessageKeys.COMPARABLE
) : SyncValidateRule<T, TProperty>(left, message, messageKey) {
    /// <summary>
    /// Gets the left property accessor.
    /// </summary>
    public IPropertyAccessor<T, TProperty> Left => Accessor;

    /// <summary>
    /// Gets the right property accessor.
    /// </summary>
    public IPropertyAccessor<T, TProperty> Right { get; } = right;

    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        var rightValue = Right.GetValue(context.Instance);

        if(!Compare(value, rightValue)) {
            AddError(errors, context);
        }
    }

    /// <summary>
    /// Performs a comparison between two property values.
    /// </summary>
    /// <param name="leftValue">The left value.</param>
    /// <param name="rightValue">The right value.</param>
    /// <returns>True if the comparison is valid; otherwise, false.</returns>
    protected virtual bool Compare(TProperty leftValue, TProperty rightValue) {
        if(leftValue is null && rightValue is null) {
            return IsValid(0); // Equal
        }

        if(leftValue is null) {
            return IsValid(-1); // Less than
        }

        if(rightValue is null) {
            return IsValid(1); // Greater than
        }

        if(leftValue is string leftString && rightValue is string rightString) {
            return IsValid(string.CompareOrdinal(leftString, rightString));
        }

        return leftValue switch {
            IComparable comparable => IsValid(comparable.CompareTo(rightValue)),
            IComparable<TProperty> comparableOfT => IsValid(comparableOfT.CompareTo(rightValue)),
            IEquatable<TProperty> equatableOfT => IsValid(equatableOfT.Equals(rightValue) ? 0 : 1),
            _ => IsValid(EqualityComparer<TProperty>.Default.Equals(leftValue, rightValue) ? 0 : 1),
        };
    }

    /// <summary>
    /// Validates the result of the comparison.
    /// </summary>
    /// <param name="comparisonResult">
    /// The result of the comparison.
    /// &lt; 0 (negative): current instance is less than the argument.
    /// 0: current instance is equal to the argument.
    /// &gt; 0 (positive): current instance is greater than the argument.
    /// </param>
    /// <returns>True if valid; otherwise, false.</returns>
    protected abstract bool IsValid(int comparisonResult);

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) {
        var leftValue = Left.GetValue(context.Instance)!;
        var rightValue = Right.GetValue(context.Instance)!;

        return new Dictionary<string, object> {
            { "0", leftValue },
            { "1", rightValue },
            { MessageParameterNames.PropertyValue, leftValue },
            { MessageParameterNames.ComparisonValue, rightValue },
            { MessageParameterNames.ComparisonProperty, Right.MemberName }
        };
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> {
            { "Other", Right.MemberName },
            { MessageParameterNames.ComparisonProperty, Right.MemberName }
        };
}
