//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Verifies that generated validators describe attribute rules, runtime custom rules, and
/// explicitly configured descriptor metadata.
/// </summary>
[TestClass]
public sealed class GeneratedDescriptorCompletenessTests {
    private const string SOURCE = """
        using System.Collections.Generic;
        using Jeninnet.Validation;
        using Jeninnet.Validation.Attributes;
        using Jeninnet.Validation.Core;
        using Jeninnet.Validation.Metadata;
        using Jeninnet.Validation.Rules;

        namespace DescriptorCompleteness {
            [Validate]
            public sealed class Customer {
                [NotEmpty(
                    ErrorCode = "name_required",
                    Severity = ValidationErrorSeverity.Warning,
                    DisplayName = "Name display",
                    CustomState = "name-state")]
                [Length(2, 50)]
                public string? Name { get; set; }
            }

            public sealed class RawRule<T> : ISyncValidationRule<T> {
                public void Validate(in ValidationContext<T> context, ValidationErrorCollector errors) {
                }
            }
        }

        namespace DescriptorCompleteness.Validators {
            public sealed partial class CustomerValidator {
                partial void AddCustomValidators() {
                    _syncRules.Add(new MustRule<DescriptorCompleteness.Customer, string?>(
                        ValidatorFactory.Accessor<DescriptorCompleteness.Customer, string?>(
                            static customer => customer.Name,
                            nameof(DescriptorCompleteness.Customer.Name)),
                        static name => name != "admin",
                        "reserved"));

                    _syncRules.Add(new DescriptorCompleteness.RawRule<DescriptorCompleteness.Customer>());
                }

                partial void ConfigureDescriptor(ValidationDescriptorBuilder builder) {
                    builder.Member(nameof(DescriptorCompleteness.Customer.Name)).Rule(
                        "ReservedName",
                        messageKey: "ReservedName",
                        severity: ValidationErrorSeverity.Info,
                        parameters: new Dictionary<string, object?> { ["Value"] = "admin" },
                        errorCode: "reserved_name",
                        displayName: "Reserved name",
                        customState: "reserved-state");
                }
            }
        }
        """;

    [TestMethod]
    public void GeneratedDescriptor_DescribesCustomAndAdvisoryRules() {
        var descriptor = Describe();

        var nameRules = descriptor.RulesForMember("Name");
        MSTestAssertions.That(() => nameRules.Select(static rule => rule.Kind)
            .SequenceEqual(["NotEmpty", "Length", "Must", "ReservedName"]).Should().BeTrue());

        var notEmpty = nameRules[0];
        MSTestAssertions.That(() => notEmpty.ErrorCode.Should().Be("name_required"));
        MSTestAssertions.That(() => notEmpty.Severity.Should().Be(ValidationErrorSeverity.Warning));
        MSTestAssertions.That(() => notEmpty.DisplayName.Should().Be("Name display"));
        MSTestAssertions.That(() => notEmpty.CustomState.Should().Be("name-state"));

        var length = nameRules[1];
        MSTestAssertions.That(() => length.Parameters["Min"].Should().Be(2));
        MSTestAssertions.That(() => length.Parameters["Max"].Should().Be(50));

        var configured = nameRules[3];
        MSTestAssertions.That(() => configured.MessageKey.Should().Be("ReservedName"));
        MSTestAssertions.That(() => configured.Severity.Should().Be(ValidationErrorSeverity.Info));
        MSTestAssertions.That(() => configured.ErrorCode.Should().Be("reserved_name"));
        MSTestAssertions.That(() => configured.DisplayName.Should().Be("Reserved name"));
        MSTestAssertions.That(() => configured.CustomState.Should().Be("reserved-state"));
        MSTestAssertions.That(() => configured.Parameters["Value"].Should().Be("admin"));

        var advisory = descriptor.RulesForMember("*").Single();
        MSTestAssertions.That(() => advisory.Kind.Should().Be("Custom"));
        MSTestAssertions.That(() => advisory.Parameters["Reason"].Should().Be("Rule has no descriptor metadata."));
        MSTestAssertions.That(() => advisory.Parameters["Type"].Should().Be("RawRule`1"));
    }

