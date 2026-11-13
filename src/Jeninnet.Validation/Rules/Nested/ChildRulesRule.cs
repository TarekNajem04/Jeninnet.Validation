//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Applies inline child validation rules to a nested property.
/// </summary>
/// <typeparam name="T">The parent type.</typeparam>
/// <typeparam name="TValue">The property type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="syncRules">The synchronous child rules.</param>
/// <param name="asyncRules">The asynchronous child rules.</param>
public sealed class ChildRulesRule<T, TValue>(
    IPropertyAccessor<T, TValue?> accessor,
    IReadOnlyList<ISyncValidationRule<TValue>> syncRules,
    IReadOnlyList<IAsyncValidationRule<TValue>> asyncRules
    ) : ISyncValidationRule<T>, IAsyncValidationRule<T>, IValidationMemberRule, IRuleOptionsTarget, IValidationSchemaRuleMetadata, IValidationRuleContainer {
    private readonly IPropertyAccessor<T, TValue?> _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    private readonly IReadOnlyList<ISyncValidationRule<TValue>> _syncRules = syncRules ?? throw new ArgumentNullException(nameof(syncRules));
    private readonly IReadOnlyList<IAsyncValidationRule<TValue>> _asyncRules = asyncRules ?? throw new ArgumentNullException(nameof(asyncRules));
    private readonly RuleOptionOverrides _options = new();
    private IValidationDescriptor? _nestedDescriptor;

    string? IValidationMemberRule.MemberName => _accessor.MemberName;

    string IValidationSchemaRuleMetadata.Kind => "Nested";

    bool IValidationSchemaRuleMetadata.IsCollection => false;

    IValidationDescriptor? IValidationSchemaRuleMetadata.NestedDescriptor =>
        _nestedDescriptor ??= BuildNestedDescriptor();

    private ValidationDescriptor BuildNestedDescriptor() {
        var builder = new ValidationDescriptorBuilder(
            typeof(TValue).FullName ?? nameof(TValue),
            typeof(TValue));

        foreach(var rule in _syncRules) {
            builder.AddRule(rule);
        }

        foreach(var rule in _asyncRules) {
            builder.AddRule(rule);
        }

        return builder.Build();
    }

    void IRuleOptionsTarget.ApplyOptions(RuleOptionOverrides options) {
        ArgumentNullException.ThrowIfNull(options);
        _options.CopyFrom(options);
    }

    /// <inheritdoc />
    public void Validate(in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        var value = _accessor.GetValue(context.Instance);
        if(value is null || _syncRules.Count is 0) {
            return;
        }

        var childContext = context.ForChild(value, _accessor.MemberName);
        var targetErrors = errors;

        if(_options.HasAny) {
            targetErrors = [];
        }

        for(var i = 0; i < _syncRules.Count; i++) {
            _syncRules[i].Validate(in childContext, targetErrors);
        }

        if(_options.HasAny) {
            ValidationErrorPath.AddRange(
                errors,
                ValidationResult.FromErrors(targetErrors),
                string.Empty,
                _options,
                collectionIndex: null,
                context.Instance,
                context);
        }
    }

    /// <inheritdoc />
    public async ValueTask ValidateAsync(
        ValidationContext<T> context,
        ValidationErrorCollector errors,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(errors);
        var value = _accessor.GetValue(context.Instance);
        if(value is null || (_syncRules.Count is 0 && _asyncRules.Count is 0)) {
            return;
        }

        var childContext = context.ForChild(value, _accessor.MemberName);
        var targetErrors = errors;

        if(_options.HasAny) {
            targetErrors = [];
        }

        for(var i = 0; i < _syncRules.Count; i++) {
            _syncRules[i].Validate(in childContext, targetErrors);
        }

        for(var i = 0; i < _asyncRules.Count; i++) {
            await _asyncRules[i].ValidateAsync(childContext, targetErrors, cancellationToken).ConfigureAwait(false);
        }

        if(_options.HasAny) {
            ValidationErrorPath.AddRange(
                errors,
                ValidationResult.FromErrors(targetErrors),
                string.Empty,
                _options,
                collectionIndex: null,
                context.Instance,
                context);
        }
    }

    IEnumerable<object> IValidationRuleContainer.InnerRules {
        get {
            foreach(var rule in _syncRules) {
                yield return rule;
            }

            foreach(var rule in _asyncRules) {
                yield return rule;
            }
        }
    }

    bool IValidationRuleContainer.HasSyncContent => RuleContentInspector.HasSyncContent(_syncRules);

    IReadOnlyDictionary<string, object?>? IValidationRuleContainer.Parameters => null;

    bool IValidationRuleContainer.HasCondition => false;

    string? IValidationRuleContainer.MemberName => _accessor.MemberName;
}
