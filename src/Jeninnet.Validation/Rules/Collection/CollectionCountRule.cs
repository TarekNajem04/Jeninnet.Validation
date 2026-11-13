//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a collection member contains a number of elements within the given inclusive range.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The collection property type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="min">The minimum number of elements.</param>
/// <param name="max">The maximum number of elements.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class CountRule<T, TProperty>(
    IPropertyAccessor<T, TProperty> accessor,
    int min,
    int max,
    string? message,
    string? messageKey = ValidationMessageKeys.COUNT)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <summary>
    /// Gets the minimum number of elements.
    /// </summary>
    public int Min { get; } = min;

    /// <summary>
    /// Gets the maximum number of elements.
    /// </summary>
    public int Max { get; } = max;

    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(value is not IEnumerable enumerable) {
            return;
        }

        var count = CollectionCounting.Count(enumerable);
        if(count < Min || count > Max) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) {
        var value = Accessor.GetValue(context.Instance);
        var count = value is IEnumerable enumerable ? CollectionCounting.Count(enumerable) : 0;

        return new Dictionary<string, object> {
            { "0", Min },
            { "1", Max },
            { MessageParameterNames.Min, Min },
            { MessageParameterNames.Max, Max },
            { MessageParameterNames.Count, count },
            { MessageParameterNames.PropertyValue, value! },
            { "PropertyValue.Count", count }
        };
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> {
            { MessageParameterNames.Min, Min },
            { MessageParameterNames.Max, Max }
        };
}

/// <summary>
/// Ensures that a collection member contains at least the given number of elements.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The collection property type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="min">The minimum number of elements.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class MinCountRule<T, TProperty>(
    IPropertyAccessor<T, TProperty> accessor,
    int min,
    string? message,
    string? messageKey = ValidationMessageKeys.MIN_COUNT)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <summary>
    /// Gets the minimum number of elements.
    /// </summary>
    public int Min { get; } = min;

    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(value is not IEnumerable enumerable) {
            return;
        }

        if(CollectionCounting.Count(enumerable) < Min) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) {
        var value = Accessor.GetValue(context.Instance);
        var count = value is IEnumerable enumerable ? CollectionCounting.Count(enumerable) : 0;

        return new Dictionary<string, object> {
            { "0", Min },
            { MessageParameterNames.Min, Min },
            { MessageParameterNames.Count, count },
            { MessageParameterNames.PropertyValue, value! },
            { "PropertyValue.Count", count }
        };
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> { { MessageParameterNames.Min, Min } };
}

/// <summary>
/// Ensures that a collection member contains at most the given number of elements.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The collection property type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="max">The maximum number of elements.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class MaxCountRule<T, TProperty>(
    IPropertyAccessor<T, TProperty> accessor,
    int max,
    string? message,
    string? messageKey = ValidationMessageKeys.MAX_COUNT)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <summary>
    /// Gets the maximum number of elements.
    /// </summary>
    public int Max { get; } = max;

    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(value is not IEnumerable enumerable) {
            return;
        }

        if(CollectionCounting.Count(enumerable) > Max) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) {
        var value = Accessor.GetValue(context.Instance);
        var count = value is IEnumerable enumerable ? CollectionCounting.Count(enumerable) : 0;

        return new Dictionary<string, object> {
            { "0", Max },
            { MessageParameterNames.Max, Max },
            { MessageParameterNames.Count, count },
            { MessageParameterNames.PropertyValue, value! },
            { "PropertyValue.Count", count }
        };
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> { { MessageParameterNames.Max, Max } };
}

file static class CollectionCounting {
    public static int Count(IEnumerable enumerable) {
        if(enumerable is ICollection collection) {
            return collection.Count;
        }

        var count = 0;
        foreach(var _ in enumerable) {
            count++;
        }

        return count;
    }
}
