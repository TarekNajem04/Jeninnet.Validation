//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.OpenApi.Swashbuckle;

/// <summary>
/// Registration helpers for Swashbuckle schema constraints.
/// </summary>
public static class SwashbuckleValidationExtensions {
    /// <summary>
    /// Adds a schema filter that applies the constraints described by the supplied validators.
    /// </summary>
    /// <param name="options">The Swashbuckle options.</param>
    /// <param name="validators">The validator descriptor providers.</param>
    /// <returns>The same options for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> or <paramref name="validators"/> is null.</exception>
    public static SwaggerGenOptions AddSchemaConstraintsFromValidators(
        this SwaggerGenOptions options,
        params IValidationDescriptorProvider[] validators) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(validators);

        return options.AddSchemaConstraintsFromValidators(new ValidationSchemaRegistry(validators));
    }

    /// <summary>
    /// Adds a schema filter that applies constraints from an existing registry.
    /// </summary>
    /// <param name="options">The Swashbuckle options.</param>
    /// <param name="registry">The descriptor registry.</param>
    /// <returns>The same options for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> or <paramref name="registry"/> is null.</exception>
    public static SwaggerGenOptions AddSchemaConstraintsFromValidators(
        this SwaggerGenOptions options,
        ValidationSchemaRegistry registry) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(registry);

        options.AddSchemaFilterInstance(new JeninnetValidationSchemaFilter(registry));
        return options;
    }
}
