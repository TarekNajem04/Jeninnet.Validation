//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Shared emptiness semantics for the <c>Empty</c> and <c>NotEmpty</c> rules.
/// </summary>
internal static class ValueEmptiness {
    internal static bool IsEmpty<TValue>(TValue value) =>
        value switch {
            null => true,
            string text => string.IsNullOrWhiteSpace(text),
            System.Collections.ICollection collection => collection.Count == 0,
            System.Collections.IEnumerable enumerable => !enumerable.Cast<object>().Any(),
            Guid guid => guid == Guid.Empty,
            _ => false
        };
}
