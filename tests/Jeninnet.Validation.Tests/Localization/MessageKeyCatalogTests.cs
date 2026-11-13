//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Localization;

// ---------------------------------------------------------------------------
// Catalog model (mirrors scripts/localization/check-message-keys.ps1)
// ---------------------------------------------------------------------------

/// <summary>
/// The outcome of comparing message-key constants against message packs.
/// </summary>
internal sealed class MessageKeyCatalogResult {
    public List<string> Constants { get; } = [];
    public List<string> Cultures { get; } = [];
    public Dictionary<string, List<string>> Missing { get; } = [with(StringComparer.Ordinal)];
    public Dictionary<string, List<string>> Extra { get; } = [with(StringComparer.Ordinal)];
    public List<string> StaleAllowList { get; } = [];

    public bool IsClean =>
        Missing.Count == 0 && Extra.Count == 0 && StaleAllowList.Count == 0;
}

/// <summary>
/// Compares message-key constants with pack entries using ordinal ordering.
/// </summary>
internal static class MessageKeyCatalog {
    public static MessageKeyCatalogResult Compute(
        IReadOnlyDictionary<string, string> constantsByName,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> packs,
        IEnumerable<string> allowList) {
        ArgumentNullException.ThrowIfNull(constantsByName);
        ArgumentNullException.ThrowIfNull(packs);
        ArgumentNullException.ThrowIfNull(allowList);

        var allowed = new HashSet<string>(allowList, StringComparer.Ordinal);
        var constantValues = new HashSet<string>(constantsByName.Values, StringComparer.Ordinal);
        var usedAllowList = new HashSet<string>(StringComparer.Ordinal);
        var result = new MessageKeyCatalogResult();

        result.Constants.AddRange(constantValues.OrderBy(static key => key, StringComparer.Ordinal));
        result.Cultures.AddRange(packs.Keys.OrderBy(static culture => culture, StringComparer.Ordinal));

        foreach(var culture in result.Cultures) {
            var missing = constantValues
                .Where(value => !packs[culture].ContainsKey(value))
                .OrderBy(static key => key, StringComparer.Ordinal)
                .ToList();
            var extra = packs[culture].Keys
                .Where(key => !constantValues.Contains(key))
                .OrderBy(static key => key, StringComparer.Ordinal)
                .ToList();

            if(missing.Count > 0) {
                result.Missing[culture] = missing;
            }

            var unlisted = extra.Where(key => !allowed.Contains(key)).ToList();

            if(unlisted.Count > 0) {
                result.Extra[culture] = unlisted;
            }

            foreach(var key in extra) {
                if(allowed.Contains(key)) {
                    usedAllowList.Add(key);
                }
            }
        }

        result.StaleAllowList.AddRange(allowed
            .Where(key => !usedAllowList.Contains(key))
            .OrderBy(static key => key, StringComparer.Ordinal));

        return result;
    }

