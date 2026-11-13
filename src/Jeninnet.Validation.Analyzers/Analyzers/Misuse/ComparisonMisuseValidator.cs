//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Misuse;

internal static class ComparisonMisuseValidator {
    public static bool IsMisused(ITypeSymbol propertyType, Compilation compilation, string ruleName, out string reason) {
        reason = string.Empty;
        const string pattern = ", the attribute '{0}' only supports the type that implements the `IComparable` or `IComparable<T>` interface.";
        var targetAttribute = string.Empty;

        const string exclusiveBetweenAttribute = AttributeNames.SIMPLE_EXCLUSIVE_BETWEEN;
        const string greaterThanAttribute = AttributeNames.SIMPLE_GREATER_THAN;
        const string greaterThanOrEqualAttribute = AttributeNames.SIMPLE_GREATER_THAN_OR_EQUAL;
        const string inclusiveBetweenAttribute = AttributeNames.SIMPLE_INCLUSIVE_BETWEEN;
        const string lessThanAttribute = AttributeNames.SIMPLE_LESS_THAN;
        const string lessThanOrEqualAttribute = AttributeNames.SIMPLE_LESS_THAN_OR_EQUAL;

        if(!propertyType.IsComparable(compilation)) {
            if(exclusiveBetweenAttribute.Equals(ruleName, StringComparison.OrdinalIgnoreCase)) {
                targetAttribute = exclusiveBetweenAttribute;
            } else if(greaterThanAttribute.Equals(ruleName, StringComparison.OrdinalIgnoreCase)) {
                targetAttribute = greaterThanAttribute;
            } else if(greaterThanOrEqualAttribute.Equals(ruleName, StringComparison.OrdinalIgnoreCase)) {
                targetAttribute = greaterThanOrEqualAttribute;
            } else if(inclusiveBetweenAttribute.Equals(ruleName, StringComparison.OrdinalIgnoreCase)) {
                targetAttribute = inclusiveBetweenAttribute;
            } else if(lessThanAttribute.Equals(ruleName, StringComparison.OrdinalIgnoreCase)) {
                targetAttribute = lessThanAttribute;
            } else if(lessThanOrEqualAttribute.Equals(ruleName, StringComparison.OrdinalIgnoreCase)) {
                targetAttribute = lessThanOrEqualAttribute;
            }
        }

        if(!string.IsNullOrEmpty(targetAttribute)) {
            reason = string.Format(CultureInfo.InvariantCulture, pattern, targetAttribute);
            return true;
        }

        return false;
    }
}
