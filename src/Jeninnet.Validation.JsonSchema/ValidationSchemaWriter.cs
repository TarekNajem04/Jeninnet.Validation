//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.JsonSchema;

/// <summary>
/// Writes validator descriptors as deterministic JSON Schema (draft 2020-12).
/// </summary>
public static class ValidationSchemaWriter {
    /// <summary>
    /// Writes a JSON Schema object for the supplied descriptor.
    /// </summary>
    /// <param name="descriptor">The validator descriptor.</param>
    /// <param name="options">Optional schema options.</param>
    /// <returns>The schema object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="descriptor"/> is null.</exception>
    public static JsonObject Write(IValidationDescriptor descriptor, ValidationSchemaOptions? options = null) {
        ArgumentNullException.ThrowIfNull(descriptor);

        options ??= new ValidationSchemaOptions();
        return new SchemaWriteContext(options).WriteRoot(descriptor);
    }

    /// <summary>
    /// Writes a JSON Schema string for the supplied descriptor.
    /// </summary>
    /// <param name="descriptor">The validator descriptor.</param>
    /// <param name="options">Optional schema options.</param>
    /// <returns>The indented JSON Schema string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="descriptor"/> is null.</exception>
    public static string WriteToString(IValidationDescriptor descriptor, ValidationSchemaOptions? options = null) =>
        Write(descriptor, options).ToJsonString(new JsonSerializerOptions { WriteIndented = true });

    private sealed class SchemaWriteContext(ValidationSchemaOptions options) {
        private readonly Dictionary<Type, string> _typeNames = [];
        private readonly Dictionary<IValidationDescriptor, string> _descriptorNames =
            [with(ReferenceEqualityComparer.Instance)];
        private readonly Dictionary<string, JsonObject> _definitions = [with(StringComparer.Ordinal)];

        public JsonObject WriteRoot(IValidationDescriptor descriptor) {
            var root = WriteObject(descriptor);
            root.Insert(0, "$schema", options.SchemaDialect);

            if(_definitions.Count > 0) {
                var definitions = new JsonObject();

                foreach(var name in _definitions.Keys.OrderBy(static name => name, StringComparer.Ordinal)) {
                    definitions[name] = _definitions[name].DeepClone();
                }

                root["$defs"] = definitions;
            }

            return root;
        }

        public string AddReference(IValidationDescriptor descriptor) {
            if(descriptor.ValidatedType is { } type && _typeNames.TryGetValue(type, out var existing)) {
                return GetReference(existing);
            }

            if(_descriptorNames.TryGetValue(descriptor, out existing)) {
                return GetReference(existing);
            }

            var name = CreateDefinitionName(descriptor);
            _descriptorNames[descriptor] = name;

            if(descriptor.ValidatedType is { } validatedType) {
                _typeNames[validatedType] = name;
            }

            _definitions[name] = WriteObject(descriptor);
            return GetReference(name);
        }

        private static string GetReference(string name) => $"#/$defs/{name}";

        private static JsonArray WriteAdvisoryRules(IReadOnlyList<IValidationRuleDescriptor> rules) {
            var array = new JsonArray();

            foreach(var rule in rules) {
                var entry = new JsonObject {
                    ["kind"] = rule.Kind,
                    ["messageKey"] = rule.MessageKey
                };

                AddAdvisoryMetadata(entry, rule);
                array.Add((JsonNode)entry);
            }

            return array;
        }

        private static void AddAdvisoryMetadata(JsonObject entry, IValidationRuleDescriptor rule) {
            if(rule.Severity is not ValidationErrorSeverity.Error) {
                entry["severity"] = rule.Severity.ToString();
            }

            if(rule.ErrorCode is not null) {
                entry["errorCode"] = rule.ErrorCode;
            }

            if(rule.DisplayName is not null) {
                entry["displayName"] = rule.DisplayName;
            }

            if(rule.CustomState is not null) {
                entry["customState"] = JsonValueFactory.ToNode(rule.CustomState);
            }

            if(rule.HasCondition) {
                entry["hasCondition"] = true;
            }

            AddAdvisoryParameters(entry, rule);
        }

        private static void AddAdvisoryParameters(JsonObject entry, IValidationRuleDescriptor rule) {
            if(rule.Parameters.Count is 0) {
                return;
            }

            var parameters = new JsonObject();

            foreach(var parameter in rule.Parameters) {
                parameters[parameter.Key] = JsonValueFactory.ToNode(parameter.Value);
            }

            entry["parameters"] = parameters;
        }

        private static void Add(JsonObject target, string key, string? value) {
            if(value is not null) {
                target[key] = value;
            }
        }

