//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Common;

[TestClass]
public sealed class NotEmptyRuleTests {
    [TestMethod]
    public void String_WhenPopulated_Passes() =>
        ValidationAssert.IsValid(Run("Hello"));

    [TestMethod]
    public void String_WhenEmpty_Fails() =>
        ValidationAssert.HasError(Run(""), nameof(Person.Name));

    [TestMethod]
    public void String_WhenWhitespaceOnly_Fails() =>
        ValidationAssert.HasError(Run("   "), nameof(Person.Name));

    [TestMethod]
    public void String_WhenNull_Fails() =>
        ValidationAssert.HasError(Run(null), nameof(Person.Name));

    [TestMethod]
    public void Guid_WhenNotEmpty_Passes() {
        var rule = new NotEmptyRule<Person, Guid>(
            ValidatorFactory.Accessor<Person, Guid>(x => x.Id, nameof(Person.Id)),
            null);
        var result = RuleRunner.Run(rule, new Person { Id = Guid.NewGuid() });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void Guid_WhenEmpty_Fails() {
        var rule = new NotEmptyRule<Person, Guid>(
            ValidatorFactory.Accessor<Person, Guid>(x => x.Id, nameof(Person.Id)),
            null);
        var result = RuleRunner.Run(rule, new Person { Id = Guid.Empty });
        ValidationAssert.HasError(result, nameof(Person.Id));
    }

    private static ValidationResult Run(string? value) {
        var rule = new NotEmptyRule<Person, string?>(
            ValidatorFactory.Accessor<Person, string?>(x => x.Name, nameof(Person.Name)),
            null);
        return RuleRunner.Run(rule, new Person { Name = value });
    }
}
