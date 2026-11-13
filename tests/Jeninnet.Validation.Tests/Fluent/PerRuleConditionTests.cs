//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Fluent;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

internal sealed class ConditionModel {
    public string? Country { get; set; }
    public int Tax { get; set; }
    public string? Note { get; set; }
}

file sealed class CurrentScopeValidator : Validator<ConditionModel> {
    protected override void Configure(ValidatorRuleBuilder<ConditionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Tax, nameof(ConditionModel.Tax))
               .GreaterThan(0, "tax-positive")
               .LessThan(100, "tax-max")
               .When(static x => x.Country == "US");
    }
}

file sealed class UnlessValidator : Validator<ConditionModel> {
    protected override void Configure(ValidatorRuleBuilder<ConditionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Tax, nameof(ConditionModel.Tax))
               .GreaterThan(0, "tax-positive")
               .Unless(static x => x.Country == "US");
    }
}

file sealed class AllScopeValidator : Validator<ConditionModel> {
    public int ConditionCalls { get; private set; }

    protected override void Configure(ValidatorRuleBuilder<ConditionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Tax, nameof(ConditionModel.Tax))
               .GreaterThan(0, "tax-positive")
               .LessThan(100, "tax-max")
               .When(
                   instance => {
                       ConditionCalls++;
                       return instance.Country == "US";
                   },
                   ApplyConditionTo.AllValidators);
    }
}

file sealed class AsyncConditionValidator : Validator<ConditionModel> {
    public int ConditionCalls { get; private set; }

    protected override void Configure(ValidatorRuleBuilder<ConditionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Tax, nameof(ConditionModel.Tax))
               .GreaterThan(-1, "tax-floor");

        builder.RuleFor(static x => x.Note, nameof(ConditionModel.Note))
               .NotEmpty("note-required")
               .WhenAsync(
                   async (instance, cancellationToken) => {
                       await Task.Yield();
                       cancellationToken.ThrowIfCancellationRequested();
                       ConditionCalls++;
                       return instance.Country == "US";
                   });
    }
}

file sealed class CountingConditionValidator : Validator<ConditionModel> {
    public int ConditionCalls { get; private set; }

    protected override void Configure(ValidatorRuleBuilder<ConditionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Tax, nameof(ConditionModel.Tax))
               .GreaterThan(0, "tax-positive")
               .When(
                   _ => {
                       ConditionCalls++;
                       return true;
                   });
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class PerRuleConditionTests {
    [TestMethod]
    public void CurrentValidatorScope_GatesOnlyLastRule() {
        var validator = new CurrentScopeValidator();

        var usResult = validator.Validate(new ConditionModel { Country = "US", Tax = 200 });
        ValidationAssert.ErrorCount(usResult, 1);
        MSTestAssertions.That(() => usResult.Errors[0].Message.Should().Be("tax-max"));

        var frResult = validator.Validate(new ConditionModel { Country = "FR", Tax = 200 });
        ValidationAssert.IsValid(frResult);
    }

    [TestMethod]
    public void Unless_MirrorsCondition() {
        var validator = new UnlessValidator();

        var usResult = validator.Validate(new ConditionModel { Country = "US", Tax = -1 });
        ValidationAssert.IsValid(usResult);

        var frResult = validator.Validate(new ConditionModel { Country = "FR", Tax = -1 });
        ValidationAssert.ErrorCount(frResult, 1);
        MSTestAssertions.That(() => frResult.Errors[0].Message.Should().Be("tax-positive"));
    }

    [TestMethod]
    public void AllValidatorsScope_GatesEveryConfiguredRule() {
        var validator = new AllScopeValidator();

        var frResult = validator.Validate(new ConditionModel { Country = "FR", Tax = 200 });
        ValidationAssert.IsValid(frResult);

        var usResult = validator.Validate(new ConditionModel { Country = "US", Tax = 200 });
        ValidationAssert.ErrorCount(usResult, 1);
        MSTestAssertions.That(() => usResult.Errors[0].Message.Should().Be("tax-max"));

        MSTestAssertions.That(() => validator.ConditionCalls.Should().Be(2));
    }

    [TestMethod]
    public void SyncValidate_DoesNotEvaluateAsyncCondition() {
        var validator = new AsyncConditionValidator();

        var result = validator.Validate(new ConditionModel { Country = "US", Tax = 0, Note = "" });

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => validator.ConditionCalls.Should().Be(0));
    }

    [TestMethod]
    public async Task AsyncValidate_EvaluatesAsyncConditionAsync() {
        var validator = new AsyncConditionValidator();

        var result = await validator.ValidateAsync(
            new ConditionModel { Country = "US", Tax = 0, Note = "" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, nameof(ConditionModel.Note));
        MSTestAssertions.That(() => validator.ConditionCalls.Should().Be(1));
    }

    [TestMethod]
    public async Task AsyncValidate_WhenConditionIsFalse_SkipsRuleAsync() {
        var validator = new AsyncConditionValidator();

        var result = await validator.ValidateAsync(
            new ConditionModel { Country = "FR", Tax = 0, Note = "" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => validator.ConditionCalls.Should().Be(1));
    }

    [TestMethod]
    public void Condition_IsEvaluatedOncePerValidate() {
        var validator = new CountingConditionValidator();

        var result = validator.Validate(new ConditionModel { Tax = 0 });

        ValidationAssert.ErrorCount(result, 1);
        MSTestAssertions.That(() => validator.ConditionCalls.Should().Be(1));
    }

    [TestMethod]
    public void Conditions_DoNotChangeGroupLevelWhenBehavior() {
        var validator = new CurrentScopeValidator();

        // No condition applies to the first rule: the ungrouped positive rule still runs.
        var result = validator.Validate(new ConditionModel { Country = "FR", Tax = -1 });

        ValidationAssert.ErrorCount(result, 1);
        MSTestAssertions.That(() => result.Errors[0].Message.Should().Be("tax-positive"));
    }

    public TestContext? TestContext { get; set; }
}
