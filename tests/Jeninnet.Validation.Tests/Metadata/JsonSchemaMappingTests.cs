//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Metadata;

[TestClass]
public sealed class JsonSchemaMappingTests {
    [TestMethod]
    public void MapMember_MapsEverySupportedRuleKind() {
        AssertRule(Rule("NotNull"), static c => c.IsRequired.Should().BeTrue());
        AssertRule(Rule("NotEmpty"), static c => c.IsRequired.Should().BeTrue());
        AssertRule(Rule("StringNotEmpty"), static c => {
            c.Type.Should().Be("string");
            c.MinLength.Should().Be(1);
        });

        AssertRule(Rule("Length", ("Min", "2"), ("Max", "10")), static c => {
            c.Type.Should().Be("string");
            c.MinLength.Should().Be(2);
            c.MaxLength.Should().Be(10);
        });
        AssertRule(Rule("ExactLength", ("Length", "5")), static c => {
            c.MinLength.Should().Be(5);
            c.MaxLength.Should().Be(5);
        });
        AssertRule(Rule("Regex", ("Pattern", "^a$")), static c => c.Pattern.Should().Be("^a$"));
        AssertRule(Rule("Contains", ("Value", "x")), static c => c.Pattern.Should().Be("x"));
        AssertRule(Rule("StartsWith", ("Value", "a")), static c => c.Pattern.Should().Be("^a"));
        AssertRule(Rule("EndsWith", ("Value", "z")), static c => c.Pattern.Should().Be("z$"));
        AssertRule(Rule("Email"), static c => c.Format.Should().Be("email"));
        AssertRule(Rule("Url"), static c => c.Format.Should().Be("uri"));
        AssertRule(Rule("IPAddress"), static c => c.AnyOfFormats.Should().Equal("ipv4", "ipv6"));
        AssertRule(Rule("Base64String"), static c => c.ContentEncoding.Should().Be("base64"));
        AssertRule(Rule("FileExtensions", ("Extensions", ".pdf,.txt")), static c =>
            c.FileExtensions.Should().NotBeNull());

        AssertRule(Rule("Count", ("Min", "1"), ("Max", "3")), static c => {
            c.Type.Should().Be("array");
            c.MinItems.Should().Be(1);
            c.MaxItems.Should().Be(3);
        });
        AssertRule(Rule("MinCount", ("Min", "1")), static c => c.MinItems.Should().Be(1));
        AssertRule(Rule("MaxCount", ("Max", "3")), static c => c.MaxItems.Should().Be(3));
        AssertRule(Rule("UniqueItems"), static c => {
            c.Type.Should().Be("array");
            c.UniqueItems.Should().BeTrue();
        });

        AssertRule(Rule("GreaterThan", ("Threshold", "0")), static c => c.ExclusiveMinimum.Should().Be(0m));
        AssertRule(Rule("MultipleOf", ("Divisor", "0.1")), static c => {
            c.Type.Should().Be("number");
            c.MultipleOf.Should().Be(0.1m);
        });
        AssertRule(Rule("GreaterThanOrEqual", ("Threshold", "0")), static c => c.Minimum.Should().Be(0m));
        AssertRule(Rule("LessThan", ("Threshold", "100")), static c => c.ExclusiveMaximum.Should().Be(100m));
        AssertRule(Rule("LessThanOrEqual", ("Threshold", "100")), static c => c.Maximum.Should().Be(100m));
        AssertRule(Rule("InclusiveBetween", ("Min", "1"), ("Max", "5")), static c => {
            c.Minimum.Should().Be(1m);
            c.Maximum.Should().Be(5m);
        });
        AssertRule(Rule("ExclusiveBetween", ("Min", "1"), ("Max", "5")), static c => {
            c.ExclusiveMinimum.Should().Be(1m);
            c.ExclusiveMaximum.Should().Be(5m);
        });
        AssertRule(Rule("Range", ("Min", "1"), ("Max", "5")), static c => c.Maximum.Should().Be(5m));
        AssertRule(Rule("In", ("Values", "a,b")), static c => {
            c.Type.Should().Be("string");
            c.Enum.Should().NotBeNull();
            c.Enum!.Count.Should().Be(2);
        });
        AssertRule(Rule("ScalePrecision", ("Precision", "5"), ("Scale", "2"), ("IgnoreTrailingZeros", "true")), static c =>
            c.MultipleOf.Should().Be(0.01m));
        AssertRule(Rule("After"), static c => c.Format.Should().Be("date-time"));
        AssertRule(Rule("Before"), static c => c.Format.Should().Be("date-time"));
        AssertRule(Rule("Must"), static c => c.AdvisoryRules.Should().HaveCount(1));
    }

    [TestMethod]
    public void MapMember_ArrayNotEmptySetsMinItems() {
        var constraints = Map(Rule("Count", ("Max", "5")), Rule("NotEmpty"));

        MSTestAssertions.That(() => constraints.Type.Should().Be("array"));
        MSTestAssertions.That(() => constraints.MinItems.Should().Be(1));
        MSTestAssertions.That(() => constraints.MaxItems.Should().Be(5));
    }

    [TestMethod]
    public void Map_OrdersMembersAndUsesValidatorNameAsTitle() {
        var descriptor = new ValidationDescriptor(
            "Test.Validator",
            [
                new ValidationMemberDescriptor("Zeta", [Rule("NotNull")]),
                new ValidationMemberDescriptor("Alpha", [Rule("NotNull")])
            ],
            typeof(object));

        var mapped = ValidationSchemaMapper.Map(descriptor);

        MSTestAssertions.That(() => mapped.Title.Should().Be("Test.Validator"));
        MSTestAssertions.That(() => mapped.Properties.Keys.SequenceEqual(["Alpha", "Zeta"]).Should().BeTrue());
    }

    private static void AssertRule(
        IValidationRuleDescriptor rule,
        Action<ValidationSchemaConstraints> assertion) =>
        MSTestAssertions.That(() => assertion(Map(rule)));

    private static ValidationSchemaConstraints Map(params IValidationRuleDescriptor[] rules) =>
        ValidationSchemaMapper.MapMember(new ValidationMemberDescriptor("Value", rules));

    private static ValidationRuleDescriptor Rule(string kind, params (string Key, string Value)[] parameters) =>
        new(
            kind,
            kind,
            ValidationErrorSeverity.Error,
            hasCondition: false,
            parameters.ToDictionary(
                static parameter => parameter.Key,
                static parameter => (object?)parameter.Value,
                StringComparer.Ordinal));
}
