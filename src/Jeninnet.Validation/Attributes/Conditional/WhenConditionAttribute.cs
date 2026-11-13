//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the enclosed rules should only run when the given synchronous condition method
/// returns <see langword="true"/>. The method must be declared on the containing type and have one
/// of these signatures:
/// <c>bool MethodName(T instance)</c>,
/// <c>bool MethodName(T instance, ValidationContext&lt;T&gt; context)</c>, or
/// <c>bool MethodName(T instance, ValidationContext&lt;T&gt; context, ValidationErrorCollector errors)</c>.
/// </summary>
/// <param name="methodName">The name of the condition method.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
public sealed class WhenConditionAttribute(string methodName) : Attribute {
    /// <summary>
    /// Gets the name of the condition method.
    /// </summary>
    public string MethodName { get; } = methodName;
}
