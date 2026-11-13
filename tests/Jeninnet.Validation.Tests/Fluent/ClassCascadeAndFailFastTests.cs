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

internal sealed class CascadeModel {
    public string? First { get; set; }
    public string? Second { get; set; }
}

file sealed class CascadeValidator : Validator<CascadeModel> {
    protected override void Configure(ValidatorRuleBuilder<CascadeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.First, nameof(CascadeModel.First))
               .NotEmpty("first-notempty")
               .MinimumLength(5, "first-minlength");

        builder.RuleFor(static x => x.Second, nameof(CascadeModel.Second))
               .NotEmpty("second-notempty")
               .MinimumLength(5, "second-minlength");
    }
}

file sealed class StopCascadeValidator : Validator<CascadeModel> {
    protected override void Configure(ValidatorRuleBuilder<CascadeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        ClassLevelCascadeMode = CascadeMode.Stop;

        builder.RuleFor(static x => x.First, nameof(CascadeModel.First))
               .NotEmpty("first-notempty")
               .MinimumLength(5, "first-minlength");

        builder.RuleFor(static x => x.Second, nameof(CascadeModel.Second))
               .NotEmpty("second-notempty")
               .MinimumLength(5, "second-minlength");
    }
}

file sealed class RuleStopValidator : Validator<CascadeModel> {
    protected override void Configure(ValidatorRuleBuilder<CascadeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.First, nameof(CascadeModel.First))
               .Cascade(CascadeMode.Stop)
               .NotEmpty("first-notempty")
               .MinimumLength(5, "first-minlength");

        builder.RuleFor(static x => x.Second, nameof(CascadeModel.Second))
               .NotEmpty("second-notempty")
               .MinimumLength(5, "second-minlength");
    }
}

file sealed class AsyncCascadeValidator : Validator<CascadeModel> {
    protected override void Configure(ValidatorRuleBuilder<CascadeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.First, nameof(CascadeModel.First))
               .NotEmpty("first-sync")
               .Must(
                   static async (value, cancellationToken) => {
                       await Task.Yield();
                       cancellationToken.ThrowIfCancellationRequested();
                       return value is { Length: > 10 };
                   },
                   "first-async");

        builder.RuleFor(static x => x.Second, nameof(CascadeModel.Second))
               .NotEmpty("second-sync")
               .Must(
                   static async (value, cancellationToken) => {
                       await Task.Yield();
                       cancellationToken.ThrowIfCancellationRequested();
                       return value is { Length: > 10 };
                   },
                   "second-async");
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class ClassCascadeAndFailFastTests {
    private static CascadeModel InvalidBoth => new() { First = "", Second = "" };

    [TestMethod]
    public void DefaultCascade_ContinuesAcrossMembers() {
        var result = new CascadeValidator().Validate(InvalidBoth);

        ValidationAssert.ErrorCount(result, 4);
        MSTestAssertions.That(() => result.Errors.Select(static error => error.MemberName).Distinct().Should().HaveCount(2));
    }

    [TestMethod]
    public void ClassLevelStop_StopsAfterFirstFailingMember() {
        var result = new StopCascadeValidator().Validate(InvalidBoth);

        ValidationAssert.ErrorCount(result, 2);
        MSTestAssertions.That(() => result.Errors.All(static error => error.MemberName == nameof(CascadeModel.First)).Should().BeTrue());
    }

    [TestMethod]
    public void RuleLevelStop_StopsWithinMember() {
        var result = new RuleStopValidator().Validate(InvalidBoth);

        ValidationAssert.ErrorCount(result, 3);
        ValidationAssert.HasError(result, nameof(CascadeModel.First));
        ValidationAssert.HasError(result, nameof(CascadeModel.Second));
    }

    [TestMethod]
    public void RuleLevelStop_WinsUnderClassStop() {
        var validator = new RuleStopValidator { ClassLevelCascadeMode = CascadeMode.Stop };

        var result = validator.Validate(InvalidBoth);

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, nameof(CascadeModel.First));
    }

    [TestMethod]
    public void CallOptionsCascadeOverride_ContinuesAcrossMembers() {
        var result = new StopCascadeValidator().Validate(
            InvalidBoth,
            new ValidationCallOptions { CascadeMode = CascadeMode.Continue });

        ValidationAssert.ErrorCount(result, 4);
    }

    [TestMethod]
    public void FailFast_YieldsExactlyOneErrorOnSyncPath() {
        var result = new CascadeValidator().Validate(
            InvalidBoth,
            new ValidationCallOptions { FailFast = true });

        ValidationAssert.ErrorCount(result, 1);
        MSTestAssertions.That(() => result.Errors[0].Message.Should().Be("first-notempty"));
    }

    [TestMethod]
    public async Task FailFast_YieldsExactlyOneErrorOnAsyncPathAsync() {
        var validator = new AsyncCascadeValidator();

        var result = await validator.ValidateAsync(
            new CascadeModel { First = "abcdef", Second = "abcdef" },
            new ValidationCallOptions { FailFast = true },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.ErrorCount(result, 1);
        MSTestAssertions.That(() => result.Errors[0].Message.Should().Be("first-async"));
    }

    [TestMethod]
    public async Task FailFast_SkipsAsyncPhaseAfterSyncFailureAsync() {
        var result = await new AsyncCascadeValidator().ValidateAsync(
            InvalidBoth,
            new ValidationCallOptions { FailFast = true },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.ErrorCount(result, 1);
        MSTestAssertions.That(() => result.Errors[0].Message.Should().Be("first-sync"));
    }

    [TestMethod]
    public void CallOptions_RuleSets_AreApplied() {
        var result = new CascadeValidator().Validate(
            InvalidBoth,
            new ValidationCallOptions { RuleSets = ["missing"] });

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void CallOptions_Null_Throws() {
        var validator = new CascadeValidator();

        var act = () => validator.Validate(InvalidBoth, (ValidationCallOptions)null!);

        MSTestAssertions.That(() => act.Should().ThrowExactly<ArgumentNullException>());
    }

    [TestMethod]
    public void ClassLevelCascadeMode_DefaultsToContinue() =>
        MSTestAssertions.That(() => new CascadeValidator().ClassLevelCascadeMode.Should().Be(CascadeMode.Continue));

    public TestContext? TestContext { get; set; }
}
