//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Declares a static failure hook for a generated validator. The method must have the signature
/// <c>void MethodName(ValidationResult result)</c> and is called once when validation fails.
/// </summary>
/// <param name="methodName">The name of the static hook method.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class OnFailureAttribute(string methodName) : Attribute {
    /// <summary>
    /// Gets the name of the static hook method.
    /// </summary>
    public string MethodName { get; } = methodName;
}
