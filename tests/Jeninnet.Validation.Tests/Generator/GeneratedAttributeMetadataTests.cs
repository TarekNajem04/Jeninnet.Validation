//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Verifies that declarative rule metadata is emitted into the runtime rule, descriptor, client
/// payload, and JSON Schema advisory rules.
/// </summary>
[TestClass]
// Generator tests share process-wide metadata fixtures.
[DoNotParallelize]
public sealed class GeneratedAttributeMetadataTests {
    private const string SOURCE = """
        using System;
        using System.ComponentModel.DataAnnotations;
        using Jeninnet.Validation;
        using Jeninnet.Validation.Attributes;

        namespace AttributeMetadata;

        [Validate("AttributeMetadata")]
        public sealed class MetadataModel {
            [Email(
                ErrorCode = "email_invalid",
                Severity = ValidationErrorSeverity.Warning,
                DisplayName = "Email display",
                CustomState = "email-state")]
            public string? Email { get; set; }

            [Display(Name = "Displayed name")]
            [NotEmpty]
            public string? Name { get; set; }

            [NotEmpty(OverridePropertyName = "renamed")]
            public string? Renamed { get; set; }
        }
        """;

    [TestMethod]
    public void GeneratedRule_CarriesDeclarativeMetadata() {
        var generated = BuildGenerated();
        var model = generated.CreateModel("bad", string.Empty, string.Empty);
        var result = generated.Validate(model);

        ValidationAssert.ErrorCount(result, 3);

        var email = result.Errors.Single(static error => error.MemberName == "Email");
        MSTestAssertions.That(() => email.ErrorCode.Should().Be("email_invalid"));
        MSTestAssertions.That(() => email.Severity.Should().Be(ValidationErrorSeverity.Warning));
        MSTestAssertions.That(() => email.CustomState.Should().Be("email-state"));
        MSTestAssertions.That(() => email.MessageParameters!["PropertyName"].Should().Be("Email display"));

        var name = result.Errors.Single(static error => error.MemberName == "Name");
        MSTestAssertions.That(() => name.MessageParameters!["PropertyName"].Should().Be("Displayed name"));

        ValidationAssert.HasError(result, "renamed");
    }

    [TestMethod]
    public void GlobalResolver_WinsOverDisplayAttribute() {
        var previous = ValidationConfiguration.Options;

        try {
            ValidationConfiguration.Configure(
                static options => options with { DisplayNameResolver = static (_, _) => "Resolver name" });

            var generated = BuildGenerated();
            var result = generated.Validate(generated.CreateModel("bad", string.Empty, string.Empty));
            var name = result.Errors.Single(static error => error.MemberName == "Name");

            MSTestAssertions.That(() => name.MessageParameters!["PropertyName"].Should().Be("Resolver name"));
        }
        finally {
            ValidationConfiguration.Configure(_ => previous);
        }
    }

    [TestMethod]
    public void GeneratedDescriptor_ExposesDeclarativeMetadata() {
        var generated = BuildGenerated();
        var descriptor = (IValidationDescriptor)generated.ValidatorType
            .GetMethod("Describe")!
            .Invoke(generated.Validator, null)!;

        var emailRule = descriptor.RulesForMember("Email").Single();
        MSTestAssertions.That(() => emailRule.ErrorCode.Should().Be("email_invalid"));
        MSTestAssertions.That(() => emailRule.DisplayName.Should().Be("Email display"));
        MSTestAssertions.That(() => emailRule.Severity.Should().Be(ValidationErrorSeverity.Warning));
        MSTestAssertions.That(() => emailRule.CustomState.Should().Be("email-state"));

        var nameRule = descriptor.RulesForMember("Name").Single();
        MSTestAssertions.That(() => nameRule.DisplayName.Should().Be("Displayed name"));
    }

