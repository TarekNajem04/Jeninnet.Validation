//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Comparison;

[TestClass]
public sealed class NotEqualRuleTests {
    [TestMethod]
    public void WhenValuesDiffer_Passes() {
        var rule = StatusNotEqual("DELETED");
        var result = RuleRunner.Run(rule, new Order { Status = "OPEN" });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void WhenValuesMatch_Fails() {
        var rule = StatusNotEqual("DELETED");
        var result = RuleRunner.Run(rule, new Order { Status = "DELETED" });
        ValidationAssert.HasError(result, nameof(Order.Status));
    }

    [TestMethod]
    public void WhenValueIsNull_Skipped() {
        var rule = StatusNotEqual("DELETED");
        var result = RuleRunner.Run(rule, new Order { Status = null });
        ValidationAssert.IsValid(result, "NotEqualRule must skip null values.");
    }

    private static NotEqualRule<Order, string?> StatusNotEqual(string disallowed) =>
        new(ValidatorFactory.Accessor<Order, string?>(x => x.Status, nameof(Order.Status)),
            disallowed, null);
}