        private static void Add(JsonObject target, string key, int? value) {
            if(value is not null) {
                target[key] = value.Value;
            }
        }

        private static void Add(JsonObject target, string key, decimal? value) {
            if(value is not null) {
                target[key] = value.Value;
            }
        }

        private JsonObject WriteConstraints(ValidationSchemaConstraints constraints) {
            var property = new JsonObject();

            Add(property, "type", constraints.Type);
            Add(property, "format", constraints.Format);

            if(constraints.AnyOfFormats is { Count: > 0 }) {
                var anyOf = new JsonArray();

                foreach(var format in constraints.AnyOfFormats) {
                    anyOf.Add((JsonNode)new JsonObject {
                        ["type"] = "string",
                        ["format"] = format
                    });
                }

                property["anyOf"] = anyOf;
            }

            Add(property, "contentEncoding", constraints.ContentEncoding);
            Add(property, "minLength", constraints.MinLength);
            Add(property, "maxLength", constraints.MaxLength);
            Add(property, "pattern", constraints.Pattern);
            Add(property, "minimum", constraints.Minimum);
            Add(property, "maximum", constraints.Maximum);
            Add(property, "exclusiveMinimum", constraints.ExclusiveMinimum);
            Add(property, "exclusiveMaximum", constraints.ExclusiveMaximum);
            Add(property, "minItems", constraints.MinItems);
            Add(property, "maxItems", constraints.MaxItems);
            Add(property, "multipleOf", constraints.MultipleOf);

            if(constraints.UniqueItems is bool uniqueItems) {
                property["uniqueItems"] = uniqueItems;
            }

            if(constraints.Enum is { Count: > 0 }) {
                property["enum"] = new JsonArray([.. constraints.Enum.Select(static value => (JsonNode?)JsonValue.Create(value))]);
            }

            if(constraints.EnumType is not null) {
                property["x-enum-type"] = constraints.EnumType;
            }

            if(constraints.FileExtensions is { Count: > 0 }) {
                property["x-file-extensions"] = new JsonArray(
                    [.. constraints.FileExtensions.Select(static value => (JsonNode?)JsonValue.Create(value))]);
            }

            if(constraints.NestedDescriptor is { } nested) {
                property["$ref"] = AddReference(nested);
            } else if(constraints.ItemDescriptor is { } item) {
                property["items"] = new JsonObject {
                    ["$ref"] = AddReference(item)
                };
            }

            if(options.IncludeAdvisoryRules && constraints.AdvisoryRules.Count > 0) {
                property["x-validation-rules"] = WriteAdvisoryRules(constraints.AdvisoryRules);
            }

            return property;
        }

        private JsonObject WriteObject(IValidationDescriptor descriptor) {
            var mapped = ValidationSchemaMapper.Map(descriptor);
            var root = new JsonObject {
                ["title"] = mapped.Title,
                ["type"] = "object"
            };

            var properties = new JsonObject();
            var required = new JsonArray();

            foreach(var property in mapped.Properties.OrderBy(static property => property.Key, StringComparer.Ordinal)) {
                properties[property.Key] = WriteConstraints(property.Value);

                if(IsRequired(property.Value)) {
                    required.Add((JsonNode?)JsonValue.Create(property.Key));
                }
            }

            root["properties"] = properties;

            if(required.Count > 0) {
                root["required"] = required;
            }

            return root;
        }

        private string CreateDefinitionName(IValidationDescriptor descriptor) {
            var baseName = GetBaseName(descriptor);
            var name = baseName;

            if(_definitions.ContainsKey(name)) {
                var suffix = 2;

                do {
                    name = $"{baseName}_{suffix}";
                    suffix++;
                } while(_definitions.ContainsKey(name));
            }

            return name;
        }

        private static string GetBaseName(IValidationDescriptor descriptor) {
            var candidate = descriptor.ValidatedType?.Name ?? descriptor.ValidatorName;
            var lastDot = candidate.LastIndexOf('.');
            if(lastDot >= 0) {
                candidate = candidate[(lastDot + 1)..];
            }

            var characters = candidate
                .Where(static character => char.IsLetterOrDigit(character) || character is '_' or '-')
                .ToArray();

            return characters.Length is 0 ? "Schema" : new string(characters);
        }

        private static bool IsRequired(ValidationSchemaConstraints constraints) {
            if(!constraints.IsRequired) {
                return false;
            }

            var memberType = constraints.MemberType;
            if(memberType is null) {
                return true;
            }

            // A non-nullable value type is always present in serialized JSON. Requiredness only
            // describes presence, not a non-default value.
            return Nullable.GetUnderlyingType(memberType) is not null || !memberType.IsValueType;
        }
    }
}
