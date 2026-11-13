//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class RangeBoundModel {
    public int Age { get; set; }
    public int? OptionalAge { get; set; }
    public double Price { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
}

file sealed class ReversedInclusiveRangeValidator : Validator<RangeBoundModel> {
    protected override void Configure(ValidatorRuleBuilder<RangeBoundModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Age, nameof(RangeBoundModel.Age))
               .InclusiveBetween(10, 1);
    }
}

file sealed class EqualExclusiveRangeValidator : Validator<RangeBoundModel> {
    protected override void Configure(ValidatorRuleBuilder<RangeBoundModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Age, nameof(RangeBoundModel.Age))
               .ExclusiveBetween(5, 5);
    }
}

file sealed class ReversedRangeValidator : Validator<RangeBoundModel> {
    protected override void Configure(ValidatorRuleBuilder<RangeBoundModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Age, nameof(RangeBoundModel.Age))
               .Range(10, 1);
    }
}

file sealed class ValidRangeValidator : Validator<RangeBoundModel> {
    protected override void Configure(ValidatorRuleBuilder<RangeBoundModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Age, nameof(RangeBoundModel.Age))
               .InclusiveBetween(18, 99);

        builder.RuleFor(static model => model.Price, nameof(RangeBoundModel.Price))
               .ExclusiveBetween(0.5, 9.5);

        builder.RuleFor(static model => model.Amount, nameof(RangeBoundModel.Amount))
               .Range(1m, 10m);

        builder.RuleFor(static model => model.Date, nameof(RangeBoundModel.Date))
               .Range(new DateOnly(2020, 1, 1), new DateOnly(2030, 12, 31));

        builder.RuleFor(static model => model.OptionalAge, nameof(RangeBoundModel.OptionalAge))
               .InclusiveBetween(18, 99);
    }
}

[TestClass]
public sealed class RangeBoundValidationTests {
    [TestMethod]
    public void InclusiveBetween_ReversedBounds_Throws() {
        var act = static () => _ = new ReversedInclusiveRangeValidator();

        MSTestAssertions.That(() => act.Should()
            .ThrowExactly<ArgumentException>()
            .Which.Message.Should().Contain("10").And.Contain("1"));
    }

    [TestMethod]
    public void ExclusiveBetween_EqualBounds_Throws() {
        var act = static () => _ = new EqualExclusiveRangeValidator();

        MSTestAssertions.That(() => act.Should()
            .ThrowExactly<ArgumentException>()
            .Which.Message.Should().Contain("exclusive"));
    }

    [TestMethod]
    public void Range_ReversedBounds_Throws() {
        var act = static () => _ = new ReversedRangeValidator();

        MSTestAssertions.That(() => act.Should().ThrowExactly<ArgumentException>());
    }

    [TestMethod]
    public void ValidRanges_ForCommonTypes_Pass() {
        var validator = new ValidRangeValidator();
        var model = new RangeBoundModel {
            Age = 30,
            Price = 4.5,
            Amount = 4m,
            Date = new DateOnly(2025, 6, 1),
            OptionalAge = 30
        };

        MSTestAssertions.That(() => validator.Validate(model).IsValid.Should().BeTrue());
    }

    [TestMethod]
    public void NullableRange_NullValuePasses() {
        var validator = new ValidRangeValidator();
        var model = new RangeBoundModel {
            Age = 30,
            Price = 4.5,
            Amount = 4m,
            Date = new DateOnly(2025, 6, 1),
            OptionalAge = null
        };

        MSTestAssertions.That(() => validator.Validate(model).IsValid.Should().BeTrue());
    }

    [TestMethod]
    public void ExclusiveBetween_StillRejectsBoundaries() {
        var validator = new ValidRangeValidator();
        var model = new RangeBoundModel {
            Age = 30,
            Price = 9.5,
            Amount = 4m,
            Date = new DateOnly(2025, 6, 1),
            OptionalAge = 30
        };

        MSTestAssertions.That(() => validator.Validate(model).IsValid.Should().BeFalse());
    }
}
