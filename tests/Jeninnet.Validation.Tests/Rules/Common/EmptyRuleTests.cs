//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Common;

[TestClass]
public sealed class EmptyRuleTests {
    [TestMethod]
    public void NullValue_Passes() {
        var rule = new EmptyRule<Person, string?>(ValidatorFactory.Accessor<Person, string?>(p => p.Name, "Name"), message: null);
        var result = RuleRunner.Run(rule, new Person { Name = null });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void EmptyString_Passes() {
        var rule = new EmptyRule<Person, string?>(ValidatorFactory.Accessor<Person, string?>(p => p.Name, "Name"), message: null);
        var result = RuleRunner.Run(rule, new Person { Name = "" });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void WhitespaceString_Passes() {
        var rule = new EmptyRule<Person, string?>(ValidatorFactory.Accessor<Person, string?>(p => p.Name, "Name"), message: null);
        var result = RuleRunner.Run(rule, new Person { Name = "   " });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void NonEmptyString_Fails() {
        var rule = new EmptyRule<Person, string?>(ValidatorFactory.Accessor<Person, string?>(p => p.Name, "Name"), message: null);
        var result = RuleRunner.Run(rule, new Person { Name = "Hello" });
        ValidationAssert.HasError(result, "Name");
    }

    [TestMethod]
    public void EmptyGuid_Passes() {
        var rule = new EmptyRule<Person, Guid>(ValidatorFactory.Accessor<Person, Guid>(p => p.Id, "Id"), message: null);
        var result = RuleRunner.Run(rule, new Person { Id = Guid.Empty });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void NonEmptyGuid_Fails() {
        var rule = new EmptyRule<Person, Guid>(ValidatorFactory.Accessor<Person, Guid>(p => p.Id, "Id"), message: null);
        var result = RuleRunner.Run(rule, new Person { Id = Guid.NewGuid() });
        ValidationAssert.HasError(result, "Id");
    }

    [TestMethod]
    public void EmptyCollection_Passes() {
        var rule = new EmptyRule<Person, List<int>?>(ValidatorFactory.Accessor<Person, List<int>?>(p => p.Tags, "Tags"), message: null);
        var result = RuleRunner.Run(rule, new Person { Tags = [] });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void NonEmptyCollection_Fails() {
        var rule = new EmptyRule<Person, List<int>?>(ValidatorFactory.Accessor<Person, List<int>?>(p => p.Tags, "Tags"), message: null);
        var result = RuleRunner.Run(rule, new Person { Tags = [1, 2, 3] });
        ValidationAssert.HasError(result, "Tags");
    }

    [TestMethod]
    public void NullCollection_Passes() {
        var rule = new EmptyRule<Person, List<int>?>(ValidatorFactory.Accessor<Person, List<int>?>(p => p.Tags, "Tags"), message: null);
        var result = RuleRunner.Run(rule, new Person { Tags = null });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void IntValue_Fails() {
        var rule = new EmptyRule<Person, int>(ValidatorFactory.Accessor<Person, int>(p => p.Age, "Age"), message: null);
        var result = RuleRunner.Run(rule, new Person { Age = 25 });
        ValidationAssert.HasError(result, "Age");
    }

    [TestMethod]
    public void ZeroInt_Fails() {
        var rule = new EmptyRule<Person, int>(ValidatorFactory.Accessor<Person, int>(p => p.Age, "Age"), message: null);
        var result = RuleRunner.Run(rule, new Person { Age = 0 });
        ValidationAssert.HasError(result, "Age");
    }

    private sealed class Person {
        public string? Name { get; init; }
        public Guid Id { get; init; }
        public List<int>? Tags { get; init; }
        public int Age { get; init; }
    }
}
