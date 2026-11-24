//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.OpenApi;

namespace Jeninnet.Validation.OpenApi.Adapters.Tests;

internal enum ConformanceKind {
    None = 0,
    Basic = 1,
    Premium = 2
}

internal sealed class ConformanceAddress {
    public string? City { get; set; }
}

internal sealed class ConformanceLine {
    public string? Sku { get; set; }
}

internal sealed class ConformanceModel {
    public string? Name { get; set; }

    public string? FirstName { get; set; }

    public string? Email { get; set; }

    public string? Code { get; set; }

    public int Age { get; set; }

    public decimal Price { get; set; }

    public List<string>? Tags { get; set; }

    public string? Address { get; set; }

    public string? Color { get; set; }

    public ConformanceKind Kind { get; set; }

    public ConformanceAddress? Home { get; set; }

    public List<ConformanceLine>? Lines { get; set; }

    public string? Conditional { get; set; }

    [JsonPropertyName("custom-code")]
    public string? CustomCode { get; set; }
}

file sealed class ConformanceModelValidator : Validator<ConformanceModel> {
    protected override void Configure(ValidatorRuleBuilder<ConformanceModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(ConformanceModel.Name))
               .NotEmpty()
               .Length(2, 50);

        builder.RuleFor(static model => model.FirstName, nameof(ConformanceModel.FirstName))
               .Length(2, 20);

        builder.RuleFor(static model => model.Email, nameof(ConformanceModel.Email))
               .NotEmpty()
               .EmailAddress();

        builder.RuleFor(static model => model.Code, nameof(ConformanceModel.Code))
               .Matches("^[A-Z]{3}$");

        builder.RuleFor(static model => model.Age, nameof(ConformanceModel.Age))
               .GreaterThan(18)
               .LessThan(120)
               .MultipleOf(2);

        builder.RuleFor(static model => model.Price, nameof(ConformanceModel.Price))
               .ScalePrecision(5, 2);

        builder.RuleFor(static model => model.Tags, nameof(ConformanceModel.Tags))
               .Count(1, 5)
               .Unique();

        builder.RuleFor(static model => model.Address, nameof(ConformanceModel.Address))
               .IPAddress();

        builder.RuleFor(static model => model.Color, nameof(ConformanceModel.Color))
               .In(["red", "green"]);

        builder.RuleFor(static model => model.Kind, nameof(ConformanceModel.Kind))
               .Enum();

        builder.RuleFor(static model => model.Home, nameof(ConformanceModel.Home))
               .SetValidator(new ConformanceAddressValidator());

        builder.RuleFor(static model => model.Lines, nameof(ConformanceModel.Lines))
               .ForEach(new ConformanceLineValidator());

        builder.RuleFor(static model => model.Conditional, nameof(ConformanceModel.Conditional))
               .NotEmpty()
               .When(static model => model.Age >= 18);

        builder.RuleFor(static model => model.CustomCode, nameof(ConformanceModel.CustomCode))
               .NotEmpty();
    }
}

file sealed class ConformanceAddressValidator : Validator<ConformanceAddress> {
    protected override void Configure(ValidatorRuleBuilder<ConformanceAddress> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.City, nameof(ConformanceAddress.City))
               .NotEmpty();
    }
}

file sealed class ConformanceLineValidator : Validator<ConformanceLine> {
    protected override void Configure(ValidatorRuleBuilder<ConformanceLine> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Sku, nameof(ConformanceLine.Sku))
               .NotEmpty();
    }
}