    [TestMethod]
    public void ClientRulesAndSchema_ExposeDeclarativeMetadata() {
        var generated = BuildGenerated();
        var descriptor = (IValidationDescriptor)generated.ValidatorType
            .GetMethod("Describe")!
            .Invoke(generated.Validator, null)!;

        var clientJson = ClientRulesWriter.WriteToString([descriptor]);
        var client = JsonNode.Parse(clientJson)!;
        var emailRule = FindRule(client, "MetadataModelValidator", "Email");
        MSTestAssertions.That(() => emailRule["errorCode"]!.GetValue<string>().Should().Be("email_invalid"));
        MSTestAssertions.That(() => emailRule["displayName"]!.GetValue<string>().Should().Be("Email display"));
        MSTestAssertions.That(() => emailRule["severity"]!.GetValue<string>().Should().Be("Warning"));

        var advisoryDescriptor = new ValidationDescriptor(
            "AdvisoryValidator",
            [
                new ValidationMemberDescriptor(
                    "Email",
                    [
                        new ValidationRuleDescriptor(
                            "Custom",
                            "Custom",
                            ValidationErrorSeverity.Warning,
                            hasCondition: false,
                            parameters: null,
                            errorCode: "email_invalid",
                            displayName: "Email display",
                            customState: "email-state")
                    ])
            ]);
        var schema = ValidationSchemaWriter.Write(
            advisoryDescriptor,
            new ValidationSchemaOptions { IncludeAdvisoryRules = true });
        var emailAdvisory = schema["properties"]!["Email"]!["x-validation-rules"]!.AsArray();
        var emailSchemaRule = emailAdvisory[0]!.AsObject();
        MSTestAssertions.That(() => emailSchemaRule["errorCode"]!.GetValue<string>().Should().Be("email_invalid"));
        MSTestAssertions.That(() => emailSchemaRule["displayName"]!.GetValue<string>().Should().Be("Email display"));
        MSTestAssertions.That(() => emailSchemaRule["severity"]!.GetValue<string>().Should().Be("Warning"));
        MSTestAssertions.That(() => emailSchemaRule["customState"]!.GetValue<string>().Should().Be("email-state"));
    }

    private static JsonObject FindRule(JsonNode root, string validatorSuffix, string memberName) {
        var validator = root["validators"]!.AsArray()
            .Select(static node => node!.AsObject())
            .Single(node => node["name"]!.GetValue<string>().EndsWith(validatorSuffix, StringComparison.Ordinal));
        var member = validator["members"]!.AsArray()
            .Select(static node => node!.AsObject())
            .Single(node => node["name"]!.GetValue<string>() == memberName);
        return member["rules"]!.AsArray()[0]!.AsObject();
    }

    private GeneratedMetadataValidator BuildGenerated() {
        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        var syntaxTree = CSharpSyntaxTree.ParseText(SOURCE, cancellationToken: cancellationToken);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => assembly.Location)
                                  .Append(typeof(DisplayAttribute).Assembly.Location)
                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                  .Select(static location => MetadataReference.CreateFromFile(location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            $"GeneratedAttributeMetadataAssembly_{Guid.NewGuid():N}",
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

        var assembly = Assembly.Load(stream.ToArray());
        var modelType = assembly.GetType("AttributeMetadata.MetadataModel", throwOnError: true)!;
        var validatorType = assembly.GetType("AttributeMetadata.MetadataModelValidator", throwOnError: true)!;
        return new GeneratedMetadataValidator(modelType, validatorType, Activator.CreateInstance(validatorType)!);
    }

    private sealed class GeneratedMetadataValidator(Type modelType, Type validatorType, object validator) {
        public Type ValidatorType { get; } = validatorType;

        public object Validator { get; } = validator;

        public object CreateModel(string email, string name, string renamed) {
            var model = Activator.CreateInstance(modelType)!;
            modelType.GetProperty("Email")!.SetValue(model, email);
            modelType.GetProperty("Name")!.SetValue(model, name);
            modelType.GetProperty("Renamed")!.SetValue(model, renamed);
            return model;
        }

        public ValidationResult Validate(object model) =>
            (ValidationResult)ValidatorType
                .GetMethod("Validate", [modelType])!
                .Invoke(Validator, [model])!;
    }

    public TestContext? TestContext { get; set; }
}
