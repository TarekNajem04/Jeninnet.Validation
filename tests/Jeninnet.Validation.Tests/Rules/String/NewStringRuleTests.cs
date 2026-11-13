//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.String;

[TestClass]
public sealed class StringRuleTests {
    private sealed class Model { public string? Value { get; set; } }

    private static ValidationResult Run(string? value, Action<RuleBuilder<Model, string?>> configure) {
        var syncRules = new List<ISyncValidationRule<Model>>();
        var asyncRules = new List<IAsyncValidationRule<Model>>();
        var builder = new RuleBuilder<Model, string?>(
            new PropertyAccessor<Model, string?>(m => m.Value, nameof(Model.Value)),
            new ValidatorRuleBuilder<Model>(syncRules, asyncRules)
        );
        configure(builder);
        var validator = new CompositeValidator<Model>(syncRules, asyncRules);
        return validator.Validate(new Model { Value = value });
    }

    [TestMethod]
    [DataRow("start", "sta", true)]
    [DataRow("start", "end", false)]
    [DataRow(null, "sta", true)]
    public void StartsWith_Tests(string? value, string prefix, bool isValid) {
        var result = Run(value, b => b.StartsWith(prefix));
        MSTestAssertions.That(() => (result.IsValid == isValid).Should().BeTrue());
    }

    [TestMethod]
    [DataRow("end", "end", true)]
    [DataRow("end", "sta", false)]
    [DataRow(null, "end", true)]
    public void EndsWith_Tests(string? value, string suffix, bool isValid) {
        var result = Run(value, b => b.EndsWith(suffix));
        MSTestAssertions.That(() => (result.IsValid == isValid).Should().BeTrue());
    }

    [TestMethod]
    [DataRow("contains", "tain", true)]
    [DataRow("contains", "xyz", false)]
    [DataRow(null, "tain", true)]
    public void Contains_Tests(string? value, string substring, bool isValid) {
        var result = Run(value, b => b.Contains(substring));
        MSTestAssertions.That(() => (result.IsValid == isValid).Should().BeTrue());
    }
}
