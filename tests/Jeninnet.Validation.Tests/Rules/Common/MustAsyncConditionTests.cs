//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Common;

/// <summary>Tests for <see cref="MustAsyncCondition{T,TValue}"/>.</summary>
[TestClass]
public sealed class MustAsyncConditionTests {
    [TestMethod]
    public async Task WhenPredicatePasses_ResultIsValidAsync() {
        var rule = PasswordDigitCondition();
        var result = await RuleRunner.RunAsync(
            rule,
            new Person { Name = "Pass1word" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public async Task WhenPredicateFails_ResultIsInvalidAsync() {
        var rule = PasswordDigitCondition();
        var result = await RuleRunner.RunAsync(
            rule,
            new Person { Name = "Password" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.HasError(result, nameof(Person.Name));
    }

    [TestMethod]
    public async Task WhenValueIsNull_RuleIsSkippedAsync() {
        var rule = PasswordDigitCondition();
        var result = await RuleRunner.RunAsync(
            rule,
            new Person { Name = null },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.IsValid(result, "MustAsyncCondition must not fire when the value is null.");
    }

    private static MustAsyncCondition<Person, string> PasswordDigitCondition() =>
        new(
            ValidatorFactory.Accessor<Person, string?>(static x => x.Name, nameof(Person.Name)),
            static (value, _) => Task.FromResult(value.Any(char.IsDigit)),
            "Must contain at least one digit.");

    public TestContext? TestContext { get; set; }
}
