//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Misuse;

internal static class DateTimeMisuseValidator {
    public static bool IsMisused(ITypeSymbol propertyType, string ruleName, out string reason) {
        reason = string.Empty;
        var pattern = $", the attribute '{{0}}' only supports the '{nameof(DateTime)}' type.";
        var targetAttribute = string.Empty;
        const string afterAttribute = AttributeNames.SIMPLE_AFTER;
        const string beforeAttribute = AttributeNames.SIMPLE_BEFORE;

        if(!propertyType.IsDateOrTime()) {
            if(afterAttribute.Equals(ruleName, StringComparison.OrdinalIgnoreCase)) {
                targetAttribute = afterAttribute;
            } else if(beforeAttribute.Equals(ruleName, StringComparison.OrdinalIgnoreCase)) {
                targetAttribute = beforeAttribute;
            }
        }

        if(!string.IsNullOrEmpty(targetAttribute)) {
            reason = string.Format(CultureInfo.InvariantCulture, pattern, targetAttribute);
            return true;
        }

        return false;
    }
}
