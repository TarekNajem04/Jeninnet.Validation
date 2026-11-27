//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Core;

internal sealed class DeadlineModel {
    public string? Value { get; set; }
}

internal sealed class DeadlineParentModel {
    public DeadlineModel? Child { get; set; }
}

internal sealed class DeadlineCollectionModel {
    public List<DeadlineModel> Items { get; set; } = [];

    public string? Value { get; set; }
}

file sealed class SimpleDeadlineValidator : Validator<DeadlineModel> {
    protected override void Configure(ValidatorRuleBuilder<DeadlineModel> builder) =>
        builder.RuleFor(static x => x.Value, nameof(DeadlineModel.Value))
               .NotEmpty("Value is required.");
}

file sealed class SlowDeadlineValidator : Validator<DeadlineModel> {
    protected override void Configure(ValidatorRuleBuilder<DeadlineModel> builder) =>
        builder.RuleFor(static x => x.Value, nameof(DeadlineModel.Value))
               .Must(
                   static async (_, cancellationToken) => {
                       await Task.Delay(Timeout.Infinite, cancellationToken);
                       return true;
                   },
                   "Slow rule.");
}

file sealed class GatedSlowDeadlineValidator(TaskCompletionSource started) : Validator<DeadlineModel> {
    protected override void Configure(ValidatorRuleBuilder<DeadlineModel> builder) =>
        builder.RuleFor(static x => x.Value, nameof(DeadlineModel.Value))
               .Must(
                   async (_, cancellationToken) => {
                       started.TrySetResult();
                       await Task.Delay(Timeout.Infinite, cancellationToken);
                       return true;
                   },
                   "Slow rule.");
}

file sealed class IgnoringDeadlineValidator : Validator<DeadlineModel> {
    protected override void Configure(ValidatorRuleBuilder<DeadlineModel> builder) =>
        builder.RuleFor(static x => x.Value, nameof(DeadlineModel.Value))
               .Must(
                   static async (_, _) => {
                       await Task.Delay(50, CancellationToken.None);
                       return false;
                   },
                   "Ignoring rule.");
}

file sealed class NestedDeadlineValidator : Validator<DeadlineParentModel> {
    protected override void Configure(ValidatorRuleBuilder<DeadlineParentModel> builder) =>
        builder.RuleFor(static x => x.Child, nameof(DeadlineParentModel.Child))
               .ChildRules(child =>
                   child.RuleFor(static x => x.Value, nameof(DeadlineModel.Value))
                        .Must(
                            static async (_, cancellationToken) => {
                                await Task.Delay(Timeout.Infinite, cancellationToken);
                                return true;
                            },
                            "Slow child rule."));
}

file sealed class ForEachDeadlineValidator : Validator<DeadlineCollectionModel> {
    protected override void Configure(ValidatorRuleBuilder<DeadlineCollectionModel> builder) =>
        builder.RuleFor(static x => x.Items, nameof(DeadlineCollectionModel.Items))
               .ForEach(syncValidator: null, asyncValidator: new SlowDeadlineValidator());
}

[TestClass]
public sealed class AsyncValidationDeadlineTests {
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task NoTimeout_KeepsTheExistingResultAsync() {
        var result = await new SimpleDeadlineValidator().ValidateAsync(
            new DeadlineModel(),
            new ValidationCallOptions(),
            TestContext.CancellationToken);

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1));
    }

    [TestMethod]
    public void ZeroTimeout_ThrowsArgumentOutOfRange() {
        Action act = () => _ = new SimpleDeadlineValidator().ValidateAsync(
            new DeadlineModel(),
            new ValidationCallOptions { Timeout = TimeSpan.Zero },
            TestContext.CancellationToken).AsTask();

        MSTestAssertions.That(() => act.Should().ThrowExactly<ArgumentOutOfRangeException>());
    }

    [TestMethod]
    public void NegativeTimeout_ThrowsArgumentOutOfRange() {
        Action act = () => _ = new SimpleDeadlineValidator().ValidateAsync(
            new DeadlineModel(),
            new ValidationCallOptions { Timeout = TimeSpan.FromMilliseconds(-1) },
            TestContext.CancellationToken).AsTask();

        MSTestAssertions.That(() => act.Should().ThrowExactly<ArgumentOutOfRangeException>());
    }

    [TestMethod]
    public void SyncValidate_WithTimeout_ThrowsInvalidOperationException() {
        var act = () => new SimpleDeadlineValidator().Validate(
            new DeadlineModel(),
            new ValidationCallOptions { Timeout = TimeSpan.FromSeconds(1) });

        MSTestAssertions.That(() => act.Should().ThrowExactly<InvalidOperationException>());
    }

    [TestMethod]
    public async Task Deadline_DuringAsyncRule_ThrowsValidationTimeoutAsync() {
        var timeout = TimeSpan.FromMilliseconds(20);
        var task = new SlowDeadlineValidator().ValidateAsync(
            new DeadlineModel { Value = "value" },
            new ValidationCallOptions { Timeout = timeout },
            TestContext.CancellationToken).AsTask();
        var act = async () => await task;

        var exception = await act.Should().ThrowAsync<ValidationTimeoutException>();

        await MSTestAssertions.ThatAsync(() => {
            exception.Which.Timeout.Should().Be(timeout);
            return Task.CompletedTask;
        });
    }

    [TestMethod]
    public async Task CallerCancellation_IsNotMaskedByTheDeadlineAsync() {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var task = new GatedSlowDeadlineValidator(started).ValidateAsync(
            new DeadlineModel { Value = "value" },
            new ValidationCallOptions { Timeout = TimeSpan.FromSeconds(5) },
            cancellation.Token).AsTask();

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        await cancellation.CancelAsync();

        var act = async () => await task;
        var exception = await act.Should().ThrowAsync<OperationCanceledException>();

        await MSTestAssertions.ThatAsync(() => {
            (exception.Which is ValidationTimeoutException).Should().BeFalse();
            cancellation.IsCancellationRequested.Should().BeTrue();
            return Task.CompletedTask;
        });
    }

    [TestMethod]
    public async Task NestedChildRules_ObserveTheDeadlineAsync() {
        var task = new NestedDeadlineValidator().ValidateAsync(
            new DeadlineParentModel { Child = new DeadlineModel { Value = "value" } },
            new ValidationCallOptions { Timeout = TimeSpan.FromMilliseconds(20) },
            TestContext.CancellationToken).AsTask();
        var act = async () => await task;

        _ = await act.Should().ThrowAsync<ValidationTimeoutException>();
    }

    [TestMethod]
    public async Task ForEachRules_ObserveTheDeadlineAsync() {
        var task = new ForEachDeadlineValidator().ValidateAsync(
            new DeadlineCollectionModel { Items = [new DeadlineModel { Value = "value" }] },
            new ValidationCallOptions { Timeout = TimeSpan.FromMilliseconds(20) },
            TestContext.CancellationToken).AsTask();
        var act = async () => await task;

        _ = await act.Should().ThrowAsync<ValidationTimeoutException>();
    }

    [TestMethod]
    public async Task RuleThatIgnoresCancellation_CanExceedTheBudgetAsync() {
        var stopwatch = Stopwatch.StartNew();

        var result = await new IgnoringDeadlineValidator().ValidateAsync(
            new DeadlineModel { Value = "value" },
            new ValidationCallOptions { Timeout = TimeSpan.FromMilliseconds(5) },
            TestContext.CancellationToken);

        stopwatch.Stop();

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
        MSTestAssertions.That(() => stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(20)));
    }
}
