//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Localization.AspNetCore;

/// <summary>
/// Resolves a display message for a <see cref="ValidationError"/> without changing the global
/// culture.
/// </summary>
public interface IValidationMessageResolver {
    /// <summary>
    /// Resolves the message for a validation error.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <param name="culture">
    /// The requested culture, or <see langword="null"/> to use the configured force/default
    /// culture or <see cref="CultureInfo.CurrentUICulture"/>.
    /// </param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>
    /// The localized message from the first source that has one: the configured
    /// <see cref="IStringLocalizer"/>, the registered <see cref="IMessageLocalizer"/>, then
    /// <see cref="ValidationError.Message"/>. Returns <see langword="null"/> when the error has no
    /// message and no source resolves one.
    /// </returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is cancelled.</exception>
    ValueTask<string?> ResolveAsync(
        ValidationError error,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default);
}
