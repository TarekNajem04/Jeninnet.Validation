//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the enclosed rules should only run when the given asynchronous condition method returns false.
/// The method must exist on the containing type and have the signature:
/// <c>Task&lt;bool&gt; MethodName(T instance, CancellationToken ct)</c>.
/// </summary>
/// <param name="methodName">The name of the asynchronous condition method.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
public sealed class UnlessAsyncConditionAttribute(string methodName, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the name of the asynchronous condition method.
    /// </summary>
    public string MethodName { get; } = methodName;
}
