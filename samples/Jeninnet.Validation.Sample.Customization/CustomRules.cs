//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Customization;

/// <summary>
/// A custom synchronous validation rule that verifies if a string starts with a specific prefix.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="prefix">The prefix to check.</param>
/// <param name="message">The optional error message.</param>
public sealed class StartsWithRule<T>(
    IPropertyAccessor<T, string?> accessor,
    string prefix,
    string? message = null
) : SyncValidateRule<T, string?>(accessor, message) {
    /// <summary>
    /// Validates the specified value.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="errors">The collector for validation errors.</param>
    protected override void ValidateValue(string? value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        if(value?.StartsWith(prefix, StringComparison.Ordinal) == false) {
            errors.Add(context.GetFullPath(Accessor.MemberName), $"Value must start with '{prefix}'.");
        }
    }
}

/// <summary>
/// A custom asynchronous validation rule that simulates an external check (e.g., database).
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="message">The optional error message.</param>
public sealed class UsernameAvailableRule<T>(
    IPropertyAccessor<T, string?> accessor,
    string? message = null
) : IAsyncValidationRule<T> {
    /// <summary>
    /// Validates the specified value asynchronously.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="errors">The collector for validation errors.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask ValidateAsync(ValidationContext<T> context, ValidationErrorCollector errors, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(errors);
        var value = accessor.GetValue(context.Instance);
        if(string.IsNullOrWhiteSpace(value)) {
            return;
        }

        // Simulate network/database delay
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);

        if(value.Equals("admin", StringComparison.OrdinalIgnoreCase)) {
            errors.Add(new ValidationError(
                context.GetFullPath(accessor.MemberName),
                message ?? "Username 'admin' is already taken."
            ));
        }
    }
}

public static class CustomRuleExtensions {
    /// <summary>
    /// Adds a StartsWith rule to the rule builder.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="prefix">The prefix to check.</param>
    /// <param name="message">The optional error message.</param>
    public static IRuleBuilder<T, string?> StartsWith<T>(this IRuleBuilder<T, string?> builder, string prefix, string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddRule(new StartsWithRule<T>(builder.Accessor, prefix, message));
        return builder;
    }

    /// <summary>
    /// Adds a UsernameAvailable rule to the rule builder.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">The optional error message.</param>
    public static IRuleBuilder<T, string?> UsernameAvailable<T>(this IRuleBuilder<T, string?> builder, string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddAsyncRule(new UsernameAvailableRule<T>(builder.Accessor, message));
        return builder;
    }
}
