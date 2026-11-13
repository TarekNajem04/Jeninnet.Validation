//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Provides deterministic comparison semantics for validation rules. Strings use ordinal
/// comparison so validation results do not depend on the process or request culture.
/// </summary>
internal static class ValueComparison {
    /// <summary>
    /// Compares two values using ordinal semantics for strings and the default comparer for
    /// other comparable types.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns>A negative value, zero, or a positive value following standard comparison semantics.</returns>
    internal static int Compare<T>(T left, T right) {
        if(typeof(T).IsValueType) {
            return Comparer<T>.Default.Compare(left, right);
        }

        if(left is null) {
            return right is null ? 0 : -1;
        }

        if(right is null) {
            return 1;
        }

        if(left is string leftString && right is string rightString) {
            return string.CompareOrdinal(leftString, rightString);
        }

        return Comparer<T>.Default.Compare(left, right);
    }
}