[TestClass]
public sealed class SchemaConformanceTests {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    [TestMethod]
    public void ConformanceSnapshots_MatchApprovedHashes() {
        var snapshots = BuildAllSnapshots();

        if(string.Equals(Environment.GetEnvironmentVariable("UPDATE_OPENAPI_SNAPSHOTS"), "1", StringComparison.Ordinal)) {
            WriteSnapshots(snapshots);
            return;
        }

        var approvals = ReadApprovals();
        var errors = new List<string>();

        foreach(var (adapter, snapshot) in snapshots.OrderBy(static pair => pair.Key, StringComparer.Ordinal)) {
            errors.AddRange(ValidateSnapshot(adapter, snapshot, approvals));
        }

        Assert.IsTrue(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public void SnapshotDrift_IsRejected() {
        var snapshot = BuildAllSnapshots()["json-schema"];
        var altered = snapshot.DeepClone().AsObject();
        altered["properties"]!["Name"]!["minLength"] = 999;

        var errors = ValidateSnapshot(
            "json-schema",
            altered,
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["json-schema"] = "0000000000000000000000000000000000000000000000000000000000000000"
            });

        Assert.IsTrue(errors.Count > 0, "a changed snapshot must be rejected until approved");
    }

    [TestMethod]
    public void Adaptors_AgreeOnTheCommonConstraintVocabulary() {
        var snapshots = BuildAllSnapshots();
        var baseline = ToCommonSnapshot(snapshots["microsoft-openapi"]);

        foreach(var adapter in new[] { "swashbuckle", "nswag", "json-schema" }) {
            var actual = ToCommonSnapshot(snapshots[adapter]);

            MSTestAssertions.That(() => JsonNode.DeepEquals(baseline, actual).Should().BeTrue(
                $"adapter '{adapter}' diverged from the Microsoft OpenAPI constraints"));
        }
    }

    [TestMethod]
    public void Adaptors_PreserveKnownIntentionalDifferences() {
        var snapshots = BuildAllSnapshots();

        MSTestAssertions.That(() => snapshots["json-schema"]["properties"]!["CustomCode"]!["jsonName"]!
            .GetValue<string>().Should().Be("CustomCode"));
        MSTestAssertions.That(() => snapshots["swashbuckle"]["properties"]!["CustomCode"]!["jsonName"]!
            .GetValue<string>().Should().Be("custom-code"));
        MSTestAssertions.That(() => snapshots["json-schema"]["properties"]!["Conditional"]!["advisory"]!
            .AsArray().Count.Should().Be(1));
        MSTestAssertions.That(() => snapshots["microsoft-openapi"]["properties"]!["Conditional"]!["advisory"]
            .Should().BeNull());
        MSTestAssertions.That(() => snapshots["microsoft-openapi"]["properties"]!["Home"]!["ref"]!
            .GetValue<string>().Should().Be(nameof(ConformanceAddress)));
        MSTestAssertions.That(() => snapshots["swashbuckle"]["properties"]!["Home"]!["ref"]!
            .GetValue<string>().Should().Be(nameof(ConformanceAddress)));
    }

    private static Dictionary<string, JsonObject> BuildAllSnapshots() =>
        new(StringComparer.Ordinal) {
            ["microsoft-openapi"] = BuildMicrosoftSnapshot(),
            ["swashbuckle"] = BuildSwashbuckleSnapshot(),
            ["nswag"] = BuildNSwagSnapshot(),
            ["json-schema"] = BuildJsonSchemaSnapshot()
        };

    private static JsonObject BuildMicrosoftSnapshot() {
        var descriptor = new ConformanceModelValidator().Describe();
        var mapped = ValidationSchemaMapper.Map(descriptor);
        var memberTypes = BuildMemberTypes(typeof(ConformanceModel));
        var jsonNames = BuildJsonNames(typeof(ConformanceModel));
        var properties = new JsonObject();
        var definitions = new JsonObject();

        foreach(var member in descriptor.Members.OrderBy(static member => member.Name, StringComparer.Ordinal)) {
            var schema = new OpenApiSchema();
            ValidationSchemaApplier.Apply(mapped.Properties[member.Name], schema, JsonOptions);
            var memberType = memberTypes[member.Name];
            var jsonName = jsonNames[member.Name];
            var normalized = NormalizeOpenApiProperty(schema, memberType, jsonName);
            normalized["required"] = IsRequired(mapped.Properties[member.Name], memberType);
            properties[member.Name] = normalized;

            if(IsNestedObject(memberType) && schema.Properties is { Count: > 0 }) {
                definitions[memberType.Name] = NormalizeOpenApiContainer(schema, memberType);
            }

            if(GetCollectionElementType(memberType) is { } elementType &&
               schema.Items is OpenApiSchema itemSchema &&
               itemSchema.Properties is { Count: > 0 }) {
                definitions[elementType.Name] = NormalizeOpenApiContainer(itemSchema, elementType);
            }
        }

        return BuildSnapshot("microsoft-openapi", properties, definitions, advisory: null);
    }

    private static JsonObject BuildSwashbuckleSnapshot() {
        var repository = new SchemaRepository();
        var options = new SchemaGeneratorOptions();
        options.SchemaFilters.Add(new JeninnetValidationSchemaFilter(new ValidationSchemaRegistry([
            new ConformanceModelValidator(),
            new ConformanceAddressValidator(),
            new ConformanceLineValidator()
        ])));

        var generator = new SchemaGenerator(
            options,
            new JsonSerializerDataContractResolver(JsonOptions));
        _ = generator.GenerateSchema(typeof(ConformanceModel), repository);

        var root = (OpenApiSchema)repository.Schemas[typeof(ConformanceModel).Name];
        var properties = NormalizeOpenApiContainer(root, typeof(ConformanceModel));
        var definitions = new JsonObject();

        foreach(var nestedType in new[] { typeof(ConformanceAddress), typeof(ConformanceLine) }) {
            if(repository.Schemas.TryGetValue(nestedType.Name, out var nested) && nested is OpenApiSchema nestedSchema) {
                definitions[nestedType.Name] = NormalizeOpenApiContainer(nestedSchema, nestedType);
            }
        }

        return BuildSnapshot("swashbuckle", properties, definitions, advisory: null);
    }

    private static JsonObject BuildNSwagSnapshot() {
        var settings = new SystemTextJsonSchemaGeneratorSettings {
            SerializerOptions = JsonOptions
        };
        settings.SchemaProcessors.Add(new JeninnetValidationSchemaProcessor(new ValidationSchemaRegistry([
            new ConformanceModelValidator(),
            new ConformanceAddressValidator(),
            new ConformanceLineValidator()
        ])));

        var schema = new JsonSchemaGenerator(settings).Generate(typeof(ConformanceModel));
        var properties = NormalizeNJsonSchemaContainer(schema, typeof(ConformanceModel));
        var definitions = new JsonObject();

        foreach(var nestedType in new[] { typeof(ConformanceAddress), typeof(ConformanceLine) }) {
            if(schema.Definitions.TryGetValue(nestedType.Name, out var nested)) {
                definitions[nestedType.Name] = NormalizeNJsonSchemaContainer(nested, nestedType);
            }
        }

        return BuildSnapshot("nswag", properties, definitions, advisory: null);
    }

    private static JsonObject BuildJsonSchemaSnapshot() {
        var descriptor = new ConformanceModelValidator().Describe();
        var memberTypes = BuildMemberTypes(typeof(ConformanceModel));
        var root = JsonNode.Parse(ValidationSchemaWriter.WriteToString(
            descriptor,
            new ValidationSchemaOptions { IncludeAdvisoryRules = true }))!.AsObject();

        var properties = NormalizeJsonSchemaContainer(root, typeof(ConformanceModel));
        var definitions = new JsonObject();

        foreach(var nestedType in new[] { typeof(ConformanceAddress), typeof(ConformanceLine) }) {
            if(root["$defs"]?[nestedType.Name] is JsonObject nested) {
                definitions[nestedType.Name] = NormalizeJsonSchemaContainer(nested, nestedType);
            }
        }

        var advisory = new JsonObject();
        foreach(var memberName in memberTypes.Keys) {
            if(root["properties"]?[memberName]?["x-validation-rules"] is JsonArray rules) {
                advisory[memberName] = NormalizeAdvisory(rules);
            }
        }

        return BuildSnapshot("json-schema", properties, definitions, advisory);
    }

    private static JsonObject BuildSnapshot(string adapter, JsonObject properties, JsonObject definitions, JsonObject? advisory) {
        var snapshot = new JsonObject {
            ["adapter"] = adapter,
            ["properties"] = properties,
            ["definitions"] = definitions
        };

        if(advisory is { Count: > 0 }) {
            snapshot["advisory"] = advisory;
        }

        return snapshot;
    }

    private static JsonObject NormalizeOpenApiContainer(OpenApiSchema schema, Type modelType) {
        var jsonToMember = BuildJsonToMember(modelType);
        var memberTypes = BuildMemberTypes(modelType);
        var required = new HashSet<string>(StringComparer.Ordinal);

        foreach(var name in schema.Required ?? (ISet<string>)new HashSet<string>(StringComparer.Ordinal)) {
            required.Add(jsonToMember.TryGetValue(name, out var memberName) ? memberName : name);
        }

        var properties = new JsonObject();

        foreach(var property in schema.Properties ?? new Dictionary<string, IOpenApiSchema>()) {
            var memberName = jsonToMember.TryGetValue(property.Key, out var mappedName) ? mappedName : property.Key;
            var memberType = memberTypes.TryGetValue(memberName, out var type) ? type : null;
            var value = property.Value;

            if(value is OpenApiSchemaReference reference && reference.Target is OpenApiSchema target) {
                value = target;
            }

            var normalized = value is OpenApiSchema concrete
                ? NormalizeOpenApiProperty(concrete, memberType, property.Key)
                : NormalizeOpenApiProperty(new OpenApiSchema(), memberType, property.Key);
            normalized["required"] = required.Contains(memberName);
            properties[memberName] = normalized;
        }

        return properties;
    }

    private static JsonObject NormalizeOpenApiProperty(OpenApiSchema schema, Type? memberType, string jsonName) {
        var property = new JsonObject {
            ["jsonName"] = jsonName,
            ["required"] = false
        };

        if(IsNestedObject(memberType)) {
            property["type"] = "object";
            property["ref"] = memberType!.Name;
            CopyArrayConstraints(schema, property);
            return property;
        }

        if(GetCollectionElementType(memberType) is { } elementType && IsNestedObject(elementType)) {
            property["type"] = "array";
            property["itemsRef"] = elementType.Name;
            CopyArrayConstraints(schema, property);
            return property;
        }

        var type = NormalizeOpenApiType(schema.Type) ?? NormalizeClrType(memberType);
        Add(property, "type", type);

        if(type == "string") {
            Add(property, "format", schema.Format);
        }

        AddAnyOf(property, schema.AnyOf);
        Add(property, "minLength", schema.MinLength);
        Add(property, "maxLength", schema.MaxLength);
        Add(property, "pattern", schema.Pattern);
        AddDecimal(property, "minimum", ParseDecimal(schema.Minimum));
        AddDecimal(property, "maximum", ParseDecimal(schema.Maximum));
        AddDecimal(property, "exclusiveMinimum", ParseDecimal(schema.ExclusiveMinimum));
        AddDecimal(property, "exclusiveMaximum", ParseDecimal(schema.ExclusiveMaximum));
        Add(property, "minItems", schema.MinItems);
        Add(property, "maxItems", schema.MaxItems);
        Add(property, "uniqueItems", schema.UniqueItems);
        AddDecimal(property, "multipleOf", schema.MultipleOf);
        AddOpenApiEnum(property, schema);
        AddOpenApiEnumType(property, schema);

        if(memberType?.IsEnum == true && property["enumType"] is null) {
            property["enumType"] = memberType.Name;
        }

        Add(property, "contentEncoding", string.Equals(schema.Format, "byte", StringComparison.Ordinal) ? "base64" : schema.ContentEncoding);
        return property;
    }

    private static JsonObject NormalizeNJsonSchemaContainer(NJsonSchemaType schema, Type modelType) {
        var jsonToMember = BuildJsonToMember(modelType);
        var memberTypes = BuildMemberTypes(modelType);
        var required = new HashSet<string>(StringComparer.Ordinal);

        foreach(var name in schema.RequiredProperties) {
            required.Add(jsonToMember.TryGetValue(name, out var memberName) ? memberName : name);
        }

        var properties = new JsonObject();

        foreach(var property in schema.Properties) {
            var memberName = jsonToMember.TryGetValue(property.Key, out var mappedName) ? mappedName : property.Key;
            var memberType = memberTypes.TryGetValue(memberName, out var type) ? type : null;
            var normalized = NormalizeNJsonSchemaProperty(property.Value, memberType, property.Key);
            normalized["required"] = required.Contains(memberName);
            properties[memberName] = normalized;
        }

        return properties;
    }

    private static JsonObject NormalizeNJsonSchemaProperty(NJsonSchemaType schema, Type? memberType, string jsonName) {
        var property = new JsonObject {
            ["jsonName"] = jsonName,
            ["required"] = false
        };

        if(IsNestedObject(memberType)) {
            property["type"] = "object";
            property["ref"] = memberType!.Name;
            CopyArrayConstraints(schema, property);
            return property;
        }

        if(GetCollectionElementType(memberType) is { } elementType && IsNestedObject(elementType)) {
            property["type"] = "array";
            property["itemsRef"] = elementType.Name;
            CopyArrayConstraints(schema, property);
            return property;
        }

        var type = NormalizeNJsonType(schema.Type) ?? NormalizeClrType(memberType);
        Add(property, "type", type);

        if(type == "string") {
            Add(property, "format", schema.Format);
        }

        AddAnyOf(property, schema.AnyOf);
        Add(property, "minLength", schema.MinLength > 0 ? (int?)schema.MinLength : null);
        Add(property, "maxLength", schema.MaxLength > 0 ? (int?)schema.MaxLength : null);
        Add(property, "pattern", schema.Pattern);
        AddDecimal(property, "minimum", schema.Minimum);
        AddDecimal(property, "maximum", schema.Maximum);
        AddDecimal(property, "exclusiveMinimum", schema.ExclusiveMinimum);
        AddDecimal(property, "exclusiveMaximum", schema.ExclusiveMaximum);
        Add(property, "minItems", schema.MinItems > 0 ? (int?)schema.MinItems : null);
        Add(property, "maxItems", schema.MaxItems > 0 ? (int?)schema.MaxItems : null);
        Add(property, "uniqueItems", schema.UniqueItems ? (bool?)true : null);
        AddDecimal(property, "multipleOf", schema.MultipleOf);
        AddNJsonEnum(property, schema);
        AddNJsonEnumType(property, schema);

        if(memberType?.IsEnum == true && property["enumType"] is null) {
            property["enumType"] = memberType.Name;
        }

        Add(property, "contentEncoding", string.Equals(schema.Format, "byte", StringComparison.Ordinal) ? "base64" : null);
        return property;
    }

    private static JsonObject NormalizeJsonSchemaContainer(JsonObject schema, Type modelType) {
        var memberTypes = BuildMemberTypes(modelType);
        var required = new HashSet<string>(
            schema["required"]?.AsArray().Select(static node => node!.GetValue<string>()) ?? [],
            StringComparer.Ordinal);
        var properties = new JsonObject();
        var sourceProperties = schema["properties"]?.AsObject() ?? new JsonObject();

        foreach(var (memberName, memberNode) in sourceProperties.OrderBy(static pair => pair.Key, StringComparer.Ordinal)) {
            if(memberNode is not JsonObject property) {
                continue;
            }

            var memberType = memberTypes.TryGetValue(memberName, out var type) ? type : null;
            var normalized = NormalizeJsonSchemaProperty(property, memberType, memberName);
            normalized["required"] = required.Contains(memberName);
            properties[memberName] = normalized;
        }

        return properties;
    }

    private static JsonObject NormalizeJsonSchemaProperty(JsonObject schema, Type? memberType, string jsonName) {
        var property = new JsonObject {
            ["jsonName"] = jsonName,
            ["required"] = false
        };

        if(IsNestedObject(memberType)) {
            property["type"] = "object";
            property["ref"] = memberType!.Name;
            CopyArrayConstraints(schema, property);
            return property;
        }

        if(GetCollectionElementType(memberType) is { } elementType && IsNestedObject(elementType)) {
            property["type"] = "array";
            property["itemsRef"] = elementType.Name;
            CopyArrayConstraints(schema, property);
            return property;
        }

        var type = schema["type"]?.GetValue<string>() ?? NormalizeClrType(memberType);
        Add(property, "type", type);

        if(type == "string") {
            Add(property, "format", schema["format"]?.GetValue<string>());
        }

        AddAnyOf(property, schema["anyOf"]?.AsArray());
        Add(property, "minLength", GetInt(schema, "minLength"));
        Add(property, "maxLength", GetInt(schema, "maxLength"));
        Add(property, "pattern", schema["pattern"]?.GetValue<string>());
        AddDecimal(property, "minimum", GetDecimal(schema, "minimum"));
        AddDecimal(property, "maximum", GetDecimal(schema, "maximum"));
        AddDecimal(property, "exclusiveMinimum", GetDecimal(schema, "exclusiveMinimum"));
        AddDecimal(property, "exclusiveMaximum", GetDecimal(schema, "exclusiveMaximum"));
        Add(property, "minItems", GetInt(schema, "minItems"));
        Add(property, "maxItems", GetInt(schema, "maxItems"));
        Add(property, "uniqueItems", schema["uniqueItems"]?.GetValue<bool>());
        AddDecimal(property, "multipleOf", GetDecimal(schema, "multipleOf"));
        AddJsonEnum(property, schema);
        AddJsonEnumType(property, schema);
        Add(property, "contentEncoding", schema["contentEncoding"]?.GetValue<string>());
        Add(property, "ref", GetReferenceName(schema["$ref"]?.GetValue<string>()));
        Add(property, "itemsRef", GetReferenceName(schema["items"]?["$ref"]?.GetValue<string>()));

        if(schema["x-validation-rules"] is JsonArray rules) {
            property["advisory"] = NormalizeAdvisory(rules);
        }

        return property;
    }

    private static JsonArray NormalizeAdvisory(JsonArray rules) =>
        new(rules.Select(static rule => (JsonNode?)new JsonObject {
            ["kind"] = rule!["kind"]?.GetValue<string>(),
            ["hasCondition"] = rule["hasCondition"]?.GetValue<bool>() ?? false
        }).ToArray());

    private static bool IsRequired(ValidationSchemaConstraints constraints, Type? memberType) {
        if(!constraints.IsRequired) {
            return false;
        }

        return memberType is null ||
               Nullable.GetUnderlyingType(memberType) is not null ||
               !memberType.IsValueType;
    }

    private static void CopyArrayConstraints(OpenApiSchema schema, JsonObject property) {
        Add(property, "minItems", schema.MinItems);
        Add(property, "maxItems", schema.MaxItems);
        Add(property, "uniqueItems", schema.UniqueItems);
    }

    private static void CopyArrayConstraints(NJsonSchemaType schema, JsonObject property) {
        Add(property, "minItems", schema.MinItems > 0 ? (int?)schema.MinItems : null);
        Add(property, "maxItems", schema.MaxItems > 0 ? (int?)schema.MaxItems : null);
        Add(property, "uniqueItems", schema.UniqueItems ? (bool?)true : null);
    }

    private static void CopyArrayConstraints(JsonObject schema, JsonObject property) {
        Add(property, "minItems", GetInt(schema, "minItems"));
        Add(property, "maxItems", GetInt(schema, "maxItems"));
        Add(property, "uniqueItems", schema["uniqueItems"]?.GetValue<bool>());
    }

    private static Dictionary<string, Type> BuildMemberTypes(Type modelType) =>
        modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(static property => property.Name, static property => property.PropertyType, StringComparer.Ordinal);

    private static Dictionary<string, string> BuildJsonToMember(Type modelType) {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach(var pair in BuildJsonNames(modelType)) {
            result.TryAdd(pair.Value, pair.Key);
        }

        return result;
    }

    private static Dictionary<string, string> BuildJsonNames(Type modelType) {
        var typeInfo = JsonOptions.GetTypeInfo(modelType);
        var names = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach(var property in typeInfo.Properties) {
            var memberName = (property.AttributeProvider as MemberInfo)?.Name ?? property.Name;
            names[memberName] = property.Name;
            names.TryAdd(property.Name, property.Name);
        }

        return names;
    }

    private static bool IsNestedObject(Type? type) =>
        type is not null &&
        type != typeof(string) &&
        type.IsClass &&
        type != typeof(object) &&
        !typeof(IEnumerable).IsAssignableFrom(type);

    private static Type? GetCollectionElementType(Type? type) {
        if(type is null || type == typeof(string) || !type.IsGenericType) {
            return null;
        }

        var elementType = type.GetGenericArguments().FirstOrDefault();
        return elementType is not null && IsNestedObject(elementType) ? elementType : null;
    }

    private static string? NormalizeClrType(Type? type) {
        if(type is null) {
            return null;
        }

        if(type == typeof(string) || type == typeof(char) || type.IsEnum) {
            return "string";
        }

        if(type == typeof(byte) || type == typeof(short) || type == typeof(int) ||
           type == typeof(long) || type == typeof(float) || type == typeof(double) ||
           type == typeof(decimal)) {
            return "number";
        }

        if(typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string)) {
            return "array";
        }

        return "object";
    }

