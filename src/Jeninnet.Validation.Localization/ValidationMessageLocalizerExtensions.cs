//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Localization;

/// <summary>
/// Bridges <see cref="ValidationError"/> instances into the sibling localization pipeline.
/// </summary>
public static class ValidationMessageLocalizerExtensions {
    /// <summary>
    /// Localizes a validation error, injecting the error's member name as the
    /// <c>{PropertyName}</c> parameter and merging the rule's message parameters.
    /// </summary>
    /// <param name="localizer">The message localizer.</param>
    /// <param name="error">The validation error.</param>
    /// <param name="culture">The culture; defaults to <see cref="CultureInfo.CurrentUICulture"/>.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>
    /// The localized message, or the error's fallback <see cref="ValidationError.Message"/> when no
    /// template is found (or the error has no message key).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="localizer"/> is null.</exception>
    public static async ValueTask<string?> LocalizeValidationErrorAsync(
        this IMessageLocalizer localizer,
        ValidationError error,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(localizer);

        if(error.MessageKey is null) {
            return error.Message;
        }

        if(error.MessageIsAuthored) {
            return error.Message;
        }

        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        if(error.MessageParameters is not null) {
            foreach(var parameter in error.MessageParameters) {
                parameters[parameter.Key] = parameter.Value;
            }
        }

        if(!parameters.ContainsKey("PropertyName")) {
            parameters["PropertyName"] = error.MemberName;
        }

        var localized = await localizer
            .LocalizeAsync(error.MessageKey, parameters, culture ?? CultureInfo.CurrentUICulture, cancellationToken)
            .ConfigureAwait(false);

        return localized ?? error.Message;
    }
}
