//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A specialized, type-specific rule for ensuring that a string member matches a regular expression.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="regex">The regex to match against.</param>
/// <param name="message"   >The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class RegexMatchRule<T>(IPropertyAccessor<T, string?> accessor, Regex regex, string? message, string? messageKey = ValidationMessageKeys.REGEX)
    : RegexMatchRule<T, string?>(accessor, regex, message, messageKey);

/// <summary>
/// A specialized, type-specific rule for ensuring that a string member matches a regular expression.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type (string or nullable string).</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="regex">The regex to match against.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public class RegexMatchRule<T, TProperty>(IPropertyAccessor<T, TProperty> accessor, Regex regex, string? message, string? messageKey = ValidationMessageKeys.REGEX)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <summary>
    /// Gets the regular expression pattern.
    /// </summary>
    public string Pattern { get; } = regex.ToString();

    private Regex Expression { get; } = regex;

    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        if(value is not string s || string.IsNullOrEmpty(s)) {
            return;
        }

        if(!Expression.IsMatch(s)) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) =>
        new Dictionary<string, object> {
            { "0", Pattern },
            { MessageParameterNames.Pattern, Pattern }
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> { { MessageParameterNames.Pattern, Pattern } };
}
