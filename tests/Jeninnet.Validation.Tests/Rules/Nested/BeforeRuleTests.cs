//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Nested;

[TestClass]
public sealed class BeforeRuleTests {
    private static readonly DateTime _cutoff = new(year: 2025, month: 1, day: 1, hour: 1, minute: 0, second: 0, kind: DateTimeKind.Local);

    [TestMethod]
    public void DateBeforeCutoff_Passes() {
        var rule = NonNullableBeforeRule();
        var result = RuleRunner.Run(rule, new Subscription { StartDate = _cutoff.AddDays(-1) });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void DateExactlyAtCutoff_Fails() {
        var rule = NonNullableBeforeRule();
        var result = RuleRunner.Run(rule, new Subscription { StartDate = _cutoff });
        ValidationAssert.HasError(result, nameof(Subscription.StartDate));
    }

    [TestMethod]
    public void DateAfterCutoff_Fails() {
        var rule = NonNullableBeforeRule();
        var result = RuleRunner.Run(rule, new Subscription { StartDate = _cutoff.AddDays(1) });
        ValidationAssert.HasError(result, nameof(Subscription.StartDate));
    }

    private static BeforeRule<Subscription, DateTime?> NonNullableBeforeRule() =>
        new(ValidatorFactory.Accessor<Subscription, DateTime?>(
                x => x.StartDate, nameof(Subscription.StartDate)),
            _cutoff, null);
}
