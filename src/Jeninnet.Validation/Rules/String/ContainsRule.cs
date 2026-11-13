//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a string member contains the specified substring.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="substring">The substring to check for.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class ContainsRule<T>(IPropertyAccessor<T, string?> accessor, string substring, string? message, string? messageKey = ValidationMessageKeys.CONTAINS)
    : ContainsRule<T, string?>(accessor, substring, message, messageKey);

/// <summary>
/// Ensures that a string member contains the specified substring.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property type (decimal or decimal?).</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="substring">The substring to check for.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public class ContainsRule<T, TValue>(IPropertyAccessor<T, TValue> accessor, string substring, string? message, string? messageKey = ValidationMessageKeys.CONTAINS)
    : SyncValidateRule<T, TValue>(accessor, message, messageKey) {
    /// <summary>
    /// Gets the required substring.
    /// </summary>
    public string Substring { get; } = substring;

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) =>
        new Dictionary<string, object> {
            { "0", Substring },
            { MessageParameterNames.Value, Substring }
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> { { MessageParameterNames.Value, Substring } };

    /// <summary>
    /// Validates the specified value.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="errors">The collector for validation errors.</param>
    protected override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        if(value is not string s || string.IsNullOrEmpty(s)) {
            return;
        }

        if(!s.Contains(Substring, StringComparison.Ordinal)) {
            AddError(errors, context);
        }
    }
}
