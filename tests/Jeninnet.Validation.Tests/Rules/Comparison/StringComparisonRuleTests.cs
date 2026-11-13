//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Comparison;

[TestClass]
public sealed class StringComparisonRuleTests {
    [TestMethod]
    public void GreaterThan_StringValues_UseOrdinalComparison() {
        var originalCulture = CultureInfo.CurrentCulture;

        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            var rule = new GreaterThanRule<Order, string?>(
                ValidatorFactory.Accessor<Order, string?>(static x => x.Code, nameof(Order.Code)),
                "B",
                message: null);

            var result = RuleRunner.Run(rule, new Order { Code = "a" });

            ValidationAssert.IsValid(result, "ordinal comparison treats lowercase 'a' as greater than uppercase 'B'");
        }
        finally {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void InclusiveBetween_StringValues_UseOrdinalComparison() {
        var originalCulture = CultureInfo.CurrentCulture;

        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            var rule = new InclusiveBetweenRule<Order, string?>(
                ValidatorFactory.Accessor<Order, string?>(static x => x.Code, nameof(Order.Code)),
                "B",
                "b",
                message: null);

            var result = RuleRunner.Run(rule, new Order { Code = "a" });

            ValidationAssert.IsValid(result, "ordinal comparison keeps 'a' between 'B' and 'b'");
        }
        finally {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
