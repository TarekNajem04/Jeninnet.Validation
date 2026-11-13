//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Validates that a string value is a valid name in the specified enum type.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property type.</typeparam>
public sealed class IsEnumNameRule<T, TValue> : SyncValidateRule<T, TValue> {
    /// <summary>
    /// Gets the enum type to check against.
    /// </summary>
    public Type EnumType { get; }

    /// <summary>
    /// Gets whether the comparison is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IsEnumNameRule{T, TValue}"/> class.
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="enumType">The enum type.</param>
    /// <param name="caseSensitive">Whether the comparison is case-sensitive.</param>
    /// <param name="message">Optional custom message.</param>
    /// <param name="messageKey">The message key.</param>
    public IsEnumNameRule(IPropertyAccessor<T, TValue> accessor, Type enumType, bool caseSensitive, string? message, string? messageKey = ValidationMessageKeys.IS_ENUM_NAME)
        : base(accessor, message, messageKey) {
        ArgumentNullException.ThrowIfNull(enumType);
        if(!enumType.IsEnum) {
            throw new ArgumentException($"The provided type '{enumType.FullName}' is not an enumeration.", nameof(enumType));
        }

        EnumType = enumType;
        CaseSensitive = caseSensitive;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IsEnumNameRule{T, TValue}"/> class (case-insensitive).
    /// </summary>
    /// <param name="accessor">The property accessor.</param>
    /// <param name="enumType">The enum type.</param>
    /// <param name="message">Optional custom message.</param>
    /// <param name="messageKey">The message key.</param>
    public IsEnumNameRule(IPropertyAccessor<T, TValue> accessor, Type enumType, string? message, string? messageKey = ValidationMessageKeys.IS_ENUM_NAME)
        : this(accessor, enumType, false, message, messageKey) { }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context)
        => new Dictionary<string, object> {
            { "0", EnumType.Name },
            { MessageParameterNames.EnumType, EnumType.Name }
        };

    /// <inheritdoc />
    protected override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        if(value is null) {
            return;
        }

        var str = value switch {
            string s => s,
            _ => value.ToString()
        };

        if(str is null) {
            return;
        }

        if(CaseSensitive) {
            if(!Enum.GetNames(EnumType).Contains(str)) {
                AddError(errors, context);
            }
        } else if(!Enum.GetNames(EnumType).Contains(str, StringComparer.OrdinalIgnoreCase)) {
            AddError(errors, context);
        }
    }
}
