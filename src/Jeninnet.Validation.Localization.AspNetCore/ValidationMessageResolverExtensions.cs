//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Localization.AspNetCore;

/// <summary>
/// Convenience helpers for <see cref="IValidationMessageResolver"/>.
/// </summary>
public static class ValidationMessageResolverExtensions {
    /// <summary>
    /// Resolves messages for a sequence of validation errors, preserving their order.
    /// </summary>
    /// <param name="resolver">The message resolver.</param>
    /// <param name="errors">The validation errors.</param>
    /// <param name="culture">The optional culture.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The resolved messages in the same order as <paramref name="errors"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> or <paramref name="errors"/> is null.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is cancelled.</exception>
    public static async ValueTask<IReadOnlyList<string?>> ResolveValidationErrorsAsync(
        this IValidationMessageResolver resolver,
        IEnumerable<ValidationError> errors,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(errors);

        var messages = new List<string?>();

        foreach(var error in errors) {
            cancellationToken.ThrowIfCancellationRequested();
            messages.Add(await resolver.ResolveAsync(error, culture, cancellationToken).ConfigureAwait(false));
        }

        return messages;
    }
}
