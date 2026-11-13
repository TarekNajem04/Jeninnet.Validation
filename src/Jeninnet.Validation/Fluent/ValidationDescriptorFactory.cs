//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Builds a <see cref="ValidationDescriptor"/> from a fluent validator's configured rules.
/// </summary>
internal static class ValidationDescriptorFactory {
    /// <summary>
    /// Creates a descriptor for the supplied validator definition.
    /// </summary>
    /// <typeparam name="T">The validated type.</typeparam>
    /// <param name="definition">The validator definition.</param>
    /// <returns>The descriptor.</returns>
    public static ValidationDescriptor Create<T>(ValidatorDefinition<T> definition) {
        ArgumentNullException.ThrowIfNull(definition);

        var builder = new ValidationDescriptorBuilder(
            definition.GetType().FullName ?? typeof(T).FullName!,
            typeof(T));

        foreach(var rule in definition.SyncRules) {
            builder.AddRule(rule);
        }

        foreach(var rule in definition.AsyncRules) {
            builder.AddRule(rule);
        }

        return builder.Build();
    }
}
