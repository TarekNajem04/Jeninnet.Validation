//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Common;

[TestClass]
public sealed class NotNullRuleTests {
    [TestMethod]
    public void ReferenceType_WhenNotNull_Passes() =>
        ValidationAssert.IsValid(Run("Value"));

    [TestMethod]
    public void ReferenceType_WhenNull_Fails() =>
        ValidationAssert.HasError(Run(null), nameof(Person.Name));

    [TestMethod]
    public void NullableValueType_WhenNotNull_Passes() {
        var rule = new NotNullRule<Person, int?>(
            ValidatorFactory.Accessor<Person, int?>(x => x.Age, nameof(Person.Age)),
            null);
        var result = RuleRunner.Run(rule, new Person { Age = 25 });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void NullableValueType_WhenNull_Fails() {
        var rule = new NotNullRule<Person, int?>(
            ValidatorFactory.Accessor<Person, int?>(x => x.Age, nameof(Person.Age)),
            null);
        var result = RuleRunner.Run(rule, new Person { Age = null });
        ValidationAssert.HasError(result, nameof(Person.Age));
    }

    private static ValidationResult Run(string? value) {
        var rule = new NotNullRule<Person, string?>(
            ValidatorFactory.Accessor<Person, string?>(x => x.Name, nameof(Person.Name)),
            null);
        return RuleRunner.Run(rule, new Person { Name = value });
    }
}
