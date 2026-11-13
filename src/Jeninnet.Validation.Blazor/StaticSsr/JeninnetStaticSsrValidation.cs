//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Blazor.StaticSsr;

/// <summary>
/// Renders validation attributes for server-rendered (static SSR) forms from a validator
/// descriptor. Use the returned dictionary with <c>@attributes</c> on an input element.
/// </summary>
/// <remarks>
/// The helper emits standard <c>data-val-*</c> attributes for the rules the browser-side runtime
/// supports, plus a <c>data-jeninnet-rules</c> JSON payload for the dependency-free
/// <c>jeninnet-unobtrusive.js</c> module. Rules that need the server are marked
/// <c>serverOnly</c> and never block submission in the browser; the server remains authoritative.
/// </remarks>
public static class JeninnetStaticSsrValidation {
    /// <summary>
    /// Builds the validation attributes for one member.
    /// </summary>
    /// <param name="descriptor">The validator descriptor.</param>
    /// <param name="memberName">The member name.</param>
    /// <param name="messageResolver">Optional message resolver; defaults to English fallback text.</param>
    /// <returns>The attributes to splat on the input element. Empty when the member is unknown.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="descriptor"/> or <paramref name="memberName"/> is null.</exception>
    public static IReadOnlyDictionary<string, object> For(
        IValidationDescriptor descriptor,
        string memberName,
        Func<IValidationRuleDescriptor, string?>? messageResolver = null) {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        var member = descriptor.Members.FirstOrDefault(
            candidate => string.Equals(candidate.Name, memberName, StringComparison.Ordinal));

        if(member is null) {
            return new Dictionary<string, object>(StringComparer.Ordinal);
        }

        var rulesNode = GetRulesNode(descriptor, memberName, messageResolver);
        var entries = GetEntries(member.Rules, rulesNode);
        var attributes = new Dictionary<string, object>(StringComparer.Ordinal) {
            ["data-jeninnet-member"] = memberName,
            ["data-jeninnet-rules"] = rulesNode?.ToJsonString() ?? "[]"
        };

        if(entries.Any(static entry => !entry.ServerOnly)) {
            attributes["data-val"] = "true";
        }

        if(entries.Any(static entry => entry.ServerOnly)) {
            attributes["data-jeninnet-server-only"] = "true";
        }

        var required = FirstRule(entries, "NotNull", "NotEmpty", "StringNotEmpty");
        if(required is not null) {
            attributes["data-val-required"] = ResolveMessage(required, memberName, messageResolver);
        }

        var length = FirstRule(entries, "Length");
        if(length is not null) {
            attributes["data-val-length"] = ResolveMessage(length, memberName, messageResolver);
            SetParameter(attributes, "data-val-length-min", length, "Min", "0");
            SetParameter(attributes, "data-val-length-max", length, "Max");
        }

        var exactLength = FirstRule(entries, "ExactLength");
        if(exactLength is not null) {
            attributes["data-val-length"] = ResolveMessage(exactLength, memberName, messageResolver);
            var value = GetParameter(exactLength, "Length", "0");
            if(value is not null) {
                attributes["data-val-length-min"] = value;
                attributes["data-val-length-max"] = value;
            }
        }

        var range = FirstRule(entries, "InclusiveBetween", "Range");
        if(range is not null) {
            attributes["data-val-range"] = ResolveMessage(range, memberName, messageResolver);
            SetParameter(attributes, "data-val-range-min", range, "Min", "0");
            SetParameter(attributes, "data-val-range-max", range, "Max", "1");
        }

        var regex = FirstRule(entries, "Regex");
        if(regex is not null) {
            attributes["data-val-regex"] = ResolveMessage(regex, memberName, messageResolver);
            SetParameter(attributes, "data-val-regex-pattern", regex, "Pattern", "0");
        }

        var email = FirstRule(entries, "Email");
        if(email is not null) {
            attributes["data-val-email"] = ResolveMessage(email, memberName, messageResolver);
        }

        var url = FirstRule(entries, "Url");
        if(url is not null) {
            attributes["data-val-url"] = ResolveMessage(url, memberName, messageResolver);
        }

        return attributes;
    }

