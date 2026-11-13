//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <inheritdoc cref="IPropertyAccessor{T, TProperty}"/>>
public readonly record struct PropertyAccessor<T, TProperty> : IPropertyAccessor<T, TProperty> {
    /// <inheritdoc cref="IPropertyAccessor{T, TProperty}.Getter"/>>
    public Func<T, TProperty> Getter { get; }

    /// <inheritdoc cref="IPropertyAccessor{T, TProperty}.MemberName"/>>
    public string MemberName { get; }

    /// <inheritdoc cref="IPropertyAccessor{T, TProperty}.GetValue"/>>
    public TProperty GetValue(T instance) => Getter(instance);

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyAccessor{T, TProperty}"/> record struct.
    /// </summary>
    /// <param name="getter">The function that retrieves the property value from an instance.</param>
    /// <param name="memberName">The name of the property.</param>
    public PropertyAccessor(Func<T, TProperty> getter, string memberName) {
        Getter = getter;
        MemberName = memberName;
    }
}
