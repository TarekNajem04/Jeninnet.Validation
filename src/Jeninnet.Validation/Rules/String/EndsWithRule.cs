//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a string member ends with the specified suffix.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="suffix">The suffix to check.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class EndsWithRule<T>(IPropertyAccessor<T, string?> accessor, string suffix, string? message, string? messageKey = ValidationMessageKeys.ENDS_WITH)
    : EndsWithRule<T, string?>(accessor, suffix, message, messageKey);

/// <summary>
/// Ensures that a string member ends with the specified suffix.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type (string or nullable string).</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="suffix">The suffix to check.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public class EndsWithRule<T, TProperty>(IPropertyAccessor<T, TProperty> accessor, string suffix, string? message, string? messageKey = ValidationMessageKeys.ENDS_WITH)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <summary>
    /// Gets the required suffix.
    /// </summary>
    public string Suffix { get; } = suffix;

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) =>
        new Dictionary<string, object> {
            { "0", Suffix },
            { MessageParameterNames.Value, Suffix }
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> { { MessageParameterNames.Value, Suffix } };

    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        if(value is not string s || string.IsNullOrEmpty(s)) {
            return;
        }

        if(!s.EndsWith(Suffix, StringComparison.Ordinal)) {
            AddError(errors, context);
        }
    }
}
