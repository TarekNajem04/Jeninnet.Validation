//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.OpenApi;

/// <summary>
/// Receives scalar constraint values for a generator-specific schema type.
/// </summary>
internal interface ISchemaConstraintTarget {
    /// <summary>
    /// Sets the minimum string length.
    /// </summary>
    /// <param name="value">The minimum string length.</param>
    void SetMinLength(int value);

    /// <summary>
    /// Sets the maximum string length.
    /// </summary>
    /// <param name="value">The maximum string length.</param>
    void SetMaxLength(int value);

    /// <summary>
    /// Sets the regular-expression pattern.
    /// </summary>
    /// <param name="value">The pattern.</param>
    void SetPattern(string value);

    /// <summary>
    /// Sets the inclusive minimum.
    /// </summary>
    /// <param name="value">The inclusive minimum.</param>
    void SetMinimum(decimal value);

    /// <summary>
    /// Sets the inclusive maximum.
    /// </summary>
    /// <param name="value">The inclusive maximum.</param>
    void SetMaximum(decimal value);

    /// <summary>
    /// Sets the exclusive minimum.
    /// </summary>
    /// <param name="value">The exclusive minimum.</param>
    void SetExclusiveMinimum(decimal value);

    /// <summary>
    /// Sets the exclusive maximum.
    /// </summary>
    /// <param name="value">The exclusive maximum.</param>
    void SetExclusiveMaximum(decimal value);

    /// <summary>
    /// Sets the minimum array length.
    /// </summary>
    /// <param name="value">The minimum array length.</param>
    void SetMinItems(int value);

    /// <summary>
    /// Sets the maximum array length.
    /// </summary>
    /// <param name="value">The maximum array length.</param>
    void SetMaxItems(int value);

    /// <summary>
    /// Sets whether array elements must be unique.
    /// </summary>
    /// <param name="value">The uniqueness requirement.</param>
    void SetUniqueItems(bool value);

    /// <summary>
    /// Sets the numeric multiple.
    /// </summary>
    /// <param name="value">The numeric multiple.</param>
    void SetMultipleOf(decimal value);

    /// <summary>
    /// Sets the string format.
    /// </summary>
    /// <param name="value">The string format.</param>
    void SetFormat(string value);
}