    [TestMethod]
    public void GeneratedClientRules_ExposeCustomAndExplicitMetadata() {
        var descriptor = Describe();

        var document = JsonNode.Parse(ClientRulesWriter.WriteToString([descriptor]))!;
        var validator = document["validators"]!.AsArray().Single()!;
        var name = validator["members"]!.AsArray()
            .Single(static member => member!["name"]!.GetValue<string>() == "Name")!;
        var rules = name["rules"]!.AsArray();

        var configured = rules.Single(static rule => rule!["rule"]!.GetValue<string>() == "ReservedName")!;
        MSTestAssertions.That(() => configured["severity"]!.GetValue<string>().Should().Be("Info"));
        MSTestAssertions.That(() => configured["errorCode"]!.GetValue<string>().Should().Be("reserved_name"));
        MSTestAssertions.That(() => configured["displayName"]!.GetValue<string>().Should().Be("Reserved name"));
        MSTestAssertions.That(() => configured["customState"]!.GetValue<string>().Should().Be("reserved-state"));
        MSTestAssertions.That(() => configured["params"]!["Value"]!.GetValue<string>().Should().Be("admin"));
    }

    [TestMethod]
    public void GeneratedSchema_ExposesCustomMetadataAsAdvisoryRules() {
        var descriptor = Describe();

        var schema = ValidationSchemaWriter.Write(
            descriptor,
            new ValidationSchemaOptions { IncludeAdvisoryRules = true });
        var rules = schema["properties"]!["Name"]!["x-validation-rules"]!.AsArray();

        var configured = rules.Single(static rule => rule!["kind"]!.GetValue<string>() == "ReservedName")!;
        MSTestAssertions.That(() => configured["severity"]!.GetValue<string>().Should().Be("Info"));
        MSTestAssertions.That(() => configured["errorCode"]!.GetValue<string>().Should().Be("reserved_name"));
        MSTestAssertions.That(() => configured["displayName"]!.GetValue<string>().Should().Be("Reserved name"));
        MSTestAssertions.That(() => configured["customState"]!.GetValue<string>().Should().Be("reserved-state"));
        MSTestAssertions.That(() => configured["parameters"]!["Value"]!.GetValue<string>().Should().Be("admin"));
    }

    [TestMethod]
    public void GeneratedDescriptor_IsDeterministicAcrossInstances() {
        var first = Describe();
        var second = Describe();

        var firstRules = first.Members
            .SelectMany(static member => member.Rules.Select(rule => $"{member.Name}:{rule.Kind}"))
            .ToArray();
        var secondRules = second.Members
            .SelectMany(static member => member.Rules.Select(rule => $"{member.Name}:{rule.Kind}"))
            .ToArray();

        MSTestAssertions.That(() => firstRules.SequenceEqual(secondRules).Should().BeTrue());
    }

    private IValidationDescriptor Describe() {
        var assembly = BuildGeneratedAssembly();
        var validatorType = assembly.GetType(
            "DescriptorCompleteness.Validators.CustomerValidator",
            throwOnError: true)!;
        var validator = (IValidationDescriptorProvider)Activator.CreateInstance(validatorType)!;
        return validator.Describe();
    }

    private Assembly BuildGeneratedAssembly() {
        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        var syntaxTree = CSharpSyntaxTree.ParseText(SOURCE, cancellationToken: cancellationToken);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => assembly.Location)
                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                  .Select(static location => MetadataReference.CreateFromFile(location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            $"GeneratedDescriptorCompletenessAssembly_{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, cancellationToken);
        var runResult = driver.GetRunResult();

        MSTestAssertions.That(() => (runResult.GeneratedTrees.Length > 0).Should().BeTrue(
            string.Join(Environment.NewLine, runResult.Diagnostics)));

        var generatedCompilation = compilation.AddSyntaxTrees(runResult.GeneratedTrees);

        using var stream = new MemoryStream();
        var emit = generatedCompilation.Emit(stream, cancellationToken: cancellationToken);

        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        return Assembly.Load(stream.ToArray());
    }

    public TestContext? TestContext { get; set; }
}
