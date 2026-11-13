//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Migration.Tests;

/// <summary>
/// Covers the migration readiness summary in the Markdown and JSON reports.
/// </summary>
[TestClass]
public sealed class MigrationReportTests {
    [TestMethod]
    public void Report_ComputesReadinessScoreAndFullyConvertibleFiles() {
        var report = new MigrationReport();
        var file = new FileMigration {
            Path = "UserValidator.cs",
            NewText = string.Empty,
            Changed = true
        };
        file.AddConverted(new MigrationFinding(MigrationFinding.ValidatorConverted, 1, "converted"));
        file.AddConverted(new MigrationFinding(MigrationFinding.RuleForMemberName, 2, "converted"));
        file.AddNeedsReview(new MigrationFinding(MigrationFinding.NeedsReview, 3, "review"));
        report.Files.Add(file);

        var markdown = report.ToMarkdown();
        var json = report.ToJson();

        MSTestAssertions.That(() => report.ReadinessScore.Should().Be(66.7));
        MSTestAssertions.That(() => report.FilesFullyConvertible.Should().Be(0));
        MSTestAssertions.That(() => markdown.Should().Contain("Migration readiness: 66.7% (2 auto-converted of 3 findings; 1 need review)"));
        MSTestAssertions.That(() => markdown.Should().Contain("Files fully convertible: 0 of 1"));
        MSTestAssertions.That(() => json.Should().Contain("\"scorePercent\": 66.7"));
        MSTestAssertions.That(() => json.Should().Contain("\"filesFullyConvertible\": 0"));
    }

    [TestMethod]
    public void Report_WithNoFindings_IsFullyReady() {
        var report = new MigrationReport();

        var markdown = report.ToMarkdown();
        var json = report.ToJson();

        MSTestAssertions.That(() => report.ReadinessScore.Should().Be(100.0));
        MSTestAssertions.That(() => markdown.Should().Contain("Migration readiness: 100.0% (no findings)"));
        MSTestAssertions.That(() => json.Should().Contain("\"scorePercent\": 100"));
    }

    [TestMethod]
    public void Report_CountsCleanFilesAsFullyConvertible() {
        var report = new MigrationReport();
        report.Files.Add(new FileMigration {
            Path = "CleanValidator.cs",
            NewText = string.Empty,
            Changed = false
        });

        MSTestAssertions.That(() => report.FilesFullyConvertible.Should().Be(1));
        MSTestAssertions.That(() => report.ToMarkdown().Should().Contain("Files fully convertible: 1 of 1"));
    }
}
