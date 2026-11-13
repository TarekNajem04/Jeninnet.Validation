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

internal sealed class DependentModel {
    public int Discount { get; set; }
    public int Rate { get; set; }
    public string? Code { get; set; }
    public string? Note { get; set; }
}

file sealed class DiscountDependentValidator : Validator<DependentModel> {
    protected override void Configure(ValidatorRuleBuilder<DependentModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Discount, nameof(DependentModel.Discount))
               .InclusiveBetween(0, 90)
               .DependentRules(static dependent => dependent
                   .RuleFor(static x => x.Rate, nameof(DependentModel.Rate))
                   .GreaterThan(0));
    }
}

file sealed class NestedDependentValidator : Validator<DependentModel> {
    protected override void Configure(ValidatorRuleBuilder<DependentModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Discount, nameof(DependentModel.Discount))
               .InclusiveBetween(0, 90)
               .DependentRules(static firstLevel => firstLevel
                   .RuleFor(static x => x.Rate, nameof(DependentModel.Rate))
                   .GreaterThan(0)
                   .DependentRules(static secondLevel => secondLevel
                       .RuleFor(static x => x.Code, nameof(DependentModel.Code))
                       .NotEmpty()));
    }
}

file sealed class StopDependentValidator : Validator<DependentModel> {
    protected override void Configure(ValidatorRuleBuilder<DependentModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Discount, nameof(DependentModel.Discount))
               .InclusiveBetween(0, 90)
               .Cascade(CascadeMode.Stop)
               .DependentRules(static dependent => {
                   dependent.RuleFor(static x => x.Rate, nameof(DependentModel.Rate))
                            .GreaterThan(0);

                   dependent.RuleFor(static x => x.Code, nameof(DependentModel.Code))
                            .NotEmpty();
               });
    }
}

file sealed class ContinueDependentValidator : Validator<DependentModel> {
    protected override void Configure(ValidatorRuleBuilder<DependentModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Discount, nameof(DependentModel.Discount))
               .InclusiveBetween(0, 90)
               .DependentRules(static dependent => {
                   dependent.RuleFor(static x => x.Rate, nameof(DependentModel.Rate))
                            .GreaterThan(0);

                   dependent.RuleFor(static x => x.Code, nameof(DependentModel.Code))
                            .NotEmpty();
               });
    }
}

file sealed class SyncParentAsyncDependentValidator : Validator<DependentModel> {
    public int AsyncCalls { get; private set; }

    protected override void Configure(ValidatorRuleBuilder<DependentModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Discount, nameof(DependentModel.Discount))
               .InclusiveBetween(0, 90)
               .DependentRules(dependent => dependent
                   .RuleFor(static x => x.Note, nameof(DependentModel.Note))
                   .Must(async (note, cancellationToken) => {
                       AsyncCalls++;
                       await Task.Yield();
                       cancellationToken.ThrowIfCancellationRequested();
                       return note == "ok";
                   }));
    }
}

file sealed class AsyncParentSyncDependentValidator : Validator<DependentModel> {
    public int ParentCalls { get; private set; }

    protected override void Configure(ValidatorRuleBuilder<DependentModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Code, nameof(DependentModel.Code))
               .Must(async (code, cancellationToken) => {
                   ParentCalls++;
                   await Task.Yield();
                   cancellationToken.ThrowIfCancellationRequested();
                   return code == "ok";
               })
               .DependentRules(static dependent => dependent
                   .RuleFor(static x => x.Rate, nameof(DependentModel.Rate))
                   .GreaterThan(0));
    }
}

file sealed class StoppedBeforeParentValidator : Validator<DependentModel> {
    public int AsyncCalls { get; private set; }

    protected override void Configure(ValidatorRuleBuilder<DependentModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Discount, nameof(DependentModel.Discount))
               .GreaterThan(0)
               .InclusiveBetween(0, 90)
               .Cascade(CascadeMode.Stop)
               .DependentRules(dependent => dependent
                   .RuleFor(static x => x.Note, nameof(DependentModel.Note))
                   .Must(async (note, cancellationToken) => {
                       AsyncCalls++;
                       await Task.Yield();
                       cancellationToken.ThrowIfCancellationRequested();
                       return note == "ok";
                   }));
    }
}

file sealed class MissingParentValidator : Validator<DependentModel> {
    protected override void Configure(ValidatorRuleBuilder<DependentModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Rate, nameof(DependentModel.Rate))
               .DependentRules(static _ => { });
    }
}

