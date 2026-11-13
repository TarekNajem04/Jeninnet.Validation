//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Serialization;

/// <summary>
/// Creates JSON values from descriptor parameter primitives without reflection.
/// Shared by the schema and client-rule writers.
/// </summary>
internal static class JsonValueFactory {
    internal static JsonValue? ToNode(object? value) =>
        value switch {
            null => null,
            string text => JsonValue.Create(text),
            bool boolean => JsonValue.Create(boolean),
            int number => JsonValue.Create(number),
            long number => JsonValue.Create(number),
            decimal number => JsonValue.Create(number),
            double number => JsonValue.Create(number),
            float number => JsonValue.Create(number),
            _ => JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture))
        };
}
