//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides fluent extension methods for the optional domain rule pack.
/// </summary>
public static class RuleBuilderDomainExtensions {
    /// <summary>
    /// Adds an IBAN validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="country">The optional required IBAN country code.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="country"/> is not a two-letter code.</exception>
    /// <exception cref="NotSupportedException"><paramref name="country"/> is not an IBAN country.</exception>
    public static IRuleBuilder<T, string?> Iban<T>(
        this IRuleBuilder<T, string?> builder,
        string? country = null,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new IbanRule<T>(builder.Accessor, country, message));
        return builder;
    }

    /// <summary>
    /// Adds a VAT-number validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="country">The required VAT country code.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="country"/> is not a two-letter code.</exception>
    /// <exception cref="NotSupportedException"><paramref name="country"/> is not supported.</exception>
    public static IRuleBuilder<T, string?> VatNumber<T>(
        this IRuleBuilder<T, string?> builder,
        string country,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new VatNumberRule<T>(builder.Accessor, country, message));
        return builder;
    }

    /// <summary>
    /// Adds a postal-code validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="country">The required postal-code country code.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="country"/> is not a two-letter code.</exception>
    /// <exception cref="NotSupportedException"><paramref name="country"/> is not supported.</exception>
    public static IRuleBuilder<T, string?> PostalCode<T>(
        this IRuleBuilder<T, string?> builder,
        string country,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new PostalCodeRule<T>(builder.Accessor, country, message));
        return builder;
    }

    /// <summary>
    /// Adds a national-ID validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="country">The required national-ID country code.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="country"/> is not a two-letter code.</exception>
    /// <exception cref="NotSupportedException"><paramref name="country"/> is not supported.</exception>
    public static IRuleBuilder<T, string?> NationalId<T>(
        this IRuleBuilder<T, string?> builder,
        string country,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new NationalIdRule<T>(builder.Accessor, country, message));
        return builder;
    }

    /// <summary>
    /// Adds a password-strength validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="policy">The password policy; defaults to <see cref="PasswordPolicy.Strong"/>.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The policy has invalid length bounds.</exception>
    public static IRuleBuilder<T, string?> PasswordStrength<T>(
        this IRuleBuilder<T, string?> builder,
        PasswordPolicy? policy = null,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new PasswordStrengthRule<T>(builder.Accessor, policy, message));
        return builder;
    }

    /// <summary>
    /// Adds a UUID validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRuleBuilder<T, string?> Uuid<T>(
        this IRuleBuilder<T, string?> builder,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new UuidRule<T>(builder.Accessor, message));
        return builder;
    }

    /// <summary>
    /// Adds a ULID validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRuleBuilder<T, string?> Ulid<T>(
        this IRuleBuilder<T, string?> builder,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new UlidRule<T>(builder.Accessor, message));
        return builder;
    }
}
