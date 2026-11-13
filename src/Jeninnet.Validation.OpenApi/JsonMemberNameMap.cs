//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.OpenApi;

/// <summary>
/// Resolves CLR member names to the names produced by <see cref="JsonSerializerOptions"/>.
/// The cache is keyed by <see cref="JsonTypeInfo"/>, so different serializer options do not share
/// a naming map.
/// </summary>
internal static class JsonMemberNameMap {
    private static readonly ConditionalWeakTable<JsonTypeInfo, Dictionary<string, string>> _maps = [];

    internal static string? Resolve(JsonTypeInfo typeInfo, string descriptorMemberName) {
        ArgumentNullException.ThrowIfNull(typeInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptorMemberName);

        var map = _maps.GetValue(typeInfo, static info => Create(info));

        if(map.TryGetValue(descriptorMemberName, out var serializedName)) {
            return serializedName;
        }

        // A manually constructed descriptor may already contain the serialized name.
        if(map.ContainsValue(descriptorMemberName)) {
            return descriptorMemberName;
        }

        // Preserve the previous case-insensitive fallback for descriptors that do not map cleanly.
        return map.FirstOrDefault(
            pair => string.Equals(pair.Key, descriptorMemberName, StringComparison.OrdinalIgnoreCase)).Value;
    }

    internal static string? Resolve(JsonSerializerOptions? options, Type? modelType, string descriptorMemberName) {
        if(options is null || modelType is null) {
            return descriptorMemberName;
        }

        try {
            return Resolve(options.GetTypeInfo(modelType), descriptorMemberName) ?? descriptorMemberName;
        }
        catch(NotSupportedException) {
            // Options without an explicit resolver may still be configurable by the application.
            // Fall back to the reflection resolver so nested names use the same policy/attributes.
            try {
                var typeInfo = new DefaultJsonTypeInfoResolver().GetTypeInfo(modelType, options);
                return Resolve(typeInfo, descriptorMemberName) ?? descriptorMemberName;
            }
            catch(NotSupportedException) {
                return descriptorMemberName;
            }
        }
    }

    private static Dictionary<string, string> Create(JsonTypeInfo typeInfo) {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach(var property in typeInfo.Properties) {
            var clrName = property.AttributeProvider is MemberInfo member
                ? member.Name
                : property.Name;

            map[clrName] = property.Name;
            map.TryAdd(property.Name, property.Name);
        }

        return map;
    }
}
