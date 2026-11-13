//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Applies one or more value rules directly to each element of a collection. Unlike a child
/// validator, element rules run for <see langword="null"/> elements so a <c>NotNull</c> rule can
/// report missing entries.
/// </summary>
/// <typeparam name="T">The parent type.</typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
/// <param name="accessor">The collection accessor.</param>
/// <param name="syncRules">The synchronous element rules.</param>
/// <param name="asyncRules">The asynchronous element rules.</param>
/// <param name="cascadeMode">The cascade mode used between element rules for one element.</param>
public sealed class ForEachElementRule<T, TElement>(
    IPropertyAccessor<T, IEnumerable<TElement>?> accessor,
    IReadOnlyList<ISyncValidationRule<TElement>> syncRules,
    IReadOnlyList<IAsyncValidationRule<TElement>> asyncRules,
    CascadeMode? cascadeMode = null
) : ISyncValidationRule<T>, IAsyncValidationRule<T>, IValidationMemberRule, IRuleOptionsTarget {
    private readonly ISyncValidationRule<TElement>[] _syncRules = [.. syncRules];
    private readonly IAsyncValidationRule<TElement>[] _asyncRules = [.. asyncRules];
    private readonly RuleOptionOverrides _options = new();
    private Func<TElement, bool>? _filter;
    private Func<TElement, CancellationToken, ValueTask<bool>>? _asyncFilter;
    private Func<T, int, string>? _indexer;

    string? IValidationMemberRule.MemberName => accessor.MemberName;

    void IRuleOptionsTarget.ApplyOptions(RuleOptionOverrides options) {
        ArgumentNullException.ThrowIfNull(options);
        _options.CopyFrom(options);
    }

    /// <summary>
    /// Filters the elements validated by this rule. Original indexes are preserved.
    /// </summary>
    /// <param name="predicate">The element predicate.</param>
    /// <returns>The same rule for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    public ForEachElementRule<T, TElement> Where(Func<TElement, bool> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);

        _filter = predicate;
        return this;
    }

    /// <summary>
    /// Filters the elements validated on the asynchronous path. Original indexes are preserved.
    /// </summary>
    /// <param name="predicate">The asynchronous element predicate.</param>
    /// <returns>The same rule for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    public ForEachElementRule<T, TElement> WhereAsync(
        Func<TElement, CancellationToken, ValueTask<bool>> predicate) {
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
    public ForEachElementRule<T, TElement> OverrideIndexer(Func<T, int, string> indexer) {
        ArgumentNullException.ThrowIfNull(indexer);

        _indexer = indexer;
        return this;
    }

    /// <inheritdoc />
    public void Validate(in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        var collection = accessor.Getter(context.Instance);
        if(collection is null || _syncRules.Length is 0) {
            return;
        }

        var index = 0;
        var elementErrors = new ValidationErrorCollector();

        foreach(var element in collection) {
            var currentIndex = index++;

            if(_filter is not null && !_filter(element)) {
                continue;
            }

            elementErrors.Clear();
            var elementContext = CreateElementContext(in context, element, currentIndex);
            var effectiveCascadeMode = ResolveCascadeMode(in context);
            const int startCount = 0;

            for(var i = 0; i < _syncRules.Length; i++) {
                _syncRules[i].Validate(in elementContext, elementErrors);

                if(effectiveCascadeMode is CascadeMode.Stop && elementErrors.Count > startCount) {
                    break;
                }
            }

            AddElementErrors(errors, elementErrors, currentIndex, in context);
        }
    }

    /// <inheritdoc />
    public async ValueTask ValidateAsync(
        ValidationContext<T> context,
        ValidationErrorCollector errors,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(errors);

        var collection = accessor.Getter(context.Instance);
        if(collection is null || (_syncRules.Length is 0 && _asyncRules.Length is 0)) {
            return;
        }

        var index = 0;
        var elementErrors = new ValidationErrorCollector();

        foreach(var element in collection) {
            var currentIndex = index++;

            if(_filter is not null && !_filter(element)) {
                continue;
            }

            if(_asyncFilter is not null &&
               !await _asyncFilter(element, cancellationToken).ConfigureAwait(false)) {
                continue;
            }

            elementErrors.Clear();
            var elementContext = CreateElementContext(in context, element, currentIndex);
            var effectiveCascadeMode = ResolveCascadeMode(in context);

            await ValidateElementAsync(
                elementContext,
                elementErrors,
                effectiveCascadeMode,
                cancellationToken).ConfigureAwait(false);

            AddElementErrors(errors, elementErrors, currentIndex, in context);
        }
    }

    private async ValueTask ValidateElementAsync(
        ValidationContext<TElement> elementContext,
        ValidationErrorCollector elementErrors,
        CascadeMode effectiveCascadeMode,
        CancellationToken cancellationToken) {
        ValidateSyncRules(elementContext, elementErrors, effectiveCascadeMode);

        if(effectiveCascadeMode is CascadeMode.Stop && elementErrors.Count > 0) {
            return;
        }

        for(var i = 0; i < _asyncRules.Length; i++) {
            await _asyncRules[i].ValidateAsync(elementContext, elementErrors, cancellationToken)
                               .ConfigureAwait(false);

            if(effectiveCascadeMode is CascadeMode.Stop && elementErrors.Count > 0) {
                break;
            }
        }
    }

    private void ValidateSyncRules(
        ValidationContext<TElement> elementContext,
        ValidationErrorCollector elementErrors,
        CascadeMode effectiveCascadeMode) {
        for(var i = 0; i < _syncRules.Length; i++) {
            _syncRules[i].Validate(in elementContext, elementErrors);

            if(effectiveCascadeMode is CascadeMode.Stop && elementErrors.Count > 0) {
                break;
            }
        }
    }

    private CascadeMode ResolveCascadeMode(in ValidationContext<T> context) =>
        cascadeMode ??
        context.Options?.DefaultRuleLevelCascadeMode ??
        ValidationConfiguration.Options.DefaultRuleLevelCascadeMode;

    private ValidationContext<TElement> CreateElementContext(
        in ValidationContext<T> context,
        TElement element,
        int index) =>
        _indexer is null
            ? context.ForElement(element, accessor.MemberName, index)
            : context.ForElement(element, accessor.MemberName, _indexer(context.Instance, index));

    private void AddElementErrors(
        ValidationErrorCollector errors,
        ValidationErrorCollector elementErrors,
        int collectionIndex,
        in ValidationContext<T> context) {
        if(elementErrors.Count is 0) {
            return;
        }

        ValidationErrorPath.AddRange(
            errors,
            ValidationResult.FromErrors(elementErrors),
            string.Empty,
            _options.HasAny ? _options : null,
            collectionIndex,
            context.Instance,
            context);
    }
}
