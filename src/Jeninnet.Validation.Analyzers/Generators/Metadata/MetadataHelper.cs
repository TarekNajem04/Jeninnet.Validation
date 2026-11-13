//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Metadata;

internal static class MetadataHelper {
    private const string MESSAGE_ARGUMENT_NAME = "Message";

    public record struct AttributeParam(string? Value, string TypeName);

    public static AttributeParam GetParam(AttributeData attribute, int index) {
        if(attribute.ConstructorArguments.Length <= index) {
            return new AttributeParam(null, "object");
        }

        var arg = attribute.ConstructorArguments[index];
        var value = FormatConstant(arg.Value);
        var typeName = arg.Type?.GetFullTypeName() ?? "object";

        return new AttributeParam(value, typeName);
    }

    /// <summary>
    /// Formats an attribute constant with the invariant culture so generated source does not depend
    /// on the build machine culture.
    /// </summary>
    /// <param name="value">The constant value.</param>
    /// <returns>The invariant text, or <see langword="null"/>.</returns>
    public static string? FormatConstant(object? value) =>
        value switch {
            null => null,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };

    public static IReadOnlyList<string?> GetArrayParam(AttributeData attribute, int index) {
        if(attribute.ConstructorArguments.Length <= index) {
            return [];
        }

        var arg = attribute.ConstructorArguments[index];
        if(arg.Kind is not TypedConstantKind.Array) {
            return arg.Value is string single ? [single] : [];
        }

        var values = new List<string?>(arg.Values.Length);
        foreach(var item in arg.Values) {
            values.Add(item.Value as string);
        }

        return values;
    }

    public static T? GetConstructorArgument<T>(AttributeData attribute, int index, bool throwException = false, T? defaultValue = default) {
        var args = attribute.ConstructorArguments;
        if(args.Length <= index) {
            if(throwException) {
                throw new ArgumentOutOfRangeException($"Missing constructor argument at index {index} for {attribute}");
            }

            return defaultValue;
        }

        var value = args[index].Value;
        return value is T tValue ? tValue : defaultValue;
    }

    public static T? GetNamedArgument<T>(AttributeData attribute, string name) {
        foreach(var named in attribute.NamedArguments) {
            if(named.Key == name) {
                return (T?)named.Value.Value;
            }
        }

        return default;
    }

    public static string? GetMessageArgument(AttributeData attribute) => GetNamedArgument<string>(attribute, MESSAGE_ARGUMENT_NAME);

    public static bool TryGetNamedArgument<T>(AttributeData attribute, string name, out T? value) {
        foreach(var named in attribute.NamedArguments) {
            if(named.Key == name) {
                value = named.Value.Value is T typed ? typed : default;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Reads a named enum argument and maps the validation severity values to their stable names.
    /// </summary>
    /// <param name="attribute">The attribute data to read.</param>
    public static string? GetNamedSeverity(AttributeData attribute) {
        if(!TryGetNamedArgument<object>(attribute, "Severity", out var raw) || raw is null) {
            return null;
        }

        return raw is int value
            ? value switch {
                0 => "Info",
                1 => "Warning",
                2 => "Error",
                _ => null
            }
            : null;
    }
}
