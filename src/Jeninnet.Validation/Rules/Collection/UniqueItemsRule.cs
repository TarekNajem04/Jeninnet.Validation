//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a collection member does not contain duplicate elements. Elements are compared
/// with the supplied <see cref="IEqualityComparer{T}"/>, or with
/// <see cref="EqualityComparer{T}.Default"/> when no comparer is supplied.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
/// <param name="accessor">The collection accessor.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
/// <param name="comparer">The element comparer. Defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
public sealed class UniqueItemsRule<T, TElement>(
    IPropertyAccessor<T, IEnumerable<TElement>?> accessor,
    string? message = null,
    string? messageKey = ValidationMessageKeys.UNIQUE_ITEMS,
    IEqualityComparer<TElement>? comparer = null
    )
    : SyncValidateRule<T, IEnumerable<TElement>?>(accessor, message, messageKey) {
    private readonly IEqualityComparer<TElement> _comparer = comparer ?? EqualityComparer<TElement>.Default;

    /// <inheritdoc />
    protected override void ValidateValue(
        IEnumerable<TElement>? value,
        in ValidationContext<T> context,
        ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(value is null) {
            return;
        }

        if(value.GroupBy(static element => element, _comparer).Any(static group => group.Skip(1).Any())) {
            AddError(errors, context);
        }
    }
}
