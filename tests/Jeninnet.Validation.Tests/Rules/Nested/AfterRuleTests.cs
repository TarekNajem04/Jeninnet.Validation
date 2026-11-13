//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Nested;

[TestClass]
public sealed class AfterRuleTests {
    private static readonly DateTime _cutoff = new(year: 2025, month: 1, day: 1, hour: 1, minute: 0, second: 0, kind: DateTimeKind.Local);

    [TestMethod]
    public void DateAfterCutoff_Passes() {
        var rule = NonNullableAfterRule();
        var result = RuleRunner.Run(rule, new Subscription { StartDate = _cutoff.AddDays(1) });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void DateExactlyAtCutoff_Fails() {
        var rule = NonNullableAfterRule();
        var result = RuleRunner.Run(rule, new Subscription { StartDate = _cutoff });
        ValidationAssert.HasError(result, nameof(Subscription.StartDate));
    }

    [TestMethod]
    public void DateBeforeCutoff_Fails() {
        var rule = NonNullableAfterRule();
        var result = RuleRunner.Run(rule, new Subscription { StartDate = _cutoff.AddDays(-1) });
        ValidationAssert.HasError(result, nameof(Subscription.StartDate));
    }

    [TestMethod]
    public void NullableDate_WhenNull_Skipped() {
        var rule = new AfterRule<Subscription, DateTime?>(
            ValidatorFactory.Accessor<Subscription, DateTime?>(
                x => x.EndDate, nameof(Subscription.EndDate)),
            _cutoff, null);

        var result = RuleRunner.Run(rule, new Subscription { EndDate = null });
        ValidationAssert.IsValid(result, "AfterRule must skip null DateTime?.");
    }

    private static AfterRule<Subscription, DateTime?> NonNullableAfterRule() =>
        new(ValidatorFactory.Accessor<Subscription, DateTime?>(
                x => x.StartDate, nameof(Subscription.StartDate)),
            _cutoff, null);
}
