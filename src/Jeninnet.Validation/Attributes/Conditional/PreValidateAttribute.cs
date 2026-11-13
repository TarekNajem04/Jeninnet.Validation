//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Declares a static pre-validation hook for a generated validator. The method must return
/// <see langword="false"/> to stop validation and may add errors through a
/// <see cref="ValidationErrorCollector"/>:
/// <c>bool MethodName(T instance, ValidationContext&lt;T&gt; context)</c> or
/// <c>bool MethodName(T instance, ValidationContext&lt;T&gt; context, ValidationErrorCollector errors)</c>.
/// </summary>
/// <param name="methodName">The name of the static hook method.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PreValidateAttribute(string methodName) : Attribute {
    /// <summary>
    /// Gets the name of the static hook method.
    /// </summary>
    public string MethodName { get; } = methodName;
}
