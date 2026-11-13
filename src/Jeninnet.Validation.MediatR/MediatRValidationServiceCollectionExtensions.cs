//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR;

/// <summary>
/// Registration helpers for the MediatR validation pipeline.
/// </summary>
public static class MediatRValidationServiceCollectionExtensions {
    /// <summary>
    /// Registers the validation behavior, its options, and the scoped result accessor.
    /// Validators must still be registered separately as <see cref="IValidator{T}"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional options configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddValidationPipeline(
        this IServiceCollection services,
        Action<MediatRValidationOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MediatRValidationOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddScoped<ValidationResultAccessor>();
        services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));

        return services;
    }
}
