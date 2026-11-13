//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.OpenApi.Adapters.Tests;

// ---------------------------------------------------------------------------
// Models and validators
// ---------------------------------------------------------------------------

internal sealed class AdapterModel {
    public string? Name { get; set; }

    public string? FirstName { get; set; }

    public string? Email { get; set; }

    public int Age { get; set; }

    public List<string>? Tags { get; set; }

    public string? Address { get; set; }

    [JsonPropertyName("custom-code")]
    public string? Code { get; set; }
}

internal sealed class AdapterLine {
    public string? Sku { get; set; }
}

internal sealed class AdapterOrder {
    public List<AdapterLine>? Lines { get; set; }
}

internal sealed class UnvalidatedModel {
    public string? Name { get; set; }

    public string? Note { get; set; }
}

file sealed class AdapterModelValidator : Validator<AdapterModel> {
    protected override void Configure(ValidatorRuleBuilder<AdapterModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(AdapterModel.Name))
               .NotEmpty()
               .Length(2, 50);

        builder.RuleFor(static x => x.FirstName, nameof(AdapterModel.FirstName))
               .Length(2, 20);

        builder.RuleFor(static x => x.Email, nameof(AdapterModel.Email))
               .NotEmpty()
               .EmailAddress();

        builder.RuleFor(static x => x.Age, nameof(AdapterModel.Age))
               .GreaterThanOrEqual(18)
               .MultipleOf(2);

        builder.RuleFor(static x => x.Tags, nameof(AdapterModel.Tags))
               .Count(1, 5)
               .Unique();

        builder.RuleFor(static x => x.Address, nameof(AdapterModel.Address))
               .IPAddress();

        builder.RuleFor(static x => x.Code, nameof(AdapterModel.Code))
               .NotEmpty();
    }
}

file sealed class AdapterLineValidator : Validator<AdapterLine> {
    protected override void Configure(ValidatorRuleBuilder<AdapterLine> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Sku, nameof(AdapterLine.Sku))
               .NotEmpty();
    }
}

file sealed class AdapterOrderValidator : Validator<AdapterOrder> {
    protected override void Configure(ValidatorRuleBuilder<AdapterOrder> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Lines, nameof(AdapterOrder.Lines))
               .Count(1, 3);
    }
}

// ---------------------------------------------------------------------------
// Shared helpers
// ---------------------------------------------------------------------------

internal static class AdapterSchemaFixtures {
    internal static ValidationSchemaRegistry CreateRegistry() =>
        new([
            new AdapterModelValidator(),
            new AdapterLineValidator(),
            new AdapterOrderValidator()
        ]);

    internal static JsonSerializerOptions CreateJsonOptions(JsonNamingPolicy? namingPolicy) =>
        new() {
            PropertyNamingPolicy = namingPolicy,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
}

// ---------------------------------------------------------------------------
// Swashbuckle
// ---------------------------------------------------------------------------

[TestClass]
public sealed class SwashbuckleSchemaFilterTests {
    [TestMethod]
    public void Filter_AppliesMemberConstraintsAndRequiredNames() {
        var repository = new SchemaRepository();
        var definition = Generate(typeof(AdapterModel), JsonNamingPolicy.CamelCase, repository);

        var name = Property(definition, "name");
        MSTestAssertions.That(() => name.MinLength.Should().Be(2));
        MSTestAssertions.That(() => name.MaxLength.Should().Be(50));

        var email = Property(definition, "email");
        MSTestAssertions.That(() => email.Format.Should().Be("email"));

        var age = Property(definition, "age");
        MSTestAssertions.That(() => age.Type.Should().Be(JsonSchemaType.Integer));
        MSTestAssertions.That(() => age.Minimum.Should().Be("18"));
        MSTestAssertions.That(() => age.MultipleOf.Should().Be(2m));

        var tags = Property(definition, "tags");
        MSTestAssertions.That(() => tags.MinItems.Should().Be(1));
        MSTestAssertions.That(() => tags.MaxItems.Should().Be(5));
        MSTestAssertions.That(() => tags.UniqueItems.Should().BeTrue());

        var address = Property(definition, "address");
        MSTestAssertions.That(() => address.AnyOf.Should().NotBeNull());
        MSTestAssertions.That(() => address.AnyOf!
            .Select(static schema => schema.Format)
            .SequenceEqual(["ipv4", "ipv6"]).Should().BeTrue());

        MSTestAssertions.That(() => definition.Required.Should().Contain("name"));
        MSTestAssertions.That(() => definition.Required.Should().Contain("email"));
        MSTestAssertions.That(() => definition.Required.Should().Contain("custom-code"));
    }

