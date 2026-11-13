//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Mutable set of per-rule overrides captured by fluent option calls and applied when errors are
/// emitted. Flags distinguish "not configured" from an intentional null custom state.
/// </summary>
internal sealed class RuleOptionOverrides {
    public string? Message { get; set; }

    public bool HasMessage { get; set; }

    public string? ErrorCode { get; set; }

    public bool HasErrorCode { get; set; }

    public ValidationErrorSeverity? Severity { get; set; }

    public bool HasSeverity { get; set; }

    public object? CustomState { get; set; }

    public bool HasCustomState { get; set; }

    public ICustomStateFactory? CustomStateFactory { get; set; }

    public string? MemberName { get; set; }

    public bool HasMemberName { get; set; }

    public string? DisplayName { get; set; }

    public bool HasDisplayName { get; set; }

    public Delegate? MessageFactory { get; set; }

    public Delegate? DisplayNameFactory { get; set; }

    public Delegate? MemberNameFactory { get; set; }

    public bool HasAny =>
        HasMessage || HasErrorCode || HasSeverity || HasCustomState || HasMemberName ||
        HasDisplayName || MessageFactory is not null || DisplayNameFactory is not null ||
        MemberNameFactory is not null || CustomStateFactory is not null;

    public void CopyFrom(RuleOptionOverrides source) {
        ArgumentNullException.ThrowIfNull(source);

        CopyMessage(source);
        CopyErrorCode(source);
        CopySeverity(source);
        CopyCustomState(source);
        CopyMemberName(source);
        CopyDisplayName(source);
        CopyFactories(source);
    }

    private void CopyMessage(RuleOptionOverrides source) {
        if(source.HasMessage) {
            Message = source.Message;
            HasMessage = true;

            if(source.MessageFactory is null) {
                MessageFactory = null;
            }
        }
    }

    private void CopyErrorCode(RuleOptionOverrides source) {
        if(source.HasErrorCode) {
            ErrorCode = source.ErrorCode;
            HasErrorCode = true;
        }
    }

    private void CopySeverity(RuleOptionOverrides source) {
        if(source.HasSeverity) {
            Severity = source.Severity;
            HasSeverity = true;
        }
    }

    private void CopyCustomState(RuleOptionOverrides source) {
        if(source.HasCustomState) {
            CustomState = source.CustomState;
            HasCustomState = true;
            CustomStateFactory = null;
        }
    }

    private void CopyMemberName(RuleOptionOverrides source) {
        if(source.HasMemberName) {
            MemberName = source.MemberName;
            HasMemberName = true;

            if(source.MemberNameFactory is null && source.DisplayNameFactory is null) {
                MemberNameFactory = null;
                DisplayNameFactory = null;
            }
        }
    }

    private void CopyDisplayName(RuleOptionOverrides source) {
        if(source.HasDisplayName) {
            DisplayName = source.DisplayName;
            HasDisplayName = true;

            if(source.DisplayNameFactory is null) {
                DisplayNameFactory = null;
            }
        }
    }

    private void CopyFactories(RuleOptionOverrides source) {
        if(source.MessageFactory is not null) {
            MessageFactory = source.MessageFactory;
        }

        if(source.DisplayNameFactory is not null) {
            DisplayNameFactory = source.DisplayNameFactory;
            MemberNameFactory = null;
        }

        if(source.MemberNameFactory is not null) {
            MemberNameFactory = source.MemberNameFactory;
            DisplayNameFactory = null;
        }

        if(source.CustomStateFactory is not null) {
            CustomStateFactory = source.CustomStateFactory;
            HasCustomState = false;
        }
    }

    /// <summary>
    /// Resolves a dynamic message for the current instance and context.
    /// </summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <param name="instance">The model instance.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>The message, or <see langword="null"/> when no dynamic factory applies or it returns empty.</returns>
    public string? ResolveMessage<T>(T instance, ValidationContext<T> context) {
        var message = MessageFactory switch {
            Func<T, string> simple => simple(instance),
            Func<T, ValidationContext<T>, string> contextual => contextual(instance, context),
            _ => null
        };

        return Normalize(message);
    }

