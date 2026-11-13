//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.OpenApi;

/// <summary>
/// Resolves descriptor member names to the property names an OpenAPI generator emitted in a schema.
/// The lookup honors <see cref="JsonPropertyNameAttribute"/> and common naming policies
/// (camel, snake, kebab) without a second serializer configuration.
/// </summary>
internal static class SchemaPropertyNameResolver {
    internal static string? Resolve(Type modelType, IEnumerable<string> candidates, string memberName) {
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        var keys = candidates as ICollection<string> ?? [.. candidates];

        if(keys.Contains(memberName, StringComparer.Ordinal)) {
            return memberName;
        }

        var property = modelType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
        var explicitName = property?.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;

        if(explicitName is not null && keys.Contains(explicitName, StringComparer.Ordinal)) {
            return explicitName;
        }

        var caseInsensitive = keys.FirstOrDefault(
            key => string.Equals(key, memberName, StringComparison.OrdinalIgnoreCase));

        if(caseInsensitive is not null) {
            return caseInsensitive;
        }

        var normalizedMember = Normalize(memberName);

        return keys.FirstOrDefault(
            key => string.Equals(Normalize(key), normalizedMember, StringComparison.Ordinal));
    }

    private static string Normalize(string value) =>
        new([.. value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);
}
