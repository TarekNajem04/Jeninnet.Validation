//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Catalog;

/// <summary>
/// One-call registration for generated validators. The separate <c>Jeninnet.Validation.Catalog</c>
/// namespace keeps the <c>AddValidation</c> name from colliding with
/// <c>Microsoft.Extensions.Validation</c> for code that imports
/// <c>Jeninnet.Validation.Extensions</c>.
/// </summary>
public static class ValidationCatalogServiceCollectionExtensions {
    /// <summary>
    /// Registers the validator infrastructure and every generated validator discovered by the
    /// source generator through the central catalog. This is the one-call registration path and
    /// performs no reflection or assembly scanning.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The lifetime used for new validator registrations.</param>
    /// <param name="configure">
    /// Optional transform applied to the process-wide validation defaults before registration.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Generated validators are registered from a module initializer when their assembly loads. If
    /// a validator assembly has not loaded yet, register it explicitly or make sure a type from that
    /// assembly is referenced before calling this method. When <c>Microsoft.Extensions.Validation</c>
    /// is also imported, qualify the call because both packages expose an <c>AddValidation</c>
    /// extension.
    /// </remarks>
    public static IServiceCollection AddValidation(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Func<ValidationOptions, ValidationOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidationCore(configure);

        foreach(var registration in GeneratedValidatorCatalog.Entries) {
            registration.Register(services, lifetime);
        }

        return services;
    }
}
