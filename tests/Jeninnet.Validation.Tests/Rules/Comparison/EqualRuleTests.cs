//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Comparison;

// ---------------------------------------------------------------------------
// EqualRule / NotEqualRule
// ---------------------------------------------------------------------------

[TestClass]
public sealed class EqualRuleTests {
    [TestMethod]
    public void WhenValuesMatch_Passes() {
        var rule = StatusEqual("OPEN");
        var result = RuleRunner.Run(rule, new Order { Status = "OPEN" });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void WhenValuesDiffer_Fails() {
        var rule = StatusEqual("OPEN");
        var result = RuleRunner.Run(rule, new Order { Status = "CLOSED" });
        ValidationAssert.HasError(result, nameof(Order.Status));
    }

    [TestMethod]
    public void WhenValueIsNull_Skipped() {
        var rule = StatusEqual("OPEN");
        var result = RuleRunner.Run(rule, new Order { Status = null });
        ValidationAssert.IsValid(result, "EqualRule must skip null values.");
    }

    private static EqualRule<Order, string?> StatusEqual(string expected) =>
        new(ValidatorFactory.Accessor<Order, string?>(x => x.Status, nameof(Order.Status)),
            expected, null);
}
