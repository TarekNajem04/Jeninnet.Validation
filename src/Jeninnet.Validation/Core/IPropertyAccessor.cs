//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Provides a strongly-typed accessor for a property on a validated object.
/// Used by both the fluent API and generated validators.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public interface IPropertyAccessor<in T, out TProperty> {
    /// <summary>
    /// Gets the compiled getter delegate for the property.
    /// </summary>
    public Func<T, TProperty> Getter { get; }

    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    string MemberName { get; }
    /// <summary>
    /// Gets the compiled getter delegate for the property.
    /// </summary>
    /// <param name="instance">The object instance to extract the value from.</param>
    TProperty GetValue(T instance);
}
