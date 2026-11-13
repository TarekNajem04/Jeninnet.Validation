//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Comparison;

// ---------------------------------------------------------------------------
// GreaterThanRule
// ---------------------------------------------------------------------------

[TestClass]
public sealed class GreaterThanRuleTests {
    [TestMethod]
    public void WhenValueExceedsThreshold_Passes() =>
        AssertValid(10, 5);

    [TestMethod]
    public void WhenValueEqualsThreshold_Fails() =>
        AssertFails(5, 5);

    [TestMethod]
    public void WhenValueBelowThreshold_Fails() =>
        AssertFails(3, 5);

    [TestMethod]
    public void WhenValueIsNull_Skipped() {
        var rule = NullableRule(0);
        var result = RuleRunner.Run(rule, new Order { OptionalQuantity = null });

        ValidationAssert.IsValid(result, "null must be skipped by GreaterThanRule");
    }

    [TestMethod]
    public void NegativeThreshold_ValueZero_Passes() =>
        AssertValid(0, -1);

    private static void AssertValid(int value, int threshold) =>
        ValidationAssert.IsValid(Run(value, threshold));

    private static void AssertFails(int value, int threshold) =>
        ValidationAssert.HasError(Run(value, threshold), nameof(Order.Quantity));

    private static ValidationResult Run(int value, int threshold) {
        var rule = new GreaterThanRule<Order, int?>(
            ValidatorFactory.Accessor<Order, int?>(x => x.Quantity, nameof(Order.Quantity)),
            threshold,
            null);
        return RuleRunner.Run(rule, new Order { Quantity = value });
    }

    private static GreaterThanRule<Order, int?> NullableRule(int threshold) =>
        new(
            accessor: ValidatorFactory.Accessor<Order, int?>(getter: x => x.OptionalQuantity, memberName: nameof(Order.OptionalQuantity)),
            threshold: threshold,
            message: null
        );
}
