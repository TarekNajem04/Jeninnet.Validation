//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the decorated rule belongs to one or more named rule sets.
/// Rule sets allow selective execution of validation rules.
/// </summary>
/// <param name="name">The name of the rule set.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RuleSetAttribute(string name) : Attribute {
    /// <summary>
    /// Gets the name of the rule set.
    /// </summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
}
