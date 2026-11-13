//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Registration helpers for validation interceptors.
/// </summary>
public static class ValidationInterceptorExtensions {
    /// <summary>
    /// Registers a validation interceptor with a scoped lifetime. Interceptors run in registration
    /// order.
    /// </summary>
    /// <typeparam name="T">The interceptor type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddValidationInterceptor<T>(this IServiceCollection services)
        where T : class, IValidationInterceptor =>
        AddValidationInterceptor<T>(services, ServiceLifetime.Scoped);

    /// <summary>
    /// Registers a validation interceptor. Interceptors run in registration order.
    /// </summary>
    /// <typeparam name="T">The interceptor type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The interceptor lifetime.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddValidationInterceptor<T>(
        this IServiceCollection services,
        ServiceLifetime lifetime)
        where T : class, IValidationInterceptor {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(typeof(IValidationInterceptor), typeof(T), lifetime));
        return services;
    }
}
