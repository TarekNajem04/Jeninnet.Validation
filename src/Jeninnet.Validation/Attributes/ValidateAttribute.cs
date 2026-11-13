//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Marks a class or struct as containing validation metadata.
/// The source generator will produce a validator for any type annotated with this attribute.
/// </summary>
/// <param name="targetNamespace">The namespace where the generated validator should be placed. If null, it's placed in the same namespace as the model.</param>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
    Inherited = false,
    AllowMultiple = false)]
public sealed class ValidateAttribute(string? targetNamespace = null) : Attribute {
    /// <summary>
    /// Gets the namespace where the generated validator is placed.
    /// </summary>
    public string? TargetNamespace { get; } = targetNamespace;

    /// <summary>
    /// Gets or sets a value indicating whether the generator also emits
    /// <c>Validate()</c>/<c>ValidateAsync()</c> extension methods on the model type.
    /// The default is <see langword="true"/>.
    /// </summary>
    public bool GenerateModelExtensions { get; set; } = true;
}