    /// <summary>
    /// Renders the catalog result deterministically: ordinal-sorted sections only.
    /// </summary>
    /// <param name="result">The catalog result to render.</param>
    public static string RenderReport(MessageKeyCatalogResult result) {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        builder.AppendLine("# Message-key catalog report")
            .AppendLine(CultureInfo.InvariantCulture, $"Constants: {result.Constants.Count}")
            .AppendLine(CultureInfo.InvariantCulture, $"Cultures: {result.Cultures.Count}");

        foreach(var culture in result.Cultures) {
            builder.AppendLine(CultureInfo.InvariantCulture, $"[{culture}]");
        }

        foreach(var culture in result.Missing.Keys.OrderBy(static key => key, StringComparer.Ordinal)) {
            foreach(var key in result.Missing[culture]) {
                builder.AppendLine(CultureInfo.InvariantCulture, $"missing:{culture}:{key}");
            }
        }

        foreach(var culture in result.Extra.Keys.OrderBy(static key => key, StringComparer.Ordinal)) {
            foreach(var key in result.Extra[culture]) {
                builder.AppendLine(CultureInfo.InvariantCulture, $"extra:{culture}:{key}");
            }
        }

        foreach(var key in result.StaleAllowList) {
            builder.AppendLine(CultureInfo.InvariantCulture, $"stale:{key}");
        }

        return builder.ToString();
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed partial class MessageKeyCatalogTests {
    [GeneratedRegex(
        """public\s+const\s+string\s+(?<name>[A-Za-z0-9_]+)\s*=\s*"(?<value>(?:[^"\\]|\\.)*)"\s*;""",
        RegexOptions.CultureInvariant)]
    private static partial Regex ConstantPattern();

    [TestMethod]
    public void RepoCatalog_IsClean() {
        var result = ComputeRepoCatalog();

        MSTestAssertions.That(() => result.Constants.Should().HaveCount(53,
            "the catalog pins the constant count; update the guard when keys change"));
        MSTestAssertions.That(() => result.Cultures.Should().HaveCount(40,
            "the catalog pins the culture count; update the guard when packs change"));
        MSTestAssertions.That(() => result.Missing.Should().BeEmpty(
            $"Missing keys: {Format(result.Missing)}"));
        MSTestAssertions.That(() => result.Extra.Should().BeEmpty(
            $"Extra keys: {Format(result.Extra)}"));
        MSTestAssertions.That(() => result.StaleAllowList.Should().BeEmpty(
            $"Stale allow-list entries: {string.Join(", ", result.StaleAllowList)}"));
    }

    [TestMethod]
    public void RepoCatalog_AllowListIsAValidStringArray() {
        var entries = ReadAllowList();

        MSTestAssertions.That(() => entries.Should().NotBeNull());
    }

    [TestMethod]
    public void MissingKey_IsReported() {
        var result = MessageKeyCatalog.Compute(
            new Dictionary<string, string> { ["NOT_NULL"] = "NotNull", ["EMAIL"] = "Email" },
            new Dictionary<string, IReadOnlyDictionary<string, string>> {
                ["en"] = new Dictionary<string, string> { ["NotNull"] = "required" }
            },
            []);

        MSTestAssertions.That(() => result.IsClean.Should().BeFalse());
        MSTestAssertions.That(() => result.Missing["en"].Should().BeEquivalentTo("Email"));
        MSTestAssertions.That(() => result.Extra.Should().BeEmpty());
    }

    [TestMethod]
    public void ExtraKey_IsReported() {
        var result = MessageKeyCatalog.Compute(
            new Dictionary<string, string> { ["NOT_NULL"] = "NotNull" },
            new Dictionary<string, IReadOnlyDictionary<string, string>> {
                ["en"] = new Dictionary<string, string> {
                    ["NotNull"] = "required",
                    ["ZombieKey"] = "zombie"
                }
            },
            []);

        MSTestAssertions.That(() => result.IsClean.Should().BeFalse());
        MSTestAssertions.That(() => result.Extra["en"].Should().BeEquivalentTo("ZombieKey"));
        MSTestAssertions.That(() => result.Missing.Should().BeEmpty());
    }

    [TestMethod]
    public void AllowListedExtraKey_IsExempt() {
        var result = MessageKeyCatalog.Compute(
            new Dictionary<string, string> { ["NOT_NULL"] = "NotNull" },
            new Dictionary<string, IReadOnlyDictionary<string, string>> {
                ["en"] = new Dictionary<string, string> {
                    ["NotNull"] = "required",
                    ["ZombieKey"] = "zombie"
                }
            },
            ["ZombieKey"]);

        MSTestAssertions.That(() => result.IsClean.Should().BeTrue());
    }

    [TestMethod]
    public void StaleAllowListEntry_IsReported() {
        var result = MessageKeyCatalog.Compute(
            new Dictionary<string, string> { ["NOT_NULL"] = "NotNull" },
            new Dictionary<string, IReadOnlyDictionary<string, string>> {
                ["en"] = new Dictionary<string, string> { ["NotNull"] = "required" }
            },
            ["GhostKey"]);

        MSTestAssertions.That(() => result.IsClean.Should().BeFalse());
        MSTestAssertions.That(() => result.StaleAllowList.Should().BeEquivalentTo("GhostKey"));
    }

    [TestMethod]
    public void CatalogReport_IsDeterministic() {
        var first = MessageKeyCatalog.Compute(
            new Dictionary<string, string> { ["B_KEY"] = "B", ["A_KEY"] = "A" },
            new Dictionary<string, IReadOnlyDictionary<string, string>> {
                ["en"] = new Dictionary<string, string> { ["B"] = "b", ["A"] = "a" },
                ["ar"] = new Dictionary<string, string> { ["A"] = "a" }
            },
            ["Zed"]);
        var second = MessageKeyCatalog.Compute(
            new Dictionary<string, string> { ["A_KEY"] = "A", ["B_KEY"] = "B" },
            new Dictionary<string, IReadOnlyDictionary<string, string>> {
                ["ar"] = new Dictionary<string, string> { ["A"] = "a" },
                ["en"] = new Dictionary<string, string> { ["A"] = "a", ["B"] = "b" }
            },
            ["Zed"]);

        MSTestAssertions.That(() => MessageKeyCatalog.RenderReport(first).Should().Be(
            MessageKeyCatalog.RenderReport(second)));
    }

    [TestMethod]
    public void Constants_HaveUniqueNamesAndValues() {
        var constants = ReadConstants();

        MSTestAssertions.That(() => constants.Keys.Should().OnlyHaveUniqueItems());
        MSTestAssertions.That(() => constants.Values.Should().OnlyHaveUniqueItems());
    }

    private static MessageKeyCatalogResult ComputeRepoCatalog() {
        var packs = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

        foreach(var packFile in Directory.EnumerateFiles(ResourcesDirectory, "*.json")) {
            var culture = Path.GetFileNameWithoutExtension(packFile);

            using var document = JsonDocument.Parse(File.ReadAllText(packFile));
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach(var property in document.RootElement.EnumerateObject()) {
                entries[property.Name] = property.Value.GetString() ?? string.Empty;
            }

            packs[culture] = entries;
        }

        return MessageKeyCatalog.Compute(ReadConstants(), packs, ReadAllowList());
    }

    private static Dictionary<string, string> ReadConstants() {
        var constants = new Dictionary<string, string>(StringComparer.Ordinal);
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot, "src", "Jeninnet.Validation.Abstractions",
            "Configuration", "ValidationMessageKeys.cs"));

        foreach(Match match in ConstantPattern().Matches(source)) {
            constants[match.Groups["name"].Value] = match.Groups["value"].Value;
        }

        return constants;
    }

    private static List<string> ReadAllowList() {
        using var document = JsonDocument.Parse(File.ReadAllText(AllowListPath));

        if(document.RootElement.ValueKind is not JsonValueKind.Array) {
            throw new AssertFailedException("The message-key allow-list must be a JSON array.");
        }

        return [.. document.RootElement.EnumerateArray().Select(static element => element.GetString() ?? string.Empty)];
    }

    private static string Format(Dictionary<string, List<string>> groups) =>
        string.Join(
            Environment.NewLine,
            groups
                .OrderBy(static group => group.Key, StringComparer.Ordinal)
                .Select(static group => $"{group.Key}: {string.Join(", ", group.Value)}"));

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

    private static string ResourcesDirectory =>
        Path.Combine(RepositoryRoot, "src", "Jeninnet.Validation.Localization", "Resources");

    private static string AllowListPath =>
        Path.Combine(RepositoryRoot, "src", "Jeninnet.Validation.Localization", "MessageKeyAllowList.json");
}
