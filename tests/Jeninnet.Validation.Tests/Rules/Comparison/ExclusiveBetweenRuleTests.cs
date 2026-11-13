//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Comparison;

// ---------------------------------------------------------------------------
// ExclusiveBetweenRule � Phase 0 logic-inversion bug fix
// ---------------------------------------------------------------------------

/// <summary>
/// Exhaustively tests <see cref="ExclusiveBetweenRule{T,TProperty}"/> to verify the
/// Phase 0 bug fix.  Prior to the fix every value was accepted; these tests confirm that
/// values outside the range and at the boundaries are now correctly rejected.
/// </summary>

[TestClass]
public sealed class ExclusiveBetweenRuleTests {
    private static ValidationResult Run(int value, int min, int max) {
        var rule = new ExclusiveBetweenRule<Order, int?>(
            ValidatorFactory.Accessor<Order, int?>(x => x.Quantity, nameof(Order.Quantity))
                            ,
            min, max, null);
        return RuleRunner.Run(rule, new Order { Quantity = value });
    }

    private static ValidationResult RunDecimal(decimal? value, decimal min, decimal max) {
        var rule = new ExclusiveBetweenRule<Order, decimal?>(
            ValidatorFactory.Accessor<Order, decimal?>(
                    x => x.DiscountPercentage, nameof(Order.DiscountPercentage))
                ,
            min, max, null);
        return RuleRunner.Run(rule, new Order { DiscountPercentage = value });
    }

    #region Interior values (accepted)
    [TestMethod] public void InsideRange_Passes() => Run(15, 10, 20).IsValid.Should().BeTrue();
    [TestMethod] public void JustAboveMin_Passes() => Run(11, 10, 20).IsValid.Should().BeTrue();
    [TestMethod] public void JustBelowMax_Passes() => Run(19, 10, 20).IsValid.Should().BeTrue();
    #endregion Interior values (accepted)

    #region Boundary values (rejected, exclusive endpoints)
    [TestMethod]
    public void AtMinBoundary_Fails() => ValidationAssert.HasError(Run(10, 10, 20), nameof(Order.Quantity),
            "min endpoint must be excluded in ExclusiveBetween");

    [TestMethod]
    public void AtMaxBoundary_Fails() => ValidationAssert.HasError(Run(20, 10, 20), nameof(Order.Quantity),
            "max endpoint must be excluded in ExclusiveBetween");
    #endregion Boundary values (rejected, exclusive endpoints)

    #region Outside range (core regression for the Phase 0 bug)
    [TestMethod]
    public void FarBelowMin_Fails() => ValidationAssert.HasError(Run(5, 10, 20), nameof(Order.Quantity),
            "values below min must be rejected � this caught the OR/AND bug");

    [TestMethod]
    public void FarAboveMax_Fails() => ValidationAssert.HasError(Run(25, 10, 20), nameof(Order.Quantity),
            "values above max must be rejected � this caught the OR/AND bug");
    #endregion Outside range (core regression for the Phase 0 bug)

    #region Null handling
    [TestMethod]
    public void NullValue_Skipped() {
        var result = RunDecimal(null, 0m, 100m);
        ValidationAssert.IsValid(result, "ExclusiveBetween must skip null.");
    }
    #endregion Null handling

    #region Decimal interior
    [TestMethod]
    public void Decimal_JustAboveMin_Passes() =>
        MSTestAssertions.That(() => RunDecimal(0.01m, 0m, 100m).IsValid.Should().BeTrue());

    [TestMethod]
    public void Decimal_JustBelowMax_Passes() =>
        MSTestAssertions.That(() => RunDecimal(99.99m, 0m, 100m).IsValid.Should().BeTrue());

    [TestMethod]
    public void Decimal_AtZeroMin_Fails() =>
        ValidationAssert.HasError(RunDecimal(0m, 0m, 100m), nameof(Order.DiscountPercentage));

    [TestMethod]
    public void Decimal_At100Max_Fails() =>
        ValidationAssert.HasError(RunDecimal(100m, 0m, 100m), nameof(Order.DiscountPercentage));
    #endregion Decimal interior
}
