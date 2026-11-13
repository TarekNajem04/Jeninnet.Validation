//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.ArchitectureTests;

/// <summary>
/// Executable guards for the NuGet publication contract: the shared icon asset is a valid
/// 128x128 PNG, the build wires the icon and SourceLink for every packable project, and the
/// release notes track the packable package set.
/// </summary>
[TestClass]
public sealed partial class PackagingBaselineTests {
    [GeneratedRegex(@"<IsPackable>\s*true\s*</IsPackable>", RegexOptions.CultureInvariant)]
    private static partial Regex PackablePattern();

    [GeneratedRegex("<PackageIcon>jeninnet-validation-icon-128\\.png</PackageIcon>", RegexOptions.CultureInvariant)]
    private static partial Regex PackageIconElementPattern();

    [GeneratedRegex(@"assets/jeninnet-validation-icon-128\.png", RegexOptions.CultureInvariant)]
    private static partial Regex PackageIconAssetPathPattern();

    [GeneratedRegex("PackageReference Include=\"Microsoft\\.SourceLink\\.GitHub\"", RegexOptions.CultureInvariant)]
    private static partial Regex SourceLinkReferencePattern();

    [GeneratedRegex("EnableSourceLink Condition=\"'\\$\\(CI\\)' != 'true'\">false", RegexOptions.CultureInvariant)]
    private static partial Regex SourceLinkDisabledPattern();

    [GeneratedRegex("PackageVersion\\s+Include=\"Microsoft\\.SourceLink\\.GitHub\"\\s+Version=\"[^\"]+\"", RegexOptions.CultureInvariant)]
    private static partial Regex SourceLinkVersionPattern();

    [GeneratedRegex("## Packages \\((\\d+)\\)", RegexOptions.CultureInvariant)]
    private static partial Regex ReleaseNotesPackageCountPattern();

    [TestMethod]
    public void PackageIcon_IsValid128PixelPng() {
        var path = Path.Combine(RepositoryRoot, "assets", "jeninnet-validation-icon-128.png");

        MSTestAssertions.That(() => File.Exists(path).Should().BeTrue("the shared NuGet icon must exist"));

        var bytes = File.ReadAllBytes(path);
        var signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

        MSTestAssertions.That(() => bytes.Length.Should().BeGreaterThan(33, "a PNG needs a signature, an IHDR chunk, and an IEND chunk"));
        MSTestAssertions.That(() => bytes.Take(8).Should().BeEquivalentTo(signature, "the icon must be a PNG file"));

        var width = ReadBigEndian(bytes, 16);
        var height = ReadBigEndian(bytes, 20);

        MSTestAssertions.That(() => width.Should().Be(128, "NuGet renders the icon at 128x128"));
        MSTestAssertions.That(() => height.Should().Be(128, "NuGet renders the icon at 128x128"));
        MSTestAssertions.That(() => bytes[24].Should().Be(8, "the icon must use 8-bit channels"));
        MSTestAssertions.That(() => bytes[25].Should().Be(6, "the icon must use RGBA color so rounded corners stay transparent"));
    }

    [TestMethod]
    public void BuildFiles_WireThePackageIconAndSourceLink() {
        var props = File.ReadAllText(Path.Combine(RepositoryRoot, "Directory.Build.props"));
        var packages = File.ReadAllText(Path.Combine(RepositoryRoot, "Directory.Packages.props"));

        MSTestAssertions.That(() => PackageIconElementPattern().IsMatch(props)
            .Should().BeTrue("every packable package must reference the shared icon"));
        MSTestAssertions.That(() => PackageIconAssetPathPattern().IsMatch(props)
            .Should().BeTrue("the shared icon must be packed into every nupkg"));
        MSTestAssertions.That(() => SourceLinkReferencePattern().IsMatch(props)
            .Should().BeTrue("SourceLink must stay wired for symbol-server debugging"));
        MSTestAssertions.That(() => SourceLinkDisabledPattern().IsMatch(props)
            .Should().BeTrue("SourceLink must stay off for local builds without a git remote"));
        MSTestAssertions.That(() => SourceLinkVersionPattern().IsMatch(packages)
            .Should().BeTrue("the SourceLink version must stay centrally pinned"));
    }

    [TestMethod]
    public void ReleaseNotes_TrackThePackablePackageSet() {
        var projects = FindPackableProjects();
        var notes = File.ReadAllText(Path.Combine(RepositoryRoot, ".github", "release-notes", "v1.0.0-preview.1.md"));
        var match = ReleaseNotesPackageCountPattern().Match(notes);

        MSTestAssertions.That(() => match.Success.Should().BeTrue("the release notes must state the package count"));
        MSTestAssertions.That(() => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture).Should().Be(
            projects.Count,
            "the release-notes package count must match the packable project set"));

        var missing = projects
            .Where(project => !notes.Contains($"`{project}`", StringComparison.Ordinal))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToList();

        MSTestAssertions.That(() => missing.Should().BeEmpty(
            $"the release notes must list every packable package: {string.Join(", ", missing)}"));
    }

    private static List<string> FindPackableProjects() {
        var sourceDirectory = Path.Combine(RepositoryRoot, "src");

        return [.. Directory
            .EnumerateFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories)
            .Where(static path => PackablePattern().IsMatch(File.ReadAllText(path)))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .Select(static path => Path.GetFileNameWithoutExtension(path))];
    }

    private static int ReadBigEndian(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

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
}
