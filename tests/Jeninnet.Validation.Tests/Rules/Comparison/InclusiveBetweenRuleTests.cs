//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Comparison;

// ---------------------------------------------------------------------------
// InclusiveBetweenRule � Phase 0 variable-name fix verification
// ---------------------------------------------------------------------------

/// <summary>
/// Exhaustively tests <see cref="InclusiveBetweenRule{T,TProperty}"/> across all boundary
/// and non-boundary cases to confirm the logic is correct (it was correct before Phase 0;
/// only variable names changed).
/// </summary>

[TestClass]
public sealed class InclusiveBetweenRuleTests {
    private static ValidationResult RunInt(int value, int min, int max) {
        var rule = new InclusiveBetweenRule<Order, int?>(
            ValidatorFactory.Accessor<Order, int?>(x => x.Quantity, nameof(Order.Quantity))
                            ,
            min, max, null);
        return RuleRunner.Run(rule, new Order { Quantity = value });
    }

    private static ValidationResult RunDecimal(decimal? value, decimal min, decimal max) {
        var rule = new InclusiveBetweenRule<Order, decimal?>(
            ValidatorFactory.Accessor<Order, decimal?>(
                    x => x.DiscountPercentage, nameof(Order.DiscountPercentage))
                ,
            min, max, null);
        return RuleRunner.Run(rule, new Order { DiscountPercentage = value });
    }

    #region Boundary values (accepted)
    [TestMethod] public void AtMinBoundary_Passes() => RunInt(10, 10, 20).IsValid.Should().BeTrue();
    [TestMethod] public void AtMaxBoundary_Passes() => RunInt(20, 10, 20).IsValid.Should().BeTrue();
    #endregion Boundary values (accepted)

    #region Interior values (accepted)
    [TestMethod] public void InsideRange_Passes() => RunInt(15, 10, 20).IsValid.Should().BeTrue();
    #endregion Interior values (accepted)

    #region Outside range (rejected)
    [TestMethod] public void BelowMin_Fails() => ValidationAssert.HasError(RunInt(9, 10, 20), nameof(Order.Quantity));
    [TestMethod] public void AboveMax_Fails() => ValidationAssert.HasError(RunInt(21, 10, 20), nameof(Order.Quantity));
    #endregion Outside range (rejected)

    #region Null handling
    [TestMethod]
    public void NullValue_Skipped() {
        var result = RunDecimal(null, 0m, 100m);
        ValidationAssert.IsValid(result, "InclusiveBetween must skip null.");
    }
    #endregion Null handling

    #region Decimal boundaries
    [TestMethod] public void Decimal_AtMinBoundary_Passes() => RunDecimal(0m, 0m, 100m).IsValid.Should().BeTrue();
    [TestMethod] public void Decimal_AtMaxBoundary_Passes() => RunDecimal(100m, 0m, 100m).IsValid.Should().BeTrue();
    [TestMethod] public void Decimal_JustBelow_Fails() => ValidationAssert.HasError(RunDecimal(-0.01m, 0m, 100m), nameof(Order.DiscountPercentage));
    [TestMethod] public void Decimal_JustAbove_Fails() => ValidationAssert.HasError(RunDecimal(100.01m, 0m, 100m), nameof(Order.DiscountPercentage));
    #endregion Decimal boundaries
}
