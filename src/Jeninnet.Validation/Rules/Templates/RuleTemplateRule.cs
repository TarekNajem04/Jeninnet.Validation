//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Executes a <see cref="RuleTemplate{TValue}"/> or <see cref="RuleTemplate{T, TValue}"/> against
/// one member. The template is immutable and may be shared by any number of validators.
/// </summary>
/// <typeparam name="T">The model type.</typeparam>
/// <typeparam name="TValue">The member value type.</typeparam>
internal sealed class RuleTemplateRule<T, TValue> :
    SyncValidateRule<T, TValue>,
    IRuleMetadataKindProvider {
    private readonly RuleTemplate<TValue>? _valueTemplate;
    private readonly RuleTemplate<T, TValue>? _instanceTemplate;
    private readonly Func<T, TValue, bool> _predicate;
    private readonly IReadOnlyDictionary<string, object?> _metadataParameters;
    private readonly string _kind;

    public RuleTemplateRule(
        IPropertyAccessor<T, TValue> accessor,
        RuleTemplate<TValue> template)
        : base(accessor, MessageOf(template), MessageKeyOf(template)) {
        _valueTemplate = template;
        _predicate = (_, value) => template.Predicate(value);
        _metadataParameters = template.MetadataParameters;
        _kind = template.Name;
        DefaultSeverity = template.Severity;
        ErrorCode = template.ErrorCode;
    }

    public RuleTemplateRule(
        IPropertyAccessor<T, TValue> accessor,
        RuleTemplate<T, TValue> template)
        : base(accessor, MessageOf(template), MessageKeyOf(template)) {
        _instanceTemplate = template;
        _predicate = template.Predicate;
        _metadataParameters = template.MetadataParameters;
        _kind = template.Name;
        DefaultSeverity = template.Severity;
        ErrorCode = template.ErrorCode;
    }

    protected override string? ErrorCode { get; }

    private protected override ValidationErrorSeverity DefaultSeverity { get; }

    string IRuleMetadataKindProvider.Kind => _kind;

    protected override void ValidateValue(
        TValue value,
        in ValidationContext<T> context,
        ValidationErrorCollector errors) {
        if(!_predicate(context.Instance, value)) {
            AddError(errors, context);
        }
    }

    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) {
        var parameters = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach(var parameter in _metadataParameters.Where(static parameter => parameter.Value is not null)) {
            parameters[parameter.Key] = parameter.Value!;
        }

        var value = Accessor.GetValue(context.Instance);

        if(_valueTemplate is not null) {
            foreach(var parameter in _valueTemplate.DynamicParameters) {
                var parameterValue = parameter.Value(value);

                if(parameterValue is not null) {
                    parameters[parameter.Key] = parameterValue;
                }
            }
        } else if(_instanceTemplate is not null) {
            foreach(var parameter in _instanceTemplate.DynamicParameters) {
                var parameterValue = parameter.Value(context.Instance, value);

                if(parameterValue is not null) {
                    parameters[parameter.Key] = parameterValue;
                }
            }
        }

        return parameters.Count == 0 ? null : parameters;
    }

    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() => _metadataParameters;

    private static string? MessageOf(RuleTemplate template) {
        ArgumentNullException.ThrowIfNull(template);
        return template.Message;
    }

    private static string? MessageKeyOf(RuleTemplate template) {
        ArgumentNullException.ThrowIfNull(template);
        return template.MessageKey;
    }
}
