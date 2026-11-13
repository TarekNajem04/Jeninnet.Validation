//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Comparison;

[TestClass]
public sealed class DecimalRuleTests {
    [TestMethod]
    [DataRow("123.45", 5, 2)]
    [DataRow("0.1", 1, 1)]
    [DataRow("100", 3, 0)]
    public void ScalePrecision_Valid_Passes(string valueText, int precision, int scale) {
        var value = decimal.Parse(valueText, CultureInfo.InvariantCulture);
        var result = Run(value, precision, scale);
        ValidationAssert.IsValid(result, $"Value '{value}' should pass ScalePrecision({precision}, {scale}).");
    }

    [TestMethod]
    [DataRow("123.456", 5, 2)] // scale 3 > 2
    [DataRow("1234.5", 3, 1)]  // precision 5 > 3
    [DataRow("10.123", 2, 1)]  // precision 5 > 2
    public void ScalePrecision_Invalid_Fails(string valueText, int precision, int scale) {
        var value = decimal.Parse(valueText, CultureInfo.InvariantCulture);
        var result = Run(value, precision, scale);
        ValidationAssert.HasError(result, nameof(Order.TotalAmount));
    }

    private static ValidationResult Run(decimal value, int precision, int scale) {
        var rule = new ScalePrecisionRule<Order, decimal>(
            ValidatorFactory.Accessor<Order, decimal>(x => x.TotalAmount, nameof(Order.TotalAmount)),
            precision, scale, null);
        return RuleRunner.Run(rule, new Order { TotalAmount = value });
    }
}
