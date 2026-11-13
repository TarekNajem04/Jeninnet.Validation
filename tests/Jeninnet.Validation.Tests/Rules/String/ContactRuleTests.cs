//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.String;

file sealed class Registration {
    public string? PostalCode { get; init; }
}

[TestClass]
public sealed class ContactRuleTests {
    [TestMethod]
    [DataRow("0550661234")]
    [DataRow("+1 123-456-7890")]
    [DataRow("123 456 78")]
    public void Phone_Valid_Passes(string phone) {
        var result = Run(phone);
        ValidationAssert.IsValid(result, $"'{phone}' should be a valid phone number.");
    }

    [TestMethod]
    [DataRow("abc-def-ghij")]
    [DataRow("123")]
    public void Phone_Invalid_Fails(string phone) {
        var result = Run(phone);
        ValidationAssert.HasError(result, nameof(Registration.PostalCode));
    }

    private static ValidationResult Run(string? value) {
        var rule = new PhoneRule<Registration, string?>(
            ValidatorFactory.Accessor<Registration, string?>(x => x.PostalCode, nameof(Registration.PostalCode)),
            null);
        return RuleRunner.Run(rule, new Registration { PostalCode = value });
    }
}
