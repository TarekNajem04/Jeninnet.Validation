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

file sealed class HookModel {
    public string? Name { get; set; }
}

file sealed class SyncHookValidator : Validator<HookModel> {
    public List<string> Events { get; } = [];
    public bool AllowValidation { get; set; } = true;

    protected override void Configure(ValidatorRuleBuilder<HookModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(HookModel.Name))
               .NotEmpty("Name is required.")
               .Must(
                   value => {
                       Events.Add("rule");
                       return value is { Length: > 3 };
                   },
                   "Name must be longer than 3 characters.");

        PreValidate(
            context => {
                Events.Add($"pre:{context.RuleSets?.FirstOrDefault() ?? "none"}");
                return AllowValidation;
            });

        OnFailure(result => Events.Add($"failure:{result.Errors.Count}"));
    }
}

file sealed class AsyncHookValidator : Validator<HookModel> {
    public List<string> Events { get; } = [];
    public bool AllowValidation { get; set; } = true;

    protected override void Configure(ValidatorRuleBuilder<HookModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(HookModel.Name))
               .NotEmpty("Name is required.");

        PreValidateAsync(
            async (context, cancellationToken) => {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                Events.Add($"pre-async:{context.RuleSets?.FirstOrDefault() ?? "none"}");
                return AllowValidation;
            });

        OnFailure(_ => Events.Add("failure"));

        OnFailureAsync(
            async (_, cancellationToken) => {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                Events.Add("failure-async");
            });
    }
}

file sealed class AsyncFailureOnlyValidator : Validator<HookModel> {
    public List<string> Events { get; } = [];

    protected override void Configure(ValidatorRuleBuilder<HookModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(HookModel.Name))
               .NotEmpty("Name is required.");

        OnFailure(_ => Events.Add("failure"));

        OnFailureAsync(
            async (_, cancellationToken) => {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                Events.Add("failure-async");
            });
    }
}

file sealed class ThrowingPreValidateValidator : Validator<HookModel> {
    protected override void Configure(ValidatorRuleBuilder<HookModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(HookModel.Name))
               .NotEmpty();

        PreValidate(_ => throw new InvalidOperationException("boom"));
    }
}

file sealed class ThrowingOnFailureValidator : Validator<HookModel> {
    protected override void Configure(ValidatorRuleBuilder<HookModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(HookModel.Name))
               .NotEmpty();

        OnFailure(_ => throw new InvalidOperationException("boom"));
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class LifecycleHookTests {
    [TestMethod]
    public void SyncHooks_RunOnceInOrder_AndOnFailureReceivesAllErrors() {
        var validator = new SyncHookValidator();

        var result = validator.Validate(new HookModel { Name = "" });

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(2));
        MSTestAssertions.That(() => string.Join("|", validator.Events).Should().Be("pre:none|rule|failure:2"));
    }

    [TestMethod]
    public void PreValidate_False_ShortCircuitsWithSuccess_AndSkipsRulesAndFailureHooks() {
        var validator = new SyncHookValidator { AllowValidation = false };

        var result = validator.Validate(new HookModel { Name = "" });

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => string.Join("|", validator.Events).Should().Be("pre:none"));
    }

    [TestMethod]
    public void OnFailure_DoesNotFire_WhenResultIsValid() {
        var validator = new SyncHookValidator();

        var result = validator.Validate(new HookModel { Name = "abcd" });

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => string.Join("|", validator.Events).Should().Be("pre:none|rule"));
    }

    [TestMethod]
    public void PreValidate_ReceivesRuleSets() {
        var validator = new SyncHookValidator();

        var result = validator.Validate(new HookModel { Name = "abcd" }, ["strict"]);

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => string.Join("|", validator.Events).Should().Be("pre:strict"));
    }

    [TestMethod]
    public void SyncValidate_WithAsyncPreValidate_Throws() {
        var validator = new AsyncHookValidator();

        var act = () => validator.Validate(new HookModel());

        MSTestAssertions.That(() => act.Should().ThrowExactly<InvalidOperationException>()
            .Which.Message.Should().Contain("ValidateAsync"));
    }

    [TestMethod]
    public async Task AsyncPath_RunsAsyncPreValidateAndBothFailureHooksAsync() {
        var validator = new AsyncHookValidator();

        var result = await validator.ValidateAsync(
            new HookModel { Name = "" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
        MSTestAssertions.That(() => string.Join("|", validator.Events).Should().Be("pre-async:none|failure|failure-async"));
    }

    [TestMethod]
    public async Task AsyncPreValidate_False_ShortCircuitsWithSuccessAsync() {
        var validator = new AsyncHookValidator { AllowValidation = false };

        var result = await validator.ValidateAsync(
            new HookModel { Name = "" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => string.Join("|", validator.Events).Should().Be("pre-async:none"));
    }

    [TestMethod]
    public void OnFailureAsync_IsNotCalledOnSyncPath() {
        var validator = new AsyncFailureOnlyValidator();

        var result = validator.Validate(new HookModel { Name = "" });

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
        MSTestAssertions.That(() => string.Join("|", validator.Events).Should().Be("failure"));
    }

    [TestMethod]
    public async Task OnFailureAsync_FiresOnAsyncPathAsync() {
        var validator = new AsyncFailureOnlyValidator();

        var result = await validator.ValidateAsync(
            new HookModel { Name = "" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
        MSTestAssertions.That(() => string.Join("|", validator.Events).Should().Be("failure|failure-async"));
    }

    [TestMethod]
    public void PreValidateHookException_IsWrappedAsValidationException() {
        var act = () => new ThrowingPreValidateValidator().Validate(new HookModel { Name = "abcd" });

        MSTestAssertions.That(() => act.Should().ThrowExactly<ValidationException>()
            .Which.Message.Should().Contain("PreValidate"));
        MSTestAssertions.That(() => (act.Should().ThrowExactly<ValidationException>()
            .Which.InnerException is InvalidOperationException).Should().BeTrue());
    }

    [TestMethod]
    public void OnFailureHookException_IsWrappedAsValidationException() {
        var act = () => new ThrowingOnFailureValidator().Validate(new HookModel { Name = "" });

        MSTestAssertions.That(() => act.Should().ThrowExactly<ValidationException>()
            .Which.Message.Should().Contain("OnFailure"));
    }

    public TestContext? TestContext { get; set; }
}