    /// <summary>
    /// Builds the marker attributes for the message element associated with a member.
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <returns>Attributes to splat on the message element.</returns>
    /// <exception cref="ArgumentException"><paramref name="memberName"/> is null or empty.</exception>
    public static IReadOnlyDictionary<string, object> MessageFor(string memberName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        return new Dictionary<string, object>(StringComparer.Ordinal) {
            ["data-jeninnet-message-for"] = memberName
        };
    }

    /// <summary>
    /// Builds the marker attributes for a form that the browser module should enhance.
    /// </summary>
    /// <returns>Attributes to splat on the form element.</returns>
    public static IReadOnlyDictionary<string, object> FormAttributes() =>
        new Dictionary<string, object>(StringComparer.Ordinal) {
            ["data-jeninnet-validation"] = "true"
        };

    private static JsonArray? GetRulesNode(
        IValidationDescriptor descriptor,
        string memberName,
        Func<IValidationRuleDescriptor, string?>? messageResolver) {
        var document = ClientRulesWriter.Write(
            [descriptor],
            new ClientRulesOptions { MessageResolver = messageResolver });

        var validator = document["validators"]?.AsArray().FirstOrDefault();
        var member = validator?["members"]?.AsArray().FirstOrDefault(candidate =>
            string.Equals(candidate?["name"]?.GetValue<string>(), memberName, StringComparison.Ordinal));

        return member?["rules"]?.AsArray();
    }

    private static (IValidationRuleDescriptor Rule, bool ServerOnly)[] GetEntries(
        IReadOnlyList<IValidationRuleDescriptor> rules,
        JsonArray? rulesNode) {
        var entries = new (IValidationRuleDescriptor Rule, bool ServerOnly)[rules.Count];

        for(var index = 0; index < rules.Count; index++) {
            var serverOnly = rulesNode is not null &&
                             index < rulesNode.Count &&
                             rulesNode[index]?["serverOnly"]?.GetValue<bool>() == true;
            entries[index] = (rules[index], serverOnly);
        }

        return entries;
    }

    private static IValidationRuleDescriptor? FirstRule(
        (IValidationRuleDescriptor Rule, bool ServerOnly)[] entries,
        params string[] kinds) =>
        entries.Where(static entry => !entry.ServerOnly)
               .Select(static entry => entry.Rule)
               .FirstOrDefault(rule => kinds.Any(kind => string.Equals(rule.Kind, kind, StringComparison.Ordinal)));

    private static string ResolveMessage(
        IValidationRuleDescriptor rule,
        string memberName,
        Func<IValidationRuleDescriptor, string?>? messageResolver) {
        var resolved = messageResolver?.Invoke(rule);
        if(!string.IsNullOrWhiteSpace(resolved)) {
            return resolved;
        }

        var displayName = string.IsNullOrWhiteSpace(rule.DisplayName) ? memberName : rule.DisplayName;

        return rule.Kind switch {
            "NotNull" or "NotEmpty" or "StringNotEmpty" => $"{displayName} is required.",
            "Length" or "ExactLength" => $"{displayName} has an invalid length.",
            "InclusiveBetween" or "Range" or "ExclusiveBetween" => $"{displayName} is outside the allowed range.",
            "Regex" => $"{displayName} has an invalid format.",
            "Email" => $"{displayName} must be a valid email address.",
            "Url" => $"{displayName} must be a valid URL.",
            "After" or "FutureDate" => $"{displayName} must be in the future.",
            "Before" or "PastDate" => $"{displayName} must be in the past.",
            _ => $"{displayName} is invalid."
        };
    }

    private static void SetParameter(
        Dictionary<string, object> attributes,
        string attributeName,
        IValidationRuleDescriptor rule,
        string parameterName,
        string? fallbackIndex = null) {
        var value = GetParameter(rule, parameterName, fallbackIndex);
        if(value is not null) {
            attributes[attributeName] = value;
        }
    }

    private static string? GetParameter(
        IValidationRuleDescriptor rule,
        string parameterName,
        string? fallbackIndex = null) {
        if(rule.Parameters.TryGetValue(parameterName, out var value)) {
            return Format(value);
        }

        if(fallbackIndex is not null && rule.Parameters.TryGetValue(fallbackIndex, out value)) {
            return Format(value);
        }

        return null;
    }

    private static string? Format(object? value) =>
        value switch {
            null => null,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
}
