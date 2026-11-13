//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a date/time member is in the past relative to a context-supplied current time.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
/// <param name="accessor">The property accessor to extract the value.</param>
/// <param name="nowProvider">Resolves the current UTC time for the validation call.</param>
/// <param name="includeToday">Whether an instant equal to the current time is valid.</param>
/// <param name="message">The custom error message, if any.</param>
/// <param name="messageKey">The key for the validation message.</param>
/// <exception cref="ArgumentNullException"><paramref name="accessor"/> or <paramref name="nowProvider"/> is null.</exception>
public sealed class PastDateRule<T, TValue>(
    IPropertyAccessor<T, TValue> accessor,
    Func<ValidationContext<T>, TValue> nowProvider,
    bool includeToday,
    string? message,
    string? messageKey = ValidationMessageKeys.PAST_DATE
) : SyncValidateRule<T, TValue>(accessor, message, messageKey) {
    private readonly Func<ValidationContext<T>, TValue> _nowProvider =
        nowProvider ?? throw new ArgumentNullException(nameof(nowProvider));

    /// <inheritdoc />
    protected override void ValidateValue(
        TValue value,
        in ValidationContext<T> context,
        ValidationErrorCollector errors) {
        if(value is null) {
            return;
        }

        var now = _nowProvider(context);
        var comparison = Comparer<TValue>.Default.Compare(value, now);

        if(includeToday ? comparison > 0 : comparison >= 0) {
            AddError(errors, context);
        }
    }
}
