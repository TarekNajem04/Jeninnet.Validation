//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Emitter;

/// <summary>
/// Specialized emitter for generating C# expressions for validation rules.
/// </summary>
internal static class RuleExpressionEmitter {
    private const string FALSE_LITERAL = "false";

    public static string EmitRule(
        ValidationRuleDescriptor rule,
        string typeName,
        string typeNamespace,
        List<InjectedValidator> injected) {
        var inner = EmitBaseRule(rule, typeName, typeNamespace, injected);
        inner = EmitMetadataWrapper(inner, rule);
        inner = EmitConditionalWrapper(typeName, inner, rule);
        inner = EmitRuleSetWrapper(typeName, inner, rule);
        return inner;
    }

    private static string EmitBaseRule(
        ValidationRuleDescriptor rule,
        string typeName,
        string typeNamespace,
        List<InjectedValidator> injected) {
        var propertyType = rule.PropertyTypeName ?? "object?";
        var simplePropertyType = NamingUtilities.SimplifyTypeName(propertyType, typeNamespace);
        var simpleRuleType = NamingUtilities.SimplifyRuleTypeName(rule.RuleTypeName);
        var msg = FormatMessage(rule);

        if(rule is CustomRuleDescriptor custom) {
            return EmitCustomRule(custom, typeName, simplePropertyType, msg);
        }

        var accessor = $"ValidatorFactory.Accessor<{typeName}, {simplePropertyType}>(x => x.{rule.PropertyName}, \"{rule.PropertyName}\")";

        if(rule.Kind is RuleKind.FutureDate or RuleKind.PastDate) {
            return EmitRelativeDateRule(rule, typeName, simplePropertyType, simpleRuleType, accessor, msg);
        }

        // Special handling for EnumRule which requires the enum type as second argument.
        if(rule.RuleTypeName is RuleNames.ENUM) {
            var enumType = simplePropertyType.TrimEnd('?');
            return $"new {simpleRuleType}<{typeName}, {simplePropertyType}>({accessor}, typeof({enumType}), {msg})";
        }

        return rule switch {
            BasicRuleDescriptor =>
                $"new {simpleRuleType}<{typeName}, {simplePropertyType}>({accessor}, {msg})",

            ParametricRuleDescriptor p =>
                EmitParametricRule(p, typeName, simplePropertyType, simpleRuleType, accessor, msg),

            NestedRuleDescriptor n =>
                EmitNestedRule(n, typeName, typeNamespace, simplePropertyType, simpleRuleType, accessor, injected),

            _ => throw new NotSupportedException($"Rule type not handled by emitter: {rule.RuleTypeName}")
        };
    }

    private static string EmitRelativeDateRule(
        ValidationRuleDescriptor rule,
        string typeName,
        string simplePropertyType,
        string simpleRuleType,
        string accessor,
        string msg) {
        var includeToday = rule is ParametricRuleDescriptor { Parameters.Count: > 0 } parametric &&
                           parametric.Parameters[0].Value == "true"
            ? "true"
            : FALSE_LITERAL;
        const string provider = "(global::System.TimeProvider?)context.ServiceProvider?.GetService(typeof(global::System.TimeProvider)) ?? global::System.TimeProvider.System";
        var baseType = NamingUtilities.GetSimpleName(simplePropertyType.TrimEnd('?'));
        var now = baseType switch {
            "DateTime" =>
                $"context => {{ var provider = {provider}; return provider.GetUtcNow().UtcDateTime; }}",
            "DateTimeOffset" =>
                $"context => {{ var provider = {provider}; return provider.GetUtcNow(); }}",
            "DateOnly" =>
                $"context => {{ var provider = {provider}; return global::System.DateOnly.FromDateTime(provider.GetUtcNow().UtcDateTime); }}",
            _ => throw new NotSupportedException($"Relative date rule not supported for {simplePropertyType}")
        };

        return $"new {simpleRuleType}<{typeName}, {simplePropertyType}>({accessor}, {now}, {includeToday}, {msg})";
    }

