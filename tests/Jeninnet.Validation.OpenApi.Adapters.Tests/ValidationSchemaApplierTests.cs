//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.OpenApi.Adapters.Tests;

[TestClass]
public sealed class ValidationSchemaApplierTests {
    [TestMethod]
    public void Apply_PreservesExistingConstraintsWhenTheValidatorHasNoOpinion() {
        var schema = new OpenApiSchema {
            MinLength = 5,
            MaxLength = 10,
            Pattern = "^[A-Z]+$",
            Minimum = "1",
            Maximum = "9",
            MinItems = 2,
            MaxItems = 4,
            MultipleOf = 3m,
            UniqueItems = true
        };

        ValidationSchemaApplier.Apply(new ValidationSchemaConstraints { Type = "string" }, schema);

        MSTestAssertions.That(() => schema.MinLength.Should().Be(5));
        MSTestAssertions.That(() => schema.MaxLength.Should().Be(10));
        MSTestAssertions.That(() => schema.Pattern.Should().Be("^[A-Z]+$"));
        MSTestAssertions.That(() => schema.Minimum.Should().Be("1"));
        MSTestAssertions.That(() => schema.Maximum.Should().Be("9"));
        MSTestAssertions.That(() => schema.MinItems.Should().Be(2));
        MSTestAssertions.That(() => schema.MaxItems.Should().Be(4));
        MSTestAssertions.That(() => schema.MultipleOf.Should().Be(3m));
        MSTestAssertions.That(() => schema.UniqueItems.Should().BeTrue());
    }

    [TestMethod]
    public void Apply_PreservesExistingExclusiveBoundsWhenTheValidatorHasNoOpinion() {
        var schema = new OpenApiSchema {
            ExclusiveMinimum = "0",
            ExclusiveMaximum = "10"
        };

        ValidationSchemaApplier.Apply(new ValidationSchemaConstraints { Type = "number" }, schema);

        MSTestAssertions.That(() => schema.ExclusiveMinimum.Should().Be("0"));
        MSTestAssertions.That(() => schema.ExclusiveMaximum.Should().Be("10"));
    }

    [TestMethod]
    public void Apply_OverridesExistingConstraintsWhenTheValidatorHasAnOpinion() {
        var schema = new OpenApiSchema {
            MinLength = 5,
            MaxLength = 10,
            Pattern = "^old$"
        };

        ValidationSchemaApplier.Apply(
            new ValidationSchemaConstraints {
                Type = "string",
                MinLength = 2,
                MaxLength = 4,
                Pattern = "^new$"
            },
            schema);

        MSTestAssertions.That(() => schema.MinLength.Should().Be(2));
        MSTestAssertions.That(() => schema.MaxLength.Should().Be(4));
        MSTestAssertions.That(() => schema.Pattern.Should().Be("^new$"));
    }
}
