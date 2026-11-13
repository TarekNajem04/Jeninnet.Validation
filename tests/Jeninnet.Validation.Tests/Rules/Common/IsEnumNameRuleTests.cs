//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Common;

[TestClass]
public sealed class IsEnumNameRuleTests {
    private enum Status {
        Pending = 0,
        Active = 1,
        Inactive = 2
    }

    [TestMethod]
    public void ValidEnumName_Passes() {
        var rule = new IsEnumNameRule<Model, string?>(ValidatorFactory.Accessor<Model, string?>(m => m.Status, "Status"), typeof(Status), message: null);
        var result = RuleRunner.Run(rule, new Model { Status = "Active" });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void InvalidEnumName_Fails() {
        var rule = new IsEnumNameRule<Model, string?>(ValidatorFactory.Accessor<Model, string?>(m => m.Status, "Status"), typeof(Status), message: null);
        var result = RuleRunner.Run(rule, new Model { Status = "Unknown" });
        ValidationAssert.HasError(result, "Status");
    }

    [TestMethod]
    public void NullValue_Skipped() {
        var rule = new IsEnumNameRule<Model, string?>(ValidatorFactory.Accessor<Model, string?>(m => m.Status, "Status"), typeof(Status), message: null);
        var result = RuleRunner.Run(rule, new Model { Status = null });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void CaseInsensitive_ByDefault_Passes() {
        var rule = new IsEnumNameRule<Model, string?>(ValidatorFactory.Accessor<Model, string?>(m => m.Status, "Status"), typeof(Status), message: null);
        var result = RuleRunner.Run(rule, new Model { Status = "active" });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void CaseSensitive_FailsOnWrongCase() {
        var rule = new IsEnumNameRule<Model, string?>(ValidatorFactory.Accessor<Model, string?>(m => m.Status, "Status"), typeof(Status), caseSensitive: true, message: null);
        var result = RuleRunner.Run(rule, new Model { Status = "active" });
        ValidationAssert.HasError(result, "Status");
    }

    [TestMethod]
    public void CaseSensitive_PassesOnExactCase() {
        var rule = new IsEnumNameRule<Model, string?>(ValidatorFactory.Accessor<Model, string?>(m => m.Status, "Status"), typeof(Status), caseSensitive: true, message: null);
        var result = RuleRunner.Run(rule, new Model { Status = "Active" });
        ValidationAssert.IsValid(result);
    }

    private sealed class Model {
        public string? Status { get; init; }
    }
}
