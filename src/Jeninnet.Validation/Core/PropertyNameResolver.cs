//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Resolves code-facing member names through the global validation options.
/// </summary>
internal static class PropertyNameResolver {
    /// <summary>
    /// Resolves the member name, falling back to the original name when no resolver is configured
    /// or the resolver returns a blank value.
    /// </summary>
    /// <param name="validatedType">The validated type.</param>
    /// <param name="memberName">The current member path.</param>
    /// <param name="options">The effective validation options.</param>
    /// <returns>The resolved member name.</returns>
    public static string Resolve(Type validatedType, string memberName, ValidationOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        var resolver = options.PropertyNameResolver;

        if(resolver is null) {
            return memberName;
        }

        var resolved = resolver(validatedType, memberName);

        return string.IsNullOrWhiteSpace(resolved) ? memberName : resolved;
    }
}
