//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Common;

/// <summary>Tests for <see cref="MustRule{T,TProperty}"/>.</summary>

[TestClass]
public sealed class MustRuleTests {
    [TestMethod]
    public void WhenPredicatePasses_ResultIsValid() {
        var rule = PasswordDigitRule();
        var result = RuleRunner.Run(rule, new Person { Name = "Pass1word" });

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void WhenPredicateFails_ResultIsInvalid() {
        var rule = PasswordDigitRule();
        var result = RuleRunner.Run(rule, new Person { Name = "Password" });

        ValidationAssert.HasError(result, nameof(Person.Name));
    }

    [TestMethod]
    public void WhenValueIsNull_RuleIsSkipped() {
        // Must skips null - the developer must combine with NotNull for stricter semantics.
        var rule = PasswordDigitRule();
        var result = RuleRunner.Run(rule, new Person { Name = null });

        ValidationAssert.IsValid(result,
            "MustRule must not fire when the value is null.");
    }

    private static MustRule<Person, string?> PasswordDigitRule() =>
        new(
            ValidatorFactory.Accessor<Person, string?>(x => x.Name, nameof(Person.Name)),
            static p => p?.Any(char.IsDigit) == true,
            "Must contain at least one digit."
        );
}
