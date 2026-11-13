//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Applies a child validator to each element of a collection.
/// </summary>
/// <typeparam name="T">The parent type.</typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="syncValidator">The synchronous validator.</param>
/// <param name="asyncValidator">The asynchronous validator.</param>
public sealed class ForEachRule<T, TElement>(
    IPropertyAccessor<T, IEnumerable<TElement>?> accessor,
    ISyncValidator<TElement>? syncValidator,
    IAsyncValidator<TElement>? asyncValidator
) : ISyncValidationRule<T>, IAsyncValidationRule<T>, IValidationMemberRule, IRuleOptionsTarget, IValidationSchemaRuleMetadata {
    private readonly RuleOptionOverrides _options = new();
    private Func<TElement, bool>? _filter;
    private Func<T, int, string>? _indexer;
    private IValidationDescriptor? _nestedDescriptor;

    string? IValidationMemberRule.MemberName => accessor.MemberName;

    string IValidationSchemaRuleMetadata.Kind => "ForEach";

    bool IValidationSchemaRuleMetadata.IsCollection => true;

    IValidationDescriptor? IValidationSchemaRuleMetadata.NestedDescriptor =>
        _nestedDescriptor ??= GetNestedDescriptor();

    private IValidationDescriptor? GetNestedDescriptor() =>
        (syncValidator as IValidationDescriptorProvider)?.Describe()
        ?? (asyncValidator as IValidationDescriptorProvider)?.Describe();

    void IRuleOptionsTarget.ApplyOptions(RuleOptionOverrides options) {
        ArgumentNullException.ThrowIfNull(options);
        _options.CopyFrom(options);
    }

    private Func<TElement, CancellationToken, ValueTask<bool>>? _asyncFilter;
    /// <summary>
    /// Initializes a new instance of the <see cref="ForEachRule{T, TElement}"/> class with the specified property accessor and validators.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validator">The validator.</param>
    public ForEachRule(IPropertyAccessor<T, IEnumerable<TElement>?> accessor, ISyncValidator<TElement> validator) : this(accessor, validator, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForEachRule{T, TElement}"/> class with the specified property accessor and validators.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validator">The validator.</param>
    public ForEachRule(IPropertyAccessor<T, IEnumerable<TElement>?> accessor, IAsyncValidator<TElement> validator) : this(accessor, null, validator) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForEachRule{T, TElement}"/> class with the specified property accessor and validators.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validator">The validator.</param>
    /// <param name="asyncValidator">The asynchronous validator.</param>
    public ForEachRule(IPropertyAccessor<T, ICollection<TElement>?> accessor, ISyncValidator<TElement>? validator, IAsyncValidator<TElement>? asyncValidator)
        : this(GetAccessor(accessor), validator, asyncValidator) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForEachRule{T, TElement}"/> class with the specified property accessor and validator.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validator">The validator.</param>
    public ForEachRule(IPropertyAccessor<T, ICollection<TElement>?> accessor, ISyncValidator<TElement> validator)
        : this(accessor, validator, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForEachRule{T, TElement}"/> class with the specified property accessor and validator.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validator">The validator.</param>
    public ForEachRule(IPropertyAccessor<T, ICollection<TElement>?> accessor, IAsyncValidator<TElement> validator)
        : this(accessor, null, validator) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForEachRule{T, TElement}"/> class with the specified property accessor and validators.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="syncValidator">The synchronous validator.</param>
    /// <param name="asyncValidator">The asynchronous validator.</param>
    public ForEachRule(IPropertyAccessor<T, IList<TElement>?> accessor, ISyncValidator<TElement>? syncValidator, IAsyncValidator<TElement>? asyncValidator)
        : this(GetAccessor(accessor), syncValidator, asyncValidator) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForEachRule{T, TElement}"/> class with the specified property accessor and validator.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validator">The validator.</param>
    public ForEachRule(IPropertyAccessor<T, IList<TElement>?> accessor, ISyncValidator<TElement> validator)
        : this(accessor, validator, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForEachRule{T, TElement}"/> class with the specified property accessor and validator.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validator">The validator.</param>
    public ForEachRule(IPropertyAccessor<T, IList<TElement>?> accessor, IAsyncValidator<TElement> validator)
        : this(accessor, null, validator) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForEachRule{T, TElement}"/> class with the specified property accessor and validators.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="syncValidator">The synchronous validator.</param>
    /// <param name="asyncValidator">The asynchronous validator.</param>
    public ForEachRule(IPropertyAccessor<T, List<TElement>?> accessor, ISyncValidator<TElement>? syncValidator, IAsyncValidator<TElement>? asyncValidator)
        : this(GetAccessor(accessor), syncValidator, asyncValidator) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForEachRule{T, TElement}"/> class with the specified property accessor and validator.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validator">The validator.</param>
    public ForEachRule(IPropertyAccessor<T, List<TElement>?> accessor, ISyncValidator<TElement> validator)
        : this(accessor, validator, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForEachRule{T, TElement}"/> class with the specified property accessor and validator.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="validator">The validator.</param>
    public ForEachRule(IPropertyAccessor<T, List<TElement>?> accessor, IAsyncValidator<TElement> validator)
        : this(accessor, null, validator) { }

    /// <summary>
    /// Adds a synchronous element filter. Only elements satisfying the predicate reach the child
    /// validator; original indices are preserved. The filter runs on both validation paths.
    /// </summary>
    /// <param name="predicate">The element predicate.</param>
    /// <returns>The same rule for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    public ForEachRule<T, TElement> Where(Func<TElement, bool> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);

        _filter = predicate;
        return this;
    }

    /// <summary>
    /// Adds an asynchronous element filter. Only elements satisfying the predicate reach the child
    /// validator; original indices are preserved. It runs on the asynchronous path only; when a
    /// synchronous filter is also configured, both must pass.
    /// </summary>
    /// <param name="predicate">The asynchronous element predicate.</param>
    /// <returns>The same rule for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    public ForEachRule<T, TElement> WhereAsync(Func<TElement, CancellationToken, ValueTask<bool>> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);

        _asyncFilter = predicate;
        return this;
    }

    /// <summary>
    /// Overrides the indexer text used in element error paths.
    /// </summary>
    /// <param name="indexer">
    /// A delegate receiving the parent instance and original element index; it returns the text
    /// inserted inside the brackets. The original index is still reported as
    /// <c>CollectionIndex</c>.
    /// </param>
    /// <returns>The same rule for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="indexer"/> is null.</exception>
    public ForEachRule<T, TElement> OverrideIndexer(Func<T, int, string> indexer) {
        ArgumentNullException.ThrowIfNull(indexer);

        _indexer = indexer;
        return this;
    }

    /// <summary>
    /// Validates each element of the collection using the provided synchronous validator.
    /// If the collection is null, validation is skipped.
    /// If the synchronous validator is null, validation is also skipped.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="errors">The error collector.</param>
    public void Validate(in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        var collection = accessor.Getter(context.Instance);
        if(collection is null || syncValidator is null) {
            return;
        }

        var index = 0;
        foreach(var element in collection) {
            var currentIndex = index++;

            if(ShouldSkipElement(element)) {
                continue;
            }

            // DIVING: Use ForElement to get "Collection[0]"
            var elementContext = CreateElementContext(in context, element, currentIndex);
            ValidateElement(element, elementContext, errors, currentIndex, in context);
        }
    }

    /// <summary>
    /// Validates each element of the collection using the provided asynchronous validator.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="errors">The error collector.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask ValidateAsync(ValidationContext<T> context, ValidationErrorCollector errors, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(errors);
        var collection = accessor.Getter(context.Instance);
        if(collection is null || (syncValidator is null && asyncValidator is null)) {
            return;
        }

        var index = 0;
        foreach(var element in collection) {
            var currentIndex = index++;

            if(ShouldSkipElement(element)) {
                continue;
            }

            if(_asyncFilter is not null && !await _asyncFilter(element, cancellationToken).ConfigureAwait(false)) {
                continue;
            }

            var childContext = CreateElementContext(in context, element, currentIndex);
            await ValidateElementAsync(
                element,
                childContext,
                errors,
                currentIndex,
                context,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private bool ShouldSkipElement(TElement element) =>
        element is null || (_filter is not null && !_filter(element));

    private void ValidateElement(
        TElement element,
        ValidationContext<TElement> elementContext,
        ValidationErrorCollector errors,
        int currentIndex,
        in ValidationContext<T> context) {
        if(syncValidator is IValidationContextValidator<TElement> contextValidator) {
            var contextResult = contextValidator.Validate(element, elementContext);
            AddContextErrors(errors, contextResult, elementContext, currentIndex, in context);
            return;
        }

        if(syncValidator is ISyncValidationRule<TElement> internalRule) {
            var elementErrors = new ValidationErrorCollector();
            internalRule.Validate(elementContext, elementErrors);
            ValidationErrorPath.AddRange(
                errors,
                ValidationResult.FromErrors(elementErrors),
                string.Empty,
                _options.HasAny ? _options : null,
                currentIndex,
                context.Instance,
                context);
            return;
        }

        AddErrors(elementContext, syncValidator!.Validate(element), errors, currentIndex, in context);
    }

    private async ValueTask ValidateElementAsync(
        TElement element,
        ValidationContext<TElement> childContext,
        ValidationErrorCollector errors,
        int currentIndex,
        ValidationContext<T> context,
        CancellationToken cancellationToken) {
        if(asyncValidator is IValidationContextValidator<TElement> contextAsyncValidator) {
            var contextResult = await contextAsyncValidator.ValidateAsync(element, childContext, cancellationToken).ConfigureAwait(false);
            AddContextErrors(errors, contextResult, childContext, currentIndex, in context);
        } else if(asyncValidator is not null) {
            var result = await asyncValidator.ValidateAsync(element, cancellationToken).ConfigureAwait(false);
            AddErrors(childContext, result, errors, currentIndex, in context);
        } else if(syncValidator is IValidationContextValidator<TElement> contextSyncValidator) {
            var contextResult = ValidationContextInvoker.InvokeSync(contextSyncValidator, element, childContext);
            AddContextErrors(errors, contextResult, childContext, currentIndex, in context);
        } else if(syncValidator is not null) {
            AddErrors(childContext, syncValidator.Validate(element), errors, currentIndex, in context);
        }
    }

    private void AddContextErrors(
        ValidationErrorCollector errors,
        ValidationResult result,
        ValidationContext<TElement> childContext,
        int currentIndex,
        in ValidationContext<T> context) =>
        ValidationErrorPath.AddRange(
            errors,
            result,
            childContext.MemberPath,
            _options.HasAny ? _options : null,
            currentIndex,
            context.Instance,
            context);

    private ValidationContext<TElement> CreateElementContext(
        in ValidationContext<T> context,
        TElement element,
        int index) =>
        _indexer is null
            ? context.ForElement(element, accessor.MemberName, index)
            : context.ForElement(element, accessor.MemberName, _indexer(context.Instance, index));

    /// <summary>
    /// Converts a property accessor for a collection type to a property accessor for an enumerable type.
    /// </summary>
    /// <typeparam name="TAccessor">The type of the property accessor.</typeparam>
    /// <param name="accessor">The property accessor to convert.</param>
    /// <returns>The converted property accessor.</returns>
    private static IPropertyAccessor<T, IEnumerable<TElement>?> GetAccessor<TAccessor>(TAccessor accessor) where TAccessor : IPropertyAccessor<T, ICollection<TElement>?> {
        ArgumentNullException.ThrowIfNull(accessor);
        return ValidatorFactory.Accessor<T, IEnumerable<TElement>?>(x => accessor.Getter(x), accessor.MemberName);
    }

    private void AddErrors(
        ValidationContext<TElement> childContext,
        ValidationResult result,
        ValidationErrorCollector errors,
        int collectionIndex,
        in ValidationContext<T> parentContext
    ) {
        if(result.IsValid) {
            return;
        }

        for(var i = 0; i < result.Errors.Count; i++) {

            var error = result.Errors[i];
            var memberName = GetMemberName(childContext, error);
            var parameters = error.MessageParameters is null
                ? []
                : new Dictionary<string, object>(error.MessageParameters);

            if(!parameters.ContainsKey(MessageParameterNames.CollectionIndex)) {
                parameters[MessageParameterNames.CollectionIndex] = collectionIndex;
            }

            var mapped = ValidationErrorPath.WithMemberPath(
                new ValidationError(
                    memberName,
                    error.MessageId,
                    messageKey: error.MessageKey,
                    messageParameters: parameters,
                    message: error.Message,
                    errorCode: error.ErrorCode,
                    severity: error.Severity,
                    customState: error.CustomState) {
                    MessageIsAuthored = error.MessageIsAuthored
                },
                memberName);

            errors.Add(_options.HasAny ? _options.Apply(mapped, parentContext.Instance, parentContext) : mapped);
        }
    }

    /// <summary>
    /// Gets the full member name for a validation error, combining the child context's member path with the error's member name if it exists.
    /// </summary>
    /// <param name="childContext">The validation context for the child element.</param>
    /// <param name="error">The validation error.</param>
    /// <returns>The full member name.</returns>
    private static string GetMemberName(ValidationContext<TElement> childContext, ValidationError error) =>
        string.IsNullOrEmpty(error.MemberName)
            ? childContext.MemberPath
            : $"{childContext.MemberPath}.{error.MemberName}";
}
