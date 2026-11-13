//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Comparison;

// ---------------------------------------------------------------------------
// GreaterThanOrEqualRule
// ---------------------------------------------------------------------------

[TestClass]
public sealed class GreaterThanOrEqualRuleTests {
    [TestMethod]
    public void WhenValueExceedsThreshold_Passes() =>
        MSTestAssertions.That(() => Run(10, 5).IsValid.Should().BeTrue());

    [TestMethod]
    public void WhenValueEqualsThreshold_Passes() =>
        MSTestAssertions.That(() => Run(5, 5).IsValid.Should().BeTrue());

    [TestMethod]
    public void WhenValueBelowThreshold_Fails() =>
        ValidationAssert.HasError(Run(3, 5), nameof(Order.Quantity));

    private static ValidationResult Run(int value, int threshold) {
        var rule = new GreaterThanOrEqualRule<Order, int?>(
            ValidatorFactory.Accessor<Order, int?>(x => x.Quantity, nameof(Order.Quantity))
                            ,
            threshold,
            null);
        return RuleRunner.Run(rule, new Order { Quantity = value });
    }
}
