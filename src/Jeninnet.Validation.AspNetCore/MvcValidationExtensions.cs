//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Registration helpers for automatic MVC validation.
/// </summary>
public static class MvcValidationExtensions {
    /// <summary>
    /// Registers the automatic MVC validation filter. Actions and controllers with a registered
    /// validator are validated before execution; failures populate <c>ModelState</c> and return a
    /// 400 validation problem.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional filter configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddMvcValidation(
        this IServiceCollection services,
        Action<MvcValidationOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MvcValidationOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddTransient<JeninnetValidationFilter>();
        services.Configure<MvcOptions>(mvc => mvc.Filters.AddService<JeninnetValidationFilter>());

        return services;
    }
}
