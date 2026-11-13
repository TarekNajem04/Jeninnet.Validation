//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Converts numeric rule values to <see cref="decimal"/> for divisibility checks.
/// </summary>
internal static class NumericValueConversion {
    /// <summary>
    /// Attempts to convert a value to <see cref="decimal"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted value.</param>
    /// <param name="isNumericCandidate">
    /// Whether the value is a numeric type (or a non-empty numeric string). When this is
    /// <see langword="false"/>, the rule skips the value; when it is <see langword="true"/> and
    /// conversion failed, the rule reports an error.
    /// </param>
    /// <returns><see langword="true"/> when conversion succeeded.</returns>
    public static bool TryConvert(object? value, out decimal result, out bool isNumericCandidate) {
        isNumericCandidate = true;

        switch(value) {
            case decimal decimalValue:
                result = decimalValue;
                return true;
            case int intValue:
                result = intValue;
                return true;
            case long longValue:
                result = longValue;
                return true;
            case short shortValue:
                result = shortValue;
                return true;
            case byte byteValue:
                result = byteValue;
                return true;
            case uint uintValue:
                result = uintValue;
                return true;
            case ulong ulongValue:
                result = ulongValue;
                return true;
            case ushort ushortValue:
                result = ushortValue;
                return true;
            case sbyte sbyteValue:
                result = sbyteValue;
                return true;
            case double doubleValue:
                return TryConvertDouble(doubleValue, out result);
            case float floatValue:
                return TryConvertSingle(floatValue, out result);
            case string text:
                if(string.IsNullOrWhiteSpace(text)) {
                    isNumericCandidate = false;
                    result = 0m;
                    return false;
                }

                return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
            default:
                isNumericCandidate = false;
                result = 0m;
                return false;
        }
    }

    /// <summary>
    /// Converts a numeric divisor and rejects non-positive or non-numeric values.
    /// </summary>
    /// <typeparam name="TDivisor">The divisor type.</typeparam>
    /// <param name="divisor">The divisor.</param>
    /// <returns>The positive divisor as a <see cref="decimal"/>.</returns>
    /// <exception cref="ArgumentException">The divisor is not numeric.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The divisor is zero or negative.</exception>
    public static decimal ToPositiveDivisor<TDivisor>(TDivisor divisor) {
        if(!TryConvert(divisor, out var value, out var isNumericCandidate) || !isNumericCandidate) {
            throw new ArgumentException(
                $"The divisor '{divisor}' must be a positive finite number.",
                nameof(divisor));
        }

        if(value <= 0m) {
            throw new ArgumentOutOfRangeException(
                nameof(divisor),
                value,
                "The divisor must be greater than zero.");
        }

        return value;
    }

    private static bool TryConvertDouble(double value, out decimal result) {
        if(!double.IsFinite(value) ||
           value < (double)decimal.MinValue ||
           value > (double)decimal.MaxValue) {
            result = 0m;
            return false;
        }

        return decimal.TryParse(
            value.ToString("R", CultureInfo.InvariantCulture),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static bool TryConvertSingle(float value, out decimal result) {
        if(!float.IsFinite(value) ||
           value < (float)decimal.MinValue ||
           value > (float)decimal.MaxValue) {
            result = 0m;
            return false;
        }

        return decimal.TryParse(
            value.ToString("R", CultureInfo.InvariantCulture),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result);
    }
}
