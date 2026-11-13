//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides fluent extension methods for configuring string validation rules.
/// </summary>
public static class RuleBuilderStringExtensions {
    // =======================================================================
    //  STRING
    // =======================================================================

    /// <summary>
    /// Adds a starts-with validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="prefix">The required prefix.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, string?> StartsWith<T>(
        this IRuleBuilder<T, string?> builder,
        string prefix,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddRule(new StartsWithRule<T>(builder.Accessor, prefix, message));
        return builder;
    }

    /// <summary>
    /// Adds an ends-with validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="suffix">The required suffix.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, string?> EndsWith<T>(
        this IRuleBuilder<T, string?> builder,
        string suffix,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddRule(new EndsWithRule<T>(builder.Accessor, suffix, message));
        return builder;
    }

    /// <summary>
    /// Adds a contains validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="substring">The required substring.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, string?> Contains<T>(
        this IRuleBuilder<T, string?> builder,
        string substring,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddRule(new ContainsRule<T>(builder.Accessor, substring, message));
        return builder;
    }

    // =======================================================================
    //  LENGTH
    // =======================================================================

    /// <summary>
    /// Adds a length validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="min">The minimum inclusive length.</param>
    /// <param name="max">The maximum inclusive length.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> Length<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        int min,
        int max,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        if(min > max) {
            throw new ArgumentOutOfRangeException(nameof(min), "min must be less than or equal to max");
        }

        builder.AddRule(new LengthRule<T, TValue>(builder.Accessor, min, max, message));
        return builder;
    }

    /// <summary>
    /// Adds a minimum length validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="min">The minimum inclusive length.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> MinimumLength<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        int min,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Length(min, int.MaxValue, message);
    }

    /// <summary>
    /// Adds a maximum length validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="max">The maximum inclusive length.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> MaximumLength<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        int max,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Length(0, max, message);
    }

    // =======================================================================
    //  EXACT LENGTH / BASE64 / FILE EXTENSIONS
    // =======================================================================

    /// <summary>
    /// Adds an exact-length validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="length">The required length.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
    public static IRuleBuilder<T, TValue> ExactLength<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        int length,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        builder.AddRule(new ExactLengthRule<T, TValue>(builder.Accessor, length, message));
        return builder;
    }

    /// <summary>
    /// Adds a Base64 validation rule. Whitespace is ignored; null is valid.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRuleBuilder<T, TValue> Base64String<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new Base64StringRule<T, TValue>(builder.Accessor, message));
        return builder;
    }

    /// <summary>
    /// Adds a file-extension validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="extensions">The allowed file extensions (with or without a leading dot).</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="extensions"/> is null.</exception>
    /// <exception cref="ArgumentException">No non-empty extension was supplied.</exception>
    public static IRuleBuilder<T, TValue> FileExtensions<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        params string[] extensions) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(extensions);

        builder.AddRule(new FileExtensionsRule<T, TValue>(builder.Accessor, extensions));
        return builder;
    }

    /// <summary>
    /// Adds a file-extension validation rule with matching options.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="extensions">The allowed file extensions (with or without a leading dot).</param>
    /// <param name="ignoreCase">Whether extension matching ignores case.</param>
    /// <param name="allowNonStandard">Whether values without an extension are accepted.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="extensions"/> is null.</exception>
    /// <exception cref="ArgumentException">No non-empty extension was supplied.</exception>
    public static IRuleBuilder<T, TValue> FileExtensions<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        IEnumerable<string> extensions,
        bool ignoreCase = true,
        bool allowNonStandard = false,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(extensions);

        builder.AddRule(new FileExtensionsRule<T, TValue>(builder.Accessor, extensions, ignoreCase, allowNonStandard, message));
        return builder;
    }

    // =======================================================================
    //  REGEX MATCH
    // =======================================================================

    /// <summary>
    /// Adds a regex validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="regex">The compiled regular expression.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> Matches<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        Regex regex,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new RegexMatchRule<T, TValue>(builder.Accessor, regex, message));
        return builder;
    }

    /// <summary>
    /// Adds a regex validation rule using a pattern string.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="pattern">The regular expression pattern.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> Matches<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        string pattern,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Matches(new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1)), message);
    }

    // =======================================================================
    //  EMAIL
    // =======================================================================

    /// <summary>
    /// Adds an email address validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> EmailAddress<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new EmailRule<T, TValue>(builder.Accessor, message));
        return builder;
    }

    // =======================================================================
    //  CREDIT CARD
    // =======================================================================

    /// <summary>
    /// Adds a credit card validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> CreditCard<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new CreditCardRule<T, TValue>(builder.Accessor, message));
        return builder;
    }

    // =======================================================================
    //  PHONE
    // =======================================================================

    /// <summary>
    /// Adds a phone number validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> Phone<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new PhoneRule<T, TValue>(builder.Accessor, message));
        return builder;
    }

    // =======================================================================
    //  URL
    // =======================================================================

    /// <summary>
    /// Adds a URL validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> Url<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new UrlRule<T, TValue>(builder.Accessor, message));
        return builder;
    }

    // =======================================================================
    //  IP ADDRESS
    // =======================================================================

    /// <summary>
    /// Adds an IP address validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> IPAddress<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new IPAddressRule<T, TValue>(builder.Accessor, message));
        return builder;
    }
}