    private static string? NormalizeOpenApiType(JsonSchemaType? type) {
        if(type is null) {
            return null;
        }

        if(type.Value.HasFlag(JsonSchemaType.String)) { return "string"; }
        if(type.Value.HasFlag(JsonSchemaType.Array)) { return "array"; }
        if(type.Value.HasFlag(JsonSchemaType.Object)) { return "object"; }
        if(type.Value.HasFlag(JsonSchemaType.Number) || type.Value.HasFlag(JsonSchemaType.Integer)) { return "number"; }

        return null;
    }

    private static string? NormalizeNJsonType(JsonObjectType type) {
        if(type.HasFlag(JsonObjectType.String)) { return "string"; }
        if(type.HasFlag(JsonObjectType.Array)) { return "array"; }
        if(type.HasFlag(JsonObjectType.Object)) { return "object"; }
        if(type.HasFlag(JsonObjectType.Number) || type.HasFlag(JsonObjectType.Integer)) { return "number"; }

        return null;
    }

    private static void AddAnyOf(JsonObject property, IEnumerable<IOpenApiSchema>? schemas) {
        var formats = schemas?
            .Select(static schema => schema switch {
                OpenApiSchema concrete => concrete.Format,
                OpenApiSchemaReference reference when reference.Target is OpenApiSchema concrete => concrete.Format,
                _ => null
            })
            .Where(static format => format is not null)
            .Select(static format => (JsonNode?)JsonValue.Create(format))
            .ToArray() ?? [];

        if(formats.Length > 0) {
            property["anyOfFormats"] = new JsonArray(formats);
        }
    }

