//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules;

internal sealed class PropertyComparisonModel {
    public int Low { get; set; }
    public int High { get; set; }
    public string? Password { get; set; }
    public string? Confirm { get; set; }
}

[TestClass]
public sealed class PropertyComparisonRuleTests {
    private static readonly IPropertyAccessor<PropertyComparisonModel, int> _low =
        ValidatorFactory.Accessor<PropertyComparisonModel, int>(static x => x.Low, nameof(PropertyComparisonModel.Low));

    private static readonly IPropertyAccessor<PropertyComparisonModel, int> _high =
        ValidatorFactory.Accessor<PropertyComparisonModel, int>(static x => x.High, nameof(PropertyComparisonModel.High));

    private static readonly IPropertyAccessor<PropertyComparisonModel, string?> _password =
        ValidatorFactory.Accessor<PropertyComparisonModel, string?>(static x => x.Password, nameof(PropertyComparisonModel.Password));

    private static readonly IPropertyAccessor<PropertyComparisonModel, string?> _confirm =
        ValidatorFactory.Accessor<PropertyComparisonModel, string?>(static x => x.Confirm, nameof(PropertyComparisonModel.Confirm));

    [TestMethod]
    public void ComparisonRules_ValidateOrderingAndEquality() {
        MSTestAssertions.That(() => Run(new PropertyLessThanRule<PropertyComparisonModel, int>(_low, _high, null),
            new PropertyComparisonModel { Low = 1, High = 2 }).IsValid.Should().BeTrue());
        MSTestAssertions.That(() => Run(new PropertyLessThanRule<PropertyComparisonModel, int>(_low, _high, null),
            new PropertyComparisonModel { Low = 3, High = 2 }).Errors.Should().HaveCount(1));

        MSTestAssertions.That(() => Run(new PropertyLessThanOrEqualRule<PropertyComparisonModel, int>(_low, _high, null),
            new PropertyComparisonModel { Low = 2, High = 2 }).IsValid.Should().BeTrue());
        MSTestAssertions.That(() => Run(new PropertyLessThanOrEqualRule<PropertyComparisonModel, int>(_low, _high, null),
            new PropertyComparisonModel { Low = 3, High = 2 }).Errors.Should().HaveCount(1));

        MSTestAssertions.That(() => Run(new PropertyGreaterThanRule<PropertyComparisonModel, int>(_high, _low, null),
            new PropertyComparisonModel { Low = 1, High = 2 }).IsValid.Should().BeTrue());
        MSTestAssertions.That(() => Run(new PropertyGreaterThanRule<PropertyComparisonModel, int>(_high, _low, null),
            new PropertyComparisonModel { Low = 2, High = 2 }).Errors.Should().HaveCount(1));

        MSTestAssertions.That(() => Run(new PropertyGreaterThanOrEqualRule<PropertyComparisonModel, int>(_high, _low, null),
            new PropertyComparisonModel { Low = 2, High = 2 }).IsValid.Should().BeTrue());
        MSTestAssertions.That(() => Run(new PropertyGreaterThanOrEqualRule<PropertyComparisonModel, int>(_high, _low, null),
            new PropertyComparisonModel { Low = 3, High = 2 }).Errors.Should().HaveCount(1));

        MSTestAssertions.That(() => Run(new PropertyEqualRule<PropertyComparisonModel, string?>(_password, _confirm, null),
            new PropertyComparisonModel { Password = "x", Confirm = "x" }).IsValid.Should().BeTrue());
        MSTestAssertions.That(() => Run(new PropertyEqualRule<PropertyComparisonModel, string?>(_password, _confirm, null),
            new PropertyComparisonModel { Password = "x", Confirm = "y" }).Errors.Should().HaveCount(1));

        MSTestAssertions.That(() => Run(new PropertyNotEqualRule<PropertyComparisonModel, string?>(_password, _confirm, null),
            new PropertyComparisonModel { Password = "x", Confirm = "y" }).IsValid.Should().BeTrue());
        MSTestAssertions.That(() => Run(new PropertyNotEqualRule<PropertyComparisonModel, string?>(_password, _confirm, null),
            new PropertyComparisonModel { Password = "x", Confirm = "x" }).Errors.Should().HaveCount(1));
    }

    [TestMethod]
    public void ComparisonRules_HandleNullOperands() {
        MSTestAssertions.That(() => Run(new PropertyEqualRule<PropertyComparisonModel, string?>(_password, _confirm, null),
            new PropertyComparisonModel()).IsValid.Should().BeTrue());
        MSTestAssertions.That(() => Run(new PropertyLessThanRule<PropertyComparisonModel, string?>(_password, _confirm, null),
            new PropertyComparisonModel { Password = "a" }).Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => Run(new PropertyGreaterThanRule<PropertyComparisonModel, string?>(_password, _confirm, null),
            new PropertyComparisonModel { Confirm = "b" }).Errors.Should().HaveCount(1));
    }

    private static ValidationResult Run<T>(ISyncValidationRule<T> rule, T model) =>
        RuleRunner.Run(rule, model);
}
