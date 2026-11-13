//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Comparison;

// ---------------------------------------------------------------------------
// LessThanOrEqualRule
// ---------------------------------------------------------------------------

[TestClass]
public sealed class LessThanOrEqualRuleTests {
    [TestMethod]
    public void WhenValueBelowThreshold_Passes() =>
        MSTestAssertions.That(() => Run(3, 5).IsValid.Should().BeTrue());

    [TestMethod]
    public void WhenValueEqualsThreshold_Passes() =>
        MSTestAssertions.That(() => Run(5, 5).IsValid.Should().BeTrue());

    [TestMethod]
    public void WhenValueExceedsThreshold_Fails() =>
        ValidationAssert.HasError(Run(10, 5), nameof(Order.Quantity));

    private static ValidationResult Run(int value, int threshold) {
        var rule = new LessThanOrEqualRule<Order, int?>(
            ValidatorFactory.Accessor<Order, int?>(x => x.Quantity, nameof(Order.Quantity))
                            ,
            threshold,
            null);
        return RuleRunner.Run(rule, new Order { Quantity = value });
    }
}
