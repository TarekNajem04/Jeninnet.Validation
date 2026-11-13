//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Represents a validation rule that applies a nested validator to a property of an object.
/// </summary>
/// <typeparam name="T">The type of the object being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="validator">The sync validator.</param>
/// <param name="asyncValidator">The async validator.</param>
public sealed class NestedValidatorRule<T, TValue>(
    IPropertyAccessor<T, TValue?> accessor,
    ISyncValidator<TValue>? validator,
    IAsyncValidator<TValue>? asyncValidator
) : ISyncValidationRule<T>, IAsyncValidationRule<T>, IValidationMemberRule, IRuleOptionsTarget, IValidationSchemaRuleMetadata {
    private readonly RuleOptionOverrides _options = new();
    private IValidationDescriptor? _nestedDescriptor;

    string? IValidationMemberRule.MemberName => accessor.MemberName;

    string IValidationSchemaRuleMetadata.Kind => "Nested";

    bool IValidationSchemaRuleMetadata.IsCollection => false;

    IValidationDescriptor? IValidationSchemaRuleMetadata.NestedDescriptor =>
        _nestedDescriptor ??= GetNestedDescriptor();

    private IValidationDescriptor? GetNestedDescriptor() =>
        (validator as IValidationDescriptorProvider)?.Describe()
        ?? (asyncValidator as IValidationDescriptorProvider)?.Describe();

    void IRuleOptionsTarget.ApplyOptions(RuleOptionOverrides options) {
        ArgumentNullException.ThrowIfNull(options);
        _options.CopyFrom(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NestedValidatorRule{T, TProperty}"/> class.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validator">The validator to apply.</param>
    public NestedValidatorRule(IPropertyAccessor<T, TValue?> accessor, IValidator<TValue> validator) : this(accessor, validator, validator) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NestedValidatorRule{T, TProperty}"/> class.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validator">The sync validator to apply.</param>
    public NestedValidatorRule(IPropertyAccessor<T, TValue?> accessor, ISyncValidator<TValue> validator) : this(accessor, validator, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NestedValidatorRule{T, TProperty}"/> class.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validator">The async validator to apply.</param>
    public NestedValidatorRule(IPropertyAccessor<T, TValue?> accessor, IAsyncValidator<TValue> validator) : this(accessor, null, validator) { }

    /// <summary>
    /// Validates the property using the nested validator.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="errors">The error collector.</param>
    public void Validate(in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        var value = accessor.GetValue(context.Instance);
        if(value is null || validator is null) {
            return;
        }

        // Create a new context with the updated path
        var childContext = context.ForChild(value, accessor.MemberName);

        if(validator is IValidationContextValidator<TValue> contextValidator) {
            var result = contextValidator.Validate(value, childContext);
            ValidationErrorPath.AddRange(
                errors,
                result,
                childContext.MemberPath,
                _options.HasAny ? _options : null,
                collectionIndex: null,
                context.Instance,
                context);
        } else if(validator is ISyncValidationRule<TValue> internalRule) {
            // Pass the child context to the nested validator, then apply wrapper options once.
            var childErrors = new ValidationErrorCollector();
            internalRule.Validate(childContext, childErrors);
            ValidationErrorPath.AddRange(
                errors,
                ValidationResult.FromErrors(childErrors),
                string.Empty,
                _options.HasAny ? _options : null,
                collectionIndex: null,
                context.Instance,
                context);
        } else {
            var result = validator.Validate(value);
            AddErrors(childContext, result, errors, in context);
        }
    }
    /// <summary>
    /// Validates the property asynchronously using the nested validator.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="errors">The error collector.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask ValidateAsync(
        ValidationContext<T> context,
        ValidationErrorCollector errors,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(errors);
        var value = accessor.GetValue(context.Instance);
        if(value is null) {
            return;
        }

        var childContext = context.ForChild(value, accessor.MemberName);

        if(asyncValidator is IValidationContextValidator<TValue> contextAsyncValidator) {
            var result = await contextAsyncValidator.ValidateAsync(value, childContext, cancellationToken).ConfigureAwait(false);
            ValidationErrorPath.AddRange(
                errors,
                result,
                childContext.MemberPath,
                _options.HasAny ? _options : null,
                collectionIndex: null,
                context.Instance,
                context);
        } else if(asyncValidator is not null) {
            var result = await asyncValidator.ValidateAsync(value, cancellationToken).ConfigureAwait(false);
            AddErrors(childContext, result, errors, in context);
        } else if(validator is IValidationContextValidator<TValue> contextSyncValidator) {
            var result = ValidationContextInvoker.InvokeSync(contextSyncValidator, value, childContext);
            ValidationErrorPath.AddRange(
                errors,
                result,
                childContext.MemberPath,
                _options.HasAny ? _options : null,
                collectionIndex: null,
                context.Instance,
                context);
        } else if(validator is not null) {
            var result = validator.Validate(value);
            AddErrors(childContext, result, errors, in context);
        }
    }

    /// <summary>
    /// Adds errors from the nested validation result to the main error collector.
    /// </summary>
    /// <param name="childContext">The child validation context.</param>
    /// <param name="result">The validation result.</param>
    /// <param name="errors">The error collector.</param>
    /// <param name="parentContext">The parent validation context.</param>
    private void AddErrors(
        ValidationContext<TValue> childContext,
        ValidationResult result,
        ValidationErrorCollector errors,
        in ValidationContext<T> parentContext
    ) {
        if(result.IsValid) {
            return;
        }

        for(var i = 0; i < result.Errors.Count; i++) {
            var error = result.Errors[i];
            var mapped = ValidationErrorPath.WithMemberPath(
                error,
                childContext.GetFullPath(error.MemberName));

            errors.Add(_options.HasAny ? _options.Apply(mapped, parentContext.Instance, parentContext) : mapped);
        }
    }
}