    [TestMethod]
    public void Filter_RespectsNamingPoliciesAndJsonPropertyName() {
        var cases = new (JsonNamingPolicy? Policy, string FirstName)[] {
            (null, "FirstName"),
            (JsonNamingPolicy.CamelCase, "firstName"),
            (JsonNamingPolicy.SnakeCaseLower, "first_name"),
            (JsonNamingPolicy.KebabCaseLower, "first-name")
        };

        foreach(var (Policy, FirstName) in cases) {
            var repository = new SchemaRepository();
            var definition = Generate(typeof(AdapterModel), Policy, repository);

            var firstName = Property(definition, FirstName);
            MSTestAssertions.That(() => firstName.MinLength.Should().Be(2));
            MSTestAssertions.That(() => Property(definition, "custom-code").MinLength.Should().Be(1));
        }
    }

    [TestMethod]
    public void Filter_CoversNestedDefinitions() {
        var repository = new SchemaRepository();
        _ = Generate(typeof(AdapterOrder), JsonNamingPolicy.CamelCase, repository);

        var line = (OpenApiSchema)repository.Schemas["AdapterLine"];

        MSTestAssertions.That(() => line.Required.Should().Contain("sku"));
        MSTestAssertions.That(() => Property(line, "sku").MinLength.Should().Be(1));
    }

    [TestMethod]
    public void Filter_WithoutValidator_LeavesSchemaUntouched() {
        var repository = new SchemaRepository();
        var definition = Generate(typeof(UnvalidatedModel), JsonNamingPolicy.CamelCase, repository);

        MSTestAssertions.That(() => definition.Required.Should().BeEmpty());
        MSTestAssertions.That(() => Property(definition, "name").MinLength.Should().BeNull());
    }

    [TestMethod]
    public void Registration_AddsOneSchemaFilter() {
        var options = new SwaggerGenOptions();

        options.AddSchemaConstraintsFromValidators(new AdapterModelValidator());

        MSTestAssertions.That(() => options.SchemaFilterDescriptors.Should().HaveCount(1));
    }

    private static OpenApiSchema Generate(
        Type modelType,
        JsonNamingPolicy? namingPolicy,
        SchemaRepository repository) {
        var options = new SchemaGeneratorOptions();
        options.SchemaFilters.Add(new JeninnetValidationSchemaFilter(AdapterSchemaFixtures.CreateRegistry()));

        var generator = new SchemaGenerator(
            options,
            new JsonSerializerDataContractResolver(AdapterSchemaFixtures.CreateJsonOptions(namingPolicy)));

        _ = generator.GenerateSchema(modelType, repository);
        return (OpenApiSchema)repository.Schemas[modelType.Name];
    }

