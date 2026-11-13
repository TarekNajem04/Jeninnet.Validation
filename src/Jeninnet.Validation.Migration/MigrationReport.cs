//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Migration;

/// <summary>
/// The aggregate migration outcome across scanned files.
/// </summary>
internal sealed class MigrationReport {
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    /// <summary>Gets the per-file outcomes in scan order.</summary>
    public List<FileMigration> Files { get; } = [];

    /// <summary>Gets the number of scanned files.</summary>
    public int FilesScanned => Files.Count;

    /// <summary>Gets the number of files that would change (or changed).</summary>
    public int FilesChanged => Files.Count(static file => file.Changed);

    /// <summary>Gets the number of findings the codemod can apply safely.</summary>
    public int ConvertedCount => Files.Sum(static file => file.Converted.Count);

    /// <summary>Gets the number of findings that need a human decision.</summary>
    public int NeedsReviewCount => Files.Sum(static file => file.NeedsReview.Count);

    /// <summary>Gets the number of files with no review findings.</summary>
    public int FilesFullyConvertible => Files.Count(static file => file.NeedsReview.Count == 0);

    /// <summary>
    /// Gets the percentage of findings the codemod can apply without human review. A scan with no
    /// findings is reported as 100%.
    /// </summary>
    public double ReadinessScore {
        get {
            var total = ConvertedCount + NeedsReviewCount;

            return total == 0
                ? 100.0
                : Math.Round(100.0 * ConvertedCount / total, 1);
        }
    }

    /// <summary>Gets the converted findings across all files.</summary>
    /// <returns>The converted findings in ordinal path order.</returns>
    public IReadOnlyList<(string Path, MigrationFinding Finding)> GetConverted() =>
        [.. Files.SelectMany(static file => file.Converted.Select(finding => (file.Path, Finding: finding)))
                 .OrderBy(static entry => entry.Path, StringComparer.Ordinal)
                 .ThenBy(static entry => entry.Finding.Line)];

    /// <summary>Gets the needs-review findings across all files.</summary>
    /// <returns>The needs-review findings in ordinal path order.</returns>
    public IReadOnlyList<(string Path, MigrationFinding Finding)> GetNeedsReview() =>
        [.. Files.SelectMany(static file => file.NeedsReview.Select(finding => (file.Path, Finding: finding)))
                 .OrderBy(static entry => entry.Path, StringComparer.Ordinal)
                 .ThenBy(static entry => entry.Finding.Line)];

    /// <summary>
    /// Renders a deterministic Markdown report. Sections and rows are ordinal-sorted so repeated
    /// runs produce byte-identical output.
    /// </summary>
    /// <returns>The Markdown report.</returns>
    public string ToMarkdown() {
        var builder = new StringBuilder();
        builder.AppendLine("# Jeninnet migration report")
            .AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"- Files scanned: {FilesScanned}")
            .AppendLine(CultureInfo.InvariantCulture, $"- Files to change: {FilesChanged}");
        var converted = GetConverted();
        var needsReview = GetNeedsReview();
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Converted items: {converted.Count}")
            .AppendLine(CultureInfo.InvariantCulture, $"- Needs review: {needsReview.Count}")
            .AppendLine(CultureInfo.InvariantCulture, $"- Migration readiness: {FormatReadiness()}")
            .AppendLine(CultureInfo.InvariantCulture, $"- Files fully convertible: {FilesFullyConvertible} of {FilesScanned}");

        AppendSection(builder, "Converted", converted);
        AppendSection(builder, "Needs review", needsReview);

        builder.AppendLine()
            .AppendLine("## Scope and assumptions")
            .AppendLine()
            .AppendLine("- Source libraries: FluentValidation (`AbstractValidator<T>`, `RuleFor`, `WithErrorCode`, `Custom`, `Transform`) and DataAnnotations (`[Required]`).")
            .AppendLine("- Matching is syntactic and name-based; project references are not resolved. A same-named API from another library would match too — review the Converted items before deleting the old dependency.")
            .AppendLine("- Conversions preserve behavior only for the listed safe patterns; everything else is left untouched with TODO markers.")
            .AppendLine()
            .AppendLine("## Follow-ups")
            .AppendLine()
            .AppendLine("- Remove FluentValidation package references once every file is migrated (the migrated code no longer needs the dependency).")
            .AppendLine("- Remove now-unused `using FluentValidation;` and `using System.ComponentModel.DataAnnotations;` directives (the codemod leaves them so the diff stays reviewable).")
            .AppendLine("- Resolve every `TODO(Jeninnet)` marker left by the codemod (each marks a semantic decision a human must confirm).");
        return builder.ToString();
    }

    private string FormatReadiness() {
        var total = ConvertedCount + NeedsReviewCount;

        return total == 0
            ? "100.0% (no findings)"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{ReadinessScore:0.0}% ({ConvertedCount} auto-converted of {total} findings; {NeedsReviewCount} need review)");
    }

    private static void AppendSection(
        StringBuilder builder,
        string title,
        IReadOnlyList<(string Path, MigrationFinding Finding)> items) {
        builder.AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"## {title}")
            .AppendLine();

        if(items.Count == 0) {
            builder.AppendLine("None.");
            return;
        }

        builder.AppendLine("| File | Line | Kind | Detail |")
            .AppendLine("|---|---|---|---|");

        foreach(var (path, finding) in items) {
            builder.Append(CultureInfo.InvariantCulture, $"| {Escape(path)} | {finding.Line} | {finding.Kind} | {Escape(finding.Detail)} |")
                .AppendLine();
        }
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
             .Replace("|", "\\|", StringComparison.Ordinal)
             .Replace("\r", " ", StringComparison.Ordinal)
             .Replace("\n", " ", StringComparison.Ordinal);

    /// <summary>
    /// Renders a deterministic JSON report with ordinal-sorted files and findings.
    /// </summary>
    /// <returns>The JSON report.</returns>
    public string ToJson() {
        var files = Files
            .OrderBy(static file => file.Path, StringComparer.Ordinal)
            .Select(static file => new SortedDictionary<string, object?> {
                ["path"] = file.Path,
                ["changed"] = file.Changed,
                ["converted"] = file.Converted
                    .OrderBy(static finding => finding.Line)
                    .Select(static finding => new SortedDictionary<string, object?> {
                        ["kind"] = finding.Kind,
                        ["line"] = finding.Line,
                        ["detail"] = finding.Detail
                    }).ToList(),
                ["needsReview"] = file.NeedsReview
                    .OrderBy(static finding => finding.Line)
                    .Select(static finding => new SortedDictionary<string, object?> {
                        ["kind"] = finding.Kind,
                        ["line"] = finding.Line,
                        ["detail"] = finding.Detail
                    }).ToList()
            }).ToList();

        var root = new SortedDictionary<string, object?> {
            ["filesScanned"] = FilesScanned,
            ["filesChanged"] = FilesChanged,
            ["readiness"] = new SortedDictionary<string, object?> {
                ["scorePercent"] = ReadinessScore,
                ["converted"] = ConvertedCount,
                ["needsReview"] = NeedsReviewCount,
                ["filesFullyConvertible"] = FilesFullyConvertible
            },
            ["files"] = files
        };

        return JsonSerializer.Serialize(root, _jsonOptions) + Environment.NewLine;
    }
}
