//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A custom asynchronous validation delegate that receives the member value, the validation
/// context, the error collector, and the cancellation token. The delegate may add zero or more
/// errors.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The member value type.</typeparam>
/// <param name="value">The member value.</param>
/// <param name="context">The validation context (path, root, shared bag, service provider).</param>
/// <param name="errors">The error collector; add one or more errors for a failure.</param>
/// <param name="cancellationToken">The cancellation token.</param>
public delegate ValueTask CustomValidationAsync<T, TValue>(
    TValue value,
    ValidationContext<T> context,
    ValidationErrorCollector errors,
    CancellationToken cancellationToken);

/// <summary>
/// A rule that delegates asynchronous validation to a user-supplied callback with context access
/// and multi-error emission. It runs on the asynchronous path only.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The member value type.</typeparam>
public sealed class CustomAsyncRule<T, TValue> : AsyncValidateRule<T, TValue> {
    private readonly CustomValidationAsync<T, TValue>? _validation;
    private readonly Func<TValue, IServiceProvider?, CancellationToken, ValueTask<bool>>? _predicate;
    private readonly Func<TValue, IServiceProvider?, CancellationToken, ValueTask<ValidationResult>>? _result;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomAsyncRule{T, TValue}"/> class.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validation">The custom asynchronous validation delegate.</param>
    /// <param name="message">The optional fallback message (used when the delegate throws).</param>
    /// <param name="messageKey">The optional fallback message key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="validation"/> is null.</exception>
    public CustomAsyncRule(
        IPropertyAccessor<T, TValue> accessor,
        CustomValidationAsync<T, TValue> validation,
        string? message = null,
        string? messageKey = null)
        : base(accessor, message, messageKey) {
        ArgumentNullException.ThrowIfNull(validation);

        _validation = validation;
    }

    /// <summary>
    /// Initializes a rule that delegates to an asynchronous predicate. The predicate receives the
    /// value, the optional service provider, and the cancellation token.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="predicate">The predicate; a <see langword="false"/> result adds an error.</param>
    /// <param name="message">The optional fallback message.</param>
    /// <param name="messageKey">The optional message key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    public CustomAsyncRule(
        IPropertyAccessor<T, TValue> accessor,
        Func<TValue, IServiceProvider?, CancellationToken, ValueTask<bool>> predicate,
        string? message = null,
        string? messageKey = null)
        : base(accessor, message, messageKey) {
        ArgumentNullException.ThrowIfNull(predicate);

        _predicate = predicate;
    }

    /// <summary>
    /// Initializes a rule that delegates to an asynchronous result-returning validator.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="resultFactory">The validator returning a <see cref="ValidationResult"/>.</param>
    /// <param name="message">The optional fallback message.</param>
    /// <param name="messageKey">The optional message key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resultFactory"/> is null.</exception>
    public CustomAsyncRule(
        IPropertyAccessor<T, TValue> accessor,
        Func<TValue, IServiceProvider?, CancellationToken, ValueTask<ValidationResult>> resultFactory,
        string? message = null,
        string? messageKey = null)
        : base(accessor, message, messageKey) {
        ArgumentNullException.ThrowIfNull(resultFactory);

        _result = resultFactory;
    }

    /// <inheritdoc />
    protected override async ValueTask ValidateValueAsync(
        TValue value,
        ValidationContext<T> context,
        ValidationErrorCollector errors,
        CancellationToken cancellationToken) {
        try {
            if(_validation is not null) {
                await _validation(value, context, errors, cancellationToken).ConfigureAwait(false);
                return;
            }

            if(_predicate is not null) {
                if(!await _predicate(value, context.ServiceProvider, cancellationToken).ConfigureAwait(false)) {
                    AddError(errors, context);
                }

                return;
            }

            var result = await _result!(value, context.ServiceProvider, cancellationToken).ConfigureAwait(false);
            AddResultErrors(errors, context, result);
        }
        catch(Exception exception) when(exception is not OperationCanceledException) {
            AddDelegateFailure(errors, context, exception);
        }
    }

    private void AddDelegateFailure(ValidationErrorCollector errors, ValidationContext<T> context, Exception exception) =>
        errors.Add(
            ResolveMemberName(context),
            MessageId.Invalid,
            ValidationMessageKeys.INVALID,
            messageParameters: null,
            message: ResolveMessage(context) ??
                     $"The custom validation delegate threw {exception.GetType().Name}.",
            errorCode: ErrorCodeOverride ?? "CustomValidationException",
            severity: SeverityOverride ?? ValidationErrorSeverity.Error,
            customState: CustomState);
}