    private static void AddAnyOf(JsonObject property, IEnumerable<NJsonSchemaType>? schemas) {
        var formats = schemas?
            .Select(static schema => schema.Format)
            .Where(static format => format is not null)
            .Select(static format => (JsonNode?)JsonValue.Create(format))
            .ToArray() ?? [];

        if(formats.Length > 0) {
            property["anyOfFormats"] = new JsonArray(formats);
        }
    }

    private static void AddAnyOf(JsonObject property, JsonArray? schemas) {
        var formats = schemas?
            .Select(static schema => schema?["format"]?.GetValue<string>())
            .Where(static format => format is not null)
            .Select(static format => (JsonNode?)JsonValue.Create(format))
            .ToArray() ?? [];

        if(formats.Length > 0) {
            property["anyOfFormats"] = new JsonArray(formats);
        }
    }

    private static void AddOpenApiEnum(JsonObject property, OpenApiSchema schema) {
        if(schema.Enum is { Count: > 0 } values) {
            property["enumValues"] = new JsonArray(values
                .Select(static value => (JsonNode?)JsonValue.Create(value?.ToString()))
                .ToArray());
        }
    }

    private static void AddOpenApiEnumType(JsonObject property, OpenApiSchema schema) {
        if(schema.Extensions?.TryGetValue("x-enum-type", out var extension) == true && extension is JsonNodeExtension node) {
            Add(property, "enumType", node.Node?.GetValue<string>());
        }
    }

