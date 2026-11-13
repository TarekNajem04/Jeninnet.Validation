//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Metadata;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

internal sealed class SchemaModel {
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public List<string>? Tags { get; set; }
    public string? Code { get; set; }
    public string? Color { get; set; }
    public string? Note { get; set; }
    public string? Address { get; set; }
}

internal sealed class SchemaAddress {
    public string? City { get; set; }
}

internal sealed class SchemaLine {
    public string? Sku { get; set; }
}

internal sealed class SchemaOrder {
    public SchemaAddress? Address { get; set; }
    public List<SchemaLine>? Lines { get; set; }
    public int Quantity { get; set; }
}

internal sealed class SchemaCycleNode {
    public string? Name { get; set; }
}

file sealed class SchemaAddressValidator : Validator<SchemaAddress> {
    protected override void Configure(ValidatorRuleBuilder<SchemaAddress> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.City, nameof(SchemaAddress.City)).NotEmpty();
    }
}

file sealed class SchemaLineValidator : Validator<SchemaLine> {
    protected override void Configure(ValidatorRuleBuilder<SchemaLine> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Sku, nameof(SchemaLine.Sku)).NotEmpty();
    }
}

file sealed class SchemaOrderValidator : Validator<SchemaOrder> {
    protected override void Configure(ValidatorRuleBuilder<SchemaOrder> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Address, nameof(SchemaOrder.Address))
               .SetValidator(new SchemaAddressValidator());

        builder.RuleFor(static x => x.Lines, nameof(SchemaOrder.Lines))
               .ForEach(new SchemaLineValidator());

        builder.RuleFor(static x => x.Quantity, nameof(SchemaOrder.Quantity))
               .NotEmpty();
    }
}

file sealed class SchemaValidator : Validator<SchemaModel> {
    protected override void Configure(ValidatorRuleBuilder<SchemaModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(SchemaModel.Name))
               .NotEmpty()
               .Length(2, 50);

        builder.RuleFor(static x => x.Email, nameof(SchemaModel.Email))
               .EmailAddress();

        builder.RuleFor(static x => x.Age, nameof(SchemaModel.Age))
               .GreaterThanOrEqual(18)
               .LessThan(120)
               .MultipleOf(2);

        builder.RuleFor(static x => x.Tags, nameof(SchemaModel.Tags))
               .Count(1, 5)
               .Unique();

        builder.RuleFor(static x => x.Code, nameof(SchemaModel.Code))
               .Matches("^[A-Z]{3}$");

        builder.RuleFor(static x => x.Color, nameof(SchemaModel.Color))
               .In(["red", "green"]);

        builder.RuleFor(static x => x.Note, nameof(SchemaModel.Note))
               .Must(static note => note != "bad");

        builder.RuleFor(static x => x.Address, nameof(SchemaModel.Address))
               .IPAddress();
    }
}

