//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Common;

[TestClass]
public sealed class InRuleTests {
    [TestMethod]
    public void ValueInSet_Passes() {
        var rule = new InRule<Person, string?>(
            ValidatorFactory.Accessor<Person, string?>(x => x.Name, nameof(Person.Name)),
            ["Alice", "Bob"], null);

        var result = RuleRunner.Run(rule, new Person { Name = "Alice" });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void ValueNotInSet_Fails() {
        var rule = new InRule<Person, string?>(
            ValidatorFactory.Accessor<Person, string?>(x => x.Name, nameof(Person.Name)),
            ["Alice", "Bob"], null);

        var result = RuleRunner.Run(rule, new Person { Name = "Charlie" });
        ValidationAssert.HasError(result, nameof(Person.Name));
    }

    [TestMethod]
    public void NullValue_Skips() {
        var rule = new InRule<Person, string?>(
            ValidatorFactory.Accessor<Person, string?>(x => x.Name, nameof(Person.Name)),
            ["Alice"], null);

        var result = RuleRunner.Run(rule, new Person { Name = null });
        ValidationAssert.IsValid(result);
    }
}

[TestClass]
public sealed class NotInRuleTests {
    [TestMethod]
    public void ValueNotInSet_Passes() {
        var rule = new NotInRule<Person, string?>(
            ValidatorFactory.Accessor<Person, string?>(x => x.Name, nameof(Person.Name)),
            ["Blocked"], null);

        var result = RuleRunner.Run(rule, new Person { Name = "Alice" });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void ValueInSet_Fails() {
        var rule = new NotInRule<Person, string?>(
            ValidatorFactory.Accessor<Person, string?>(x => x.Name, nameof(Person.Name)),
            ["Blocked"], null);

        var result = RuleRunner.Run(rule, new Person { Name = "Blocked" });
        ValidationAssert.HasError(result, nameof(Person.Name));
    }
}
