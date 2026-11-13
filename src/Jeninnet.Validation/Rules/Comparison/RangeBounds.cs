//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Validates the ordering of range bounds when a range rule is configured.
/// </summary>
internal static class RangeBounds {
    /// <summary>
    /// Ensures that <paramref name="min"/> precedes <paramref name="max"/>.
    /// Nullable bounds are allowed; a range with a null bound is left unchanged.
    /// </summary>
    /// <typeparam name="TValue">The bound type.</typeparam>
    /// <param name="min">The minimum bound.</param>
    /// <param name="max">The maximum bound.</param>
    /// <param name="exclusive">Whether the range excludes both bounds.</param>
    /// <exception cref="ArgumentException">
    /// The bounds are reversed, or an exclusive range has equal bounds.
    /// </exception>
    public static void EnsureOrdered<TValue>(TValue min, TValue max, bool exclusive) {
        if(min is null || max is null) {
            return;
        }

        var comparison = ValueComparison.Compare(min, max);

        if(comparison < 0 || (comparison == 0 && !exclusive)) {
            return;
        }

        var message = exclusive
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"The exclusive range requires the minimum bound to be less than the maximum bound (min: {min}, max: {max}).")
            : string.Create(
                CultureInfo.InvariantCulture,
                $"The range requires the minimum bound to be less than or equal to the maximum bound (min: {min}, max: {max}).");

        throw new ArgumentException(message, nameof(min));
    }
}
