//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Resolves human-friendly member display names through the global validation options.
/// </summary>
internal static class DisplayNameResolver {
    /// <summary>
    /// Resolves the display name for a member, falling back to the member name.
    /// </summary>
    /// <param name="validatedType">The validated type.</param>
    /// <param name="memberName">The code-facing member name.</param>
    /// <param name="options">The effective validation options.</param>
    /// <returns>The resolved display name.</returns>
    public static string Resolve(Type validatedType, string memberName, ValidationOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        var resolver = options.DisplayNameResolver;

        if(resolver is null) {
            return memberName;
        }

        var resolved = resolver(validatedType, memberName);

        return string.IsNullOrWhiteSpace(resolved) ? memberName : resolved;
    }

    /// <summary>
    /// Resolves the display name for a member with the current instance. The instance-aware
    /// resolver takes precedence over the legacy two-argument resolver.
    /// </summary>
    /// <param name="validatedType">The validated type.</param>
    /// <param name="memberName">The code-facing member name.</param>
    /// <param name="instance">The instance currently being validated.</param>
    /// <param name="options">The effective validation options.</param>
    /// <returns>The resolved display name.</returns>
    public static string Resolve(Type validatedType, string memberName, object? instance, ValidationOptions options) =>
        ResolveOrNull(validatedType, memberName, instance, options) ?? memberName;

    /// <summary>
    /// Resolves the display name when a resolver is configured and returns a non-empty result.
    /// </summary>
    /// <param name="validatedType">The validated type.</param>
    /// <param name="memberName">The code-facing member name.</param>
    /// <param name="instance">The instance currently being validated.</param>
    /// <param name="options">The effective validation options.</param>
    /// <returns>The resolved display name, or <see langword="null"/>.</returns>
    public static string? ResolveOrNull(
        Type validatedType,
        string memberName,
        object? instance,
        ValidationOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        var instanceResolver = options.DisplayNameResolverForInstance;

        if(instanceResolver is not null) {
            var resolved = instanceResolver(validatedType, memberName, instance);

            if(!string.IsNullOrWhiteSpace(resolved)) {
                return resolved;
            }
        }

        var resolver = options.DisplayNameResolver;

        if(resolver is not null) {
            var resolved = resolver(validatedType, memberName);

            if(!string.IsNullOrWhiteSpace(resolved)) {
                return resolved;
            }
        }

        return null;
    }
}
