//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a member's value is within a specified set of valid values.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
public sealed class InRule<T, TValue> : SyncValidateRule<T, TValue> {
    /// <summary>
    /// The collection of allowed values for the property. The member's value must be one of these to pass validation.
    /// </summary>
    public IEnumerable<TValue> Values { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InRule{T, TValue}"/> class.
    /// </summary>
    /// <param name="accessor">The property accessor to extract the value.</param>
    /// <param name="values">The collection of allowed values.</param>
    /// <param name="message">The custom validation message, if any.</param>
    /// <param name="messageKey">The key for the validation message.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public InRule(IPropertyAccessor<T, TValue> accessor, IEnumerable<TValue> values, string? message, string? messageKey = ValidationMessageKeys.IN)
        : base(accessor, message, messageKey) {
        ArgumentNullException.ThrowIfNull(values);

        if(!values.Any()) {
            throw new ArgumentException("The collection of allowed values cannot be empty.", nameof(values));
        }

        Values = values;
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) {
        var joined = string.Join(", ", Values);

        return new Dictionary<string, object> {
            { "0", joined },
            { MessageParameterNames.Values, joined }
        };
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> { { MessageParameterNames.Values, string.Join(", ", Values) } };

    /// <inheritdoc />
    protected override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        if(value is null) {
            return;
        }

        if(!Values.Contains(value)) {
            AddError(errors, context);
        }
    }
}
