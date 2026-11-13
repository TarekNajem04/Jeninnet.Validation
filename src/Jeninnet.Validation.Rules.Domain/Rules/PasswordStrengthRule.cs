//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A rule for ensuring that a string member satisfies a password policy.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="policy">The password policy; defaults to <see cref="PasswordPolicy.Strong"/>.</param>
/// <param name="message">The optional error message.</param>
public sealed class PasswordStrengthRule<T>(
    IPropertyAccessor<T, string?> accessor,
    PasswordPolicy? policy = null,
    string? message = null)
    : PasswordStrengthRule<T, string?>(accessor, policy, message);

/// <summary>
/// A rule for ensuring that a string member satisfies a password policy. Null and empty values are
/// skipped; pair the rule with <c>NotEmpty</c> when the password is required.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="policy">The password policy; defaults to <see cref="PasswordPolicy.Strong"/>.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
/// <exception cref="ArgumentOutOfRangeException">The policy has invalid length bounds.</exception>
/// <exception cref="ArgumentException">The policy has no name.</exception>
public class PasswordStrengthRule<T, TProperty>(
    IPropertyAccessor<T, TProperty> accessor,
    PasswordPolicy? policy,
    string? message,
    string? messageKey = ValidationMessageKeys.PASSWORD_STRENGTH)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <summary>
    /// Gets the password policy used by the rule.
    /// </summary>
    public PasswordPolicy Policy { get; } = ValidatePolicy(policy ?? PasswordPolicy.Strong);

    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(value is not string text || text.Length is 0) {
            return;
        }

        if(Policy.FindViolations(text).Count > 0) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) {
        var value = Accessor.GetValue(context.Instance);
        var text = value as string ?? string.Empty;
        var violations = Policy.FindViolations(text);

        return new Dictionary<string, object> {
            { MessageParameterNames.Policy, Policy.Name },
            { MessageParameterNames.MinLength, Policy.MinLength },
            { MessageParameterNames.MissingRequirements, string.Join(", ", violations) }
        };
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> {
            { MessageParameterNames.Policy, Policy.Name },
            { MessageParameterNames.MinLength, Policy.MinLength }
        };

    private static PasswordPolicy ValidatePolicy(PasswordPolicy policy) {
        ArgumentNullException.ThrowIfNull(policy);
        policy.EnsureValid();
        return policy;
    }
}
