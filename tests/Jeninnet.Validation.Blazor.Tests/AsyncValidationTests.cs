//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Blazor.Tests;

// ---------------------------------------------------------------------------
// Models and validators
// ---------------------------------------------------------------------------

internal sealed class AsyncBlazorModel {
    public string? Code { get; set; }
}

file sealed class AsyncBlazorModelValidator : Validator<AsyncBlazorModel> {
    protected override void Configure(ValidatorRuleBuilder<AsyncBlazorModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Code, nameof(AsyncBlazorModel.Code))
               .Must(
                   static (value, _) => ValueTask.FromResult(!string.IsNullOrWhiteSpace(value)),
                   "Code is required.");
    }
}

internal sealed class CountingBlazorModel {
    public string? Value { get; set; }
}

file sealed class CountingAsyncValidator : IValidator<CountingBlazorModel> {
    private int _calls;

    public int Calls => Volatile.Read(ref _calls);

    public ValidationResult Validate(CountingBlazorModel instance) =>
        throw new NotSupportedException("Use the asynchronous path.");

    public ValidationResult Validate(CountingBlazorModel instance, IEnumerable<string> ruleSets) =>
        throw new NotSupportedException("Use the asynchronous path.");

    public ValueTask<ValidationResult> ValidateAsync(
        CountingBlazorModel instance,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(instance, [], cancellationToken);

    public ValueTask<ValidationResult> ValidateAsync(
        CountingBlazorModel instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) {
        _ = Interlocked.Increment(ref _calls);

        return ValueTask.FromResult(
            string.IsNullOrWhiteSpace(instance.Value)
                ? Invalid(nameof(CountingBlazorModel.Value), "Value is required.")
                : ValidationResult.Success);
    }

    private static ValidationResult Invalid(string memberName, string message) {
        var errors = new ValidationErrorCollector {
            { memberName, message }
        };
        return ValidationResult.FromErrors(errors);
    }
}

internal sealed class GatedBlazorModel {
    public string? Value { get; set; }
}

file sealed class GatedAsyncValidator : IValidator<GatedBlazorModel> {
    private int _calls;

    public TaskCompletionSource FirstStarted { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource<ValidationResult> FirstResult { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ValidationResult Validate(GatedBlazorModel instance) =>
        throw new NotSupportedException("Use the asynchronous path.");

    public ValidationResult Validate(GatedBlazorModel instance, IEnumerable<string> ruleSets) =>
        throw new NotSupportedException("Use the asynchronous path.");

    public ValueTask<ValidationResult> ValidateAsync(
        GatedBlazorModel instance,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(instance, [], cancellationToken);

    public async ValueTask<ValidationResult> ValidateAsync(
        GatedBlazorModel instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) {
        var call = Interlocked.Increment(ref _calls);

        if(call == 1) {
            FirstStarted.TrySetResult();

            // Deliberately ignore cancellation so the test can prove that a late result
            // from a superseded validation is discarded.
            return await FirstResult.Task;
        }

        return ValidationResult.Success;
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class AsyncValidationTests {
    public TestContext TestContext { get; set; } = null!;
    [TestMethod]
    public async Task Coordinator_ValidateAsync_RunsAsyncOnlyValidatorAsync() {
        var model = new AsyncBlazorModel { Code = string.Empty };
        var context = new EditContext(model);
        using var coordinator = new EditContextValidationCoordinator<AsyncBlazorModel>(
            context,
            new AsyncBlazorModelValidator(),
            model,
            new EditContextValidationOptions { UseAsyncValidation = true });

        var valid = await coordinator.ValidateAsync(TestContext.CancellationToken);

        MSTestAssertions.That(() => valid.Should().BeFalse());
        MSTestAssertions.That(() => HasMessage(context, model, nameof(AsyncBlazorModel.Code), "Code is required.").Should().BeTrue());
    }

    [TestMethod]
    public void Coordinator_SyncValidate_OnAsyncOnlyValidatorThrows() {
        var model = new AsyncBlazorModel { Code = string.Empty };
        var context = new EditContext(model);
        using var coordinator = new EditContextValidationCoordinator<AsyncBlazorModel>(
            context,
            new AsyncBlazorModelValidator(),
            model);

        var act = coordinator.Validate;

        MSTestAssertions.That(() => act.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>());
    }

    [TestMethod]
    public async Task Coordinator_ValidateAsync_RunsSynchronousValidatorOnAsyncPathAsync() {
        var model = CreateInvalidOrder();
        var context = new EditContext(model);
        using var coordinator = new EditContextValidationCoordinator<BlazorOrder>(
            context,
            new BlazorOrderValidator(),
            model,
            new EditContextValidationOptions { UseAsyncValidation = true });

        var valid = await coordinator.ValidateAsync(TestContext.CancellationToken);

        MSTestAssertions.That(() => valid.Should().BeFalse());
        MSTestAssertions.That(() => HasMessage(context, model, nameof(BlazorOrder.CustomerName), "NotEmpty").Should().BeTrue());
        MSTestAssertions.That(() => HasMessage(context, model.Address!, nameof(BlazorAddress.Street), "NotEmpty").Should().BeTrue());
        MSTestAssertions.That(() => HasMessage(context, model.Lines[0], nameof(BlazorLine.ProductName), "NotEmpty").Should().BeTrue());
    }

    [TestMethod]
    public async Task Coordinator_ValidateAsync_DiscardsSupersededResultsAsync() {
        var model = new GatedBlazorModel { Value = string.Empty };
        var validator = new GatedAsyncValidator();
        var context = new EditContext(model);
        using var coordinator = new EditContextValidationCoordinator<GatedBlazorModel>(
            context,
            validator,
            model,
            new EditContextValidationOptions { UseAsyncValidation = true });

        var first = coordinator.ValidateAsync(TestContext.CancellationToken);
        await validator.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        var second = await coordinator.ValidateAsync(TestContext.CancellationToken);
        MSTestAssertions.That(() => second.Should().BeTrue());

        var errors = new ValidationErrorCollector {
            { nameof(GatedBlazorModel.Value), "Stale result." }
        };
        validator.FirstResult.TrySetResult(ValidationResult.FromErrors(errors));

        var firstResult = await first;
        MSTestAssertions.That(() => firstResult.Should().BeFalse());
        MSTestAssertions.That(() => context.GetValidationMessages().Any().Should().BeFalse());
    }

    [TestMethod]
    public async Task Coordinator_FieldChange_DebouncesAndRunsOnceAsync() {
        var model = new CountingBlazorModel { Value = string.Empty };
        var validator = new CountingAsyncValidator();
        var context = new EditContext(model);
        using var coordinator = new EditContextValidationCoordinator<CountingBlazorModel>(
            context,
            validator,
            model,
            new EditContextValidationOptions {
                ValidateOnFieldChanged = true,
                UseAsyncValidation = true,
                FieldChangedDebounce = TimeSpan.FromMilliseconds(200)
            });

        context.NotifyFieldChanged(new FieldIdentifier(model, nameof(CountingBlazorModel.Value)));
        context.NotifyFieldChanged(new FieldIdentifier(model, nameof(CountingBlazorModel.Value)));

        var pending = coordinator.PendingValidation;
        MSTestAssertions.That(() => (pending is not null).Should().BeTrue());
        await pending!;

        MSTestAssertions.That(() => validator.Calls.Should().Be(1));
        MSTestAssertions.That(() => HasMessage(context, model, nameof(CountingBlazorModel.Value), "Value is required.").Should().BeTrue());
    }

    [TestMethod]
    public async Task Coordinator_SkipIfModelStateValid_SkipsUnchangedValidModelAsync() {
        var model = new CountingBlazorModel { Value = "valid" };
        var validator = new CountingAsyncValidator();
        var context = new EditContext(model);
        using var coordinator = new EditContextValidationCoordinator<CountingBlazorModel>(
            context,
            validator,
            model,
            new EditContextValidationOptions {
                UseAsyncValidation = true,
                SkipIfModelStateValid = true
            });

        var firstResult = await coordinator.ValidateAsync(TestContext.CancellationToken);
        var secondResult = await coordinator.ValidateAsync(TestContext.CancellationToken);
        MSTestAssertions.That(() => firstResult.Should().BeTrue());
        MSTestAssertions.That(() => secondResult.Should().BeTrue());
        MSTestAssertions.That(() => validator.Calls.Should().Be(1));

        model.Value = string.Empty;
        context.NotifyFieldChanged(new FieldIdentifier(model, nameof(CountingBlazorModel.Value)));

        var thirdResult = await coordinator.ValidateAsync(TestContext.CancellationToken);
        MSTestAssertions.That(() => thirdResult.Should().BeFalse());
        MSTestAssertions.That(() => validator.Calls.Should().Be(2));

        var fourthResult = await coordinator.ValidateAsync(TestContext.CancellationToken);
        MSTestAssertions.That(() => fourthResult.Should().BeFalse());
        MSTestAssertions.That(() => validator.Calls.Should().Be(3));
    }

    [TestMethod]
    public void Coordinator_AsyncMode_SyncValidateThrowsGuidance() {
        var model = new CountingBlazorModel { Value = "valid" };
        var context = new EditContext(model);
        using var coordinator = new EditContextValidationCoordinator<CountingBlazorModel>(
            context,
            new CountingAsyncValidator(),
            model,
            new EditContextValidationOptions { UseAsyncValidation = true });

        var act = coordinator.Validate;

        MSTestAssertions.That(() => act.Should().ThrowExactly<InvalidOperationException>()
            .Which.Message.Should().Contain("ValidateAsync"));
    }

    [TestMethod]
    public void Coordinator_Options_RejectInvalidDebounceConfiguration() {
        var model = new CountingBlazorModel { Value = "valid" };
        var context = new EditContext(model);

        var syncDebounce = () => new EditContextValidationCoordinator<CountingBlazorModel>(
            context,
            new CountingAsyncValidator(),
            model,
            new EditContextValidationOptions { FieldChangedDebounce = TimeSpan.FromMilliseconds(10) });

        var negativeDebounce = () => new EditContextValidationCoordinator<CountingBlazorModel>(
            context,
            new CountingAsyncValidator(),
            model,
            new EditContextValidationOptions {
                UseAsyncValidation = true,
                FieldChangedDebounce = TimeSpan.FromMilliseconds(-1)
            });

        MSTestAssertions.That(() => syncDebounce.Should().ThrowExactly<ArgumentException>());
        MSTestAssertions.That(() => negativeDebounce.Should().ThrowExactly<ArgumentOutOfRangeException>());
    }

    private static BlazorOrder CreateInvalidOrder() => new() {
        CustomerName = string.Empty,
        Address = new BlazorAddress { Street = string.Empty },
        Lines = [new BlazorLine { ProductName = string.Empty }]
    };

    private static bool HasMessage(EditContext context, object model, string fieldName, string fragment) =>
        context.GetValidationMessages(new FieldIdentifier(model, fieldName))
               .Any(message => message.Contains(fragment, StringComparison.Ordinal));
}
