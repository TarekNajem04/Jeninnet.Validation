//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a member satisfies a user-defined predicate.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
/// <param name="accessor">The property accessor to extract the value.</param>
/// <param name="predicate">The predicate to evaluate.</param>
/// <param name="message">The custom validation message, if any.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class MustRule<T, TValue>(
    IPropertyAccessor<T, TValue> accessor,
    Func<TValue, bool> predicate,
    string? message,
    string? messageKey = ValidationMessageKeys.MUST
) : SyncValidateRule<T, TValue>(accessor, message, messageKey) {
    /// <summary>
    /// The predicate to evaluate. Cannot be null.
    /// </summary>
    public Func<TValue, bool> Predicate { get; } = predicate ?? throw new ArgumentNullException(nameof(predicate));

    /// <inheritdoc />
    protected override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        // If value is null, skip. Combine with NotNull for stricter semantics.
        if(value is null) {
            return;
        }

        if(!Predicate(value)) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context)
        => new Dictionary<string, object> { { "0", Predicate } };
}
