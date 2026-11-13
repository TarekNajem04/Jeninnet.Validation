//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Blazor;

/// <summary>
/// Resolves Jeninnet.Validation member paths (for example
/// <c>Address.Street</c> or <c>Lines[0].ProductName</c>) to Blazor
/// <see cref="FieldIdentifier"/> values.
/// </summary>
public static class FieldIdentifierResolver {
    private static readonly ConcurrentDictionary<(Type Type, string Name), PropertyInfo?> _properties = new();

    /// <summary>
    /// Resolves a member path against the supplied model.
    /// </summary>
    /// <param name="model">The root model.</param>
    /// <param name="memberName">The member path reported by the validator.</param>
    /// <returns>
    /// A field identifier for the resolved owner and leaf member; when the path cannot be
    /// resolved, the root model and the full path are used.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="memberName"/> is empty or whitespace.</exception>
    public static FieldIdentifier Resolve(object model, string memberName) {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        var segments = memberName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var current = model;

        for(var i = 0; i < segments.Length - 1; i++) {
            if(!TryStep(current, segments[i], out var next)) {
                return new FieldIdentifier(model, memberName);
            }

            current = next;
        }

        var leaf = segments.Length > 0 ? segments[^1] : memberName;
        var bracket = leaf.IndexOf('[');

        if(bracket >= 0) {
            leaf = leaf[..bracket];
        }

        return new FieldIdentifier(current, leaf);
    }

    private static bool TryStep(object current, string segment, out object next) {
        next = current;

        var bracket = segment.IndexOf('[');
        var propertyName = bracket >= 0 ? segment[..bracket] : segment;
        var property = GetProperty(current.GetType(), propertyName);

        if(property is null) {
            return false;
        }

        var value = property.GetValue(current);

        if(value is null) {
            return false;
        }

        if(bracket < 0) {
            next = value;
            return true;
        }

        var close = segment.IndexOf(']', bracket + 1);

        if(close < 0 ||
           !int.TryParse(segment[(bracket + 1)..close], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) ||
           value is not IList list ||
           index < 0 ||
           index >= list.Count ||
           list[index] is not { } item) {
            return false;
        }

        next = item;
        return true;
    }

    private static PropertyInfo? GetProperty(Type type, string name) =>
        _properties.GetOrAdd(
            (type, name),
            static key => key.Type.GetProperty(
                key.Name,
                BindingFlags.Public | BindingFlags.Instance));
}
