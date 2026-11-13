//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A custom synchronous validation delegate that receives the member value, the validation
/// context, and the error collector. The delegate may add zero or more errors.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The member value type.</typeparam>
/// <param name="value">The member value.</param>
/// <param name="context">The validation context (path, root, shared bag, service provider).</param>
/// <param name="errors">The error collector; add one or more errors for a failure.</param>
public delegate void CustomValidation<T, TValue>(
    TValue value,
    ValidationContext<T> context,
    ValidationErrorCollector errors);

/// <summary>
/// A rule that delegates synchronous validation to a user-supplied callback with context access
/// and multi-error emission.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The member value type.</typeparam>
public sealed class CustomRule<T, TValue> : SyncValidateRule<T, TValue> {
    private readonly CustomValidation<T, TValue>? _validation;
    private readonly Func<TValue, IServiceProvider?, bool>? _predicate;
    private readonly Func<TValue, IServiceProvider?, ValidationResult>? _result;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomRule{T, TValue}"/> class.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validation">The custom validation delegate.</param>
    /// <param name="message">The optional fallback message (used when the delegate throws).</param>
    /// <param name="messageKey">The optional fallback message key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="validation"/> is null.</exception>
    public CustomRule(
        IPropertyAccessor<T, TValue> accessor,
        CustomValidation<T, TValue> validation,
        string? message = null,
        string? messageKey = null)
        : base(accessor, message, messageKey) {
        ArgumentNullException.ThrowIfNull(validation);

        _validation = validation;
    }

    /// <summary>
    /// Initializes a rule that delegates to a synchronous predicate. The predicate receives the
    /// value and the optional service provider from the validation context.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="predicate">The predicate; a <see langword="false"/> result adds an error.</param>
    /// <param name="message">The optional fallback message.</param>
    /// <param name="messageKey">The optional message key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    public CustomRule(
        IPropertyAccessor<T, TValue> accessor,
        Func<TValue, IServiceProvider?, bool> predicate,
        string? message = null,
        string? messageKey = null)
        : base(accessor, message, messageKey) {
        ArgumentNullException.ThrowIfNull(predicate);

        _predicate = predicate;
    }

    /// <summary>
    /// Initializes a rule that delegates to a synchronous result-returning validator.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="resultFactory">The validator returning a <see cref="ValidationResult"/>.</param>
    /// <param name="message">The optional fallback message.</param>
    /// <param name="messageKey">The optional message key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resultFactory"/> is null.</exception>
    public CustomRule(
        IPropertyAccessor<T, TValue> accessor,
        Func<TValue, IServiceProvider?, ValidationResult> resultFactory,
        string? message = null,
        string? messageKey = null)
        : base(accessor, message, messageKey) {
        ArgumentNullException.ThrowIfNull(resultFactory);

        _result = resultFactory;
    }

    /// <inheritdoc />
    protected override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        try {
            if(_validation is not null) {
                _validation(value, context, errors);
                return;
            }

            if(_predicate is not null) {
                if(!_predicate(value, context.ServiceProvider)) {
                    AddError(errors, context);
                }

                return;
            }

            AddResultErrors(errors, context, _result!(value, context.ServiceProvider));
        }
        catch(Exception exception) when(exception is not OperationCanceledException) {
            AddDelegateFailure(errors, context, exception);
        }
    }

    private void AddDelegateFailure(ValidationErrorCollector errors, in ValidationContext<T> context, Exception exception) =>
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
