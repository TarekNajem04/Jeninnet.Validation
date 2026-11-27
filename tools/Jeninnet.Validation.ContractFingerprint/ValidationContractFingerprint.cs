//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Jeninnet.Validation.Metadata;

namespace Jeninnet.Validation.ContractFingerprint;

/// <summary>
/// Computes a deterministic, versioned fingerprint over a validation descriptor. The fingerprint
/// covers the member paths, rule order, rule kinds, message keys, severity, error code, display
/// name, condition presence, parameters, and nested descriptors. It is a contract signal, not a
/// security guarantee; predicate bodies are opaque delegates and are not part of the token.
/// </summary>
internal static class ValidationContractFingerprint {
    private const string VERSION = "v1";

    /// <summary>
    /// Computes the fingerprint for one descriptor.
    /// </summary>
    /// <param name="descriptor">The descriptor to hash.</param>
    /// <returns>A <c>v1:sha256:&lt;hex&gt;</c> fingerprint.</returns>
    public static string Compute(IValidationDescriptor descriptor) {
        ArgumentNullException.ThrowIfNull(descriptor);

        var canonical = new StringBuilder().Append(VERSION).Append('\n');
        AppendDescriptor(canonical, descriptor);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));

        return $"{VERSION}:sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static void AppendDescriptor(StringBuilder builder, IValidationDescriptor descriptor) {
        builder.Append("validator=").Append(Escape(descriptor.ValidatorName)).Append('\n')
               .Append("model=").Append(Escape(descriptor.ValidatedType?.Name ?? string.Empty)).Append('\n');

        foreach(var member in descriptor.Members.OrderBy(static member => member.Name, StringComparer.Ordinal)) {
            builder.Append("member=").Append(Escape(member.Name)).Append('\n');

            var index = 0;

            foreach(var rule in member.Rules) {
                builder.Append("  rule[").Append(index).Append("]=");
                AppendRule(builder, rule);
                builder.Append('\n');
                index++;
            }
        }
    }

    private static void AppendRule(StringBuilder builder, IValidationRuleDescriptor rule) {
        builder.Append("kind=").Append(Escape(rule.Kind))
               .Append(";messageKey=").Append(Escape(rule.MessageKey ?? string.Empty))
               .Append(";authored=").Append(rule.MessageIsAuthored ? '1' : '0')
               .Append(";severity=").Append(rule.Severity.ToString())
               .Append(";errorCode=").Append(Escape(rule.ErrorCode ?? string.Empty))
               .Append(";displayName=").Append(Escape(rule.DisplayName ?? string.Empty))
               .Append(";condition=").Append(rule.HasCondition ? "conditional" : "always")
               .Append(";params=");
        AppendParameters(builder, rule.Parameters);

        if(rule is ValidationRuleDescriptor { NestedDescriptor: { } nested }) {
            builder.Append(";nested=").Append(Compute(nested));
        }
    }

    private static void AppendParameters(
        StringBuilder builder,
        IReadOnlyDictionary<string, object?> parameters) {
        builder.Append('{');

        var index = 0;

        foreach(var pair in parameters.OrderBy(static pair => pair.Key, StringComparer.Ordinal)) {
            if(index > 0) {
                builder.Append(',');
            }

            builder.Append(Escape(pair.Key)).Append('=').Append(FormatValue(pair.Value));
            index++;
        }

        builder.Append('}');
    }

    private static string FormatValue(object? value) => value switch {
        null => "null",
        string text => "s:" + Escape(text),
        bool flag => flag ? "b:1" : "b:0",
        char character => "c:" + Escape(character.ToString()),
        Enum enumValue => "e:" + Escape(enumValue.ToString()),
        System.Collections.IDictionary dictionary => FormatDictionary(dictionary),
        System.Collections.IEnumerable sequence => FormatSequence(sequence),
        IFormattable formattable => "f:" + Escape(
            formattable.ToString(format: null, CultureInfo.InvariantCulture) ?? string.Empty),
        _ => "o:" + Escape(value.ToString() ?? string.Empty)
    };

    private static string FormatDictionary(System.Collections.IDictionary dictionary) {
        var entries = new List<string>(dictionary.Count);

        foreach(System.Collections.DictionaryEntry entry in dictionary) {
            entries.Add(
                Escape(entry.Key?.ToString() ?? string.Empty) + "=" + FormatValue(entry.Value));
        }

        entries.Sort(StringComparer.Ordinal);

        return "d:{" + string.Join(",", entries) + "}";
    }

    private static string FormatSequence(System.Collections.IEnumerable sequence) {
        var items = new List<string>();

        foreach(var item in sequence) {
            items.Add(FormatValue(item));
        }

        items.Sort(StringComparer.Ordinal);

        return "a:[" + string.Join(",", items) + "]";
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
             .Replace("|", "\\|", StringComparison.Ordinal)
             .Replace(";", "\\;", StringComparison.Ordinal)
             .Replace("\n", "\\n", StringComparison.Ordinal)
             .Replace("\r", "\\r", StringComparison.Ordinal);
}
