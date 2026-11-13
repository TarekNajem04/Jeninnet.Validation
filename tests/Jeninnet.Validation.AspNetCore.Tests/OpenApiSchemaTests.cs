//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.AspNetCore.Tests;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

internal sealed class OpenApiModel {
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public List<string>? Tags { get; set; }
    public string? Address { get; set; }
}

internal sealed class OpenApiLine {
    public string? Sku { get; set; }
}

internal sealed class OpenApiNamingModel {
    public string? FirstName { get; set; }

    [JsonPropertyName("custom-code")]
    public string? Code { get; set; }
}

file sealed class OpenApiNamingValidator : Validator<OpenApiNamingModel> {
    protected override void Configure(ValidatorRuleBuilder<OpenApiNamingModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.FirstName, nameof(OpenApiNamingModel.FirstName))
               .NotEmpty()
               .Length(2, 50);

        builder.RuleFor(static x => x.Code, nameof(OpenApiNamingModel.Code))
               .NotEmpty();
    }
}

file sealed class OpenApiModelValidator : Validator<OpenApiModel> {
    protected override void Configure(ValidatorRuleBuilder<OpenApiModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(OpenApiModel.Name))
               .NotEmpty()
               .Length(2, 50);

        builder.RuleFor(static x => x.Email, nameof(OpenApiModel.Email))
               .EmailAddress();

        builder.RuleFor(static x => x.Age, nameof(OpenApiModel.Age))
               .GreaterThanOrEqual(18);

        builder.RuleFor(static x => x.Tags, nameof(OpenApiModel.Tags))
               .Count(1, 5);

        builder.RuleFor(static x => x.Address, nameof(OpenApiModel.Address))
               .IPAddress();
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class OpenApiSchemaTests {
    [TestMethod]
    public void Registry_FindsDescriptorsByValidatedType() {
        var registry = new ValidationSchemaRegistry([new OpenApiModelValidator()]);

        var found = registry.TryGet(typeof(OpenApiModel), out var descriptor);

        MSTestAssertions.That(() => found.Should().BeTrue());
        MSTestAssertions.That(() => descriptor.Should().NotBeNull());
        MSTestAssertions.That(() => descriptor!.ValidatedType.Should().Be(typeof(OpenApiModel)));
    }

    [TestMethod]
    public void Applier_MapsConstraintsToOpenApiSchema() {
        var descriptor = new OpenApiModelValidator().Describe();

        var nameSchema = ApplyMember(descriptor, nameof(OpenApiModel.Name));
        MSTestAssertions.That(() => nameSchema.Type.Should().Be(JsonSchemaType.String));
        MSTestAssertions.That(() => nameSchema.MinLength.Should().Be(2));
        MSTestAssertions.That(() => nameSchema.MaxLength.Should().Be(50));

        var emailSchema = ApplyMember(descriptor, nameof(OpenApiModel.Email));
        MSTestAssertions.That(() => emailSchema.Format.Should().Be("email"));

        var ageSchema = ApplyMember(descriptor, nameof(OpenApiModel.Age));
        MSTestAssertions.That(() => ageSchema.Type.Should().Be(JsonSchemaType.Number));
        MSTestAssertions.That(() => ageSchema.Minimum.Should().Be("18"));

        var tagsSchema = ApplyMember(descriptor, nameof(OpenApiModel.Tags));
        MSTestAssertions.That(() => tagsSchema.Type.Should().Be(JsonSchemaType.Array));
        MSTestAssertions.That(() => tagsSchema.MinItems.Should().Be(1));
        MSTestAssertions.That(() => tagsSchema.MaxItems.Should().Be(5));

        var addressSchema = ApplyMember(descriptor, nameof(OpenApiModel.Address));
        MSTestAssertions.That(() => addressSchema.AnyOf!
            .Select(static schema => schema.Format)
            .SequenceEqual(["ipv4", "ipv6"]).Should().BeTrue());
    }

    [TestMethod]
    public async Task Transformer_AppliesRequiredMembersAndPropertyConstraintsAsync() {
        var validator = new OpenApiModelValidator();
        var registry = new ValidationSchemaRegistry([validator]);
        var transformer = new ValidationSchemaTransformer(registry);
        var typeInfo = JsonSerializerOptions.Default.GetTypeInfo<OpenApiModel>();
        var services = new ServiceCollection().BuildServiceProvider();

        var objectSchema = new OpenApiSchema();
        await transformer.TransformAsync(
            objectSchema,
            new OpenApiSchemaTransformerContext {
                DocumentName = "v1",
                ParameterDescription = null,
                JsonTypeInfo = typeInfo,
                JsonPropertyInfo = null,
                ApplicationServices = services
            },
            CancellationToken.None);

        MSTestAssertions.That(() => objectSchema.Required.Should().Contain(nameof(OpenApiModel.Name)));

        var propertySchema = new OpenApiSchema();
        await transformer.TransformAsync(
            propertySchema,
            new OpenApiSchemaTransformerContext {
                DocumentName = "v1",
                ParameterDescription = null,
                JsonTypeInfo = typeInfo,
                JsonPropertyInfo = typeInfo.Properties.First(static property => property.Name == nameof(OpenApiModel.Name)),
                ApplicationServices = services
            },
            CancellationToken.None);

        MSTestAssertions.That(() => propertySchema.Type.Should().Be(JsonSchemaType.String));
        MSTestAssertions.That(() => propertySchema.MinLength.Should().Be(2));
    }

    [TestMethod]
    public async Task Transformer_RespectsNamingPoliciesAndJsonPropertyNameAsync() {
        var cases = new (JsonNamingPolicy? Policy, string FirstName, string Code)[] {
            (null, "FirstName", "custom-code"),
            (JsonNamingPolicy.CamelCase, "firstName", "custom-code"),
            (JsonNamingPolicy.SnakeCaseLower, "first_name", "custom-code"),
            (JsonNamingPolicy.KebabCaseLower, "first-name", "custom-code")
        };

        var services = new ServiceCollection().BuildServiceProvider();

        foreach(var (Policy, FirstName, Code) in cases) {
            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = Policy,
                TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
            };
            var typeInfo = options.GetTypeInfo<OpenApiNamingModel>();
            var transformer = new ValidationSchemaTransformer(
                new ValidationSchemaRegistry([new OpenApiNamingValidator()]));

            var objectSchema = new OpenApiSchema();
            await transformer.TransformAsync(
                objectSchema,
                new OpenApiSchemaTransformerContext {
                    DocumentName = "v1",
                    ParameterDescription = null,
                    JsonTypeInfo = typeInfo,
                    JsonPropertyInfo = null,
                    ApplicationServices = services
                },
                CancellationToken.None);

            MSTestAssertions.That(() => objectSchema.Required.Should().Contain(FirstName));
            MSTestAssertions.That(() => objectSchema.Required.Should().Contain(Code));

            var firstNameInfo = typeInfo.Properties.Single(property => property.Name == FirstName);
            var firstNameSchema = new OpenApiSchema();
            await transformer.TransformAsync(
                firstNameSchema,
                new OpenApiSchemaTransformerContext {
                    DocumentName = "v1",
                    ParameterDescription = null,
                    JsonTypeInfo = typeInfo,
                    JsonPropertyInfo = firstNameInfo,
                    ApplicationServices = services
                },
                CancellationToken.None);

            MSTestAssertions.That(() => firstNameSchema.MinLength.Should().Be(2));

            var codeInfo = typeInfo.Properties.Single(property => property.Name == Code);
            var codeSchema = new OpenApiSchema();
            await transformer.TransformAsync(
                codeSchema,
                new OpenApiSchemaTransformerContext {
                    DocumentName = "v1",
                    ParameterDescription = null,
                    JsonTypeInfo = typeInfo,
                    JsonPropertyInfo = codeInfo,
                    ApplicationServices = services
                },
                CancellationToken.None);

            MSTestAssertions.That(() => codeSchema.MinLength.Should().Be(1));
        }
    }

    [TestMethod]
    public void Applier_ResolvesNestedMembersThroughSerializerOptions() {
        var nested = new OpenApiNamingValidator().Describe();
        var parent = new ValidationDescriptor(
            "OpenApiParentValidator",
            [
                new ValidationMemberDescriptor(
                    "Address",
                    [new ValidationRuleDescriptor("Nested", nestedDescriptor: nested)],
                    typeof(OpenApiNamingModel))
            ],
            typeof(OpenApiModel));

        var constraints = ValidationSchemaMapper.MapMember(parent.Members.Single());
        var schema = new OpenApiSchema();
        var options = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };

        ValidationSchemaApplier.Apply(constraints, schema, options);

        var properties = schema.Properties!;
        MSTestAssertions.That(() => properties.Should().ContainKey("first_name"));
        MSTestAssertions.That(() => properties["first_name"].MinLength.Should().Be(2));
        MSTestAssertions.That(() => properties.Should().ContainKey("custom-code"));
        MSTestAssertions.That(() => schema.Required.Should().Contain("first_name"));
    }

    [TestMethod]
    public void Applier_ComposesCollectionItemConstraints() {
        var line = new ValidationDescriptor(
            "OpenApiLineValidator",
            [
                new ValidationMemberDescriptor(
                    "Sku",
                    [new ValidationRuleDescriptor("NotEmpty")],
                    typeof(string))
            ],
            typeof(OpenApiLine));

        var order = new ValidationDescriptor(
            "OpenApiOrderValidator",
            [
                new ValidationMemberDescriptor(
                    "Lines",
                    [new ValidationRuleDescriptor("ForEach", nestedDescriptor: line)],
                    typeof(List<OpenApiLine>))
            ],
            typeof(OpenApiModel));

        var constraints = ValidationSchemaMapper.MapMember(order.Members.Single());
        var schema = new OpenApiSchema();

        ValidationSchemaApplier.Apply(constraints, schema);

        var item = schema.Items!;
        var sku = item.Properties!["Sku"];

        MSTestAssertions.That(() => schema.Type.Should().Be(JsonSchemaType.Array));
        MSTestAssertions.That(() => item.Type.Should().Be(JsonSchemaType.Object));
        MSTestAssertions.That(() => sku.Type.Should().Be(JsonSchemaType.String));
        MSTestAssertions.That(() => sku.MinLength.Should().Be(1));
    }

    [TestMethod]
    public void Applier_EmitsEnumTypeExtension() {
        var schema = new OpenApiSchema();

        ValidationSchemaApplier.Apply(
            new ValidationSchemaConstraints { EnumType = "OpenApiColor" },
            schema);

        MSTestAssertions.That(() => schema.Extensions.Should().ContainKey("x-enum-type"));
    }

    private static OpenApiSchema ApplyMember(IValidationDescriptor descriptor, string memberName) {
        var member = descriptor.Members.Single(candidate => candidate.Name == memberName);
        var schema = new OpenApiSchema();

        ValidationSchemaApplier.Apply(ValidationSchemaMapper.MapMember(member), schema);
        return schema;
    }
}