    private static string EmitCustomRule(
        CustomRuleDescriptor rule,
        string typeName,
        string simplePropertyType,
        string msg) {
        var accessor = rule.IsModelRule
            ? $"ValidatorFactory.Accessor<{typeName}, {simplePropertyType}>(x => x, string.Empty)"
            : $"ValidatorFactory.Accessor<{typeName}, {simplePropertyType}>(x => x.{rule.PropertyName}, \"{rule.PropertyName}\")";

        var providerParameter = rule.HasServiceProvider ? "services" : "_";
        var providerArgument = rule.HasServiceProvider ? ", services!" : string.Empty;
        var ruleType = rule.Options.IsAsyncRule
            ? NamingUtilities.SimplifyRuleTypeName(RuleNames.CUSTOM_ASYNC)
            : NamingUtilities.SimplifyRuleTypeName(RuleNames.CUSTOM);
        var messageKey = rule.ReturnsResult
            ? "null"
            : GetPredicateMessageKey(rule.Options.IsAsyncRule);
        var lambda = rule.Options.IsAsyncRule
            ? $"(value, {providerParameter}, ct) => {rule.MethodExpression}(value{providerArgument}, ct)"
            : $"(value, {providerParameter}) => {rule.MethodExpression}(value{providerArgument})";

        return $"new {ruleType}<{typeName}, {simplePropertyType}>({accessor}, {lambda}, {msg}, {messageKey})";
    }

    private static string GetPredicateMessageKey(bool isAsync) =>
        isAsync ? "\"MustAsync\"" : "\"Must\"";

    private static string EmitParametricRule(
        ParametricRuleDescriptor rule,
        string typeName,
        string simplePropertyType,
        string simpleRuleType,
        string accessor,
        string msg
    ) {

        var parameters = rule.Parameters;
        var p1 = FormatParam(parameters[0].Value, parameters[0].TypeName ?? simplePropertyType);
        var p2 = parameters.Count > 1 ? FormatParam(parameters[1].Value, parameters[1].TypeName ?? simplePropertyType) : "default";
        var p3 = parameters.Count > 2 ? FormatParam(parameters[2].Value, parameters[2].TypeName ?? "bool") : FALSE_LITERAL;

        // ------------------------------------------------------------------
        // Special handling for In and NotIn rules (array parameters)
        // ------------------------------------------------------------------
        if(rule.RuleTypeName is RuleNames.IN or RuleNames.NOT_IN) {
            return EmitInRule(rule, typeName, simplePropertyType, simpleRuleType, accessor, msg);
        }

        // ------------------------------------------------------------------
        // Special handling for Regex rules
        // ------------------------------------------------------------------
        if(rule.RuleTypeName is RuleNames.REGEX_MATCH) {
            return EmitRegexRule(rule, typeName, simplePropertyType, simpleRuleType, accessor, msg);
        }

        // ------------------------------------------------------------------
        // Special handling for Property comparison rules
        // ------------------------------------------------------------------
        if(rule.Kind.IsPropertyComparisonRule()) {
            return EmitPropertyComparisonRule(rule, typeName, simplePropertyType, simpleRuleType, accessor, msg);
        }

        // ------------------------------------------------------------------
        // Special handling for file-extension rules
        // ------------------------------------------------------------------
        if(rule.RuleTypeName is RuleNames.FILE_EXTENSIONS) {
            return EmitFileExtensionsRule(rule, typeName, simplePropertyType, simpleRuleType, accessor, msg);
        }

        // ------------------------------------------------------------------
        // Special handling for domain rule packs
        // ------------------------------------------------------------------
        if(rule.Kind is RuleKind.Iban or RuleKind.VatNumber or RuleKind.PostalCode or RuleKind.NationalId) {
            return EmitCountryRule(rule, typeName, simplePropertyType, simpleRuleType, accessor, msg);
        }

        if(rule.Kind is RuleKind.PasswordStrength) {
            return EmitPasswordStrengthRule(rule, typeName, simplePropertyType, simpleRuleType, accessor, msg);
        }

        return EmitStandardParametricRule(
            rule,
            typeName,
            simplePropertyType,
            simpleRuleType,
            accessor,
            msg,
            (p1, p2, p3));
    }

    private static string EmitInRule(
        ParametricRuleDescriptor rule,
        string typeName,
        string simplePropertyType,
        string simpleRuleType,
        string accessor,
        string msg) {
        var valuesArray = rule.Parameters[0].Value?
            .Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(value => FormatParam(value.Trim(), simplePropertyType))
            .ToList() ?? [];

        var arrayText = $"new {simplePropertyType}[] {{ {string.Join(", ", valuesArray)} }}";

        return $"new {simpleRuleType}<{typeName}, {simplePropertyType}>({accessor}, {arrayText}, {msg})";
    }

    private static string EmitRegexRule(
        ParametricRuleDescriptor rule,
        string typeName,
        string simplePropertyType,
        string simpleRuleType,
        string accessor,
        string msg) =>
        $"new {simpleRuleType}<{typeName}, {simplePropertyType}>({accessor}, {FormatRegex(rule.Parameters[0].Value)}, {msg})";