    private static OpenApiSchema Property(OpenApiSchema schema, string name) =>
        (OpenApiSchema)schema.Properties![name];
}

// ---------------------------------------------------------------------------
// NSwag / NJsonSchema
// ---------------------------------------------------------------------------

[TestClass]
public sealed class NSwagSchemaProcessorTests {
    [TestMethod]
    public void Processor_AppliesMemberConstraintsAndRequiredNames() {
        var schema = Generate(typeof(AdapterModel), JsonNamingPolicy.CamelCase);

        var name = schema.Properties["name"];
        MSTestAssertions.That(() => name.MinLength.Should().Be(2));
        MSTestAssertions.That(() => name.MaxLength.Should().Be(50));

        var email = schema.Properties["email"];
        MSTestAssertions.That(() => email.Format.Should().Be("email"));

        var age = schema.Properties["age"];
        MSTestAssertions.That(() => age.Type.Should().Be(JsonObjectType.Integer));
        MSTestAssertions.That(() => age.Minimum.Should().Be(18));
        MSTestAssertions.That(() => age.MultipleOf.Should().Be(2m));

        var tags = schema.Properties["tags"];
        MSTestAssertions.That(() => tags.MinItems.Should().Be(1));
        MSTestAssertions.That(() => tags.MaxItems.Should().Be(5));
        MSTestAssertions.That(() => tags.UniqueItems.Should().BeTrue());

        var address = schema.Properties["address"];
        MSTestAssertions.That(() => address.AnyOf
            .Select(static item => item.Format)
            .SequenceEqual(["ipv4", "ipv6"]).Should().BeTrue());

        MSTestAssertions.That(() => schema.RequiredProperties.Should().Contain("name"));
        MSTestAssertions.That(() => schema.RequiredProperties.Should().Contain("email"));
        MSTestAssertions.That(() => schema.RequiredProperties.Should().Contain("custom-code"));
    }

    [TestMethod]
    public void Processor_RespectsNamingPoliciesAndJsonPropertyName() {
        var cases = new (JsonNamingPolicy? Policy, string FirstName)[] {
            (null, "FirstName"),
            (JsonNamingPolicy.CamelCase, "firstName"),
            (JsonNamingPolicy.SnakeCaseLower, "first_name"),
            (JsonNamingPolicy.KebabCaseLower, "first-name")
        };

        foreach(var (Policy, FirstName) in cases) {
            var schema = Generate(typeof(AdapterModel), Policy);

            MSTestAssertions.That(() => schema.Properties[FirstName].MinLength.Should().Be(2));
            MSTestAssertions.That(() => schema.Properties["custom-code"].MinLength.Should().Be(1));
        }
    }

    [TestMethod]
    public void Processor_CoversNestedDefinitions() {
        var schema = Generate(typeof(AdapterOrder), JsonNamingPolicy.CamelCase);
        var line = schema.Definitions["AdapterLine"];

        MSTestAssertions.That(() => line.RequiredProperties.Should().Contain("sku"));
        MSTestAssertions.That(() => line.Properties["sku"].MinLength.Should().Be(1));
    }

    [TestMethod]
    public void Processor_WithoutValidator_LeavesSchemaUntouched() {
        var schema = Generate(typeof(UnvalidatedModel), JsonNamingPolicy.CamelCase);

        MSTestAssertions.That(() => schema.RequiredProperties.Should().BeEmpty());
        MSTestAssertions.That(() => schema.Properties["name"].MinLength.Should().BeNull());
    }

    [TestMethod]
    public void Registration_AddsOneProcessor() {
        var settings = new SystemTextJsonSchemaGeneratorSettings();

        settings.AddSchemaConstraintsFromValidators(new AdapterModelValidator());

        MSTestAssertions.That(() => settings.SchemaProcessors.Should().HaveCount(1));
    }

    [TestMethod]
    public void Registration_OnDocumentSettings_AddsOneProcessor() {
        var settings = new OpenApiDocumentGeneratorSettings();

        settings.AddSchemaConstraintsFromValidators(new AdapterModelValidator());

        MSTestAssertions.That(() => settings.SchemaSettings.SchemaProcessors.Should().HaveCount(1));
    }

    private static NJsonSchemaType Generate(Type modelType, JsonNamingPolicy? namingPolicy) {
        var settings = new SystemTextJsonSchemaGeneratorSettings {
            SerializerOptions = AdapterSchemaFixtures.CreateJsonOptions(namingPolicy)
        };
        settings.SchemaProcessors.Add(new JeninnetValidationSchemaProcessor(AdapterSchemaFixtures.CreateRegistry()));

        return new JsonSchemaGenerator(settings).Generate(modelType);
    }
}
