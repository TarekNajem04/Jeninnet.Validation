//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class MultipleOfModel {
    public int Quantity { get; set; }

    public long Total { get; set; }

    public decimal Price { get; set; }

    public double Ratio { get; set; }

    public string? AmountText { get; set; }

    public int? Optional { get; set; }
}

file sealed class MultipleOfValidator : Validator<MultipleOfModel> {
    protected override void Configure(ValidatorRuleBuilder<MultipleOfModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Quantity, nameof(MultipleOfModel.Quantity))
               .MultipleOf(5);

        builder.RuleFor(static model => model.Total, nameof(MultipleOfModel.Total))
               .MultipleOf(10L);

        builder.RuleFor(static model => model.Price, nameof(MultipleOfModel.Price))
               .MultipleOf(0.05m);

        builder.RuleFor(static model => model.Ratio, nameof(MultipleOfModel.Ratio))
               .MultipleOf(0.1);

        builder.RuleFor(static model => model.AmountText, nameof(MultipleOfModel.AmountText))
               .MultipleOf("2.5");

        builder.RuleFor(static model => model.Optional, nameof(MultipleOfModel.Optional))
               .MultipleOf(7);
    }
}

[TestClass]
public sealed class MultipleOfRuleTests {
    [TestMethod]
    public void MultipleOf_ValidValues_Pass() {
        var validator = new MultipleOfValidator();
        var model = new MultipleOfModel {
            Quantity = 10,
            Total = 100,
            Price = 0.30m,
            Ratio = 0.3,
            AmountText = "7.5",
            Optional = null
        };

        MSTestAssertions.That(() => validator.Validate(model).IsValid.Should().BeTrue());
    }

    [TestMethod]
    public void MultipleOf_InvalidValues_FailWithMemberAndKey() {
        var validator = new MultipleOfValidator();
        var model = new MultipleOfModel {
            Quantity = 11,
            Total = 105,
            Price = 0.31m,
            Ratio = 0.35,
            AmountText = "7.3",
            Optional = 8
        };

        var result = validator.Validate(model);

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(6));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be(nameof(MultipleOfModel.Quantity)));
        MSTestAssertions.That(() => result.Errors[0].MessageKey.Should().Be("MultipleOf"));
    }

    [TestMethod]
    public async Task MultipleOf_InvalidValue_FailsOnAsyncPathAsync() {
        var validator = new MultipleOfValidator();
        var model = new MultipleOfModel { Quantity = 11 };

        var result = await validator.ValidateAsync(model, CancellationToken.None);

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
    }

    [TestMethod]
    public void MultipleOf_DecimalUsesExactDecimalArithmetic() {
        var validator = new MultipleOfValidator();
        var valid = new MultipleOfModel { Price = 0.30m, Quantity = 10, Total = 100 };
        var invalid = new MultipleOfModel { Price = 0.31m, Quantity = 10, Total = 100 };

        MSTestAssertions.That(() => validator.Validate(valid).IsValid.Should().BeTrue());
        MSTestAssertions.That(() => validator.Validate(invalid).IsValid.Should().BeFalse());
    }

    [TestMethod]
    public void MultipleOf_StringValueMustBeNumeric() {
        var validator = new MultipleOfValidator();
        var model = new MultipleOfModel { Quantity = 10, Total = 100, AmountText = "not-a-number" };

        MSTestAssertions.That(() => validator.Validate(model).IsValid.Should().BeFalse());
    }

    [TestMethod]
    public void MultipleOf_ZeroOrNegativeDivisor_Throws() {
        var zeroInt = static () => _ = new ZeroDivisorValidator();
        var negativeDecimal = static () => _ = new NegativeDivisorValidator();
        var zeroDouble = static () => _ = new ZeroDoubleDivisorValidator();
        var badString = static () => _ = new InvalidStringDivisorValidator();

        MSTestAssertions.That(() => zeroInt.Should().Throw<ArgumentException>());
        MSTestAssertions.That(() => negativeDecimal.Should().Throw<ArgumentException>());
        MSTestAssertions.That(() => zeroDouble.Should().Throw<ArgumentException>());
        MSTestAssertions.That(() => badString.Should().Throw<ArgumentException>());
    }
}

file sealed class ZeroDivisorValidator : Validator<MultipleOfModel> {
    protected override void Configure(ValidatorRuleBuilder<MultipleOfModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static model => model.Quantity, nameof(MultipleOfModel.Quantity)).MultipleOf(0);
    }
}

file sealed class NegativeDivisorValidator : Validator<MultipleOfModel> {
    protected override void Configure(ValidatorRuleBuilder<MultipleOfModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static model => model.Price, nameof(MultipleOfModel.Price)).MultipleOf(-0.5m);
    }
}

file sealed class ZeroDoubleDivisorValidator : Validator<MultipleOfModel> {
    protected override void Configure(ValidatorRuleBuilder<MultipleOfModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static model => model.Ratio, nameof(MultipleOfModel.Ratio)).MultipleOf(0.0);
    }
}

file sealed class InvalidStringDivisorValidator : Validator<MultipleOfModel> {
    protected override void Configure(ValidatorRuleBuilder<MultipleOfModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static model => model.AmountText, nameof(MultipleOfModel.AmountText)).MultipleOf("nope");
    }
}
