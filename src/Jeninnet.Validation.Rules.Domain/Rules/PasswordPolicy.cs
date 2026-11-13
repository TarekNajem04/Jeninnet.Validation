//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Describes a configurable password policy used by <see cref="PasswordStrengthRule{T, TProperty}"/>.
/// </summary>
public sealed record PasswordPolicy {
    private static readonly HashSet<string> _commonPasswords = [
        with(StringComparer.OrdinalIgnoreCase),
        "123456", "12345678", "123456789", "abc123", "admin", "baseball",
        "dragon", "football", "iloveyou", "letmein", "login", "master",
        "monkey", "passw0rd", "password", "password1", "princess", "qwerty",
        "sunshine", "wachtwoord", "welkom", "welcome"
    ];

    /// <summary>
    /// Gets the display name of the policy.
    /// </summary>
    public string Name { get; init; } = "Custom";

    /// <summary>
    /// Gets the minimum number of characters.
    /// </summary>
    public int MinLength { get; init; } = 12;

    /// <summary>
    /// Gets the maximum number of characters. <c>0</c> means no maximum.
    /// </summary>
    public int MaxLength { get; init; } = 128;

    /// <summary>
    /// Gets a value indicating whether an upper-case letter is required.
    /// </summary>
    public bool RequireUppercase { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a lower-case letter is required.
    /// </summary>
    public bool RequireLowercase { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a digit is required.
    /// </summary>
    public bool RequireDigit { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a non-alphanumeric character is required.
    /// </summary>
    public bool RequireSymbol { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether whitespace is rejected.
    /// </summary>
    public bool DisallowWhitespace { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a small built-in common-password list is rejected.
    /// </summary>
    public bool DisallowCommonPasswords { get; init; } = true;

    /// <summary>
    /// Gets the built-in basic policy: eight characters, letters, and digits.
    /// </summary>
    public static PasswordPolicy Basic { get; } = new() {
        Name = "Basic",
        MinLength = 8,
        RequireSymbol = false,
        DisallowCommonPasswords = false
    };

    /// <summary>
    /// Gets the built-in strong policy: twelve characters with every character class.
    /// </summary>
    public static PasswordPolicy Strong { get; } = new() {
        Name = "Strong"
    };

    /// <summary>
    /// Gets the built-in strict policy: sixteen characters with every character class.
    /// </summary>
    public static PasswordPolicy Strict { get; } = new() {
        Name = "Strict",
        MinLength = 16
    };

    /// <summary>
    /// Returns the stable requirement codes that the supplied password does not satisfy.
    /// </summary>
    /// <param name="value">The password to inspect.</param>
    /// <returns>The missing requirement codes in a deterministic order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public IReadOnlyList<string> FindViolations(string value) {
        ArgumentNullException.ThrowIfNull(value);

        var violations = new List<string>(8);
        var (hasUppercase, hasLowercase, hasDigit, hasSymbol, hasWhitespace) = AnalyzeCharacterClasses(value);

        if(value.Length < MinLength) {
            violations.Add("length");
        }

        if(MaxLength > 0 && value.Length > MaxLength) {
            violations.Add("max-length");
        }

        if(RequireUppercase && !hasUppercase) {
            violations.Add("uppercase");
        }

        if(RequireLowercase && !hasLowercase) {
            violations.Add("lowercase");
        }

        if(RequireDigit && !hasDigit) {
            violations.Add("digit");
        }

        if(RequireSymbol && !hasSymbol) {
            violations.Add("symbol");
        }

        if(DisallowWhitespace && hasWhitespace) {
            violations.Add("whitespace");
        }

        if(DisallowCommonPasswords && _commonPasswords.Contains(value)) {
            violations.Add("common");
        }

        return violations;
    }

    private static (bool HasUppercase, bool HasLowercase, bool HasDigit, bool HasSymbol, bool HasWhitespace)
        AnalyzeCharacterClasses(string value) {
        var hasUppercase = false;
        var hasLowercase = false;
        var hasDigit = false;
        var hasSymbol = false;
        var hasWhitespace = false;

        foreach(var character in value) {
            if(char.IsUpper(character)) {
                hasUppercase = true;
            } else if(char.IsLower(character)) {
                hasLowercase = true;
            } else if(char.IsAsciiDigit(character)) {
                hasDigit = true;
            } else if(char.IsWhiteSpace(character)) {
                hasWhitespace = true;
            } else {
                hasSymbol = true;
            }
        }

        return (hasUppercase, hasLowercase, hasDigit, hasSymbol, hasWhitespace);
    }

    /// <summary>
    /// Validates the policy configuration.
    /// </summary>
    /// <exception cref="ArgumentException">The name is empty, or the length bounds are invalid.</exception>
    internal void EnsureValid() {
        if(string.IsNullOrWhiteSpace(Name)) {
            throw new ArgumentException("A password policy name is required.");
        }

        if(MinLength <= 0) {
            throw new ArgumentException("The minimum password length must be positive.");
        }

        if(MaxLength > 0 && MaxLength < MinLength) {
            throw new ArgumentException(
                "The maximum password length must be zero or at least the minimum length.");
        }
    }
}
