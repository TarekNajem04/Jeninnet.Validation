//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Rules;

internal sealed class StringRuleModel {
    public string? Value { get; set; }
}

[TestClass]
public sealed partial class SingleGenericStringRuleTests {
    private static readonly IPropertyAccessor<StringRuleModel, string?> _value =
        ValidatorFactory.Accessor<StringRuleModel, string?>(static x => x.Value, nameof(StringRuleModel.Value));

    [GeneratedRegex("^[a-z]+$")]
    private static partial Regex LettersRegex();

    [TestMethod]
    public void SingleGenericStringRules_ValidateValidAndInvalidValues() {
        AssertRule(new EmailRule<StringRuleModel>(_value, null), "buyer@example.com", "not-an-email");
        AssertRule(new CreditCardRule<StringRuleModel>(_value, null), "4111111111111111", "123");
        AssertRule(new LengthRule<StringRuleModel>(_value, 2, 4, null), "abc", "a");
        AssertRule(new ExactLengthRule<StringRuleModel>(_value, 3, null), "abc", "ab");
        AssertRule(new RegexMatchRule<StringRuleModel>(_value, LettersRegex(), null), "abc", "123");
        AssertRule(new StringNotEmptyRule<StringRuleModel>(_value, null), "a", "");
        AssertRule(new UrlRule<StringRuleModel>(_value, null), "https://example.com", "not-a-url");
        AssertRule(new PhoneRule<StringRuleModel>(_value, null), "0612345678", "abc");
        AssertRule(new IPAddressRule<StringRuleModel>(_value, null), "127.0.0.1", "999.1.1.1");
        AssertRule(new ContainsRule<StringRuleModel>(_value, "bc", null), "abcd", "xyz");
        AssertRule(new StartsWithRule<StringRuleModel>(_value, "ab", null), "abcd", "xabcd");
        AssertRule(new EndsWithRule<StringRuleModel>(_value, "cd", null), "abcd", "abcx");
    }

    private static void AssertRule(
        ISyncValidationRule<StringRuleModel> rule,
        string validValue,
        string invalidValue) {
        var validModel = new StringRuleModel { Value = validValue };
        var invalidModel = new StringRuleModel { Value = invalidValue };

        MSTestAssertions.That(() => RuleRunner.Run(rule, validModel).IsValid.Should().BeTrue());
        MSTestAssertions.That(() => RuleRunner.Run(rule, invalidModel).Errors.Should().HaveCount(1));
    }
}
