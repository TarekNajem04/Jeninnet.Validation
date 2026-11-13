//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a member satisfies a user-defined asynchronous predicate.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
/// <param name="accessor">The property accessor to extract the value.</param>
/// <param name="predicate">The asynchronous predicate to evaluate.</param>
/// <param name="message">The custom validation message, if any.</param>
/// <param name="messageKey">The key for the validation message.</param>
/// <exception cref="ArgumentNullException"></exception>
public sealed class MustAsyncCondition<T, TValue>(
    IPropertyAccessor<T, TValue?> accessor,
    Func<TValue, CancellationToken, Task<bool>> predicate,
    string? message,
    string? messageKey = ValidationMessageKeys.MUST_ASYNC
) : AsyncValidateRule<T, TValue?>(accessor, message, messageKey) {
    /// <summary>
    /// The asynchronous predicate to evaluate. Cannot be null.
    /// </summary>
    public Func<TValue, CancellationToken, Task<bool>> Predicate { get; } =
        predicate ?? throw new ArgumentNullException(nameof(predicate));

    /// <inheritdoc />
    protected override async ValueTask ValidateValueAsync(TValue? value, ValidationContext<T> context, ValidationErrorCollector errors, CancellationToken cancellationToken) {
        // If value is null, skip. Combine with NotNull for stricter semantics.
        if(value is null) {
            return;
        }

        if(!await Predicate(value, cancellationToken).ConfigureAwait(false)) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context)
        => new Dictionary<string, object> { { "0", Predicate } };
}
