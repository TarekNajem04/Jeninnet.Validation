//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Migration.Tests;

/// <summary>
/// Covers <see cref="MigrationEngine"/>: safe codemods, untouched patterns, and report lines.
/// </summary>
[TestClass]
public sealed class MigrationEngineTests {
    private const string VALIDATOR_SOURCE = """
        using FluentValidation;

        public class User {
            public string Name { get; set; } = string.Empty;
        }

        public class UserValidator : AbstractValidator<User> {
            public UserValidator() {
                RuleFor(x => x.Name).NotEmpty().WithErrorCode("X1");
            }
        }
        """;

    [TestMethod]
    public void MigrateSource_ConvertsAbstractValidator_ToFluentValidator() {
        var migration = MigrationEngine.MigrateSource("UserValidator.cs", VALIDATOR_SOURCE);

        MSTestAssertions.That(() => migration.Changed.Should().BeTrue());
        MSTestAssertions.That(() => migration.NewText.Should().Contain("Validator<User>"));
        MSTestAssertions.That(() => migration.NewText.Should().Contain("protected override void Configure(ValidatorRuleBuilder<User> builder)"));
        MSTestAssertions.That(() => migration.NewText.Should().Contain("builder.RuleFor(x => x.Name, nameof(User.Name))"));
        MSTestAssertions.That(() => migration.NewText.Should().NotContain("AbstractValidator"));
        MSTestAssertions.That(() => migration.Converted.Should().HaveCount(3));
        MSTestAssertions.That(() => migration.Converted.Select(static finding => finding.Kind).Should()
            .Contain(MigrationFinding.ValidatorConverted)
            .And.Contain(MigrationFinding.RuleForMemberName)
            .And.Contain(MigrationFinding.WithCodeRenamed));
        MSTestAssertions.That(() => migration.Converted
            .Single(static finding => finding.Kind == MigrationFinding.ValidatorConverted).Detail.Should()
            .Contain("FluentValidation"));
        MSTestAssertions.That(() => migration.Converted
            .Single(static finding => finding.Kind == MigrationFinding.RuleForMemberName).Detail.Should()
            .Contain("Jeninnet"));
    }

    [TestMethod]
    public void MigrateSource_RenamesWithErrorCode_ToWithCode() {
        var migration = MigrationEngine.MigrateSource("UserValidator.cs", VALIDATOR_SOURCE);
        var finding = migration.Converted.Single(static f => f.Kind == MigrationFinding.WithCodeRenamed);

        MSTestAssertions.That(() => finding.Line.Should().Be(9));
        MSTestAssertions.That(() => migration.NewText.Should().Contain(".WithCode(\"X1\")"));
        MSTestAssertions.That(() => migration.NewText.Should().NotContain("WithErrorCode"));
    }

    [TestMethod]
    public void MigrateSource_ConvertsRequired_ToNotNullWithTodo() {
        const string source = """
            using System.ComponentModel.DataAnnotations;

            public class Dto {
                [Required]
                public string? Title { get; set; }
            }
            """;

        var migration = MigrationEngine.MigrateSource("Dto.cs", source);

        MSTestAssertions.That(() => migration.Changed.Should().BeTrue());
        MSTestAssertions.That(() => migration.NewText.Should().Contain("[NotNull]"));
        MSTestAssertions.That(() => migration.NewText.Should().Contain("// TODO(Jeninnet)"));
        MSTestAssertions.That(() => migration.NewText.Should().NotContain("[Required]\n"));
        MSTestAssertions.That(() => migration.Converted.Should().HaveCount(1));
        MSTestAssertions.That(() => migration.Converted[0].Detail.Should().Contain("DataAnnotations"));
        MSTestAssertions.That(() => migration.Converted[0].Detail.Should().Contain("Jeninnet"));
        MSTestAssertions.That(() => migration.Converted[0].Line.Should().Be(4));
        MSTestAssertions.That(() => migration.NeedsReview.Should().HaveCount(1));
    }

    [TestMethod]
    public void MigrateSource_LeavesCustomCall_WithNeedsReview() {
        const string source = """
            using FluentValidation;

            public class UserValidator : AbstractValidator<User> {
                public UserValidator() {
                    RuleFor(x => x.Name).NotEmpty().Custom((value, context) => { });
                }
            }
            """;

        var migration = MigrationEngine.MigrateSource("UserValidator.cs", source);

        MSTestAssertions.That(() => migration.NewText.Should().Contain(".Custom("));
        MSTestAssertions.That(() => migration.NeedsReview.Should().HaveCount(1));
        MSTestAssertions.That(() => migration.NeedsReview[0].Line.Should().Be(5));
        MSTestAssertions.That(() => migration.NeedsReview[0].Detail.Should().Contain("Custom"));
    }

    [TestMethod]
    public void MigrateSource_LeavesParameterizedConstructor_Untouched() {
        const string source = """
            using FluentValidation;

            public class UserValidator : AbstractValidator<User> {
                public UserValidator(string connectionString) {
                }
            }
            """;

        var migration = MigrationEngine.MigrateSource("UserValidator.cs", source);

        MSTestAssertions.That(() => migration.Changed.Should().BeFalse());
        MSTestAssertions.That(() => migration.NewText.Should().Be(source));
        MSTestAssertions.That(() => migration.NeedsReview.Should().HaveCount(1));
        MSTestAssertions.That(() => migration.NeedsReview[0].Line.Should().Be(3));
    }

    [TestMethod]
    public void MigrateSource_LeavesComplexSelector_WithNeedsReview() {
        const string source = """
            using FluentValidation;

            public class UserValidator : AbstractValidator<User> {
                public UserValidator() {
                    RuleFor(x => x.Name.Trim()).NotEmpty();
                }
            }
            """;

        var migration = MigrationEngine.MigrateSource("UserValidator.cs", source);

        MSTestAssertions.That(() => migration.Changed.Should().BeTrue());
        MSTestAssertions.That(() => migration.NewText.Should().Contain("builder.RuleFor"));
        MSTestAssertions.That(() => migration.NewText.Should().NotContain("nameof("));
        MSTestAssertions.That(() => migration.NeedsReview.Should().HaveCount(1));
        MSTestAssertions.That(() => migration.NeedsReview[0].Line.Should().Be(5));
    }

    [TestMethod]
    public void MigrateSource_ConvertsDottedPath_ToStringLiteral() {
        const string source = """
            using FluentValidation;

            public class UserValidator : AbstractValidator<User> {
                public UserValidator() {
                    RuleFor(x => x.Address.City).NotEmpty();
                }
            }
            """;

        var migration = MigrationEngine.MigrateSource("UserValidator.cs", source);

        MSTestAssertions.That(() => migration.NewText.Should().Contain("builder.RuleFor(x => x.Address.City, \"Address.City\")"));
    }

    [TestMethod]
    public void MigrateSource_IsIdempotent() {
        var first = MigrationEngine.MigrateSource("UserValidator.cs", VALIDATOR_SOURCE);
        var second = MigrationEngine.MigrateSource("UserValidator.cs", first.NewText);

        MSTestAssertions.That(() => first.Changed.Should().BeTrue());
        MSTestAssertions.That(() => second.Changed.Should().BeFalse());
        MSTestAssertions.That(() => second.NewText.Should().Be(first.NewText));
        MSTestAssertions.That(() => second.Converted.Should().BeEmpty());
        MSTestAssertions.That(() => second.NeedsReview.Should().BeEmpty());
    }
}