    /// <summary>
    /// Resolves a dynamic display name for the current instance.
    /// </summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <param name="instance">The model instance.</param>
    /// <returns>The display name, or <see langword="null"/> when no factory applies or it returns empty.</returns>
    public string? ResolveDisplayName<T>(T instance) {
        var displayName = DisplayNameFactory is Func<T, string> factory ? factory(instance) : null;

        return Normalize(displayName);
    }

    /// <summary>
    /// Resolves a dynamic member name for the current instance.
    /// </summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <param name="instance">The model instance.</param>
    /// <returns>The member name, or <see langword="null"/> when no factory applies or it returns empty.</returns>
    public string? ResolveMemberName<T>(T instance) {
        var memberName = MemberNameFactory is Func<T, string> factory ? factory(instance) : null;

        return Normalize(memberName);
    }

    /// <summary>
    /// Applies the static overrides to an error.
    /// </summary>
    /// <param name="error">The error to enrich.</param>
    public ValidationError Apply(ValidationError error) {
        if(!HasAny) {
            return error;
        }

        var parameters = error.MessageParameters;

        if(HasDisplayName) {
            parameters = CloneParameters(error.MessageParameters);
            parameters["PropertyName"] = DisplayName!;
        }

        return new ValidationError(
            memberName: HasMemberName ? MemberName! : error.MemberName,
            messageId: error.MessageId,
            messageKey: error.MessageKey,
            messageParameters: parameters,
            message: HasMessage ? Message : error.Message,
            errorCode: HasErrorCode ? ErrorCode : error.ErrorCode,
            severity: HasSeverity ? Severity!.Value : error.Severity,
            customState: HasCustomState ? CustomState : error.CustomState) {
            MessageIsAuthored = error.MessageIsAuthored || HasMessage
        };
    }

    /// <summary>
    /// Applies all configured overrides to an error using the current instance and context.
    /// </summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <param name="error">The error to enrich.</param>
    /// <param name="instance">The model instance.</param>
    /// <param name="context">The validation context.</param>
    public ValidationError Apply<T>(
        ValidationError error,
        T instance,
        ValidationContext<T> context) {
        if(!HasAny) {
            return error;
        }

        var message = ResolveMessage(instance, context) ?? (HasMessage ? Message : error.Message);
        var memberName = ResolveMemberName(instance) ?? (HasMemberName ? MemberName! : error.MemberName);
        var displayName = ResolveDisplayName(instance) ?? (HasDisplayName ? DisplayName : null);
        var parameters = error.MessageParameters;

        if(displayName is not null) {
            parameters = CloneParameters(error.MessageParameters);
            parameters["PropertyName"] = displayName;
        }

        return new ValidationError(
            memberName: memberName,
            messageId: error.MessageId,
            messageKey: error.MessageKey,
            messageParameters: parameters,
            message: message,
            errorCode: HasErrorCode ? ErrorCode : error.ErrorCode,
            severity: HasSeverity ? Severity!.Value : error.Severity,
            customState: HasCustomState ? CustomState : error.CustomState) {
            MessageIsAuthored = error.MessageIsAuthored || message is not null
        };
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static Dictionary<string, object> CloneParameters(IDictionary<string, object>? source) {
        var result = new Dictionary<string, object>();

        if(source is not null) {
            foreach(var parameter in source) {
                result[parameter.Key] = parameter.Value;
            }
        }

        return result;
    }
}

/// <summary>
/// Implemented by rules that can accept per-rule message/code/severity/state/name overrides.
/// </summary>
internal interface IRuleOptionsTarget {
    void ApplyOptions(RuleOptionOverrides options);
}

/// <summary>
/// Implemented by fluent builders that can apply per-rule option overrides to their last rule.
/// </summary>
internal interface IRuleOptionsTargetBuilder {
    void ConfigureLastRule(RuleOptionOverrides options);
}

/// <summary>
/// Evaluates a context-aware custom-state factory without reflection.
/// </summary>
internal interface ICustomStateFactory {
    object? Create(object instance, object? value, object context);
}

/// <summary>
/// Strongly typed adapter for a context-aware custom-state factory.
/// </summary>
/// <typeparam name="T">The model type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <param name="factory">The typed factory that produces custom state.</param>
internal sealed class CustomStateFactory<T, TProperty>(
    Func<T, TProperty, ValidationContext<T>, object?> factory) : ICustomStateFactory {
    public object? Create(object instance, object? value, object context) =>
        factory((T)instance, (TProperty)value!, (ValidationContext<T>)context);
}
