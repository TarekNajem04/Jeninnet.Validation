//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Metadata;

internal sealed class ClientParityModel {
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public List<string>? Tags { get; set; }
}

file sealed class ClientParityValidator : Validator<ClientParityModel> {
    protected override void Configure(ValidatorRuleBuilder<ClientParityModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(ClientParityModel.Name))
               .NotEmpty()
               .Length(2, 10);

        builder.RuleFor(static model => model.Email, nameof(ClientParityModel.Email))
               .EmailAddress();

        builder.RuleFor(static model => model.Age, nameof(ClientParityModel.Age))
               .InclusiveBetween(18, 120)
               .MultipleOf(2);

        builder.RuleFor(static model => model.Tags, nameof(ClientParityModel.Tags))
               .Count(1, 3)
               .Unique();
    }
}

[TestClass]
public sealed class ClientRulesParityTests {
    [TestMethod]
    public void Manifest_CSharpSupport_AndJavaScriptSupport_AreIdentical() {
        var manifestRules = ReadManifestRules();
        var csharpRules = ClientRuleSupport.SupportedRules
            .OrderBy(static rule => rule, StringComparer.Ordinal)
            .ToArray();
        var javascriptSource = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "client",
            "src",
            "client-rule-support.generated.js"));
        var javascriptRulePattern = new Regex(
            "^\\s+\"(?<rule>[^\"]+)\",$",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);
        var javascriptRules = javascriptRulePattern.Matches(javascriptSource)
            .Select(static match => match.Groups["rule"].Value)
            .OrderBy(static rule => rule, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(manifestRules, csharpRules, "The C# support list drifted from the manifest.");
        CollectionAssert.AreEqual(manifestRules, javascriptRules, "The JavaScript support list drifted from the manifest.");
    }

    [TestMethod]
    public void AllSupportedRulesFixture_CoversTheManifestAndHasNoServerOnlyRules() {
        var fixture = ReadFixture();
        var fixtureCase = FindCase(fixture, "all-supported-rules-valid");
        var payloadRules = fixtureCase["payload"]!["validators"]![0]!["members"]!.AsArray()
            .SelectMany(static member => member!["rules"]!.AsArray())
            .ToArray();

        CollectionAssert.AreEquivalent(
            ReadManifestRules(),
            payloadRules.Select(static rule => rule!["rule"]!.GetValue<string>()).ToArray(),
            "The parity fixture must cover every supported rule.");

        foreach(var rule in payloadRules) {
            Assert.IsNull(rule!["serverOnly"], $"Rule '{rule["rule"]}' must not be server-only in the supported fixture.");
        }
    }

    [TestMethod]
    public void CSharpExporter_MatchesTheSharedErrorFixturePayload() {
        var fixture = ReadFixture();
        var fixtureCase = FindCase(fixture, "supported-errors");
        var actual = JsonNode.Parse(ClientRulesWriter.WriteToString([BuildErrorDescriptor()]));

        MSTestAssertions.That(() => JsonNode.DeepEquals(actual, fixtureCase["payload"]).Should().BeTrue(
            "the C# exporter payload must match the shared fixture"));
    }

    [TestMethod]
    public void CSharpValidator_ProducesTheSharedErrorFixtureErrors() {
        var fixture = ReadFixture();
        var fixtureCase = FindCase(fixture, "supported-errors");
        var modelNode = fixtureCase["model"]!.AsObject();
        var model = new ClientParityModel {
            Name = modelNode["Name"]!.GetValue<string>(),
            Email = modelNode["Email"]!.GetValue<string>(),
            Age = modelNode["Age"]!.GetValue<int>(),
            Tags = modelNode["Tags"]!.AsArray().Select(static tag => tag!.GetValue<string>()).ToList()
        };

        var actualErrors = new JsonArray(
            new ClientParityValidator().Validate(model).Errors
                .OrderBy(static error => error.MemberName, StringComparer.Ordinal)
                .ThenBy(static error => error.MessageKey, StringComparer.Ordinal)
                .Select(static error => (JsonNode)new JsonObject {
                    ["member"] = error.MemberName,
                    ["rule"] = error.MessageKey,
                    ["messageKey"] = error.MessageKey,
                    ["severity"] = error.Severity.ToString()
                })
                .ToArray());

        MSTestAssertions.That(() => JsonNode.DeepEquals(actualErrors, fixtureCase["expected"]!["errors"]).Should().BeTrue(
            "the C# validator errors must match the shared fixture"));
    }

    [TestMethod]
    public void ServerOnlyFixture_IsMarkedServerOnlyByTheCSharpExporter() {
        var fixture = ReadFixture();
        var fixtureCase = FindCase(fixture, "server-only-conditional-custom");
        var actual = JsonNode.Parse(ClientRulesWriter.WriteToString([BuildServerOnlyDescriptor()]));

        MSTestAssertions.That(() => JsonNode.DeepEquals(actual, fixtureCase["payload"]).Should().BeTrue(
            "the server-only fixture payload must match the C# exporter"));

        var rules = fixtureCase["payload"]!["validators"]![0]!["members"]!.AsArray()
            .SelectMany(static member => member!["rules"]!.AsArray())
            .ToArray();

        Assert.IsTrue(rules.All(static rule => rule!["serverOnly"]!.GetValue<bool>()),
            "every rule in the server-only fixture must be marked serverOnly");
    }

    private static ValidationDescriptor BuildErrorDescriptor() =>
        new(
            "Parity.CustomerValidator",
            [
                new ValidationMemberDescriptor("Name", [Rule("NotEmpty"), Rule("Length", ("Min", 2), ("Max", 10))]),
                new ValidationMemberDescriptor("Email", [Rule("Email")]),
                new ValidationMemberDescriptor("Age", [Rule("InclusiveBetween", ("Min", 18), ("Max", 120)), Rule("MultipleOf", ("Divisor", 2m))]),
                new ValidationMemberDescriptor("Tags", [Rule("Count", ("Min", 1), ("Max", 3)), Rule("UniqueItems")])
            ]);

    private static ValidationDescriptor BuildServerOnlyDescriptor() =>
        new(
            "Parity.ServerOnly",
            [
                new ValidationMemberDescriptor("CustomValue", [Rule("Must")]),
                new ValidationMemberDescriptor("ConditionalValue", [Rule("NotEmpty", hasCondition: true)])
            ]);

    private static ValidationRuleDescriptor Rule(string kind, params (string Key, object? Value)[] parameters) =>
        new(
            kind,
            parameters: parameters.ToDictionary(static item => item.Key, static item => item.Value, StringComparer.Ordinal));

    private static ValidationRuleDescriptor Rule(string kind, bool hasCondition) =>
        new(kind, hasCondition: hasCondition);

    private static string[] ReadManifestRules() =>
        ReadManifest()["supportedRules"]!.AsArray()
            .Select(static rule => rule!.GetValue<string>())
            .OrderBy(static rule => rule, StringComparer.Ordinal)
            .ToArray();

    private static JsonObject ReadManifest() =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "client", "rules-manifest.json")))!.AsObject();

    private static JsonObject ReadFixture() =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "client",
            "test",
            "fixtures",
            "client-rules-parity.json")))!.AsObject();

    private static JsonObject FindCase(JsonObject fixture, string name) =>
        fixture["cases"]!.AsArray()
            .Select(static item => item!.AsObject())
            .Single(item => item["name"]!.GetValue<string>() == name);

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
