//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

file sealed class DefaultSemanticsModel {
    public int Count { get; set; }
    public Guid Id { get; set; }
    public bool Flag { get; set; }
    public DateTime When { get; set; }
    public decimal Amount { get; set; }
}

file sealed class NotEmptyValueTypesValidator : Validator<DefaultSemanticsModel> {
    protected override void Configure(ValidatorRuleBuilder<DefaultSemanticsModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Count, nameof(DefaultSemanticsModel.Count)).NotEmpty();
        builder.RuleFor(static x => x.Id, nameof(DefaultSemanticsModel.Id)).NotEmpty();
        builder.RuleFor(static x => x.Flag, nameof(DefaultSemanticsModel.Flag)).NotEmpty();
        builder.RuleFor(static x => x.When, nameof(DefaultSemanticsModel.When)).NotEmpty();
        builder.RuleFor(static x => x.Amount, nameof(DefaultSemanticsModel.Amount)).NotEmpty();
    }
}

file sealed class NotDefaultValidator : Validator<DefaultSemanticsModel> {
    protected override void Configure(ValidatorRuleBuilder<DefaultSemanticsModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Count, nameof(DefaultSemanticsModel.Count)).NotDefault();
        builder.RuleFor(static x => x.Id, nameof(DefaultSemanticsModel.Id)).NotDefault();
        builder.RuleFor(static x => x.Flag, nameof(DefaultSemanticsModel.Flag)).NotDefault();
        builder.RuleFor(static x => x.When, nameof(DefaultSemanticsModel.When)).NotDefault();
        builder.RuleFor(static x => x.Amount, nameof(DefaultSemanticsModel.Amount)).NotDefault();
    }
}

file sealed class ComparerModel {
    public string? Name { get; set; }
    public string? Other { get; set; }
}

file sealed class ComparerValidator : Validator<ComparerModel> {
    protected override void Configure(ValidatorRuleBuilder<ComparerModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ComparerModel.Name))
               .Equal("Admin", StringComparer.OrdinalIgnoreCase);

        builder.RuleFor(static x => x.Other, nameof(ComparerModel.Other))
               .NotEqual("Admin", StringComparer.OrdinalIgnoreCase);
    }
}

file sealed class ScalePrecisionModel {
    public decimal Strict { get; set; }
    public decimal Lenient { get; set; }
}

file sealed class ScalePrecisionValidator : Validator<ScalePrecisionModel> {
    protected override void Configure(ValidatorRuleBuilder<ScalePrecisionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Strict, nameof(ScalePrecisionModel.Strict))
               .ScalePrecision(5, 2);

        builder.RuleFor(static x => x.Lenient, nameof(ScalePrecisionModel.Lenient))
               .ScalePrecision(5, 2, ignoreTrailingZeros: true);
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class RuleSemanticsParityTests {
    [TestMethod]
    public void NotEmpty_ValueTypes_DoNotTreatDefaultValuesAsEmpty() {
        var result = new NotEmptyValueTypesValidator().Validate(new DefaultSemanticsModel {
            Id = Guid.NewGuid()
        });

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void NotEmpty_GuidEmpty_IsStillEmpty() {
        var result = new NotEmptyValueTypesValidator().Validate(
            new DefaultSemanticsModel { Id = Guid.Empty });

        ValidationAssert.HasError(result, nameof(DefaultSemanticsModel.Id));
    }

    [TestMethod]
    public void NotDefault_ValueTypes_TreatDefaultsAsInvalid() {
        var result = new NotDefaultValidator().Validate(new DefaultSemanticsModel());

        ValidationAssert.HasError(result, nameof(DefaultSemanticsModel.Count));
        ValidationAssert.HasError(result, nameof(DefaultSemanticsModel.Id));
        ValidationAssert.HasError(result, nameof(DefaultSemanticsModel.Flag));
        ValidationAssert.HasError(result, nameof(DefaultSemanticsModel.When));
        ValidationAssert.HasError(result, nameof(DefaultSemanticsModel.Amount));
    }

    [TestMethod]
    public void NotDefault_NonDefaultValues_Pass() {
        var model = new DefaultSemanticsModel {
            Count = 1,
            Id = Guid.NewGuid(),
            Flag = true,
            When = DateTime.UtcNow,
            Amount = 1m
        };

        var result = new NotDefaultValidator().Validate(model);

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void EqualAndNotEqual_StringComparer_IsHonored() {
        var validator = new ComparerValidator();

        ValidationAssert.IsValid(validator.Validate(new ComparerModel { Name = "admin", Other = "other" }));
        ValidationAssert.IsValid(validator.Validate(new ComparerModel { Name = "ADMIN", Other = "other" }));

        var result = validator.Validate(new ComparerModel { Name = "other", Other = "admin" });
        ValidationAssert.HasError(result, nameof(ComparerModel.Name));
        ValidationAssert.HasError(result, nameof(ComparerModel.Other));
    }

    [TestMethod]
    public void ScalePrecision_IgnoreTrailingZeros_ChangesTheScaleCount() {
        var validator = new ScalePrecisionValidator();

        var trailingZeros = validator.Validate(new ScalePrecisionModel {
            Strict = 1.2300m,
            Lenient = 1.2300m
        });

        ValidationAssert.HasError(trailingZeros, nameof(ScalePrecisionModel.Strict));
        ValidationAssert.NoErrorFor(trailingZeros, nameof(ScalePrecisionModel.Lenient));

        var invalid = validator.Validate(new ScalePrecisionModel {
            Strict = 123.456m,
            Lenient = 123.456m
        });

        ValidationAssert.HasError(invalid, nameof(ScalePrecisionModel.Strict));
        ValidationAssert.HasError(invalid, nameof(ScalePrecisionModel.Lenient));
    }
}
