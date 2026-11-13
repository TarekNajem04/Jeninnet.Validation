//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.ArchitectureTests;

/// <summary>
/// Executable guards for the public-API baseline contract: every packable package has committed
/// PublicAPI files, the files stay analyzable, and DocFX documents every package.
/// </summary>
[TestClass]
public sealed partial class PublicApiBaselineTests {
    [GeneratedRegex(@"<IsPackable>\s*true\s*</IsPackable>", RegexOptions.CultureInvariant)]
    private static partial Regex PackablePattern();

    [GeneratedRegex("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", RegexOptions.CultureInvariant)]
    private static partial Regex TreatWarningsAsErrorsPattern();

    [GeneratedRegex("PackageVersion\\s+Include=\"Microsoft\\.CodeAnalysis\\.PublicApiAnalyzers\"\\s+Version=\"[^\"]+\"", RegexOptions.CultureInvariant)]
    private static partial Regex PublicApiAnalyzerVersionPattern();

    [TestMethod]
    public void PackableProjects_HaveValidPublicApiBaselines() {
        var projects = FindPackableProjects();
        var violations = new List<string>();

        MSTestAssertions.That(() => projects.Count.Should().Be(
            25,
            "the repository ships 25 packable packages; update this guard when the package set changes"));

        foreach(var project in projects) {
            CollectBaselineViolations(project, violations);
        }

        MSTestAssertions.That(() => violations.Should().BeEmpty(Format(violations)));
    }

    [TestMethod]
    public void PublicApiBaselines_AreOrdinalSorted() {
        var violations = new List<string>();

        foreach(var project in FindPackableProjects()) {
            foreach(var path in BaselinePaths(project)) {
                if(!File.Exists(path)) {
                    continue;
                }

                var entries = ReadEntries(path);
                var sorted = entries.OrderBy(static entry => entry, StringComparer.Ordinal).ToList();

                if(!entries.SequenceEqual(sorted, StringComparer.Ordinal)) {
                    violations.Add($"{Relative(path)} must be sorted with StringComparer.Ordinal.");
                }
            }
        }

        MSTestAssertions.That(() => violations.Should().BeEmpty(Format(violations)));
    }

    [TestMethod]
    public void DocFxMetadata_CoversEveryPackableProject() {
        var docfxPath = Path.Combine(RepositoryRoot, "docs", "docfx.json");
        using var document = JsonDocument.Parse(File.ReadAllText(docfxPath));

        var configured = document.RootElement
            .GetProperty("metadata")[0]
            .GetProperty("src")[0]
            .GetProperty("files")
            .EnumerateArray()
            .Select(static element => element.GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => Path.GetFileNameWithoutExtension(value!.Replace('\\', '/')))
            .ToHashSet(StringComparer.Ordinal);

        var packable = FindPackableProjects()
            .Select(static project => project.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = packable.Except(configured).OrderBy(static name => name, StringComparer.Ordinal).ToList();
        var extra = configured.Except(packable).OrderBy(static name => name, StringComparer.Ordinal).ToList();

        MSTestAssertions.That(() => missing.Should().BeEmpty(
            $"DocFX metadata is missing packable projects: {Format(missing)}"));
        MSTestAssertions.That(() => extra.Should().BeEmpty(
            $"DocFX metadata lists projects that are not packable: {Format(extra)}"));
    }

    [TestMethod]
    public void BuildFiles_WireThePublicApiAnalyzer() {
        var buildProps = File.ReadAllText(Path.Combine(RepositoryRoot, "Directory.Build.props"));
        var targets = File.ReadAllText(Path.Combine(RepositoryRoot, "Directory.Build.targets"));
        var packages = File.ReadAllText(Path.Combine(RepositoryRoot, "Directory.Packages.props"));

        MSTestAssertions.That(() => TreatWarningsAsErrorsPattern().IsMatch(buildProps)
            .Should().BeTrue("the analyzer diagnostics must stay build-breaking"));
        MSTestAssertions.That(() => targets.Contains(
                "Microsoft.CodeAnalysis.PublicApiAnalyzers",
                StringComparison.Ordinal)
            .Should().BeTrue("the analyzer must be referenced by the build"));
        MSTestAssertions.That(() => targets.Contains("IsPackable", StringComparison.Ordinal)
            .Should().BeTrue("the analyzer must stay scoped to packable projects"));
        MSTestAssertions.That(() => PublicApiAnalyzerVersionPattern().IsMatch(packages)
            .Should().BeTrue("the analyzer version must stay centrally pinned"));
    }

    private static void CollectBaselineViolations(PackableProject project, List<string> violations) {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach(var path in BaselinePaths(project)) {
            var fileName = Path.GetFileName(path);

            if(!File.Exists(path)) {
                violations.Add($"{Relative(path)} is missing.");
                continue;
            }

            var bytes = File.ReadAllBytes(path);

            if(bytes.Length >= 3 && bytes[0] is 0xEF && bytes[1] is 0xBB && bytes[2] is 0xBF) {
                violations.Add($"{Relative(path)} must be UTF-8 without BOM.");
            }

            string text;

            try {
                text = new System.Text.UTF8Encoding(false, true).GetString(bytes);
            }
            catch(DecoderFallbackException) {
                violations.Add($"{Relative(path)} is not valid UTF-8.");
                continue;
            }

            if(text.Contains('\r')) {
                violations.Add($"{Relative(path)} must use LF line endings.");
            }

            if(!text.EndsWith('\n')) {
                violations.Add($"{Relative(path)} must end with a newline.");
            }

            var lines = text.Split('\n');

            if(lines.Length is 0 || lines[0] is not "#nullable enable") {
                violations.Add($"{Relative(path)} must start with '#nullable enable'.");
            }

            var entries = ReadEntries(path);

            if(fileName is "PublicAPI.Shipped.txt" && entries.Count is 0) {
                violations.Add($"{Relative(path)} must contain at least one API entry.");
            }

            foreach(var entry in entries) {
                if(seen.TryGetValue(entry, out var otherFile)) {
                    violations.Add($"{Relative(path)} duplicates '{entry}' from {otherFile}.");
                } else {
                    seen[entry] = Relative(path);
                }
            }
        }
    }

    private static List<PackableProject> FindPackableProjects() {
        var sourceDirectory = Path.Combine(RepositoryRoot, "src");

        return [.. Directory
            .EnumerateFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories)
            .Where(static path => PackablePattern().IsMatch(File.ReadAllText(path)))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .Select(static path => new PackableProject(
                Path.GetFileNameWithoutExtension(path),
                Path.GetDirectoryName(path)!))];
    }

    private static IEnumerable<string> BaselinePaths(PackableProject project) {
        yield return Path.Combine(project.Directory, "PublicAPI.Shipped.txt");
        yield return Path.Combine(project.Directory, "PublicAPI.Unshipped.txt");
    }

    private static List<string> ReadEntries(string path) =>
        [.. File.ReadAllLines(path).Where(static line => line.Length > 0 && !line.StartsWith('#'))];

    private static string Relative(string path) =>
        Path.GetRelativePath(RepositoryRoot, path).Replace('\\', '/');

    private static string Format(IEnumerable<string> messages) =>
        string.Join(Environment.NewLine, messages);

    private static string RepositoryRoot {
        get {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while(directory is not null) {
                if(File.Exists(Path.Combine(directory.FullName, "Jeninnet.Validation.slnx"))) {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
        }
    }

    private sealed record PackableProject(string Name, string Directory);
}