file sealed class EmptyDependentValidator : Validator<DependentModel> {
    protected override void Configure(ValidatorRuleBuilder<DependentModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Discount, nameof(DependentModel.Discount))
               .InclusiveBetween(0, 90)
               .DependentRules(static _ => { });
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class DependentRuleTests {
    [TestMethod]
    public void DependentRules_SkipWhenParentFails() {
        var result = new DiscountDependentValidator().Validate(
            new DependentModel { Discount = 95, Rate = 0 });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, nameof(DependentModel.Discount));
        ValidationAssert.NoErrorFor(result, nameof(DependentModel.Rate));
    }

    [TestMethod]
    public void DependentRules_RunWhenParentPasses() {
        var result = new DiscountDependentValidator().Validate(
            new DependentModel { Discount = 50, Rate = 0 });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, nameof(DependentModel.Rate));
        ValidationAssert.NoErrorFor(result, nameof(DependentModel.Discount));
    }

    [TestMethod]
    public void DependentRules_NestedBlocksGateAtEachLevel() {
        var parentFailed = new NestedDependentValidator().Validate(
            new DependentModel { Discount = 95, Rate = 0, Code = string.Empty });

        ValidationAssert.ErrorCount(parentFailed, 1);
        ValidationAssert.HasError(parentFailed, nameof(DependentModel.Discount));

        var firstLevelFailed = new NestedDependentValidator().Validate(
            new DependentModel { Discount = 50, Rate = 0, Code = string.Empty });

        ValidationAssert.ErrorCount(firstLevelFailed, 1);
        ValidationAssert.HasError(firstLevelFailed, nameof(DependentModel.Rate));
        ValidationAssert.NoErrorFor(firstLevelFailed, nameof(DependentModel.Code));

        var secondLevelFailed = new NestedDependentValidator().Validate(
            new DependentModel { Discount = 50, Rate = 5, Code = string.Empty });

        ValidationAssert.ErrorCount(secondLevelFailed, 1);
        ValidationAssert.HasError(secondLevelFailed, nameof(DependentModel.Code));
    }

    [TestMethod]
    public void DependentRules_StopCascadeHaltsRemainingDependents() {
        var result = new StopDependentValidator().Validate(
            new DependentModel { Discount = 50, Rate = 0, Code = string.Empty });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, nameof(DependentModel.Rate));
        ValidationAssert.NoErrorFor(result, nameof(DependentModel.Code));
    }

    [TestMethod]
    public void DependentRules_ContinueCascadeRunsAllDependents() {
        var result = new ContinueDependentValidator().Validate(
            new DependentModel { Discount = 50, Rate = 0, Code = string.Empty });

        ValidationAssert.ErrorCount(result, 2);
        ValidationAssert.HasError(result, nameof(DependentModel.Rate));
        ValidationAssert.HasError(result, nameof(DependentModel.Code));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be(nameof(DependentModel.Rate)));
        MSTestAssertions.That(() => result.Errors[1].MemberName.Should().Be(nameof(DependentModel.Code)));
    }

    [TestMethod]
    public void DependentRules_SyncParentSkipsAsyncDependentOnSyncPath() {
        var validator = new SyncParentAsyncDependentValidator();

        var result = validator.Validate(new DependentModel { Discount = 50, Note = "bad" });

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(0));
    }

    [TestMethod]
    public async Task DependentRules_SyncParentGatesAsyncDependentOnAsyncPathAsync() {
        var parentFailedValidator = new SyncParentAsyncDependentValidator();

        var parentFailed = await parentFailedValidator.ValidateAsync(
            new DependentModel { Discount = 95, Note = "bad" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.HasError(parentFailed, nameof(DependentModel.Discount));
        ValidationAssert.NoErrorFor(parentFailed, nameof(DependentModel.Note));
        MSTestAssertions.That(() => parentFailedValidator.AsyncCalls.Should().Be(0));

        var parentPassedValidator = new SyncParentAsyncDependentValidator();

        var parentPassed = await parentPassedValidator.ValidateAsync(
            new DependentModel { Discount = 50, Note = "bad" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.HasError(parentPassed, nameof(DependentModel.Note));
        MSTestAssertions.That(() => parentPassedValidator.AsyncCalls.Should().Be(1));
    }

    [TestMethod]
    public async Task DependentRules_AsyncParentGatesSyncDependentOnAsyncPathAsync() {
        var parentFailedValidator = new AsyncParentSyncDependentValidator();

        var parentFailed = await parentFailedValidator.ValidateAsync(
            new DependentModel { Code = "bad", Rate = 0 },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.HasError(parentFailed, nameof(DependentModel.Code));
        ValidationAssert.NoErrorFor(parentFailed, nameof(DependentModel.Rate));
        MSTestAssertions.That(() => parentFailedValidator.ParentCalls.Should().Be(1));

        var parentPassedValidator = new AsyncParentSyncDependentValidator();

        var parentPassed = await parentPassedValidator.ValidateAsync(
            new DependentModel { Code = "ok", Rate = 0 },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.HasError(parentPassed, nameof(DependentModel.Rate));
        MSTestAssertions.That(() => parentPassedValidator.ParentCalls.Should().Be(1));
    }

    [TestMethod]
    public async Task DependentRules_ParentSkippedByCascadeStopDoesNotRunDependentAsync() {
        var skippedValidator = new StoppedBeforeParentValidator();

        var skipped = await skippedValidator.ValidateAsync(
            new DependentModel { Discount = -5, Note = "bad" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.HasError(skipped, nameof(DependentModel.Discount));
        ValidationAssert.NoErrorFor(skipped, nameof(DependentModel.Note));
        MSTestAssertions.That(() => skippedValidator.AsyncCalls.Should().Be(0));

        var reachedValidator = new StoppedBeforeParentValidator();

        var reached = await reachedValidator.ValidateAsync(
            new DependentModel { Discount = 50, Note = "bad" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.HasError(reached, nameof(DependentModel.Note));
        MSTestAssertions.That(() => reachedValidator.AsyncCalls.Should().Be(1));
    }

    [TestMethod]
    public void DependentRules_AsyncParentThrowsOnSyncPath() {
        var validator = new AsyncParentSyncDependentValidator();

        Action validate = () => _ = validator.Validate(new DependentModel { Code = "ok", Rate = 0 });

        MSTestAssertions.That(() => validate.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>());
    }

    [TestMethod]
    public void DependentRules_WithoutParentThrows() {
        Action act = static () => _ = new MissingParentValidator();

        MSTestAssertions.That(() => act.Should().ThrowExactly<InvalidOperationException>());
    }

    [TestMethod]
    public void DependentRules_EmptyBlockKeepsParentBehaviour() {
        var result = new EmptyDependentValidator().Validate(new DependentModel { Discount = 95 });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, nameof(DependentModel.Discount));
    }

    public TestContext? TestContext { get; set; }
}
