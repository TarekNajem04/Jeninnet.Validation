//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Analyzers.Misuse;

internal static class CommonMisuseValidator {
    public static bool IsMisusedNotEmpty(ITypeSymbol propertyType, Compilation compilation, string ruleName, out string reason) {
        reason = string.Empty;
        const string targetAttribute = AttributeNames.SIMPLE_NOT_EMPTY;

        if(
            targetAttribute.Equals(ruleName, StringComparison.OrdinalIgnoreCase) &&
            !propertyType.IsString() &&
            !propertyType.IsGuid(compilation) &&
            !propertyType.IsCollection(compilation)
        ) {
            reason = $", the attribute '{targetAttribute}' only supports ('string', 'Guid', 'ICollection', 'ICollection<T>', 'IEnumerable', 'IEnumerable<T>')";
            return true;
        }

        return false;
    }

    public static bool IsMisusedNotNull(ITypeSymbol propertyType, string ruleName, out string reason) {
        reason = string.Empty;
        const string targetAttribute = AttributeNames.SIMPLE_NOT_NULL;

        if(
            targetAttribute.Equals(ruleName, StringComparison.OrdinalIgnoreCase) &&
            !propertyType.IsNullable() &&
            !propertyType.IsReferenceType()
        ) {
            reason = $", the attribute '{targetAttribute}' only supports nullable or refrence type";
            return true;
        }

        return false;
    }

    public static bool IsMisusedValidateEach(ITypeSymbol propertyType, Compilation compilation, string ruleName, out string reason) {
        reason = string.Empty;
        const string targetAttribute = AttributeNames.SIMPLE_FOR_EACH;

        if(
            !propertyType.IsCollection(compilation) &&
            targetAttribute.Equals(ruleName, StringComparison.OrdinalIgnoreCase)
        ) {
            reason = $", the attribute '{targetAttribute}' only supports ('ICollection', 'ICollection<T>', 'IEnumerable', 'IEnumerable<T>')";
            return true;
        }

        return false;
    }
}
