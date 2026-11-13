//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Observable.Tests;

// ---------------------------------------------------------------------------
// Models and validators
// ---------------------------------------------------------------------------

public sealed class ObservableOrder : INotifyPropertyChanged {
    public string? CustomerName {
        get;
        set {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CustomerName)));
        }
    }

    public ObservableAddress? Address { get; set; }

    public List<ObservableLine> Lines { get; set; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class ObservableAddress {
    public string? Street { get; set; }
}

public sealed class ObservableLine {
    public string? ProductName { get; set; }
}

public sealed class ObservableOrderValidator : Validator<ObservableOrder> {
    protected override void Configure(ValidatorRuleBuilder<ObservableOrder> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.CustomerName, nameof(ObservableOrder.CustomerName))
               .NotEmpty();

        builder.RuleFor(static x => x.Address, nameof(ObservableOrder.Address))
               .ChildRules(address =>
                   address.RuleFor(static x => x.Street, nameof(ObservableAddress.Street))
                          .NotEmpty());

        builder.RuleFor(static x => x.Lines, nameof(ObservableOrder.Lines))
               .ForEach(new ObservableLineValidator());
    }
}

file sealed class ObservableLineValidator : Validator<ObservableLine> {
    protected override void Configure(ValidatorRuleBuilder<ObservableLine> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.ProductName, nameof(ObservableLine.ProductName))
               .NotEmpty();
    }
}

