//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Defines the fluent interface for building validation rules for a specific member.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The member type.</typeparam>
public interface IRuleBuilder<T, TProperty> {
    /// <summary>
    /// Gets the property accessor used by the rule builder.
    /// </summary>
    IPropertyAccessor<T, TProperty> Accessor { get; }

    /// <summary>
    /// Adds a synchronous validation rule for the configured member.
    /// </summary>
    /// <param name="rule">The rule to add.</param>
    void AddRule(ISyncValidationRule<T> rule);

    /// <summary>
    /// Adds an asynchronous validation rule to the current validator.
    /// </summary>
    /// <param name="rule">The asynchronous validation rule to add. Cannot be null.</param>
    void AddAsyncRule(IAsyncValidationRule<T> rule);

    /// <summary>
    /// Specifies the cascade behavior to use when validating the rule.
    /// </summary>
    /// <remarks>
    /// Use this method to control whether validation should stop or continue when a rule fails.
    /// This can be useful for optimizing validation performance or customizing error reporting.
    /// </remarks>
    /// <param name="mode">
    /// The cascade mode that determines how validation should proceed when a rule fails.
    /// For example, use <see cref="CascadeMode.Stop"/> to halt further validation on failure, or <see cref="CascadeMode.Continue"/> to continue validating subsequent rules.
    /// </param>
    /// <returns>A <see cref="IRuleBuilder{T, TProperty}"/> instance configured with the specified cascade mode, allowing further rule customization.</returns>
    IRuleBuilder<T, TProperty> Cascade(CascadeMode mode);

    /// <summary>
    /// Registers sanitizers that run before this member's validation rules. Sanitization mutates
    /// the member in place, so the member must have been configured with a setter
    /// (see the <c>RuleFor</c> overload that accepts one).
    /// </summary>
    /// <param name="sanitizers">The transforms applied in order.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sanitizers"/> is null.</exception>
    /// <exception cref="ArgumentException">No sanitizer is supplied.</exception>
    /// <exception cref="InvalidOperationException">The member was configured without a setter.</exception>
    IRuleBuilder<T, TProperty> Sanitize(params Func<TProperty?, TProperty?>[] sanitizers);

    /// <summary>
    /// Registers flag-based sanitizers that run before this member's validation rules. Only
    /// string members are supported; other member types must use the transform overload.
    /// </summary>
    /// <param name="options">The sanitization flags.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The member was configured without a setter, or is not a string member.</exception>
    IRuleBuilder<T, TProperty> Sanitize(SanitizeOptions options);
}