    private static void AddNJsonEnum(JsonObject property, NJsonSchemaType schema) {
        if(schema.Enumeration.Count > 0) {
            property["enumValues"] = new JsonArray(schema.Enumeration
                .Select(static value => (JsonNode?)JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture)))
                .ToArray());
        }
    }

    private static void AddNJsonEnumType(JsonObject property, NJsonSchemaType schema) {
        if(schema.ExtensionData?.TryGetValue("x-enum-type", out var value) == true) {
            Add(property, "enumType", Convert.ToString(value, CultureInfo.InvariantCulture));
        }
    }

    private static void AddJsonEnum(JsonObject property, JsonObject schema) {
        if(schema["enum"] is JsonArray values) {
            property["enumValues"] = new JsonArray(values
                .Select(static value => (JsonNode?)JsonValue.Create(value?.ToString()))
                .ToArray());
        }
    }

    private static void AddJsonEnumType(JsonObject property, JsonObject schema) {
        Add(property, "enumType", schema["x-enum-type"]?.GetValue<string>());
    }

    private static string? GetReferenceName(string? reference) =>
        reference?.Split('/').LastOrDefault();

    private static int? GetInt(JsonObject schema, string key) =>
        schema[key] is JsonValue value && value.TryGetValue<int>(out var parsed) ? parsed : null;

    private static decimal? GetDecimal(JsonObject schema, string key) =>
        schema[key] is JsonValue value && value.TryGetValue<decimal>(out var parsed) ? parsed : null;

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static void Add(JsonObject target, string key, string? value) {
        if(!string.IsNullOrWhiteSpace(value)) {
            target[key] = value;
        }
    }

    private static void Add(JsonObject target, string key, int? value) {
        if(value is not null) {
            target[key] = value.Value;
        }
    }

    private static void Add(JsonObject target, string key, bool? value) {
        if(value is not null) {
            target[key] = value.Value;
        }
    }

    private static void AddDecimal(JsonObject target, string key, decimal? value) {
        if(value is not null) {
            target[key] = value.Value;
        }
    }

    private static JsonObject ToCommonSnapshot(JsonObject snapshot) {
        var common = snapshot.DeepClone().AsObject();
        common.Remove("adapter");
        common.Remove("advisory");

        foreach(var (_, property) in common["properties"]!.AsObject()) {
            var propertyObject = property!.AsObject();
            propertyObject.Remove("jsonName");
            propertyObject.Remove("advisory");
        }

        foreach(var (_, definition) in common["definitions"]!.AsObject()) {
            foreach(var (_, property) in definition!.AsObject()) {
                property!.AsObject().Remove("jsonName");
            }
        }

        return common;
    }

    private static Dictionary<string, string> ReadApprovals() {
        var path = Path.Combine(SnapshotDirectory, "approved.json");
        Assert.IsTrue(File.Exists(path), $"snapshot approval file not found: {path}");

        var document = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var snapshots = document["snapshots"]!.AsObject();

        return snapshots.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value!.GetValue<string>(),
            StringComparer.Ordinal);
    }

    private static List<string> ValidateSnapshot(
        string adapter,
        JsonObject snapshot,
        Dictionary<string, string> approvals) {
        var errors = new List<string>();

        if(!approvals.TryGetValue(adapter, out var expectedHash)) {
            errors.Add($"No approval exists for adapter '{adapter}'.");
            return errors;
        }

        var text = Canonicalize(snapshot);
        var hash = Hash(text);

        if(!string.Equals(hash, expectedHash, StringComparison.Ordinal)) {
            errors.Add($"Snapshot '{adapter}' hash changed: expected {expectedHash}, found {hash}. Review the diff and update the approval.");
        }

        var path = Path.Combine(SnapshotDirectory, $"{adapter}.json");
        if(!File.Exists(path)) {
            errors.Add($"Snapshot file is missing: {path}");
        } else if(!string.Equals(NormalizeNewLines(File.ReadAllText(path)), text, StringComparison.Ordinal)) {
            errors.Add($"Snapshot file changed: {path}. Review the diff and update the approval.");
        }

        return errors;
    }

    private static void WriteSnapshots(IReadOnlyDictionary<string, JsonObject> snapshots) {
        Directory.CreateDirectory(SnapshotDirectory);
        var approvals = new JsonObject {
            ["schemaVersion"] = 1,
            ["algorithm"] = "SHA256",
            ["snapshots"] = new JsonObject()
        };

        foreach(var (adapter, snapshot) in snapshots.OrderBy(static pair => pair.Key, StringComparer.Ordinal)) {
            var text = Canonicalize(snapshot);
            File.WriteAllText(Path.Combine(SnapshotDirectory, $"{adapter}.json"), text);
            approvals["snapshots"]![adapter] = Hash(text);
        }

        File.WriteAllText(
            Path.Combine(SnapshotDirectory, "approved.json"),
            approvals.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    private static string Canonicalize(JsonNode node) =>
        NormalizeNewLines(Sort(node)!.ToJsonString(new JsonSerializerOptions { WriteIndented = true })) + "\n";

    private static JsonNode? Sort(JsonNode? node) {
        switch(node) {
            case JsonObject obj:
                var sorted = new JsonObject();
                foreach(var (key, value) in obj.OrderBy(static pair => pair.Key, StringComparer.Ordinal)) {
                    sorted[key] = Sort(value);
                }
                return sorted;
            case JsonArray array:
                return new JsonArray(array.Select(Sort).ToArray());
            default:
                return node?.DeepClone();
        }
    }

    private static string Hash(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    private static string NormalizeNewLines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static string SnapshotDirectory {
        get {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while(directory is not null) {
                if(File.Exists(Path.Combine(directory.FullName, "Jeninnet.Validation.slnx"))) {
                    return Path.Combine(directory.FullName, "tests", "Jeninnet.Validation.OpenApi.Adapters.Tests", "Snapshots");
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
        }
    }
}
