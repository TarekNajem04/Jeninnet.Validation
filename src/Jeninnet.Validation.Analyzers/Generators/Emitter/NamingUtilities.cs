//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Emitter;

/// <summary>
/// Provides common naming and namespace utilities for the source generator.
/// </summary>
internal static class NamingUtilities {
    public const string GLOBAL_PREFIX = "global::";

    public static string GetNamespace(string fullTypeName) {
        var lastDot = fullTypeName.LastIndexOf('.');
        return lastDot > 0 ? fullTypeName.Substring(0, lastDot) : string.Empty;
    }

    public static string GetSimpleName(string fullTypeName) {
        var lastDot = fullTypeName.LastIndexOf('.');
        return lastDot >= 0 ? fullTypeName.Substring(lastDot + 1) : fullTypeName;
    }

    public static string GetValidatorClassName(string fullTypeName) =>
        GetSimpleName(fullTypeName.Replace(GLOBAL_PREFIX, "")) + ValidatorEmitter.CLASS_SUFFIX;

    public static string SimplifyTypeName(string? fullTypeName, string knownNamespace) =>
        fullTypeName?.Replace($"{knownNamespace}.", string.Empty) ?? string.Empty;

    public static string SimplifyRuleTypeName(string fullRuleTypeName) =>
        SimplifyTypeName(fullRuleTypeName, NamespaceNames.Rules);

    public static string ToParamName(string fieldName) {
        // _addressValidator → addressValidator
        var trimmed = fieldName.TrimStart('_');
        return trimmed.Length is 0 ? fieldName : trimmed;
    }
}
