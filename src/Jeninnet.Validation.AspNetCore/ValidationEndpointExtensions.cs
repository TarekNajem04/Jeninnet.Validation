//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Registration and endpoint extensions for minimal API validation.
/// </summary>
public static class ValidationEndpointExtensions {
    /// <summary>
    /// Adds request validation to a minimal API endpoint.
    /// </summary>
    /// <typeparam name="T">The request model type.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static RouteHandlerBuilder AddRequestValidation<T>(this RouteHandlerBuilder builder)
        where T : class {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddEndpointFilter<ValidationEndpointFilter<T>>();
    }

    /// <summary>
    /// Adds request validation to a minimal API endpoint and validates body, query, route, and
    /// header parameters that have registered validators.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static RouteHandlerBuilder AddRequestValidation(this RouteHandlerBuilder builder) {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddEndpointFilterFactory(static (factoryContext, next) => {
            var filter = new ValidationEndpointFilter(factoryContext.MethodInfo);
            return context => filter.InvokeAsync(context, next);
        });
    }

    /// <summary>
    /// Adds request validation to every endpoint in a route group.
    /// </summary>
    /// <typeparam name="T">The request model type.</typeparam>
    /// <param name="builder">The route group builder.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static RouteGroupBuilder AddRequestValidation<T>(this RouteGroupBuilder builder)
        where T : class {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddEndpointFilter<ValidationEndpointFilter<T>>();
    }

    /// <summary>
    /// Registers the validation problem options, ProblemDetails, and the
    /// <see cref="ValidationException"/> exception handler.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional problem-detail configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddValidationEndpoints(
        this IServiceCollection services,
        Action<ValidationProblemOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ValidationProblemOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.AddProblemDetails();
        services.AddExceptionHandler<ValidationExceptionHandler>();

        return services;
    }
}
