//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Static façade that forwards calls to the DI‑registered <see cref="IValidatorFactory"/>.
/// This preserves the original generated‑code contract that expects a static class named <c>ValidatorFactory</c>.
/// The provider is set during service registration.
/// </summary>
public static class ValidatorFactory {
    private static IValidatorFactory Fallback { get; } = new ValidatorFactoryImplementation();
    private static IServiceProvider? Provider { get; set; }

    internal static void SetProvider(IServiceProvider provider) => Provider = provider;

    private static IValidatorFactory Factory => Provider?.GetService<IValidatorFactory>() ?? Fallback;

    /// <summary>
    /// Creates a property accessor for the specified getter and member name.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="getter">The function that retrieves the property value.</param>
    /// <param name="memberName">The name of the property.</param>
    /// <returns>An <see cref="IPropertyAccessor{T, TProperty}"/> for the specified member.</returns>
    public static IPropertyAccessor<T, TProperty> Accessor<T, TProperty>(Func<T, TProperty> getter, string memberName) =>
        Factory.Accessor(getter, memberName);

    /// <summary>
    /// Wraps a non-nullable accessor as a nullable accessor.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The underlying value type.</typeparam>
    /// <param name="accessor">The non-nullable accessor.</param>
    /// <returns>A nullable accessor for the same property.</returns>
    public static IPropertyAccessor<T, TProperty?> ToNullable<T, TProperty>(IPropertyAccessor<T, TProperty> accessor)
        where TProperty : struct =>
        Factory.ToNullable(accessor);

    /// <summary>
    /// Wraps a nullable accessor as a non-nullable accessor.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The underlying value type.</typeparam>
    /// <param name="accessor">The nullable accessor.</param>
    /// <returns>A non-nullable accessor for the same property.</returns>
    public static IPropertyAccessor<T, TProperty> AsNonNullable<T, TProperty>(IPropertyAccessor<T, TProperty?> accessor)
        where TProperty : struct =>
        Factory.AsNonNullable(accessor);

    /// <summary>
    /// Creates a property accessor that ensures the value is not null.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="getter">The function that retrieves the property value.</param>
    /// <param name="memberName">The name of the property.</param>
    /// <returns>An <see cref="IPropertyAccessor{T, TProperty}"/> that enforces a non-null value.</returns>
    public static IPropertyAccessor<T, TProperty> NotNullAccessor<T, TProperty>(Func<T, TProperty> getter, string memberName)
        where TProperty : notnull =>
        Factory.NotNullAccessor(getter, memberName);
}
