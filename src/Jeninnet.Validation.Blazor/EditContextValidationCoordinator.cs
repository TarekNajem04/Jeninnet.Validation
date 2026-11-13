//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Blazor;

/// <summary>
/// Runs a Jeninnet.Validation validator on an <see cref="EditContext"/> and maps
/// errors to Blazor field identifiers.
/// </summary>
/// <typeparam name="TModel">The model type.</typeparam>
public sealed class EditContextValidationCoordinator<TModel> : IDisposable
    where TModel : class {
    private readonly EditContext _editContext;
    private readonly IValidator<TModel> _validator;
    private readonly TModel _model;
    private readonly ValidationMessageStore _messages;
    private readonly bool _validateOnFieldChanged;
    private readonly bool _useAsyncValidation;
    private readonly TimeSpan _fieldChangedDebounce;
    private readonly bool _skipIfModelStateValid;
    private CancellationTokenSource? _validationCancellation;
    private CancellationTokenSource? _fieldChangedCancellation;
    private Task? _pendingValidation;
    private bool _hasValidated;
    private bool _lastResultValid;
    private bool _isDirty = true;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditContextValidationCoordinator{TModel}"/> class.
    /// </summary>
    /// <param name="editContext">The Blazor edit context.</param>
    /// <param name="validator">The validator.</param>
    /// <param name="model">The model instance.</param>
    /// <param name="validateOnFieldChanged">Whether to validate when a field changes.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public EditContextValidationCoordinator(
        EditContext editContext,
        IValidator<TModel> validator,
        TModel model,
        bool validateOnFieldChanged = false)
        : this(
            editContext,
            validator,
            model,
            new EditContextValidationOptions { ValidateOnFieldChanged = validateOnFieldChanged }) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EditContextValidationCoordinator{TModel}"/> class.
    /// </summary>
    /// <param name="editContext">The Blazor edit context.</param>
    /// <param name="validator">The validator.</param>
    /// <param name="model">The model instance.</param>
    /// <param name="options">The validation options.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="EditContextValidationOptions.FieldChangedDebounce"/> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <see cref="EditContextValidationOptions.FieldChangedDebounce"/> is set without
    /// <see cref="EditContextValidationOptions.UseAsyncValidation"/>.
    /// </exception>
    public EditContextValidationCoordinator(
        EditContext editContext,
        IValidator<TModel> validator,
        TModel model,
        EditContextValidationOptions options) {
        ArgumentNullException.ThrowIfNull(editContext);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(options);

        if(options.FieldChangedDebounce is { } debounce && debounce < TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                debounce,
                "FieldChangedDebounce cannot be negative.");
        }

        if(options.FieldChangedDebounce is not null && !options.UseAsyncValidation) {
            throw new ArgumentException(
                "FieldChangedDebounce requires UseAsyncValidation to avoid blocking the render thread.",
                nameof(options));
        }

        _editContext = editContext;
        _validator = validator;
        _model = model;
        _validateOnFieldChanged = options.ValidateOnFieldChanged;
        _useAsyncValidation = options.UseAsyncValidation;
        _fieldChangedDebounce = options.FieldChangedDebounce ?? TimeSpan.Zero;
        _skipIfModelStateValid = options.SkipIfModelStateValid;
        _messages = new ValidationMessageStore(editContext);

        editContext.OnValidationRequested += HandleValidationRequested;

        if(_validateOnFieldChanged || _skipIfModelStateValid) {
            editContext.OnFieldChanged += HandleFieldChanged;
        }
    }

    /// <summary>
    /// Gets the pending asynchronous validation started by a field change, if any.
    /// Await it before submitting when field-change validation is enabled.
    /// </summary>
    public Task? PendingValidation => Volatile.Read(ref _pendingValidation);

    /// <summary>
    /// Validates the model synchronously, replaces the messages, and notifies the edit context.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Async validation is enabled; call <see cref="ValidateAsync(CancellationToken)"/> from
    /// <c>EditForm.OnSubmit</c> instead.
    /// </exception>
    public void Validate() {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if(_useAsyncValidation) {
            throw new InvalidOperationException(
                "Asynchronous validation is enabled for this coordinator. " +
                "Call ValidateAsync() from EditForm.OnSubmit instead of EditContext.Validate().");
        }

        CancelPendingValidation();

        if(ShouldSkipValidation()) {
            return;
        }

        ApplyResult(_validator.Validate(_model));
    }

    /// <summary>
    /// Validates the model asynchronously, replaces the messages, and notifies the edit context.
    /// A newer validation supersedes an in-flight one; superseded results are discarded.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the validation.</param>
    /// <returns><see langword="true"/> when the model is valid; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The coordinator has been disposed.</exception>
    public ValueTask<bool> ValidateAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CancelFieldChangeDelay();

        return ValidateCoreAsync(cancellationToken);
    }

    /// <summary>
    /// Clears the messages and notifies the edit context.
    /// </summary>
    public void Clear() {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CancelPendingValidation();
        _messages.Clear();
        _hasValidated = false;
        _lastResultValid = false;
        _isDirty = true;
        _editContext.NotifyValidationStateChanged();
    }

    /// <inheritdoc />
    public void Dispose() {
        if(_disposed) {
            return;
        }

        _disposed = true;
        _editContext.OnValidationRequested -= HandleValidationRequested;

        if(_validateOnFieldChanged || _skipIfModelStateValid) {
            _editContext.OnFieldChanged -= HandleFieldChanged;
        }

        CancelPendingValidation();
    }

    private async ValueTask<bool> ValidateCoreAsync(CancellationToken cancellationToken) {
        if(ShouldSkipValidation()) {
            return true;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var previous = Interlocked.Exchange(ref _validationCancellation, cts);

        try {
            if(previous is not null) {
                await previous.CancelAsync();
            }

            // Do not use ConfigureAwait(false): validation state belongs to the renderer's
            // synchronization context and must be applied on the UI thread.
            var result = await _validator.ValidateAsync(_model, cts.Token);

            if(cts.IsCancellationRequested) {
                return false;
            }

            ApplyResult(result);
            return result.IsValid;
        }
        catch(OperationCanceledException) when(cts.IsCancellationRequested) {
            return false;
        }
        finally {
            Interlocked.CompareExchange(ref _validationCancellation, null, cts);
            cts.Dispose();
        }
    }

    private void ApplyResult(ValidationResult result) {
        _messages.Clear();

        foreach(var error in result.Errors) {
            var field = FieldIdentifierResolver.Resolve(_model, error.MemberName);
            _messages.Add(field, error.Message ?? error.MessageKey ?? "Invalid");
        }

        _hasValidated = true;
        _lastResultValid = result.IsValid;
        _isDirty = false;
        _editContext.NotifyValidationStateChanged();
    }

    private bool ShouldSkipValidation() =>
        _skipIfModelStateValid &&
        _hasValidated &&
        _lastResultValid &&
        !_isDirty &&
        !_editContext.GetValidationMessages().Any();

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs args) => Validate();

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs args) {
        _isDirty = true;

        if(!_validateOnFieldChanged) {
            return;
        }

        if(_useAsyncValidation) {
            StartFieldChangedValidation();
            return;
        }

        Validate();
    }

    private void StartFieldChangedValidation() {
        var cts = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _fieldChangedCancellation, cts);
        previous?.Cancel();

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Volatile.Write(ref _pendingValidation, completion.Task);
        _ = RunFieldChangedValidationAsync(cts, completion);
    }

    private async Task RunFieldChangedValidationAsync(
        CancellationTokenSource cancellation,
        TaskCompletionSource completion) {
        try {
            if(_fieldChangedDebounce > TimeSpan.Zero) {
                await Task.Delay(_fieldChangedDebounce, cancellation.Token);
            }

            await ValidateCoreAsync(cancellation.Token);
            completion.TrySetResult();
        }
        catch(OperationCanceledException) when(cancellation.IsCancellationRequested) {
            completion.TrySetCanceled(cancellation.Token);
        }
        catch(Exception exception) {
            completion.TrySetException(exception);
        }
        finally {
            Interlocked.CompareExchange(ref _fieldChangedCancellation, null, cancellation);
            cancellation.Dispose();

            if(ReferenceEquals(Volatile.Read(ref _pendingValidation), completion.Task)) {
                _ = Interlocked.CompareExchange(ref _pendingValidation, null, completion.Task);
            }
        }
    }

    private void CancelFieldChangeDelay() {
        var cancellation = Interlocked.Exchange(ref _fieldChangedCancellation, null);
        cancellation?.Cancel();
    }

    private void CancelPendingValidation() {
        CancelFieldChangeDelay();

        var cancellation = Interlocked.Exchange(ref _validationCancellation, null);
        cancellation?.Cancel();
    }
}
