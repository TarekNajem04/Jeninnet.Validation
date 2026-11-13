//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Misuse;

internal static class StringMisuseValidator {
    public static bool IsMisusedLength(ITypeSymbol propertyType, string ruleName, out string reason) {
        reason = string.Empty;

        var targetAttribute = string.Empty;
        const string pattern = ", the attribute '{0}' only supports the 'string' type.";

        if(!propertyType.IsString()) {
            if(AttributeNames.SIMPLE_LENGTH.Equals(ruleName, StringComparison.OrdinalIgnoreCase)) {
                targetAttribute = AttributeNames.SIMPLE_LENGTH;
            } else if(AttributeNames.SIMPLE_MAXIMUM_LENGTH.Equals(ruleName, StringComparison.OrdinalIgnoreCase)) {
                targetAttribute = AttributeNames.SIMPLE_MAXIMUM_LENGTH;
            } else if(AttributeNames.SIMPLE_MINIMUM_LENGTH.Equals(ruleName, StringComparison.OrdinalIgnoreCase)) {
                targetAttribute = AttributeNames.SIMPLE_MINIMUM_LENGTH;
            }
        }

        if(!string.IsNullOrEmpty(targetAttribute)) {
            reason = string.Format(CultureInfo.InvariantCulture, pattern, targetAttribute);
            return true;
        }

        return false;
    }
}
