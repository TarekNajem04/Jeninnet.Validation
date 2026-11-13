//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Common;

[TestClass]
public sealed class NullRuleTests {
    [TestMethod]
    public void NullValue_Passes() {
        var rule = new NullRule<Person, string?>(ValidatorFactory.Accessor<Person, string?>(p => p.MiddleName, "MiddleName"), message: null);
        var result = RuleRunner.Run(rule, new Person { MiddleName = null });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void NonNullValue_Fails() {
        var rule = new NullRule<Person, string?>(ValidatorFactory.Accessor<Person, string?>(p => p.MiddleName, "MiddleName"), message: null);
        var result = RuleRunner.Run(rule, new Person { MiddleName = "John" });
        ValidationAssert.HasError(result, "MiddleName");
    }

    [TestMethod]
    public void EmptyString_Fails() {
        var rule = new NullRule<Person, string?>(ValidatorFactory.Accessor<Person, string?>(p => p.MiddleName, "MiddleName"), message: null);
        var result = RuleRunner.Run(rule, new Person { MiddleName = "" });
        ValidationAssert.HasError(result, "MiddleName");
    }

    [TestMethod]
    public void CustomMessage_AppearsInError() {
        var rule = new NullRule<Person, string?>(ValidatorFactory.Accessor<Person, string?>(p => p.MiddleName, "MiddleName"), "Must be null");
        var result = RuleRunner.Run(rule, new Person { MiddleName = "John" });
        ValidationAssert.HasErrorWithMessage(result, "MiddleName", "Must be null");
    }

    private sealed class Person {
        public string? MiddleName { get; init; }
    }
}
