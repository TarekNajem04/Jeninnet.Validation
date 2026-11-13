//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers;

/// <summary>
/// Classifies target types for collection-only rules without guessing. <see langword="null"/>
/// means the analyzer cannot prove the answer and must stay silent.
/// </summary>
internal static class CollectionTypeHelper {
    /// <summary>
    /// Determines whether a type is statically known to be a collection.
    /// </summary>
    /// <param name="type">The target type.</param>
    /// <param name="compilation">The compilation used to resolve collection contracts.</param>
    /// <returns>
    /// <see langword="true"/> for a known collection, <see langword="false"/> for a known
    /// non-collection, and <see langword="null"/> when the answer is not provable.
    /// </returns>
    public static bool? IsCollection(ITypeSymbol type, Compilation compilation) {
        type = UnwrapNullable(type);

        if(type.SpecialType is SpecialType.System_String) {
            return false;
        }

        if(type is IArrayTypeSymbol) {
            return true;
        }

        if(type.TypeKind is TypeKind.Dynamic or TypeKind.Pointer or TypeKind.FunctionPointer) {
            return null;
        }

        if(type is ITypeParameterSymbol typeParameter) {
            return ConstraintsContainCollection(typeParameter, compilation);
        }

        if(type is not INamedTypeSymbol named || type.SpecialType is SpecialType.System_Object) {
            return null;
        }

        return ImplementsEnumerable(named, compilation);
    }

    private static bool? ConstraintsContainCollection(ITypeParameterSymbol typeParameter, Compilation compilation) {
        foreach(var constraint in typeParameter.ConstraintTypes) {
            if(IsCollection(constraint, compilation) is true) {
                return true;
            }
        }

        return null;
    }

    private static bool ImplementsEnumerable(INamedTypeSymbol type, Compilation compilation) {
        var enumerable = compilation.GetTypeByMetadataName("System.Collections.IEnumerable");
        var genericEnumerable = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");

        if(IsSameOrConstructedFrom(type, enumerable) || IsSameOrConstructedFrom(type, genericEnumerable)) {
            return true;
        }

        return HasEnumerableInterface(type, enumerable, genericEnumerable);
    }

    private static bool HasEnumerableInterface(
        INamedTypeSymbol type,
        INamedTypeSymbol? enumerable,
        INamedTypeSymbol? genericEnumerable) =>
        type.AllInterfaces.Any(implemented =>
            IsSameOrConstructedFrom(implemented, enumerable) ||
            IsSameOrConstructedFrom(implemented, genericEnumerable));

    /// <summary>
    /// Unwraps <see cref="Nullable{T}"/>.
    /// </summary>
    /// <param name="type">The type to unwrap.</param>
    /// <returns>The underlying type for nullable value types; otherwise the original type.</returns>
    public static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            ? nullable.TypeArguments[0]
            : type;

    private static bool IsSameOrConstructedFrom(INamedTypeSymbol candidate, INamedTypeSymbol? definition) =>
        definition is not null &&
        SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, definition);
}
