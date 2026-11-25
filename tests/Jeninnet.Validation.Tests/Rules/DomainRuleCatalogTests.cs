//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Jeninnet.Validation.Tests.Rules;

/// <summary>
/// Keeps the domain rule pack, its public documentation, message keys, and localization packs in
/// sync. Rules are discovered from the domain assembly, so adding a rule without documentation is
/// a test failure rather than an invisible gap.
/// </summary>
[TestClass]
public sealed class DomainRuleCatalogTests {
    [TestMethod]
    public void EveryDomainRuleHasDocumentationMessageKeyAndCultureCoverage() {
        var repositoryRoot = FindRepositoryRoot();
        var rules = DiscoverRules();
        var documentation = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "docs",
            "en",
            "03-reference",
            "02-rule-reference.md"));
        var messageKeys = ReadMessageKeys();
        var packs = ReadMessagePacks(Path.Combine(
            repositoryRoot,
            "src",
            "Jeninnet.Validation.Localization",
            "Resources"));

        try {
            var failures = Validate(rules, documentation, messageKeys, packs);

            MSTestAssertions.That(() => rules.Should().HaveCount(7));
            MSTestAssertions.That(() => packs.Should().NotBeEmpty());
            MSTestAssertions.That(() => failures.Should().BeEmpty(string.Join(Environment.NewLine, failures)));
        }
        finally {
            foreach(var pack in packs) {
                pack.Document.Dispose();
            }
        }
    }

    [TestMethod]
    public void CatalogCheck_ReportsAnUndocumentedRule() {
        using var pack = JsonDocument.Parse("""{"Iban":"IBAN is invalid."}""");
        var packs = new[] { (Culture: "en", Document: pack) };
        var failures = Validate(
            ["Iban", "UndocumentedRule"],
            "| `Iban(country?)` | `[Iban(\"NL\")]` | `Iban` | Structural check. |",
            new Dictionary<string, string>(StringComparer.Ordinal) { ["Iban"] = "IBAN" },
            packs);

        MSTestAssertions.That(() => failures.Any(static failure =>
            failure.Contains("UndocumentedRule", StringComparison.Ordinal)).Should().BeTrue());
    }

    private static IReadOnlyList<string> DiscoverRules() =>
        [.. typeof(IbanRule<,>).Assembly
            .GetTypes()
            .Where(static type =>
                type.IsPublic &&
                type.IsGenericTypeDefinition &&
                type.GetGenericArguments().Length is 2 &&
                type.Name.EndsWith("Rule`2", StringComparison.Ordinal))
            .Select(static type => type.Name[..^6])
            .OrderBy(static name => name, StringComparer.Ordinal)];

    private static Dictionary<string, string> ReadMessageKeys() {
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach(var field in typeof(ValidationMessageKeys).GetFields(BindingFlags.Public | BindingFlags.Static)) {
            if(field.IsLiteral && field.FieldType == typeof(string) && field.GetRawConstantValue() is string value) {
                keys[value] = field.Name;
            }
        }

        return keys;
    }

    private static IReadOnlyList<(string Culture, JsonDocument Document)> ReadMessagePacks(string directory) =>
        [.. Directory.GetFiles(directory, "*.json")
            .OrderBy(static path => path, StringComparer.Ordinal)
            .Select(static path => (
                Culture: Path.GetFileNameWithoutExtension(path),
                Document: JsonDocument.Parse(File.ReadAllText(path))))];

    private static List<string> Validate(
        IReadOnlyList<string> rules,
        string documentation,
        Dictionary<string, string> messageKeys,
        IReadOnlyList<(string Culture, JsonDocument Document)> packs) {
        var failures = new List<string>();

        foreach(var rule in rules) {
            var documentationPattern = $@"^\|\s*`{Regex.Escape(rule)}\(";

            if(!Regex.IsMatch(documentation, documentationPattern, RegexOptions.Multiline)) {
                failures.Add($"{rule}: missing row in docs/en/03-reference/02-rule-reference.md.");
            }

            if(!messageKeys.ContainsKey(rule)) {
                failures.Add($"{rule}: no ValidationMessageKeys constant owns the key.");
            }

            foreach(var pack in packs) {
                if(!pack.Document.RootElement.TryGetProperty(rule, out var template) ||
                   template.ValueKind is not JsonValueKind.String ||
                   string.IsNullOrWhiteSpace(template.GetString())) {
                    failures.Add($"{rule}: message pack '{pack.Culture}' is missing a non-empty template.");
                }
            }
        }

        return failures;
    }

    private static string FindRepositoryRoot() {
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
