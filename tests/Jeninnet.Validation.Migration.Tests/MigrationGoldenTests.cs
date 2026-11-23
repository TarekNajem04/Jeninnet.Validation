//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Migration.Tests;

/// <summary>
/// Golden migration tests for the codemod output shape.
/// </summary>
[TestClass]
public sealed class MigrationGoldenTests {
    private const string VALIDATOR_SOURCE = """
        using FluentValidation;

        public class User {
            public string? Name { get; set; }
        }

        public class UserValidator : AbstractValidator<User> {
            public UserValidator() {
                RuleFor(x => x.Name).NotEmpty().WithErrorCode("NAME");
            }
        }
        """;

    private const string DATA_ANNOTATIONS_SOURCE = """
        using System.ComponentModel.DataAnnotations;

        public class User {
            [Required]
            public string? Name { get; set; }
        }
        """;

    [TestMethod]
    public void Codemod_ValidatorFixture_ProducesTheGoldenJeninnetShape() {
        var migration = MigrationEngine.MigrateSource("UserValidator.cs", VALIDATOR_SOURCE);

        MSTestAssertions.That(() => migration.Changed.Should().BeTrue());
        MSTestAssertions.That(() => migration.NewText.Should().Contain("Validator<User>"));
        MSTestAssertions.That(() => migration.NewText.Should().Contain("protected override void Configure(ValidatorRuleBuilder<User> builder)"));
        MSTestAssertions.That(() => migration.NewText.Should().Contain("builder.RuleFor(x => x.Name, nameof(User.Name))"));
        MSTestAssertions.That(() => migration.NewText.Should().Contain(".WithCode(\"NAME\")"));
        MSTestAssertions.That(() => migration.NewText.Should().NotContain("AbstractValidator"));
        MSTestAssertions.That(() => migration.Converted.Select(static finding => finding.Kind).Should()
            .Contain(MigrationFinding.ValidatorConverted)
            .And.Contain(MigrationFinding.RuleForMemberName)
            .And.Contain(MigrationFinding.WithCodeRenamed));
    }

    [TestMethod]
    public void Codemod_DataAnnotationsFixture_ProducesNotNullWithAReviewMarker() {
        var migration = MigrationEngine.MigrateSource("User.cs", DATA_ANNOTATIONS_SOURCE);

        MSTestAssertions.That(() => migration.Changed.Should().BeTrue());
        MSTestAssertions.That(() => migration.NewText.Should().Contain("[NotNull]"));
        MSTestAssertions.That(() => migration.NewText.Should().Contain("TODO(Jeninnet)"));
        MSTestAssertions.That(() => migration.Converted.Select(static finding => finding.Kind).Should()
            .Contain(MigrationFinding.RequiredConverted));
        MSTestAssertions.That(() => migration.NeedsReview.Should().HaveCount(1));
    }
}