    private static string EmitPropertyComparisonRule(
        ParametricRuleDescriptor rule,
        string typeName,
        string simplePropertyType,
        string simpleRuleType,
        string accessor,
        string msg) {
        var otherProperty = rule.Parameters[0].Value;
        var rightAccessor = $"ValidatorFactory.Accessor<{typeName}, {simplePropertyType}>(x => x.{otherProperty}, \"{otherProperty}\")";

        return $"new {simpleRuleType}<{typeName}, {simplePropertyType}>({accessor}, {rightAccessor}, {msg})";
    }

    private static string EmitFileExtensionsRule(
        ParametricRuleDescriptor rule,
        string typeName,
        string simplePropertyType,
        string simpleRuleType,
        string accessor,
        string msg) {
        var extensions = rule.Parameters[0].Value?
            .Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(static extension => $"\"{extension.Trim().Replace("\"", "\"\"")}\"")
            .ToList() ?? [];

        var ignoreCase = ToBooleanLiteral(rule.Parameters, 1);
        var allowNonStandard = ToBooleanLiteral(rule.Parameters, 2);

        return $"new {simpleRuleType}<{typeName}, {simplePropertyType}>" +
               $"({accessor}, new string[] {{ {string.Join(", ", extensions)} }}, {ignoreCase}, {allowNonStandard}, {msg})";
    }

    private static string ToBooleanLiteral(IReadOnlyList<RuleParameter> parameters, int index) =>
        parameters.Count > index && parameters[index].Value == "true" ? "true" : FALSE_LITERAL;

    private static string EmitCountryRule(
        ParametricRuleDescriptor rule,
        string typeName,
        string simplePropertyType,
        string simpleRuleType,
        string accessor,
        string msg) {
        var country = rule.Parameters[0].Value is null
            ? "null"
            : $"@\"{rule.Parameters[0].Value!.Replace("\"", "\"\"")}\"";

        return $"new {simpleRuleType}<{typeName}, {simplePropertyType}>({accessor}, {country}, {msg})";
    }

    private static string EmitPasswordStrengthRule(
        ParametricRuleDescriptor rule,
        string typeName,
        string simplePropertyType,
        string simpleRuleType,
        string accessor,
        string msg) {
        var policy = rule.Parameters[0].Value switch {
            "Basic" => "Basic",
            "Strict" => "Strict",
            _ => "Strong"
        };

        return $"new {simpleRuleType}<{typeName}, {simplePropertyType}>({accessor}, PasswordPolicy.{policy}, {msg})";
    }

    private static string EmitStandardParametricRule(
        ParametricRuleDescriptor rule,
        string typeName,
        string simplePropertyType,
        string simpleRuleType,
        string accessor,
        string msg,
        (string P1, string P2, string P3) formatted
    ) =>
        rule.RuleTypeName switch {
            // Comparison
            RuleNames.EQUAL or RuleNames.NOT_EQUAL
                or RuleNames.GREATER_THAN or RuleNames.GREATER_THAN_OR_EQUAL
                or RuleNames.LESS_THAN or RuleNames.LESS_THAN_OR_EQUAL =>
                $"new {simpleRuleType}<{typeName}, {simplePropertyType}>({accessor}, {formatted.P1}, {msg})",

            RuleNames.INCLUSIVE_BETWEEN or RuleNames.EXCLUSIVE_BETWEEN or RuleNames.RANGE =>
                $"new {simpleRuleType}<{typeName}, {simplePropertyType}>({accessor}, {formatted.P1}, {formatted.P2}, {msg})",

            RuleNames.SCALE_PRECISION =>
                $"new {simpleRuleType}<{typeName}, {simplePropertyType}>({accessor}, {formatted.P1}, {formatted.P2}, {msg}, ignoreTrailingZeros: {formatted.P3})",

            // DateTime, strings, and collection counts with a single argument
            RuleNames.MULTIPLE_OF or RuleNames.AFTER or RuleNames.BEFORE
                or RuleNames.EXACT_LENGTH or RuleNames.CONTAINS or RuleNames.STARTS_WITH
                or RuleNames.ENDS_WITH or RuleNames.MIN_COUNT or RuleNames.MAX_COUNT =>
                $"new {simpleRuleType}<{typeName}, {simplePropertyType}>({accessor}, {formatted.P1}, {msg})",

            RuleNames.LENGTH or RuleNames.COUNT =>
                $"new {simpleRuleType}<{typeName}, {simplePropertyType}>({accessor}, {formatted.P1}, {formatted.P2}, {msg})",

            _ => throw new NotSupportedException($"Parametric rule not handled: {rule.RuleTypeName}")
        };

    private static string EmitNestedRule(
        NestedRuleDescriptor rule,
        string typeName,
        string typeNamespace,
        string simplePropertyType,
        string simpleRuleType,
        string accessor,
        List<InjectedValidator> injected) {

        var targetTypeName = rule.TargetTypeName;
        var simpleTargetType = NamingUtilities.SimplifyTypeName(targetTypeName, typeNamespace);

        if(rule.Kind is RuleKind.Nested) {
            var injectedField = ResolveInjectedField(injected, simpleTargetType
                .Replace("?", "")
                .Replace(ValidatorEmitter.CLASS_SUFFIX, ""));

            // For NestedValidatorRule, the second generic argument must be the non-nullable type.
            var nonNullablePropertyType = simplePropertyType.TrimEnd('?');

            return injectedField is not null
                ? $"new {simpleRuleType}<{typeName}, {nonNullablePropertyType}>" +
                  $"({accessor}, {injectedField})"
                : $"new {simpleRuleType}<{typeName}, {nonNullablePropertyType}>" +
                  $"({accessor}, null)";
        }

        if(rule.Kind is RuleKind.ForEach) {
            // Determine the element type from the target type name (strip Validator suffix).
            var elementTypeName = simpleTargetType
                .Replace("global::", "")
                .Replace("?", "");

            if(elementTypeName.EndsWith(ValidatorEmitter.CLASS_SUFFIX, StringComparison.Ordinal)) {
                elementTypeName = elementTypeName.Substring(
                    0, elementTypeName.Length - ValidatorEmitter.CLASS_SUFFIX.Length);
            }

            var injectedField = ResolveInjectedField(injected, elementTypeName);

            return injectedField is not null
                ? $"new {simpleRuleType}<{typeName}, {elementTypeName}>" +
                  $"({accessor}, {injectedField}, {injectedField})"
                : $"new {simpleRuleType}<{typeName}, {elementTypeName}>" +
                  $"({accessor}, null, null)";
        }

        if(rule.Kind is RuleKind.UniqueItems) {
            var elementTypeName = simpleTargetType
                .Replace("global::", "")
                .Replace("?", "");
            var uniqueAccessor =
                $"ValidatorFactory.Accessor<{typeName}, global::System.Collections.Generic.IEnumerable<{elementTypeName}>?>" +
                $"(x => x.{rule.PropertyName}, \"{rule.PropertyName}\")";

            return $"new {simpleRuleType}<{typeName}, {elementTypeName}>" +
                   $"({uniqueAccessor}, {FormatMessage(rule)})";
        }

        throw new NotSupportedException($"Nested rule kind not handled: {rule.Kind}");
    }

    private static string? ResolveInjectedField(
        List<InjectedValidator> injected,
        string simpleTargetType) {
        // Match on simple name: Address → _addressValidator
        var match = injected.FirstOrDefault(v =>
            string.Equals(v.SimpleTargetType, simpleTargetType, StringComparison.Ordinal));
        return match?.FieldName;
    }

    private static string EmitMetadataWrapper(string inner, ValidationRuleDescriptor rule) {
        var options = rule.Options;
        var arguments = new List<string>(5);

        if(options.ErrorCode is not null) {
            arguments.Add($"errorCode: {FormatStringLiteral(options.ErrorCode)}");
        }

        if(options.Severity is not null) {
            arguments.Add($"severity: ValidationErrorSeverity.{options.Severity}");
        }

        if(options.DisplayName is not null) {
            arguments.Add($"displayName: {FormatStringLiteral(options.DisplayName)}");
        }

        if(options.OverridePropertyName is not null) {
            arguments.Add($"memberName: {FormatStringLiteral(options.OverridePropertyName)}");
        }

        if(options.CustomState is not null) {
            arguments.Add($"customState: {FormatStringLiteral(options.CustomState)}");
        }

        return arguments.Count is 0
            ? inner
            : $"ValidationRuleMetadata.Apply({inner}, {string.Join(", ", arguments)})";
    }

    private static string FormatStringLiteral(string value) =>
        $"@\"{value.Replace("\"", "\"\"")}\"";

    private static string EmitConditionalWrapper(
        string typeName,
        string inner,
        ValidationRuleDescriptor rule) {
        if(rule.Options.WhenCondition is { } whenCondition) {
            inner = rule.Options.WhenConditionHasContext
                ? $"new ConditionalRule<{typeName}>({whenCondition}, [{inner}], [])"
                : $"new ConditionalRule<{typeName}>({whenCondition}, null, [{inner}], [])";
        }

        if(rule.Options.UnlessCondition is { } unlessCondition) {
            inner = rule.Options.UnlessConditionHasContext
                ? $"new ConditionalRule<{typeName}>({unlessCondition}, [{inner}], [])"
                : $"new ConditionalRule<{typeName}>({unlessCondition}, null, [{inner}], [])";
        }

        if(rule.Options.WhenAsyncCondition is { } when) {
            inner = $"new ConditionalRule<{typeName}>(null, {when}, [{inner}], [])";
        }

        if(rule.Options.UnlessAsyncCondition is { } unless) {
            inner = $"new ConditionalRule<{typeName}>(null, async (x, ct) => !await {unless}(x, ct), [{inner}], [])";
        }

        return inner;
    }

    private static string EmitRuleSetWrapper(
        string typeName,
        string inner,
        ValidationRuleDescriptor rule) {
        if(rule.Options.RuleSets is not { Count: > 0 }) {
            return inner;
        }

        var setsArray = "new string[] { " +
            string.Join(", ", rule.Options.RuleSets.Select(s => $"\"{s}\"")) +
            " }";

        return $"new RuleSetRule<{typeName}>({setsArray}, [{inner}], [])";
    }

    private static string FormatMessage(ValidationRuleDescriptor rule) =>
        rule.Options.Message is null ? "null" : $"@\"{rule.Options.Message.Replace("\"", "\"\"")}\"";

    private static string FormatRegex(string? pattern) =>
        pattern is null
            ? "null!"
            : $"new System.Text.RegularExpressions.Regex(@\"{pattern.Replace("\"", "\"\"")}\"" +
              ", System.Text.RegularExpressions.RegexOptions.Compiled" +
              ", System.TimeSpan.FromSeconds(1))";

    internal static string FormatParam(string? value, string typeHint) {
        if(value is null) {
            return "default";
        }

        var normalized = typeHint.Trim().TrimEnd('?');
        var simple = NamingUtilities.GetSimpleName(normalized).ToLowerInvariant();

        return simple switch {
            "string" => $"@\"{value.Replace("\"", "\"\"")}\"",
            "bool" or "boolean" => value.Trim().ToLowerInvariant(),
            "char" => $"'{value[0]}'",
            "guid" => $"Guid.Parse(\"{value}\")",
            "datetime" => $"global::System.DateTime.Parse(\"{value}\", global::System.Globalization.CultureInfo.InvariantCulture, global::System.Globalization.DateTimeStyles.RoundtripKind)",
            "datetimeoffset" => $"global::System.DateTimeOffset.Parse(\"{value}\", global::System.Globalization.CultureInfo.InvariantCulture, global::System.Globalization.DateTimeStyles.AssumeUniversal)",
            "dateonly" => $"global::System.DateOnly.Parse(\"{value}\", global::System.Globalization.CultureInfo.InvariantCulture)",
            "timeonly" => $"global::System.TimeOnly.Parse(\"{value}\", global::System.Globalization.CultureInfo.InvariantCulture)",
            "timespan" => $"global::System.TimeSpan.Parse(\"{value}\", global::System.Globalization.CultureInfo.InvariantCulture)",
            "decimal" => EnsureSuffix(value, "m", "M"),
            "float" or "single" => EnsureSuffix(value, "f", "F"),
            "double" => EnsureSuffix(value, "d", "D"),
            "long" or "int64" => EnsureSuffix(value, "L"),
            "uint" or "uint32" => EnsureSuffix(value, "u", "U"),
            "ulong" or "uint64" => EnsureSuffix(value, "ul", "UL"),
            "byte" or "sbyte"
                or "short" or "ushort"
                or "int" or "int32" => value,
            _ when normalized.Contains('.') && IsNumericLiteral(value) =>
                $"({normalized})({value})",
            _ => value   // enum / other constant
        };
    }

    private static bool IsNumericLiteral(string value) {
        var start = value.Length > 0 && value[0] is '-' or '+' ? 1 : 0;
        if(value.Length <= start) {
            return false;
        }

        var hasDigit = false;
        for(var i = start; i < value.Length; i++) {
            var character = value[i];
            if(char.IsDigit(character)) {
                hasDigit = true;
                continue;
            }

            if(character is '.' or 'e' or 'E' or '+' or '-') {
                continue;
            }

            return false;
        }

        return hasDigit;
    }

    private static string EnsureSuffix(string value, params string[] suffixes) {
        for(var i = 0; i < suffixes.Length; i++) {
            if(value.EndsWith(suffixes[i], StringComparison.Ordinal)) {
                return value;
            }
        }

        return value + suffixes[0];
    }
}
