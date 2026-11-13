//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Misuse;

internal static class NestedMisuseValidator {
    public static bool IsMisused(ITypeSymbol propertyType, string ruleName, out string reason) {
        reason = string.Empty;
        const string targetAttribute = AttributeNames.SIMPLE_NESTED;

        if(
            targetAttribute.Equals(ruleName, StringComparison.OrdinalIgnoreCase) &&
            propertyType.IsPrimitiveOrString()
        ) {
            reason = $", the attribute '{targetAttribute}' only supports complex types.";
            return true;
        }

        return false;
    }
}