internal sealed class CountingModel : INotifyPropertyChanged {
    public string? Value {
        get;
        set {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

internal sealed class CountingValidator : IValidator<CountingModel> {
    private int _calls;

    public int Calls => Volatile.Read(ref _calls);

    public ValidationResult Validate(CountingModel instance) => ValidateValue(instance);

    public ValidationResult Validate(CountingModel instance, IEnumerable<string> ruleSets) => ValidateValue(instance);

    public ValueTask<ValidationResult> ValidateAsync(
        CountingModel instance,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValidateValue(instance));

    public ValueTask<ValidationResult> ValidateAsync(
        CountingModel instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValidateValue(instance));

    private ValidationResult ValidateValue(CountingModel instance) {
        _ = Interlocked.Increment(ref _calls);

        return string.IsNullOrWhiteSpace(instance.Value)
            ? Invalid(nameof(CountingModel.Value), "Value is required.")
            : ValidationResult.Success;
    }

    private static ValidationResult Invalid(string memberName, string message) {
        var errors = new ValidationErrorCollector {
            { memberName, message }
        };
        return ValidationResult.FromErrors(errors);
    }
}

public sealed class AsyncObservableModel {
    public string? Code { get; set; }
}

file sealed class AsyncObservableModelValidator : Validator<AsyncObservableModel> {
    protected override void Configure(ValidatorRuleBuilder<AsyncObservableModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Code, nameof(AsyncObservableModel.Code))
               .Must(
                   static (value, _) => ValueTask.FromResult(!string.IsNullOrWhiteSpace(value)),
                   "Code is required.");
    }
}

internal sealed class GatedModel {
    public string? Value { get; set; }
}

file sealed class GatedValidator : IValidator<GatedModel> {
    private int _calls;

    public TaskCompletionSource FirstStarted { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource<ValidationResult> FirstResult { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ValidationResult Validate(GatedModel instance) =>
        throw new NotSupportedException("Use the asynchronous path.");

    public ValidationResult Validate(GatedModel instance, IEnumerable<string> ruleSets) =>
        throw new NotSupportedException("Use the asynchronous path.");

    public ValueTask<ValidationResult> ValidateAsync(
        GatedModel instance,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(instance, [], cancellationToken);

    public async ValueTask<ValidationResult> ValidateAsync(
        GatedModel instance,
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

public sealed class SeverityModel {
    public string? Info { get; set; }

    public string? Warning { get; set; }

    public string? Error { get; set; }
}

file sealed class SeverityValidator : Validator<SeverityModel> {
    protected override void Configure(ValidatorRuleBuilder<SeverityModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Info, nameof(SeverityModel.Info))
               .NotEmpty("Info.")
               .WithSeverity(ValidationErrorSeverity.Info);

        builder.RuleFor(static x => x.Warning, nameof(SeverityModel.Warning))
               .NotEmpty("Warning.")
               .WithSeverity(ValidationErrorSeverity.Warning);

        builder.RuleFor(static x => x.Error, nameof(SeverityModel.Error))
               .NotEmpty("Error.")
               .WithSeverity(ValidationErrorSeverity.Error);
    }
}

internal sealed class PlainModel {
    public string? Value { get; set; }
}

file sealed class PlainModelValidator : Validator<PlainModel> {
    protected override void Configure(ValidatorRuleBuilder<PlainModel> builder) => ArgumentNullException.ThrowIfNull(builder);
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class ObservableValidationAdapterTests {
    public TestContext TestContext { get; set; } = null!;
    [TestMethod]
    public void Adapter_MapsRootNestedAndIndexedPaths() {
        var model = CreateInvalidOrder();
        using var adapter = new ObservableValidationAdapter<ObservableOrder>(model, new ObservableOrderValidator());

        var valid = adapter.Validate();

        MSTestAssertions.That(() => valid.Should().BeFalse());
        MSTestAssertions.That(() => adapter.HasErrors.Should().BeTrue());
        MSTestAssertions.That(() => Messages(adapter, nameof(ObservableOrder.CustomerName)).Should().Contain("NotEmpty"));
        MSTestAssertions.That(() => Messages(adapter, nameof(ObservableOrder.Address)).Should().Contain("NotEmpty"));
        MSTestAssertions.That(() => Messages(adapter, "Address.Street").Should().Contain("NotEmpty"));
        MSTestAssertions.That(() => Messages(adapter, nameof(ObservableOrder.Lines)).Should().Contain("NotEmpty"));
        MSTestAssertions.That(() => Messages(adapter, "Lines[0].ProductName").Should().Contain("NotEmpty"));
        MSTestAssertions.That(() => Messages(adapter, "Missing").Should().BeEmpty());
        MSTestAssertions.That(() => Messages(adapter, null).Should().BeEmpty());
    }

    [TestMethod]
    public void Adapter_RaisesErrorsChangedForAffectedPaths() {
        var model = CreateInvalidOrder();
        using var adapter = new ObservableValidationAdapter<ObservableOrder>(model, new ObservableOrderValidator());
        var changed = new List<string>();
        adapter.ErrorsChanged += (_, args) => changed.Add(args.PropertyName ?? string.Empty);

        adapter.Validate();

        MSTestAssertions.That(() => changed.Should().Contain(nameof(ObservableOrder.CustomerName)));
        MSTestAssertions.That(() => changed.Should().Contain(nameof(ObservableOrder.Address)));
        MSTestAssertions.That(() => changed.Should().Contain("Address.Street"));
        MSTestAssertions.That(() => changed.Should().Contain(nameof(ObservableOrder.Lines)));
        MSTestAssertions.That(() => changed.Should().Contain("Lines[0].ProductName"));
    }

    [TestMethod]
    public void Adapter_ValidateOnPropertyChanged_RunsValidator() {
        var model = new CountingModel { Value = "valid" };
        var validator = new CountingValidator();
        using var adapter = new ObservableValidationAdapter<CountingModel>(
            model,
            validator,
            new ObservableValidationOptions { ValidateOnPropertyChanged = true });

        model.Value = string.Empty;

        MSTestAssertions.That(() => validator.Calls.Should().Be(1));
        MSTestAssertions.That(() => adapter.HasErrors.Should().BeTrue());
        MSTestAssertions.That(() => Messages(adapter, nameof(CountingModel.Value)).Should().Contain("Value is required."));
    }

    [TestMethod]
    public async Task Adapter_ValidateAsync_RunsAsyncOnlyValidatorAsync() {
        var model = new AsyncObservableModel { Code = string.Empty };
        using var adapter = new ObservableValidationAdapter<AsyncObservableModel>(
            model,
            new AsyncObservableModelValidator(),
            new ObservableValidationOptions { UseAsyncValidation = true });

        var valid = await adapter.ValidateAsync(TestContext.CancellationToken);

        MSTestAssertions.That(() => valid.Should().BeFalse());
        MSTestAssertions.That(() => adapter.HasErrors.Should().BeTrue());
        MSTestAssertions.That(() => Messages(adapter, nameof(AsyncObservableModel.Code)).Should().Contain("Code is required."));
    }

    [TestMethod]
    public void Adapter_SyncValidate_OnAsyncOnlyValidatorThrows() {
        var model = new AsyncObservableModel { Code = string.Empty };
        using var adapter = new ObservableValidationAdapter<AsyncObservableModel>(
            model,
            new AsyncObservableModelValidator());

        var act = adapter.Validate;

        MSTestAssertions.That(() => act.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>());
    }

    [TestMethod]
    public async Task Adapter_ValidateAsync_DiscardsSupersededResultsAsync() {
        var model = new GatedModel { Value = string.Empty };
        var validator = new GatedValidator();
        using var adapter = new ObservableValidationAdapter<GatedModel>(
            model,
            validator,
            new ObservableValidationOptions { UseAsyncValidation = true });

        var first = adapter.ValidateAsync(TestContext.CancellationToken);
        await validator.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        var second = await adapter.ValidateAsync(TestContext.CancellationToken);
        MSTestAssertions.That(() => second.Should().BeTrue());

        var errors = new ValidationErrorCollector {
            { nameof(GatedModel.Value), "Stale result." }
        };
        validator.FirstResult.TrySetResult(ValidationResult.FromErrors(errors));

        var firstResult = await first;
        MSTestAssertions.That(() => firstResult.Should().BeFalse());
        MSTestAssertions.That(() => adapter.HasErrors.Should().BeFalse());
        MSTestAssertions.That(() => Messages(adapter, nameof(GatedModel.Value)).Should().BeEmpty());
    }

    [TestMethod]
    public async Task Adapter_PropertyChangedDebounce_RunsOnceAsync() {
        var model = new CountingModel { Value = string.Empty };
        var validator = new CountingValidator();
        using var adapter = new ObservableValidationAdapter<CountingModel>(
            model,
            validator,
            new ObservableValidationOptions {
                ValidateOnPropertyChanged = true,
                UseAsyncValidation = true,
                PropertyChangedDebounce = TimeSpan.FromMilliseconds(200)
            });

        model.Value = "a";
        model.Value = string.Empty;

        var pending = adapter.PendingValidation;
        MSTestAssertions.That(() => (pending is not null).Should().BeTrue());
        await pending!;

        MSTestAssertions.That(() => validator.Calls.Should().Be(1));
        MSTestAssertions.That(() => adapter.HasErrors.Should().BeTrue());
    }

    [TestMethod]
    public void Adapter_SeverityFilter_AppliesMinimumSeverity() {
        var model = new SeverityModel();
        using var all = new ObservableValidationAdapter<SeverityModel>(
            model,
            new SeverityValidator(),
            new ObservableValidationOptions { MinimumSeverity = ValidationErrorSeverity.Info });
        using var warnings = new ObservableValidationAdapter<SeverityModel>(
            model,
            new SeverityValidator(),
            new ObservableValidationOptions { MinimumSeverity = ValidationErrorSeverity.Warning });
        using var errors = new ObservableValidationAdapter<SeverityModel>(
            model,
            new SeverityValidator(),
            new ObservableValidationOptions { MinimumSeverity = ValidationErrorSeverity.Error });

        all.Validate();
        warnings.Validate();
        errors.Validate();

        MSTestAssertions.That(() => all.GetValidationErrors(nameof(SeverityModel.Info)).Count.Should().Be(1));
        MSTestAssertions.That(() => warnings.GetValidationErrors(nameof(SeverityModel.Info)).Count.Should().Be(0));
        MSTestAssertions.That(() => warnings.GetValidationErrors(nameof(SeverityModel.Warning)).Count.Should().Be(1));
        MSTestAssertions.That(() => errors.GetValidationErrors(nameof(SeverityModel.Warning)).Count.Should().Be(0));
        MSTestAssertions.That(() => errors.GetValidationErrors(nameof(SeverityModel.Error)).Count.Should().Be(1));
        MSTestAssertions.That(() => errors.HasErrors.Should().BeTrue());
    }

    [TestMethod]
    public void Adapter_SkipIfValid_SkipsUnchangedValidModel() {
        var model = new CountingModel { Value = "valid" };
        var validator = new CountingValidator();
        using var adapter = new ObservableValidationAdapter<CountingModel>(
            model,
            validator,
            new ObservableValidationOptions { SkipIfValid = true });

        MSTestAssertions.That(() => adapter.Validate().Should().BeTrue());
        MSTestAssertions.That(() => adapter.Validate().Should().BeTrue());
        MSTestAssertions.That(() => validator.Calls.Should().Be(1));

        model.Value = string.Empty;

        MSTestAssertions.That(() => adapter.Validate().Should().BeFalse());
        MSTestAssertions.That(() => validator.Calls.Should().Be(2));
        MSTestAssertions.That(() => adapter.Validate().Should().BeFalse());
        MSTestAssertions.That(() => validator.Calls.Should().Be(3));
    }

    [TestMethod]
    public void Adapter_Options_RejectInvalidConfiguration() {
        var debounceWithoutAsync = () => new ObservableValidationAdapter<CountingModel>(
            new CountingModel(),
            new CountingValidator(),
            new ObservableValidationOptions { PropertyChangedDebounce = TimeSpan.FromMilliseconds(10) });

        var changeTrackingWithoutNotifications = () => new ObservableValidationAdapter<PlainModel>(
            new PlainModel(),
            new PlainModelValidator(),
            new ObservableValidationOptions { ValidateOnPropertyChanged = true });

        var negativeDebounce = () => new ObservableValidationAdapter<CountingModel>(
            new CountingModel(),
            new CountingValidator(),
            new ObservableValidationOptions {
                UseAsyncValidation = true,
                PropertyChangedDebounce = TimeSpan.FromMilliseconds(-1)
            });

        var invalidSeverity = () => new ObservableValidationAdapter<CountingModel>(
            new CountingModel(),
            new CountingValidator(),
            new ObservableValidationOptions { MinimumSeverity = (ValidationErrorSeverity)99 });

        MSTestAssertions.That(() => debounceWithoutAsync.Should().ThrowExactly<ArgumentException>());
        MSTestAssertions.That(() => changeTrackingWithoutNotifications.Should().ThrowExactly<ArgumentException>());
        MSTestAssertions.That(() => negativeDebounce.Should().ThrowExactly<ArgumentOutOfRangeException>());
        MSTestAssertions.That(() => invalidSeverity.Should().ThrowExactly<ArgumentOutOfRangeException>());
    }

    private static ObservableOrder CreateInvalidOrder() => new() {
        CustomerName = string.Empty,
        Address = new ObservableAddress { Street = string.Empty },
        Lines = [new ObservableLine { ProductName = string.Empty }]
    };

    private static string[] Messages<T>(ObservableValidationAdapter<T> adapter, string? propertyName)
        where T : class =>
        [.. adapter.GetErrors(propertyName).Cast<string>()];
}
