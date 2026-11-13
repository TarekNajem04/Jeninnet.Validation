//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Observable;

/// <summary>
/// Adapts a Jeninnet.Validation validator to <see cref="INotifyDataErrorInfo"/> for WPF, MAUI,
/// and Avalonia view models. The adapter is a decorator: the view model delegates the interface
/// members to it instead of inheriting from a base class.
/// </summary>
/// <typeparam name="TModel">The model type.</typeparam>
public sealed class ObservableValidationAdapter<TModel> : INotifyDataErrorInfo, IDisposable
    where TModel : class {
    private readonly IValidator<TModel> _validator;
    private readonly bool _validateOnPropertyChanged;
    private readonly bool _useAsyncValidation;
    private readonly TimeSpan _propertyChangedDebounce;
    private readonly bool _skipIfValid;
    private readonly ValidationErrorSeverity _minimumSeverity;
    private readonly Dictionary<string, List<ValidationError>> _errors = [with(StringComparer.Ordinal)];
    private readonly Dictionary<string, ValidationError[]> _errorsByProperty = [with(StringComparer.Ordinal)];
    private readonly Dictionary<string, string[]> _messagesByProperty = [with(StringComparer.Ordinal)];
    private CancellationTokenSource? _validationCancellation;
    private CancellationTokenSource? _propertyChangedCancellation;
    private Task? _pendingValidation;
    private bool _hasValidated;
    private bool _lastResultValid;
    private bool _isDirty = true;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableValidationAdapter{TModel}"/> class.
    /// </summary>
    /// <param name="model">The model instance.</param>
    /// <param name="validator">The validator.</param>
    /// <param name="options">The optional validation options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> or <paramref name="validator"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="ObservableValidationOptions.PropertyChangedDebounce"/> is negative or
    /// <see cref="ObservableValidationOptions.MinimumSeverity"/> is not a defined severity.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <see cref="ObservableValidationOptions.PropertyChangedDebounce"/> is set without async
    /// validation, or change tracking is enabled on a model that does not implement
    /// <see cref="INotifyPropertyChanged"/>.
    /// </exception>
    public ObservableValidationAdapter(
        TModel model,
        IValidator<TModel> validator,
        ObservableValidationOptions? options = null) {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(validator);

        options ??= new ObservableValidationOptions();

        if(options.PropertyChangedDebounce is { } debounce && debounce < TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                debounce,
                "PropertyChangedDebounce cannot be negative.");
        }

        if(options.PropertyChangedDebounce is not null && !options.UseAsyncValidation) {
            throw new ArgumentException(
                "PropertyChangedDebounce requires UseAsyncValidation to avoid blocking the UI thread.",
                nameof(options));
        }

        if(!Enum.IsDefined(options.MinimumSeverity)) {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.MinimumSeverity,
                "MinimumSeverity must be a defined ValidationErrorSeverity value.");
        }

        var requiresChangeTracking = options.ValidateOnPropertyChanged || options.SkipIfValid;

        if(requiresChangeTracking && model is not INotifyPropertyChanged) {
            throw new ArgumentException(
                "ValidateOnPropertyChanged and SkipIfValid require the model to implement INotifyPropertyChanged.",
                nameof(model));
        }

        Model = model;
        _validator = validator;
        _validateOnPropertyChanged = options.ValidateOnPropertyChanged;
        _useAsyncValidation = options.UseAsyncValidation;
        _propertyChangedDebounce = options.PropertyChangedDebounce ?? TimeSpan.Zero;
        _skipIfValid = options.SkipIfValid;
        _minimumSeverity = options.MinimumSeverity;

        if(requiresChangeTracking) {
            ((INotifyPropertyChanged)model).PropertyChanged += HandlePropertyChanged;
        }
    }

    /// <inheritdoc />
    public bool HasErrors => _errors.Count > 0;

    /// <inheritdoc />
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>
    /// Gets the adapted model.
    /// </summary>
    public TModel Model { get; }

    /// <summary>
    /// Gets the pending asynchronous validation started by a property change, if any.
    /// </summary>
    public Task? PendingValidation => Volatile.Read(ref _pendingValidation);

    /// <summary>
    /// Returns the validation errors for the supplied property path. A path that names a nested
    /// object or collection includes errors from its descendants (for example, <c>Address</c>
    /// includes <c>Address.Street</c>).
    /// </summary>
    /// <param name="propertyName">The property path; <see langword="null"/> returns model-level errors.</param>
    /// <returns>The matching validation errors; empty when the path has no errors.</returns>
    public IReadOnlyList<ValidationError> GetValidationErrors(string? propertyName) {
        var key = Normalize(propertyName);

        return _errorsByProperty.TryGetValue(key, out var errors)
            ? errors
            : [];
    }

    /// <inheritdoc />
    public IEnumerable GetErrors(string? propertyName) {
        var key = Normalize(propertyName);

        return _messagesByProperty.TryGetValue(key, out var messages)
            ? messages
            : [];
    }

    /// <summary>
    /// Validates the model synchronously and raises <see cref="ErrorsChanged"/> for every
    /// affected property path.
    /// </summary>
    /// <returns><see langword="true"/> when the model is valid; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Async validation is enabled; call <see cref="ValidateAsync(CancellationToken)"/> instead.
    /// </exception>
    public bool Validate() {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if(_useAsyncValidation) {
            throw new InvalidOperationException(
                "Asynchronous validation is enabled for this adapter. Call ValidateAsync() instead.");
        }

        CancelPendingValidation();

        if(ShouldSkipValidation()) {
            return true;
        }

        var result = _validator.Validate(Model);
        ApplyResult(result);
        return result.IsValid;
    }

    /// <summary>
    /// Validates the model asynchronously and raises <see cref="ErrorsChanged"/> for every
    /// affected property path. A newer validation supersedes an in-flight one; superseded
    /// results are discarded.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the validation.</param>
    /// <returns><see langword="true"/> when the model is valid; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The adapter has been disposed.</exception>
    public ValueTask<bool> ValidateAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CancelPropertyChangedDelay();

        return ValidateCoreAsync(cancellationToken);
    }

    /// <summary>
    /// Clears all errors and raises <see cref="ErrorsChanged"/> for the previously affected paths.
    /// </summary>
    public void ClearErrors() {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CancelPendingValidation();
        var previous = new HashSet<string>(_errorsByProperty.Keys, StringComparer.Ordinal);
        _errors.Clear();
        _errorsByProperty.Clear();
        _messagesByProperty.Clear();
        _hasValidated = false;
        _lastResultValid = false;
        _isDirty = true;

        foreach(var propertyName in previous) {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        if(_disposed) {
            return;
        }

        _disposed = true;

        if(_validateOnPropertyChanged || _skipIfValid) {
            ((INotifyPropertyChanged)Model).PropertyChanged -= HandlePropertyChanged;
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

            // Do not use ConfigureAwait(false): the adapter is consumed by UI frameworks and
            // errors must be raised on the caller's synchronization context.
            var result = await _validator.ValidateAsync(Model, cts.Token);

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
        var previous = new HashSet<string>(_errorsByProperty.Keys, StringComparer.Ordinal);
        _errors.Clear();
        _errorsByProperty.Clear();
        _messagesByProperty.Clear();

        foreach(var error in result.Errors) {
            if(error.Severity < _minimumSeverity) {
                continue;
            }

            var key = Normalize(error.MemberName);

            if(!_errors.TryGetValue(key, out var errors)) {
                errors = [];
                _errors[key] = errors;
            }

            errors.Add(error);
        }

        var grouped = new Dictionary<string, List<ValidationError>>(StringComparer.Ordinal);

        foreach(var pair in _errors) {
            if(pair.Key.Length == 0) {
                AddRange(grouped, string.Empty, pair.Value);
                continue;
            }

            foreach(var propertyName in EnumerateAncestors(pair.Key)) {
                AddRange(grouped, propertyName, pair.Value);
            }
        }

        foreach(var pair in grouped) {
            var errors = pair.Value.ToArray();
            _errorsByProperty[pair.Key] = errors;
            _messagesByProperty[pair.Key] = [.. errors.Select(static error => error.Message ?? error.MessageKey ?? "Invalid")];
        }

        _hasValidated = true;
        _lastResultValid = result.IsValid;
        _isDirty = false;

        var affected = new HashSet<string>(previous, StringComparer.Ordinal);
        affected.UnionWith(_errorsByProperty.Keys);

        foreach(var propertyName in affected) {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }

    private bool ShouldSkipValidation() =>
        _skipIfValid && _hasValidated && _lastResultValid && !_isDirty;

    private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs args) {
        _isDirty = true;

        if(!_validateOnPropertyChanged) {
            return;
        }

        if(_useAsyncValidation) {
            StartPropertyChangedValidation();
            return;
        }

        _ = Validate();
    }

    private void StartPropertyChangedValidation() {
        var cts = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _propertyChangedCancellation, cts);
        previous?.Cancel();

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Volatile.Write(ref _pendingValidation, completion.Task);
        _ = RunPropertyChangedValidationAsync(cts, completion);
    }

    private async Task RunPropertyChangedValidationAsync(
        CancellationTokenSource cancellation,
        TaskCompletionSource completion) {
        try {
            if(_propertyChangedDebounce > TimeSpan.Zero) {
                await Task.Delay(_propertyChangedDebounce, cancellation.Token);
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
            Interlocked.CompareExchange(ref _propertyChangedCancellation, null, cancellation);
            cancellation.Dispose();

            if(ReferenceEquals(Volatile.Read(ref _pendingValidation), completion.Task)) {
                _ = Interlocked.CompareExchange(ref _pendingValidation, null, completion.Task);
            }
        }
    }

    private void CancelPropertyChangedDelay() {
        var cancellation = Interlocked.Exchange(ref _propertyChangedCancellation, null);
        cancellation?.Cancel();
    }

    private void CancelPendingValidation() {
        CancelPropertyChangedDelay();

        var cancellation = Interlocked.Exchange(ref _validationCancellation, null);
        cancellation?.Cancel();
    }

    private static void AddRange(
        Dictionary<string, List<ValidationError>> grouped,
        string propertyName,
        List<ValidationError> errors) {
        if(!grouped.TryGetValue(propertyName, out var existing)) {
            existing = [];
            grouped[propertyName] = existing;
        }

        existing.AddRange(errors);
    }

    private static IEnumerable<string> EnumerateAncestors(string memberName) {
        yield return memberName;

        var end = memberName.Length;

        while(end > 0) {
            var dot = memberName.LastIndexOf('.', end - 1);
            var bracket = memberName.LastIndexOf('[', end - 1);
            var boundary = Math.Max(dot, bracket);

            if(boundary < 0) {
                yield break;
            }

            if(boundary > 0) {
                yield return memberName[..boundary];
            }

            end = boundary;
        }
    }

    private static string Normalize(string? memberName) =>
        string.IsNullOrWhiteSpace(memberName)
            ? string.Empty
            : memberName.Trim();
}
