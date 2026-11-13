//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.String;

file sealed class Registration {
    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? CardNumber { get; init; }
    public string? PostalCode { get; init; }
}

[TestClass]
public sealed class LengthRuleTests {
    [TestMethod]
    public void AtMinBoundary_Passes() =>
        MSTestAssertions.That(() => Run("ab", 2, 10).IsValid.Should().BeTrue());

    [TestMethod]
    public void AtMaxBoundary_Passes() =>
        MSTestAssertions.That(() => Run("1234567890", 2, 10).IsValid.Should().BeTrue());

    [TestMethod]
    public void InsideRange_Passes() =>
        MSTestAssertions.That(() => Run("hello", 2, 10).IsValid.Should().BeTrue());

    [TestMethod]
    public void BelowMin_Fails() =>
        ValidationAssert.HasError(Run("a", 2, 10), nameof(Registration.Username));

    [TestMethod]
    public void AboveMax_Fails() =>
        ValidationAssert.HasError(Run("12345678901", 2, 10), nameof(Registration.Username));

    [TestMethod]
    public void NullValue_Skipped() =>
        ValidationAssert.IsValid(Run(null, 2, 10), "LengthRule must skip null.");

    [TestMethod]
    public void EmptyString_WithMinZero_Passes() =>
        MSTestAssertions.That(() => Run("", 0, 10).IsValid.Should().BeTrue());

    [TestMethod]
    public void EmptyString_WithMinOne_Fails() =>
        ValidationAssert.HasError(Run("", 1, 10), nameof(Registration.Username));

    [TestMethod]
    public void CustomMessage_AppearsInError() {
        const string msg = "Username must be 2-10 characters.";
        var rule = new LengthRule<Registration, string?>(
            ValidatorFactory.Accessor<Registration, string?>(
                x => x.Username, nameof(Registration.Username)),
            2, 10, msg);

        var result = RuleRunner.Run(rule, new Registration { Username = "x" });

        ValidationAssert.HasErrorWithMessage(result, nameof(Registration.Username), msg);
    }

    [TestMethod]
    public void MinGreaterThanMax_Throws() {
        const string msg = "Min must be less than or equal to max.";
        var rule = new LengthRule<Registration, string?>(
                accessor: ValidatorFactory.Accessor<Registration, string?>(x => x.Username, nameof(Registration.Username)),
                min: 10,
                max: 2,
                // Default message: The length of the value must be between 10 and 2.
                message: msg
        );
        var result = RuleRunner.Run(rule, new Registration { Username = "x" });

        ValidationAssert.HasErrorWithMessage(result, nameof(Registration.Username), msg);
    }

    private static ValidationResult Run(string? value, int min, int max) {
        var rule = new LengthRule<Registration, string?>(
            ValidatorFactory.Accessor<Registration, string?>(
                x => x.Username, nameof(Registration.Username)),
            min, max, null);
        return RuleRunner.Run(rule, new Registration { Username = value });
    }
}

[TestClass]
public sealed class EmailRuleTests {
    [TestMethod]
    [DataRow("user@example.com")]
    [DataRow("first.last@domain.co.uk")]
    [DataRow("user+tag@mail.org")]
    public void ValidEmail_Passes(string email) {
        var result = Run(email);
        ValidationAssert.IsValid(result, $"'{email}' should be accepted as valid email.");
    }

    [TestMethod]
    [DataRow("notanemail")]
    [DataRow("@nodomain")]
    [DataRow("missing@")]
    [DataRow("two@@at.com")]
    public void InvalidEmail_Fails(string email) {
        var result = Run(email);
        ValidationAssert.HasError(result, nameof(Registration.Email),
            $"'{email}' should be rejected as invalid email.");
    }

    [TestMethod]
    public void NullValue_Skipped() =>
        ValidationAssert.IsValid(Run(null), "EmailRule must skip null.");

    private static ValidationResult Run(string? value) {
        var rule = new EmailRule<Registration, string?>(
            ValidatorFactory.Accessor<Registration, string?>(
                x => x.Email, nameof(Registration.Email)),
            null);
        return RuleRunner.Run(rule, new Registration { Email = value });
    }
}

[TestClass]
public sealed class CreditCardRuleTests {
    [TestMethod]
    [DataRow("4111111111111111")]
    [DataRow("5500005555555559")]
    [DataRow("340000000000009")]
    public void ValidCard_Passes(string number) {
        var result = Run(number);
        ValidationAssert.IsValid(result, $"'{number}' should pass Luhn check.");
    }

    [TestMethod]
    [DataRow("1234567890123456")]
    [DataRow("not-a-card")]
    [DataRow("411111111111111")]
    public void InvalidCard_Fails(string number) {
        var result = Run(number);
        ValidationAssert.HasError(result, nameof(Registration.CardNumber));
    }

    [TestMethod]
    public void NullValue_Skipped() =>
        ValidationAssert.IsValid(Run(null), "CreditCardRule must skip null.");

    private static ValidationResult Run(string? value) {
        var rule = new CreditCardRule<Registration>(
            ValidatorFactory.Accessor<Registration, string?>(x => x.CardNumber, nameof(Registration.CardNumber)),
            null
        );
        return RuleRunner.Run(rule, new Registration { CardNumber = value });
    }
}

[TestClass]
public sealed partial class RegexMatchRuleTests {
    private static readonly Regex _postalCodeRegex = GetPostalCodeRegex();

    [TestMethod]
    [DataRow("1234AB")]
    [DataRow("9999ZZ")]
    public void MatchingPattern_Passes(string code) {
        var result = Run(code);
        ValidationAssert.IsValid(result, $"'{code}' should match the postal pattern.");
    }

    [TestMethod]
    [DataRow("ABCDEF")]
    [DataRow("12345")]
    [DataRow("1234abc")]
    public void NonMatchingPattern_Fails(string code) {
        var result = Run(code);
        ValidationAssert.HasError(result, nameof(Registration.PostalCode));
    }

    [TestMethod]
    public void NullValue_Skipped() =>
        ValidationAssert.IsValid(Run(null), "RegexMatchRule must skip null.");

    private static ValidationResult Run(string? value) {
        var rule = new RegexMatchRule<Registration, string?>(
            ValidatorFactory.Accessor<Registration, string?>(
                x => x.PostalCode, nameof(Registration.PostalCode)),
            _postalCodeRegex,
            null);
        return RuleRunner.Run(rule, new Registration { PostalCode = value });
    }

    [GeneratedRegex(@"^\d{4}[A-Z]{2}$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex GetPostalCodeRegex();
}
