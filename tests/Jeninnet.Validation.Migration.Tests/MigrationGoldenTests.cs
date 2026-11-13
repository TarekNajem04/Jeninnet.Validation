//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Migration.Tests;

internal sealed class MigrationGoldenModel {
    public string? Name { get; set; }

    public string? Email { get; set; }

    public int Age { get; set; }
}

file sealed class FluentGoldenValidator : AbstractValidator<MigrationGoldenModel> {
    public FluentGoldenValidator() {
        RuleFor(static model => model.Name).NotEmpty().MaximumLength(10);
        RuleFor(static model => model.Email).NotEmpty().EmailAddress();
        RuleFor(static model => model.Age).InclusiveBetween(18, 120);
    }
}

file sealed class JeninnetGoldenValidator : Jeninnet.Validation.Fluent.Validator<MigrationGoldenModel> {
    protected override void Configure(ValidatorRuleBuilder<MigrationGoldenModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(MigrationGoldenModel.Name))
               .NotEmpty()
               .MaximumLength(10);

        builder.RuleFor(static model => model.Email, nameof(MigrationGoldenModel.Email))
               .NotEmpty()
               .EmailAddress();

        builder.RuleFor(static model => model.Age, nameof(MigrationGoldenModel.Age))
               .InclusiveBetween(18, 120);
    }
}

/// <summary>
/// Golden migration tests: the codemod output shape and the runtime equivalence of the common
/// FluentValidation/DataAnnotations rule set.
/// </summary>
[TestClass]
public sealed class MigrationGoldenTests {
    private const string FLUENT_SOURCE = """
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
    public void CommonRules_ProduceTheSameValidityAndMemberNames() {
        var invalid = new MigrationGoldenModel { Name = "", Email = "not-an-email", Age = 10 };
        var valid = new MigrationGoldenModel { Name = "Alice", Email = "alice@example.com", Age = 30 };

        var fluent = new FluentGoldenValidator();
        var jeninnet = new JeninnetGoldenValidator();

        AssertSameOutcome(fluent.Validate(invalid), jeninnet.Validate(invalid), expectedValid: false);
        AssertSameOutcome(fluent.Validate(valid), jeninnet.Validate(valid), expectedValid: true);
    }

    [TestMethod]
    public void Codemod_FluentFixture_ProducesTheGoldenJeninnetShape() {
        var migration = MigrationEngine.MigrateSource("UserValidator.cs", FLUENT_SOURCE);

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

    private static void AssertSameOutcome(FvValidationResult fluent, Jeninnet.Validation.ValidationResult jeninnet, bool expectedValid) {
        MSTestAssertions.That(() => (fluent.IsValid == expectedValid).Should().BeTrue());
        MSTestAssertions.That(() => (jeninnet.IsValid == expectedValid).Should().BeTrue());

        var fluentMembers = fluent.Errors
            .Select(static error => error.PropertyName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToList();
        var jeninnetMembers = jeninnet.Errors
            .Select(static error => error.MemberName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToList();

        MSTestAssertions.That(() => jeninnetMembers.SequenceEqual(fluentMembers, StringComparer.Ordinal).Should().BeTrue(
            $"expected members [{string.Join(", ", fluentMembers)}]; found [{string.Join(", ", jeninnetMembers)}]"));
    }
}
