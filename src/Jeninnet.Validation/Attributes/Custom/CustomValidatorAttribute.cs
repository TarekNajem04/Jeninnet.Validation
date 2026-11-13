//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Declares a custom validation rule that delegates to a static method on a rules type.
/// The method must return <see cref="bool"/> or <see cref="ValidationResult"/> for synchronous
/// rules, or <see cref="System.Threading.Tasks.ValueTask{TResult}"/> of either type when
/// <see cref="IsAsync"/> is <see langword="true"/>.
/// </summary>
/// <param name="validatorType">The type that declares the static validator method.</param>
/// <param name="methodName">The name of the static validator method.</param>
/// <param name="message">Optional custom validation message used when a predicate returns false.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
public sealed class CustomValidatorAttribute(
    Type validatorType,
    string methodName,
    string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the type that declares the static validator method.
    /// </summary>
    public Type ValidatorType { get; } = validatorType;

    /// <summary>
    /// Gets the name of the static validator method.
    /// </summary>
    public string MethodName { get; } = methodName;

    /// <summary>
    /// Gets or sets a value indicating whether the validator method is asynchronous.
    /// </summary>
    public bool IsAsync { get; set; }
}
