//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.OpenApi;

/// <summary>
/// Maps validated model types to their validator descriptors.
/// </summary>
public sealed class ValidationSchemaRegistry {
    private readonly Dictionary<Type, IValidationDescriptor> _descriptors = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationSchemaRegistry"/> class.
    /// </summary>
    /// <param name="providers">The descriptor providers.</param>
    /// <exception cref="ArgumentNullException"><paramref name="providers"/> is null.</exception>
    public ValidationSchemaRegistry(IEnumerable<IValidationDescriptorProvider> providers) {
        ArgumentNullException.ThrowIfNull(providers);

        foreach(var provider in providers) {
            var descriptor = provider.Describe();

            if(descriptor.ValidatedType is { } validatedType) {
                _descriptors[validatedType] = descriptor;
            }
        }
    }

    /// <summary>
    /// Tries to get the descriptor for a validated model type.
    /// </summary>
    /// <param name="modelType">The model type.</param>
    /// <param name="descriptor">The descriptor when found.</param>
    /// <returns><see langword="true"/> when a descriptor exists.</returns>
    public bool TryGet(Type modelType, out IValidationDescriptor? descriptor) {
        ArgumentNullException.ThrowIfNull(modelType);

        return _descriptors.TryGetValue(modelType, out descriptor);
    }
}
