//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Provides factory methods for creating property accessors used by validators.
/// This interface replaces the previous static <c>ValidatorFactory</c> with an injectable service.
/// </summary>
public interface IValidatorFactory {
    /// <summary>
    /// Creates a non‑nullable property accessor.
    /// </summary>
    /// <typeparam name="T">The containing type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="getter">The compiled getter delegate.</param>
    /// <param name="memberName">The name of the property.</param>
    IPropertyAccessor<T, TProperty> Accessor<T, TProperty>(Func<T, TProperty> getter, string memberName);

    /// <summary>
    /// Converts a non‑nullable accessor into a nullable one.
    /// </summary>
    /// <typeparam name="T">The containing type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="accessor">The accessor to convert.</param>
    IPropertyAccessor<T, TProperty?> ToNullable<T, TProperty>(IPropertyAccessor<T, TProperty> accessor)
        where TProperty : struct;

    /// <summary>
    /// Converts a nullable accessor into a non‑nullable accessor (defaults to the default value when null).
    /// </summary>
    /// <typeparam name="T">The containing type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="accessor">The accessor to convert.</param>
    IPropertyAccessor<T, TProperty> AsNonNullable<T, TProperty>(IPropertyAccessor<T, TProperty?> accessor)
        where TProperty : struct;

    /// <summary>
    /// Creates a non‑nullable property accessor for reference‑type properties.
    /// </summary>
    /// <typeparam name="T">The containing type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="getter">The compiled getter delegate.</param>
    /// <param name="memberName">The name of the property.</param>
    IPropertyAccessor<T, TProperty> NotNullAccessor<T, TProperty>(Func<T, TProperty> getter, string memberName)
        where TProperty : notnull;
}
