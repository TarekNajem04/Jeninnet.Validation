//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Extensions;

internal static class SymbolExtensions {
    public static string GetFullTypeName(this ITypeSymbol type) {
        var format = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
        );
        return type.ToDisplayString(format);
    }

    public static string GetAttributeTypeName(this ISymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");

    public static ITypeSymbol UnwrapNullable(this ITypeSymbol type) {
        if(type is INamedTypeSymbol named && named.IsGenericType && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
            return named.TypeArguments[0];
        }

        return type;
    }

    public static ITypeSymbol? GetElementType(this ITypeSymbol type) {
        if(type is IArrayTypeSymbol arrayType) {
            return arrayType.ElementType;
        }

        foreach(var iface in type.AllInterfaces) {
            if(iface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T) {
                return iface.TypeArguments[0];
            }
        }

        if(type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T) {
            return named.TypeArguments[0];
        }

        return null;
    }

    public static bool HasAttribute(this ISymbol symbol, string fullName)
        => symbol.GetAttributes().Any(a => a.AttributeClass?.GetAttributeTypeName() == fullName);

    public static bool IsNullable(this ITypeSymbol type) =>
        type.NullableAnnotation is NullableAnnotation.Annotated ||
        (type is INamedTypeSymbol named && named.IsGenericType && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);

    public static bool IsString(this ITypeSymbol type) => type.SpecialType == SpecialType.System_String;

    public static bool IsStringOrNullableString(this ITypeSymbol type) => type.UnwrapNullable().IsString();

    public static bool IsNumeric(this ITypeSymbol type) =>
        type.UnwrapNullable().SpecialType is
            SpecialType.System_Byte or SpecialType.System_SByte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Single or SpecialType.System_Double or
            SpecialType.System_Decimal;

    public static bool IsNullableNumeric(this ITypeSymbol type) => type.IsNullable() && type.IsNumeric();

    public static bool IsDateOrTime(this ITypeSymbol type) {
        var unwrapped = type.UnwrapNullable();
        if(unwrapped.SpecialType == SpecialType.System_DateTime) {
            return true;
        }

        var fullName = unwrapped.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return fullName is "global::System.DateOnly" or "global::System.TimeOnly" or "global::System.DateTimeOffset";
    }

    public static bool IsNullableDateOrTime(this ITypeSymbol type) => type.IsNullable() && type.IsDateOrTime();

    public static bool IsGuid(this ITypeSymbol type, Compilation compilation) {
        var targetType = compilation.GetTypeByMetadataName("System.Guid");
        return targetType is not null && SymbolEqualityComparer.Default.Equals(type.UnwrapNullable(), targetType);
    }

    public static bool IsPrimitive(this ITypeSymbol type) =>
        type.SpecialType is not SpecialType.None and not SpecialType.System_String;

    public static bool IsPrimitiveOrString(this ITypeSymbol type) {
        var unwrapped = type.UnwrapNullable();
        return (unwrapped.IsValueType && unwrapped.SpecialType != SpecialType.None) || unwrapped.IsString();
    }

    public static bool IsCollection(this ITypeSymbol type, Compilation compilation) {
        var iEnumerable = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
        var iEnumerableOfT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
        var iCollection = compilation.GetTypeByMetadataName("System.Collections.ICollection");
        var iCollectionOfT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T);

        return type.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, iEnumerableOfT) ||
            SymbolEqualityComparer.Default.Equals(i, iEnumerable) ||
            SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, iCollectionOfT) ||
            SymbolEqualityComparer.Default.Equals(i, iCollection)
        );
    }

    public static bool IsComparable(this ITypeSymbol type, Compilation compilation) {
        var unwrapped = type.UnwrapNullable();
        var iComparable = compilation.GetTypeByMetadataName("System.IComparable");
        var iComparableOfT = compilation.GetTypeByMetadataName("System.IComparable`1");

        return unwrapped.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i, iComparable) ||
            SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, iComparableOfT));
    }

    public static bool IsReferenceType(this ITypeSymbol type) => type.IsReferenceType;
}
