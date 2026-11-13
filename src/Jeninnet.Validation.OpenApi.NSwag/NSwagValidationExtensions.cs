//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.OpenApi.NSwag;

/// <summary>
/// Registration helpers for NSwag/NJsonSchema schema constraints.
/// </summary>
public static class NSwagValidationExtensions {
    /// <summary>
    /// Adds a schema processor that applies the constraints described by the supplied validators.
    /// </summary>
    /// <param name="settings">The JSON schema generator settings.</param>
    /// <param name="validators">The validator descriptor providers.</param>
    /// <returns>The same settings for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> or <paramref name="validators"/> is null.</exception>
    public static JsonSchemaGeneratorSettings AddSchemaConstraintsFromValidators(
        this JsonSchemaGeneratorSettings settings,
        params IValidationDescriptorProvider[] validators) {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(validators);

        return settings.AddSchemaConstraintsFromValidators(new ValidationSchemaRegistry(validators));
    }

    /// <summary>
    /// Adds a schema processor that applies constraints from an existing registry.
    /// </summary>
    /// <param name="settings">The JSON schema generator settings.</param>
    /// <param name="registry">The descriptor registry.</param>
    /// <returns>The same settings for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> or <paramref name="registry"/> is null.</exception>
    public static JsonSchemaGeneratorSettings AddSchemaConstraintsFromValidators(
        this JsonSchemaGeneratorSettings settings,
        ValidationSchemaRegistry registry) {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(registry);

        settings.SchemaProcessors.Add(new JeninnetValidationSchemaProcessor(registry));
        return settings;
    }

    /// <summary>
    /// Adds a schema processor to the NSwag document settings.
    /// </summary>
    /// <param name="settings">The NSwag document generator settings.</param>
    /// <param name="validators">The validator descriptor providers.</param>
    /// <returns>The same settings for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> or <paramref name="validators"/> is null.</exception>
    public static OpenApiDocumentGeneratorSettings AddSchemaConstraintsFromValidators(
        this OpenApiDocumentGeneratorSettings settings,
        params IValidationDescriptorProvider[] validators) {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(validators);

        settings.SchemaSettings.AddSchemaConstraintsFromValidators(validators);
        return settings;
    }

    /// <summary>
    /// Adds a schema processor from an existing registry to the NSwag document settings.
    /// </summary>
    /// <param name="settings">The NSwag document generator settings.</param>
    /// <param name="registry">The descriptor registry.</param>
    /// <returns>The same settings for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> or <paramref name="registry"/> is null.</exception>
    public static OpenApiDocumentGeneratorSettings AddSchemaConstraintsFromValidators(
        this OpenApiDocumentGeneratorSettings settings,
        ValidationSchemaRegistry registry) {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(registry);

        settings.SchemaSettings.AddSchemaConstraintsFromValidators(registry);
        return settings;
    }
}
