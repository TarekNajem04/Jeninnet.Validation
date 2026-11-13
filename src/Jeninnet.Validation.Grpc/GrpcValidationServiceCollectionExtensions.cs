//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Grpc;

/// <summary>
/// Registration helpers for the gRPC validation interceptor.
/// </summary>
public static class GrpcValidationServiceCollectionExtensions {
    /// <summary>
    /// Registers the validation options and interceptor. Add
    /// <see cref="ValidationInterceptor"/> to the gRPC pipeline with
    /// <c>AddGrpc(options =&gt; options.Interceptors.Add&lt;ValidationInterceptor&gt;())</c>.
    /// Validators must still be registered separately as <see cref="IValidator{T}"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional options configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddJeninnetGrpcValidation(
        this IServiceCollection services,
        Action<GrpcValidationOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);

        var options = new GrpcValidationOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<ValidationInterceptor>();

        return services;
    }
}
