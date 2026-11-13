//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Implements <see cref="IValidatorFactory"/> as an injectable service.
/// The original static helper methods have been converted to instance methods.
/// </summary>
internal sealed class ValidatorFactoryImplementation : IValidatorFactory {
    /// <inheritdoc/>
    public IPropertyAccessor<T, TProperty> Accessor<T, TProperty>(Func<T, TProperty> getter, string memberName) =>
        new PropertyAccessor<T, TProperty>(getter, memberName);

    /// <inheritdoc/>
    public IPropertyAccessor<T, TProperty?> ToNullable<T, TProperty>(IPropertyAccessor<T, TProperty> accessor)
        where TProperty : struct {
        ArgumentNullException.ThrowIfNull(accessor);
        return new PropertyAccessor<T, TProperty?>(x => accessor.GetValue(x), accessor.MemberName);
    }

    /// <inheritdoc/>
    public IPropertyAccessor<T, TProperty> AsNonNullable<T, TProperty>(IPropertyAccessor<T, TProperty?> accessor)
        where TProperty : struct {
        ArgumentNullException.ThrowIfNull(accessor);
        return new PropertyAccessor<T, TProperty>(x => accessor.GetValue(x).GetValueOrDefault(), accessor.MemberName);
    }

    /// <inheritdoc/>
    public IPropertyAccessor<T, TProperty> NotNullAccessor<T, TProperty>(Func<T, TProperty> getter, string memberName)
        where TProperty : notnull => new PropertyAccessor<T, TProperty>(getter, memberName);
}
