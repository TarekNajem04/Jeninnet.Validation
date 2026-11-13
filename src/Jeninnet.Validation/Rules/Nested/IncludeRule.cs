//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Includes validation rules from another validator of the same type.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="validator">The sync validator to include.</param>
/// <param name="asyncValidator">The async validator to include.</param>
public sealed class IncludeRule<T>(IValidator<T> validator, IAsyncValidator<T>? asyncValidator) : ISyncValidationRule<T>, IAsyncValidationRule<T> {
    private readonly IValidator<T> _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly IAsyncValidator<T>? _asyncValidator = asyncValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncludeRule{T}"/> class with a combined sync/async validator.
    /// </summary>
    /// <param name="validator">The validator to include.</param>
    public IncludeRule(IValidator<T> validator) : this(validator, validator) { }

    /// <inheritdoc />
    public void Validate(in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(_validator is IValidationContextValidator<T> contextValidator) {
            var result = contextValidator.Validate(context.Instance, context);
            ValidationErrorPath.AddRange(errors, result, context.MemberPath);
        } else if(_validator is ISyncValidationRule<T> syncRule) {
            syncRule.Validate(in context, errors);
        } else {
            var result = _validator.Validate(context.Instance);
            AddErrors(result, errors);
        }
    }

    /// <inheritdoc />
    public async ValueTask ValidateAsync(
        ValidationContext<T> context,
        ValidationErrorCollector errors,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(errors);

        if(_asyncValidator is IValidationContextValidator<T> contextAsyncValidator) {
            var result = await contextAsyncValidator.ValidateAsync(context.Instance, context, cancellationToken).ConfigureAwait(false);
            ValidationErrorPath.AddRange(errors, result, context.MemberPath);
        } else if(_asyncValidator is not null) {
            if(_asyncValidator is IAsyncValidationRule<T> asyncRule) {
                await asyncRule.ValidateAsync(context, errors, cancellationToken).ConfigureAwait(false);
            } else {
                var result = await _asyncValidator.ValidateAsync(context.Instance, cancellationToken).ConfigureAwait(false);
                AddErrors(result, errors);
            }
        } else if(_validator is IValidationContextValidator<T> contextSyncValidator) {
            var result = ValidationContextInvoker.InvokeSync(contextSyncValidator, context.Instance, context);
            ValidationErrorPath.AddRange(errors, result, context.MemberPath);
        } else if(_validator is ISyncValidationRule<T> syncRule) {
            syncRule.Validate(in context, errors);
        } else {
            var result = _validator.Validate(context.Instance);
            AddErrors(result, errors);
        }
    }

    private static void AddErrors(ValidationResult result, ValidationErrorCollector errors) {
        if(result.IsValid) {
            return;
        }

        for(var i = 0; i < result.Errors.Count; i++) {
            errors.Add(result.Errors[i]);
        }
    }
}