file sealed class RecursiveDescriptor : IValidationDescriptor {
    public RecursiveDescriptor() => Members = [
            new ValidationMemberDescriptor(
                "Child",
                [
                    new ValidationRuleDescriptor("Nested", nestedDescriptor: this)
                ],
                typeof(SchemaCycleNode))
        ];

    public string ValidatorName => "SchemaCycleNodeValidator";

    public Type? ValidatedType => typeof(SchemaCycleNode);

    public IReadOnlyList<IValidationMemberDescriptor> Members { get; }

    public IReadOnlyList<IValidationRuleDescriptor> RulesForMember(string memberName) =>
        Members.FirstOrDefault(member => member.Name == memberName)?.Rules ?? [];
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class JsonSchemaTests {
    [TestMethod]
    public void Writer_MapsRulesToDraft202012Schema() {
        var schema = JsonNode.Parse(ValidationSchemaWriter.WriteToString(new SchemaValidator().Describe()))!;
        var properties = schema["properties"]!;

        MSTestAssertions.That(() => schema["$schema"]!.GetValue<string>().Should().Be("https://json-schema.org/draft/2020-12/schema"));
        MSTestAssertions.That(() => schema["title"]!.GetValue<string>().Should().Contain(nameof(SchemaValidator)));
        MSTestAssertions.That(() => schema["required"]!.AsArray().Select(static node => node!.GetValue<string>()).Should().Contain(nameof(SchemaModel.Name)));

        MSTestAssertions.That(() => properties[nameof(SchemaModel.Name)]!["type"]!.GetValue<string>().Should().Be("string"));
        MSTestAssertions.That(() => properties[nameof(SchemaModel.Name)]!["minLength"]!.GetValue<int>().Should().Be(2));
        MSTestAssertions.That(() => properties[nameof(SchemaModel.Name)]!["maxLength"]!.GetValue<int>().Should().Be(50));

        MSTestAssertions.That(() => properties[nameof(SchemaModel.Email)]!["format"]!.GetValue<string>().Should().Be("email"));

        MSTestAssertions.That(() => properties[nameof(SchemaModel.Age)]!["minimum"]!.GetValue<decimal>().Should().Be(18m));
        MSTestAssertions.That(() => properties[nameof(SchemaModel.Age)]!["multipleOf"]!.GetValue<decimal>().Should().Be(2m));
        MSTestAssertions.That(() => properties[nameof(SchemaModel.Age)]!["exclusiveMaximum"]!.GetValue<decimal>().Should().Be(120m));

        MSTestAssertions.That(() => properties[nameof(SchemaModel.Tags)]!["type"]!.GetValue<string>().Should().Be("array"));
        MSTestAssertions.That(() => properties[nameof(SchemaModel.Tags)]!["minItems"]!.GetValue<int>().Should().Be(1));
        MSTestAssertions.That(() => properties[nameof(SchemaModel.Tags)]!["maxItems"]!.GetValue<int>().Should().Be(5));
        MSTestAssertions.That(() => properties[nameof(SchemaModel.Tags)]!["uniqueItems"]!.GetValue<bool>().Should().BeTrue());

        MSTestAssertions.That(() => properties[nameof(SchemaModel.Code)]!["pattern"]!.GetValue<string>().Should().Be("^[A-Z]{3}$"));

        var colors = properties[nameof(SchemaModel.Color)]!["enum"]!.AsArray()
            .Select(static node => node!.GetValue<string>())
            .ToArray();
        MSTestAssertions.That(() => colors.SequenceEqual(["red", "green"]).Should().BeTrue());

        var advisory = properties[nameof(SchemaModel.Note)]!["x-validation-rules"]!.AsArray();
        MSTestAssertions.That(() => advisory.Select(static node => node!["kind"]!.GetValue<string>()).Should().Contain("Must"));

        var addressAnyOf = properties[nameof(SchemaModel.Address)]!["anyOf"]!.AsArray();
        MSTestAssertions.That(() => addressAnyOf
            .Select(static node => node!["format"]!.GetValue<string>())
            .SequenceEqual(["ipv4", "ipv6"]).Should().BeTrue());
    }

    [TestMethod]
    public void Writer_EmitsDefsAndRefsForNestedAndCollectionRules() {
        var schema = JsonNode.Parse(ValidationSchemaWriter.WriteToString(new SchemaOrderValidator().Describe()))!;
        var properties = schema["properties"]!;
        var definitions = schema["$defs"]!;

        var addressRef = properties[nameof(SchemaOrder.Address)]!["$ref"]!.GetValue<string>();
        MSTestAssertions.That(() => addressRef.Should().StartWith("#/$defs/"));
        var addressDefinition = definitions[addressRef["#/$defs/".Length..]]!;
        MSTestAssertions.That(() => addressDefinition["properties"]!["City"]!["type"]!.GetValue<string>().Should().Be("string"));
        MSTestAssertions.That(() => addressDefinition["properties"]!["City"]!["minLength"]!.GetValue<int>().Should().Be(1));

        var lineRef = properties[nameof(SchemaOrder.Lines)]!["items"]!["$ref"]!.GetValue<string>();
        MSTestAssertions.That(() => lineRef.Should().StartWith("#/$defs/"));
        var lineDefinition = definitions[lineRef["#/$defs/".Length..]]!;
        MSTestAssertions.That(() => lineDefinition["properties"]!["Sku"]!["minLength"]!.GetValue<int>().Should().Be(1));

        var required = schema["required"]?.AsArray()
            .Select(static node => node!.GetValue<string>())
            .ToArray() ?? [];

        MSTestAssertions.That(() => required.Should().NotContain(nameof(SchemaOrder.Quantity)));
    }

    [TestMethod]
    public void Writer_KeepsConditionalRulesAdvisory() {
        var descriptor = new ValidationDescriptor(
            "ConditionalValidator",
            [
                new ValidationMemberDescriptor(
                    "Name",
                    [
                        new ValidationRuleDescriptor("NotEmpty", hasCondition: true)
                    ],
                    typeof(string))
            ],
            typeof(string));

        var schema = JsonNode.Parse(ValidationSchemaWriter.WriteToString(descriptor))!;
        var property = schema["properties"]!["Name"]!;

        MSTestAssertions.That(() => schema["required"].Should().BeNull());
        MSTestAssertions.That(() => property["minLength"].Should().BeNull());
        MSTestAssertions.That(() => property["x-validation-rules"]!.AsArray()[0]!["hasCondition"]!.GetValue<bool>().Should().BeTrue());
    }

    [TestMethod]
    public void Writer_MapsEnumTypeAsVendorExtension() {
        var descriptor = new ValidationDescriptor(
            "EnumValidator",
            [
                new ValidationMemberDescriptor(
                    "Color",
                    [
                        new ValidationRuleDescriptor(
                            "Enum",
                            parameters: new Dictionary<string, object?> { ["EnumType"] = "ColorKind" })
                    ],
                    typeof(string))
            ],
            typeof(string));

        var schema = JsonNode.Parse(ValidationSchemaWriter.WriteToString(descriptor))!;

        MSTestAssertions.That(() => schema["properties"]!["Color"]!["x-enum-type"]!.GetValue<string>().Should().Be("ColorKind"));
    }

    [TestMethod]
    public void Writer_HandlesRecursiveDescriptorsWithoutStackOverflow() {
        var descriptor = new RecursiveDescriptor();

        var schema = JsonNode.Parse(ValidationSchemaWriter.WriteToString(descriptor))!;
        var addressRef = schema["properties"]!["Child"]!["$ref"]!.GetValue<string>();

        MSTestAssertions.That(() => addressRef.Should().StartWith("#/$defs/"));
        MSTestAssertions.That(() => schema["$defs"]!.AsObject()
            .ContainsKey(addressRef["#/$defs/".Length..]).Should().BeTrue());
    }

    [TestMethod]
    public void Writer_IsDeterministicAndCanOmitAdvisoryRules() {
        var descriptor = new SchemaValidator().Describe();

        var first = ValidationSchemaWriter.WriteToString(descriptor);
        var second = ValidationSchemaWriter.WriteToString(descriptor);
        var withoutAdvisory = ValidationSchemaWriter.WriteToString(
            descriptor,
            new ValidationSchemaOptions { IncludeAdvisoryRules = false });

        MSTestAssertions.That(() => first.Should().Be(second));
        MSTestAssertions.That(() => withoutAdvisory.Should().NotContain("x-validation-rules"));
    }
}
