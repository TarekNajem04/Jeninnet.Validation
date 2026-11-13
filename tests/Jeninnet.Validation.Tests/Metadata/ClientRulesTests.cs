//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Metadata;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

internal sealed class ClientRulesModel {
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public List<string>? Tags { get; set; }
}

internal sealed class OtherClientRulesModel {
    public string? Code { get; set; }
}

file sealed class ClientRulesValidator : Validator<ClientRulesModel> {
    protected override void Configure(ValidatorRuleBuilder<ClientRulesModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ClientRulesModel.Name))
               .NotEmpty()
               .Length(2, 50)
               .WithMessage("Authored length");

        builder.RuleFor(static x => x.Email, nameof(ClientRulesModel.Email))
               .EmailAddress();

        builder.RuleFor(static x => x.Age, nameof(ClientRulesModel.Age))
               .InclusiveBetween(18, 120)
               .MultipleOf(2);

        builder.RuleFor(static x => x.Tags, nameof(ClientRulesModel.Tags))
               .Count(1, 5)
               .Unique();
    }
}

file sealed class OtherClientRulesValidator : Validator<OtherClientRulesModel> {
    protected override void Configure(ValidatorRuleBuilder<OtherClientRulesModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Code, nameof(OtherClientRulesModel.Code))
               .NotEmpty();
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class ClientRulesTests {
    [TestMethod]
    public void Writer_ExportsDeterministicJsonPayload() {
        var descriptor = new ClientRulesValidator().Describe();

        var first = ClientRulesWriter.WriteToString([descriptor]);
        var second = ClientRulesWriter.WriteToString([descriptor]);
        var document = JsonNode.Parse(first)!;

        MSTestAssertions.That(() => first.Should().Be(second));
        MSTestAssertions.That(() => document["schemaVersion"]!.GetValue<string>().Should().Be("1.0"));

        var validators = document["validators"]!.AsArray();
        MSTestAssertions.That(() => validators.Should().HaveCount(1));

        var validator = validators[0]!;
        MSTestAssertions.That(() => validator["name"]!.GetValue<string>().Should().Be(typeof(ClientRulesValidator).FullName));
        MSTestAssertions.That(() => validator["type"]!.GetValue<string>().Should().Be(typeof(ClientRulesModel).FullName));

        var members = validator["members"]!.AsArray();
        var memberNames = members.Select(static member => member!["name"]!.GetValue<string>()).ToArray();
        MSTestAssertions.That(() => memberNames.SequenceEqual(["Age", "Email", "Name", "Tags"]).Should().BeTrue());

        var nameRules = FindRules(members, nameof(ClientRulesModel.Name));
        MSTestAssertions.That(() => nameRules.Should().HaveCount(2));

        var notEmpty = nameRules.Single(static rule => rule["rule"]!.GetValue<string>() == "NotEmpty");
        MSTestAssertions.That(() => notEmpty["messageKey"]!.GetValue<string>().Should().Be("NotEmpty"));
        MSTestAssertions.That(() => notEmpty["severity"]!.GetValue<string>().Should().Be("Error"));
        MSTestAssertions.That(() => notEmpty["hasCondition"]!.GetValue<bool>().Should().BeFalse());

        var length = nameRules.Single(static rule => rule["rule"]!.GetValue<string>() == "Length");
        MSTestAssertions.That(() => length["params"]!["Min"]!.GetValue<int>().Should().Be(2));
        MSTestAssertions.That(() => length["params"]!["Max"]!.GetValue<int>().Should().Be(50));
        MSTestAssertions.That(() => length["messageIsAuthored"]!.GetValue<bool>().Should().BeTrue());

        var ageRules = FindRules(members, nameof(ClientRulesModel.Age));
        var ageRange = ageRules.Single(static rule => rule["rule"]!.GetValue<string>() == "InclusiveBetween");
        MSTestAssertions.That(() => ageRange["params"]!["Min"]!.GetValue<int>().Should().Be(18));
        MSTestAssertions.That(() => ageRange["params"]!["Max"]!.GetValue<int>().Should().Be(120));

        var ageMultiple = ageRules.Single(static rule => rule["rule"]!.GetValue<string>() == "MultipleOf");
        MSTestAssertions.That(() => ageMultiple["serverOnly"].Should().BeNull());
        MSTestAssertions.That(() => ageMultiple["params"]!["Divisor"]!.GetValue<decimal>().Should().Be(2m));

        var tagRules = FindRules(members, nameof(ClientRulesModel.Tags));
        var tagCount = tagRules.Single(static rule => rule["rule"]!.GetValue<string>() == "Count");
        MSTestAssertions.That(() => tagCount["params"]!["Min"]!.GetValue<int>().Should().Be(1));
        MSTestAssertions.That(() => tagCount["params"]!["Max"]!.GetValue<int>().Should().Be(5));

        var tagUnique = tagRules.Single(static rule => rule["rule"]!.GetValue<string>() == "UniqueItems");
        MSTestAssertions.That(() => tagUnique["serverOnly"].Should().BeNull());
        MSTestAssertions.That(() => tagUnique["messageKey"]!.GetValue<string>().Should().Be("UniqueItems"));
    }

    [TestMethod]
    public void Writer_AppliesAllowListAndMessageResolver() {
        var options = new ClientRulesOptions {
            MessageResolver = static rule => rule.Kind == "NotEmpty" ? "Required" : null
        };
        options.IncludedValidators.Add(typeof(ClientRulesValidator).FullName!);

        var document = JsonNode.Parse(ClientRulesWriter.WriteToString(
            [new ClientRulesValidator().Describe(), new OtherClientRulesValidator().Describe()],
            options))!;

        var validators = document["validators"]!.AsArray();
        MSTestAssertions.That(() => validators.Should().HaveCount(1));
        MSTestAssertions.That(() => validators[0]!["name"]!.GetValue<string>().Should().Be(typeof(ClientRulesValidator).FullName));

        var members = validators[0]!["members"]!.AsArray();
        var notEmpty = FindRules(members, nameof(ClientRulesModel.Name))
            .Single(static rule => rule["rule"]!.GetValue<string>() == "NotEmpty");
        var length = FindRules(members, nameof(ClientRulesModel.Name))
            .Single(static rule => rule["rule"]!.GetValue<string>() == "Length");

        MSTestAssertions.That(() => notEmpty["message"]!.GetValue<string>().Should().Be("Required"));
        MSTestAssertions.That(() => length["message"].Should().BeNull());
    }

    [TestMethod]
    public void Writer_EmitsTypeScriptAndJavaScriptModules() {
        var descriptor = new ClientRulesValidator().Describe();

        var typescript = ClientRulesWriter.WriteTypeScript([descriptor]);
        var javascript = ClientRulesWriter.WriteJavaScript([descriptor]);

        MSTestAssertions.That(() => typescript.Should().Contain("export interface ClientRule"));
        MSTestAssertions.That(() => typescript.Should().Contain("export const clientRules"));
        MSTestAssertions.That(() => typescript.Should().Contain("\"schemaVersion\": \"1.0\""));

        MSTestAssertions.That(() => javascript.Should().Contain("export const clientRules"));
        MSTestAssertions.That(() => javascript.Should().NotContain("interface"));
        MSTestAssertions.That(() => javascript.Should().Contain("\"schemaVersion\": \"1.0\""));
    }

    [TestMethod]
    public void Writer_MarksUnsupportedAndConditionalRulesAsServerOnly() {
        var descriptor = new ValidationDescriptor(
            "ServerOnly.Validator",
            [
                new ValidationMemberDescriptor(
                    "Value",
                    [
                        new ValidationRuleDescriptor("NotEmpty"),
                        new ValidationRuleDescriptor("Must"),
                        new ValidationRuleDescriptor("NotEmpty", hasCondition: true)
                    ],
                    typeof(string))
            ],
            typeof(string));

        var document = JsonNode.Parse(ClientRulesWriter.WriteToString([descriptor]))!;
        var rules = document["validators"]![0]!["members"]![0]!["rules"]!.AsArray();

        MSTestAssertions.That(() => rules[0]!["serverOnly"].Should().BeNull());
        MSTestAssertions.That(() => rules[1]!["serverOnly"]!.GetValue<bool>().Should().BeTrue());
        MSTestAssertions.That(() => rules[2]!["serverOnly"]!.GetValue<bool>().Should().BeTrue());
    }

    [TestMethod]
    public void Writer_MatchesGoldenClientRulesSnapshot() {
        var descriptor = new ValidationDescriptor(
            "Golden.Validator",
            [
                new ValidationMemberDescriptor(
                    "Name",
                    [
                        new ValidationRuleDescriptor("NotEmpty", "NotEmpty"),
                        new ValidationRuleDescriptor(
                            "Length",
                            "Length",
                            parameters: new Dictionary<string, object?> {
                                ["Min"] = 2,
                                ["Max"] = 10
                            })
                    ],
                    typeof(string))
            ],
            typeof(string));

        const string expected = """
            {
              "schemaVersion": "1.0",
              "validators": [
                {
                  "name": "Golden.Validator",
                  "type": "System.String",
                  "members": [
                    {
                      "name": "Name",
                      "rules": [
                        {
                          "rule": "NotEmpty",
                          "messageKey": "NotEmpty",
                          "severity": "Error",
                          "hasCondition": false,
                          "params": {}
                        },
                        {
                          "rule": "Length",
                          "messageKey": "Length",
                          "severity": "Error",
                          "hasCondition": false,
                          "params": {
                            "Max": 10,
                            "Min": 2
                          }
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        var actual = ClientRulesWriter.WriteToString([descriptor]);

        MSTestAssertions.That(() => JsonNode.DeepEquals(
            JsonNode.Parse(actual),
            JsonNode.Parse(expected)).Should().BeTrue(
            "the client-rules payload changed; update the golden snapshot deliberately"));
    }

    [TestMethod]
    public void Generator_EmitsClientRulesCatalogWhenEnabled() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class Customer {
                [NotEmpty]
                [Length(2, 50)]
                public string Name { get; init; } = string.Empty;

                [Email]
                public string Email { get; init; } = string.Empty;
            }
            """;

        var (sources, compilation) = RunGenerator(source, emitClientRules: true);

        MSTestAssertions.That(() => sources.Any(static text => text.Contains("ClientRulesCatalog", StringComparison.Ordinal))
            .Should().BeTrue("the opt-in property must emit a client-rules catalog"));

        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream, cancellationToken: TestCancellation);
        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        var assembly = Assembly.Load(stream.ToArray());
        var catalog = assembly.GetType("Jeninnet.Validation.Generated.ClientRulesCatalog")!;
        var json = (string)catalog.GetField("Json")!.GetValue(null)!;

        var validatorType = assembly.GetTypes().Single(static type => type.Name == "CustomerValidator");
        var provider = (IValidationDescriptorProvider)Activator.CreateInstance(validatorType)!;
        var expected = ClientRulesWriter.WriteToString([provider.Describe()]);

        MSTestAssertions.That(() => JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(expected))
            .Should().BeTrue("the build-time catalog must match the descriptor-driven writer"));
    }

    [TestMethod]
    public void Generator_ClientCatalogMarksConditionalRulesServerOnly() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class ConditionalCatalogModel {
                [NotEmpty, WhenCondition(nameof(IsRequired))]
                public string Name { get; init; } = string.Empty;

                public bool IsRequired(ConditionalCatalogModel instance) => true;
            }
            """;

        var (_, compilation) = RunGenerator(source, emitClientRules: true);

        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream, cancellationToken: TestCancellation);
        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        var assembly = Assembly.Load(stream.ToArray());
        var catalog = assembly.GetType("Jeninnet.Validation.Generated.ClientRulesCatalog")!;
        var json = (string)catalog.GetField("Json")!.GetValue(null)!;

        var validatorType = assembly.GetTypes().Single(static type => type.Name == "ConditionalCatalogModelValidator");
        var provider = (IValidationDescriptorProvider)Activator.CreateInstance(validatorType)!;
        var expected = ClientRulesWriter.WriteToString([provider.Describe()]);

        MSTestAssertions.That(() => JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(expected))
            .Should().BeTrue("the catalog must mark conditional rules server-only like the runtime writer"));
    }

    [TestMethod]
    public void Generator_ClientCatalogMatchesTypedDescriptorParameters() {
        MSTestAssertions.That(() => typeof(PasswordPolicy).Assembly.GetName().Name
            .Should().Be("Jeninnet.Validation.Rules.Domain"));

        const string source = """
            using System;
            using Jeninnet.Validation.Attributes;
            using Jeninnet.Validation.Attributes.Common;
            namespace MyApp;

            public enum CatalogKind {
                None,
                Basic
            }

            [Validate]
            public sealed class CatalogModel {
                [NotEmpty]
                [MaximumLength(10)]
                public string Name { get; init; } = string.Empty;

                [FileExtensions("pdf", "txt")]
                public string Attachment { get; init; } = string.Empty;

                [In(new object[] { 1, 2, 3 })]
                public int Choice { get; init; }

                [ScalePrecision(5, 2)]
                public decimal Amount { get; init; }

                [PropertyEqual(nameof(Password))]
                public string PasswordConfirmation { get; init; } = string.Empty;

                public string Password { get; init; } = string.Empty;

                [Jeninnet.Validation.Attributes.Common.Enum]
                public CatalogKind Kind { get; init; }

                [After("2025-01-01")]
                public DateTime? Created { get; init; }

                [Iban("NL")]
                public string Iban { get; init; } = string.Empty;

                [VatNumber("NL")]
                public string Vat { get; init; } = string.Empty;

                [PostalCode("NL")]
                public string Postal { get; init; } = string.Empty;

                [NationalId("NL")]
                public string Bsn { get; init; } = string.Empty;

                [PasswordStrength(PasswordStrengthPreset.Strict)]
                public string Secret { get; init; } = string.Empty;

                [Uuid]
                public string RequestId { get; init; } = string.Empty;

                [Ulid]
                public string EventId { get; init; } = string.Empty;
            }
            """;

        var (_, compilation) = RunGenerator(source, emitClientRules: true);

        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream, cancellationToken: TestCancellation);
        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        var assembly = Assembly.Load(stream.ToArray());
        var catalog = assembly.GetType("Jeninnet.Validation.Generated.ClientRulesCatalog")!;
        var json = (string)catalog.GetField("Json")!.GetValue(null)!;

        var validatorType = assembly.GetTypes().Single(static type => type.Name == "CatalogModelValidator");
        var provider = (IValidationDescriptorProvider)Activator.CreateInstance(validatorType)!;
        var expected = ClientRulesWriter.WriteToString([provider.Describe()]);

        MSTestAssertions.That(() => JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(expected))
            .Should().BeTrue("the build-time catalog must match typed descriptor parameters"));
    }

    [TestMethod]
    public void Generator_DoesNotEmitCatalogByDefault() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class Customer {
                [NotEmpty]
                public string Name { get; init; } = string.Empty;
            }
            """;

        var (sources, _) = RunGenerator(source, emitClientRules: false);

        MSTestAssertions.That(() => sources.Any(static text => text.Contains("ClientRulesCatalog", StringComparison.Ordinal))
            .Should().BeFalse("the catalog is opt-in"));
    }

    private static JsonNode[] FindRules(JsonArray members, string memberName) =>
        [.. members.Single(member => member!["name"]!.GetValue<string>() == memberName)!["rules"]!
                   .AsArray()
                   .Select(static rule => rule!)];

    private CancellationToken TestCancellation => TestContext?.CancellationToken ?? CancellationToken.None;

    public TestContext? TestContext { get; set; }

    private static (string[] Sources, Compilation Compilation) RunGenerator(string source, bool emitClientRules) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain
                                  .GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            assemblyName: "ClientRulesTestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                         .WithNullableContextOptions(NullableContextOptions.Enable));

        var optionsProvider = new TestAnalyzerConfigOptionsProvider(
            emitClientRules ? "true" : "false");

        var driver = CSharpGeneratorDriver.Create(
            [new ValidatorGenerator().AsSourceGenerator()],
            optionsProvider: optionsProvider);

        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees;

        return (
            generatedTrees.Select(static tree => tree.ToString()).ToArray(),
            compilation.AddSyntaxTrees(generatedTrees));
    }

    private sealed class TestAnalyzerConfigOptionsProvider(string emitClientRules) : AnalyzerConfigOptionsProvider {
        private readonly TestAnalyzerConfigOptions _options = new(emitClientRules);

        public override AnalyzerConfigOptions GlobalOptions => _options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;
    }

    private sealed class TestAnalyzerConfigOptions(string emitClientRules) : AnalyzerConfigOptions {
        public override bool TryGetValue(string key, out string value) {
            if(string.Equals(key, "build_property.JeninnetEmitClientRules", StringComparison.Ordinal)) {
                value = emitClientRules;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
