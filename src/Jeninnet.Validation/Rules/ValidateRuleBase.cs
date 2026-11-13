//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Provides the metadata, accessor, and error-emission behavior shared by
/// synchronous and asynchronous validation rules.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <param name="accessor">The property accessor to extract the value.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The optional message key.</param>
public abstract class ValidateRuleBase<T, TProperty>(
    IPropertyAccessor<T, TProperty> accessor,
    string? message = null,
    string? messageKey = null) : IRuleMetadataProvider,
    IValidationMemberRule,
    IRuleOptionsTarget,
    IRuleDefinition,
    IRuleDefinitionOptions {
    private readonly RuleOptionOverrides _options = new();

    /// <summary>
    /// Gets or sets an optional custom state attached to emitted errors.
    /// </summary>
    public object? CustomState { get; set; }

    /// <summary>
    /// Gets the property accessor used to extract the value being validated.
    /// </summary>
    protected IPropertyAccessor<T, TProperty> Accessor { get; } = accessor;

    /// <summary>
    /// Gets the optional custom error message.
    /// </summary>
    protected string? Message { get; } = message;

    /// <summary>
    /// Gets the optional message key used for localized message lookup.
    /// </summary>
    protected string? MessageKey { get; } = messageKey;

    /// <summary>
    /// Gets the optional programmatic error code.
    /// </summary>
    protected virtual string? ErrorCode => null;

    /// <summary>
    /// Gets the default severity used when no per-rule override is configured. The default is
    /// <see cref="ValidationErrorSeverity.Error"/>.
    /// </summary>
    private protected virtual ValidationErrorSeverity DefaultSeverity => ValidationErrorSeverity.Error;

    /// <summary>
    /// Gets or sets a custom message that overrides the rule's default message.
    /// </summary>
    public string? MessageOverride { get; set; }

    /// <summary>
    /// Gets or sets a custom error code that overrides the rule's default error code.
    /// </summary>
    public string? ErrorCodeOverride { get; set; }

    /// <summary>
    /// Gets or sets a severity that overrides the default error severity.
    /// </summary>
    public ValidationErrorSeverity? SeverityOverride { get; set; }

    /// <summary>
    /// Gets or sets a member name that overrides the default member name from the accessor.
    /// </summary>
    public string? OverriddenMemberName { get; set; }

    /// <summary>
    /// Gets or sets a display name used for the <c>{PropertyName}</c> message parameter without
    /// changing the code-facing member name.
    /// </summary>
    public string? DisplayNameOverride { get; set; }

    /// <summary>
    /// Adds a validation error to the collector using the rule's metadata.
    /// </summary>
    /// <param name="errors">The validation error collector.</param>
    /// <param name="context">The validation context.</param>
    protected void AddError(ValidationErrorCollector errors, in ValidationContext<T> context) {
        ArgumentNullException.ThrowIfNull(errors);

        var memberName = ResolveMemberName(context);
        var displayName = ResolveDisplayNameWithFallback(in context, memberName);
        var value = Accessor.GetValue(context.Instance);
        var parameters = EnrichParameters(
            GetMessageParameters(context),
            memberName,
            displayName,
            value);
        var resolvedMessage = ResolveMessage(context);

        errors.Add(new ValidationError(
            memberName,
            MessageKey is not null ? new MessageId(MessageKey) : MessageId.Invalid,
            MessageKey,
            parameters,
            resolvedMessage,
            ErrorCodeOverride ?? ErrorCode,
            ResolveSeverity(),
            ResolveCustomState(in context, value)) {
            MessageIsAuthored = resolvedMessage is not null
        });
    }

    private object? ResolveCustomState(in ValidationContext<T> context, TProperty value) =>
        _options.CustomStateFactory?.Create(context.Instance!, value, context) ?? CustomState;

    /// <summary>
    /// Adds errors from a custom validation result. Errors that do not carry a member name are
    /// attributed to this rule's member; errors with an explicit path are preserved.
    /// </summary>
    /// <param name="errors">The validation error collector.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="result">The custom validation result.</param>
    protected void AddResultErrors(
        ValidationErrorCollector errors,
        in ValidationContext<T> context,
        ValidationResult result) {
        ArgumentNullException.ThrowIfNull(errors);

        if(result.IsValid) {
            return;
        }

        var memberName = ResolveMemberName(context);
        var displayName = ResolveDisplayNameWithFallback(in context, memberName);
        var resolvedMessage = ResolveMessage(context);
        var value = Accessor.GetValue(context.Instance);
        var customState = ResolveCustomState(in context, value);

        foreach(var error in result.Errors) {
            var errorMemberName = string.IsNullOrEmpty(error.MemberName) ? memberName : error.MemberName;
            var effectiveDisplayName = string.Equals(errorMemberName, memberName, StringComparison.Ordinal)
                ? displayName
                : null;

            var authoredMessage = error.Message ?? resolvedMessage;

            errors.Add(new ValidationError(
                errorMemberName,
                error.MessageId,
                error.MessageKey,
                EnrichParameters(error.MessageParameters, errorMemberName, effectiveDisplayName, value),
                authoredMessage,
                error.ErrorCode ?? ErrorCodeOverride,
                error.Severity,
                error.CustomState ?? customState) {
                MessageIsAuthored = error.MessageIsAuthored || error.Message is not null || resolvedMessage is not null
            });
        }
    }

    /// <summary>
    /// Resolves the member name using the dynamic override, the static override, or the accessor.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <returns>The resolved member name.</returns>
    protected string ResolveMemberName(in ValidationContext<T> context) =>
        _options.ResolveMemberName(context.Instance) ??
        OverriddenMemberName ??
        PropertyNameResolver.Resolve(
            typeof(T),
            context.PathFor(Accessor),
            context.Options ?? ValidationConfiguration.Options);

    /// <summary>
    /// Resolves a dynamic or static message for the current instance.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <returns>The resolved message, or the rule's default message when no override applies.</returns>
    protected string? ResolveMessage(in ValidationContext<T> context) =>
        _options.ResolveMessage(context.Instance, context) ?? MessageOverride ?? Message;

    /// <summary>
    /// Resolves a dynamic display name for the current instance.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <returns>The display name, or <see langword="null"/> when no dynamic name applies.</returns>
    protected string? ResolveDisplayName(in ValidationContext<T> context) =>
        _options.ResolveDisplayName(context.Instance);

    private string? ResolveDisplayNameWithFallback(in ValidationContext<T> context, string memberName) {
        var displayName = ResolveDisplayName(in context);

        if(displayName is null && OverriddenMemberName is not null) {
            displayName = OverriddenMemberName;
        }

        displayName ??= DisplayNameResolver.ResolveOrNull(
            typeof(T),
            memberName,
            context.Instance,
            context.Options ?? ValidationConfiguration.Options);

        if(displayName is null && DisplayNameOverride is not null) {
            displayName = DisplayNameOverride;
        }

        return displayName;
    }

    private static Dictionary<string, object> EnrichParameters(
        IDictionary<string, object>? parameters,
        string memberName,
        string? displayName,
        object? value) {
        var enriched = parameters is null
            ? [with(StringComparer.Ordinal)]
            : new Dictionary<string, object>(parameters, StringComparer.Ordinal);

        enriched[MessageParameterNames.PropertyName] = displayName ?? memberName;
        enriched[MessageParameterNames.PropertyPath] = memberName;

        if(!enriched.ContainsKey(MessageParameterNames.PropertyValue)) {
            enriched[MessageParameterNames.PropertyValue] = value!;
        }

        return enriched;
    }

    /// <summary>
    /// Gets the parameters used to format the validation message.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <returns>The formatting parameters.</returns>
    protected virtual IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) => null;

    string? IValidationMemberRule.MemberName => Accessor.MemberName;

    void IRuleOptionsTarget.ApplyOptions(RuleOptionOverrides options) {
        ArgumentNullException.ThrowIfNull(options);

        _options.CopyFrom(options);

        if(options.HasMessage) {
            MessageOverride = options.Message;

            if(options.MessageFactory is null) {
                _options.MessageFactory = null;
            }
        }

        if(options.HasErrorCode) {
            ErrorCodeOverride = options.ErrorCode;
        }

        if(options.HasSeverity) {
            SeverityOverride = options.Severity;
        }

        if(options.HasCustomState) {
            CustomState = options.CustomState;
        }

        if(options.CustomStateFactory is not null) {
            _options.CustomStateFactory = options.CustomStateFactory;
            CustomState = null;
        }

        if(options.HasDisplayName) {
            DisplayNameOverride = options.DisplayName;
        }

        if(options.HasMemberName) {
            OverriddenMemberName = options.MemberName;

            if(options.MemberNameFactory is null) {
                _options.MemberNameFactory = null;
                _options.DisplayNameFactory = null;
            }
        }

        if(options.DisplayNameFactory is not null) {
            OverriddenMemberName = null;
            _options.MemberNameFactory = null;
        }

        if(options.MemberNameFactory is not null) {
            _options.DisplayNameFactory = null;
        }
    }

    private ValidationErrorSeverity ResolveSeverity() => SeverityOverride ?? DefaultSeverity;

    string? IRuleDefinition.ErrorMessage => MessageOverride ?? Message;

    string? IRuleDefinitionOptions.ErrorMessage {
        get => MessageOverride ?? Message;
        set => MessageOverride = value;
    }

    string? IRuleDefinitionOptions.ErrorCode {
        get => ErrorCodeOverride ?? ErrorCode;
        set => ErrorCodeOverride = value;
    }

    ValidationErrorSeverity IRuleDefinitionOptions.Severity {
        get => ResolveSeverity();
        set => SeverityOverride = value;
    }

    string IRuleMetadataProvider.Kind => RuleMetadataKind.FromType(GetType());

    string? IRuleMetadataProvider.MessageKey => MessageKey;

    bool IRuleMetadataProvider.MessageIsAuthored =>
        Message is not null ||
        MessageOverride is not null ||
        _options.HasMessage ||
        _options.MessageFactory is not null;

    ValidationErrorSeverity IRuleMetadataProvider.Severity => ResolveSeverity();

    string? IRuleMetadataProvider.ErrorCode => ErrorCodeOverride ?? ErrorCode;

    string? IRuleMetadataProvider.DisplayName => DisplayNameOverride;

    object? IRuleMetadataProvider.CustomState => CustomState;

    bool IRuleMetadataProvider.HasCondition => false;

    Type? IRuleMetadataProvider.MemberType => typeof(TProperty);

    IReadOnlyDictionary<string, object?> IRuleMetadataProvider.GetMetadataParameters() => GetMetadataParameters();

    /// <summary>
    /// Gets the parameters exposed by the metadata descriptor.
    /// </summary>
    /// <returns>The metadata parameters.</returns>
    protected virtual IReadOnlyDictionary<string, object?> GetMetadataParameters() => ValidationRuleDescriptor.EmptyParameters;
}
