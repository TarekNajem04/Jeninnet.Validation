//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.GeneratorModel;

/// <summary>
/// Describes a property within a validator,
/// </summary>
/// <param name="Property">The property symbol.</param>
/// <param name="Rules">The validation rules applied to the property  [<c>NotNull</c>, <c>...</c>].</param>
internal sealed record ValidatorPropertyDescriptor(
    IPropertySymbol Property,
    IReadOnlyList<ValidationRuleDescriptor> Rules
) {
    /// <summary>
    /// Gets the type symbol associated with the property.
    /// </summary>
    public ITypeSymbol Type => Property.Type;

    /// <summary>
    /// Gets the property name <c>for example 'DiscountPercentage'</c>. Returns the empty string if unnamed.
    /// </summary>
    public string Name => Property.Name;
    /// <summary>
    /// Gets the property name <c>for example 'decimal'</c>. Returns the empty string if unnamed.
    /// </summary>
    public string TypeName => Property.Type.Name;

    /// <summary>
    /// Gets the locations where the symbol was originally defined, either in source or metadata.
    /// Some symbols (for example, partial classes) may be defined in more than one location.
    /// </summary>
    public Location? Location => Property.Locations.FirstOrDefault();    // For diagnostics 
    /// <summary>
    /// Gets the locations where the symbol was originally defined, either in source or
    /// metadata. Some symbols (for example, partial classes) may be defined in more than one
    /// location.
    /// </summary>
    public ImmutableArray<Location> Locations => Property.Locations;
}
