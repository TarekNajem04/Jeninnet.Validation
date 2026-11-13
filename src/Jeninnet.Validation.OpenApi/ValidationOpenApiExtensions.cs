//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.OpenApi;

/// <summary>
/// Registration helpers for OpenAPI schema constraints.
/// </summary>
public static class ValidationOpenApiExtensions {
    /// <summary>
    /// Registers validator descriptors and attaches the schema transformer to the default
    /// <c>v1</c> OpenAPI document.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="validators">The validator descriptor providers.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="validators"/> is null.</exception>
    public static IServiceCollection AddSchemaConstraintsFromValidators(
        this IServiceCollection services,
        params IValidationDescriptorProvider[] validators) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(validators);

        services.AddSingleton(new ValidationSchemaRegistry(validators));
        services.AddSingleton<ValidationSchemaTransformer>();
        services.Configure<OpenApiOptions>(
            "v1",
            static options => options.AddSchemaTransformer<ValidationSchemaTransformer>());

        return services;
    }

    /// <summary>
    /// Attaches the schema transformer to the supplied OpenAPI options.
    /// </summary>
    /// <param name="options">The OpenAPI options.</param>
    /// <returns>The same options for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public static OpenApiOptions AddSchemaConstraintsFromValidators(this OpenApiOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        options.AddSchemaTransformer<ValidationSchemaTransformer>();
        return options;
    }
}
