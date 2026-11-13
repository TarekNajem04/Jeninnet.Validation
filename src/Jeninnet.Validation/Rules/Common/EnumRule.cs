//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a member's value is a valid member of an enumeration.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
public sealed class EnumRule<T, TValue> : SyncValidateRule<T, TValue> {
    private Type? _metadataEnumType;
    private bool _isFlags;
    private bool _signedUnderlying;
    private ulong _flagsMask;

    /// <summary>
    /// Gets the type of the enumeration to validate against.
    /// </summary>
    public Type EnumType { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumRule{T, TProperty}"/> class.
    /// </summary>
    /// <param name="accessor">The property accessor to extract the value.</param>
    /// <param name="enumType">The type of the enumeration to validate against.</param>
    /// <param name="message">The custom validation message, if any.</param>
    /// <param name="messageKey">The key for the validation message.</param>
    public EnumRule(IPropertyAccessor<T, TValue> accessor, Type enumType, string? message = null, string? messageKey = ValidationMessageKeys.ENUM) : base(accessor, message, messageKey) {
        ArgumentNullException.ThrowIfNull(enumType);

        if(!enumType.IsEnum) {
            throw new ArgumentException($"The provided type '{enumType.FullName}' is not an enumeration.", nameof(enumType));
        }

        EnumType = enumType;
        EnsureMetadata();
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context)
        => new Dictionary<string, object> {
            { "0", EnumType.Name },
            { MessageParameterNames.EnumType, EnumType.Name }
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> { { MessageParameterNames.EnumType, EnumType.Name } };

    /// <inheritdoc />
    protected override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        if(value is null) {
            return;
        }

        EnsureMetadata();

        var valid = _isFlags
            ? IsValidFlagsValue(value)
            : Enum.IsDefined(EnumType, value);

        if(!valid) {
            AddError(errors, context);
        }
    }

    private void EnsureMetadata() {
        if(ReferenceEquals(_metadataEnumType, EnumType)) {
            return;
        }

        _metadataEnumType = EnumType;
        _isFlags = EnumType.IsDefined(typeof(FlagsAttribute), inherit: false);
        _flagsMask = 0;

        if(!_isFlags) {
            _signedUnderlying = false;
            return;
        }

        _signedUnderlying = Type.GetTypeCode(Enum.GetUnderlyingType(EnumType)) is
            TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64;

        foreach(var declared in Enum.GetValuesAsUnderlyingType(EnumType)) {
            if(declared is not null) {
                _flagsMask |= ToBits(declared);
            }
        }
    }

    private bool IsValidFlagsValue(TValue value) {
        var bits = ToBits(value!);

        // Zero is always a valid flags value; otherwise every set bit must be declared.
        return bits is 0 || (bits & ~_flagsMask) is 0;
    }

    private ulong ToBits(object value) =>
        _signedUnderlying
            ? unchecked((ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture))
            : Convert.ToUInt64(value, CultureInfo.InvariantCulture);
}
