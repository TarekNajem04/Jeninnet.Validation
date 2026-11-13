//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Verifies rule-set selection and metadata for attribute-generated validators.
/// </summary>
[TestClass]
public sealed class GeneratedRuleSetTests {
    private const string SOURCE = """
        using Jeninnet.Validation.Attributes;
        using Jeninnet.Validation.Attributes.Common;

        namespace AttributeRuleSets;

        [Validate("AttributeRuleSets")]
        public sealed class RuleSetModel {
            [NotEmpty, RuleSet("Strict")]
            public string? Name { get; set; }

            [NotEmpty(RuleSet = "Admin")]
            public string? Code { get; set; }

            [NotEmpty]
            public string? Always { get; set; }

            [ScalePrecision(5, 2, IgnoreTrailingZeros = true)]
            public decimal Amount { get; set; }
        }
        """;

    [TestMethod]
    public void GeneratedRuleSets_DefaultAndNamedSelection() {
        var generated = BuildValidator();
        var model = generated.CreateModel();

        var all = generated.Validate(model);
        var strict = generated.Validate(model, new ValidationCallOptions { RuleSets = ["Strict"] });
        var admin = generated.Validate(model, new ValidationCallOptions { RuleSets = ["Admin"] });

        ValidationAssert.ErrorCount(all, 3);
        ValidationAssert.ErrorCount(strict, 1);
        ValidationAssert.HasError(strict, "Name");
        ValidationAssert.ErrorCount(admin, 1);
        ValidationAssert.HasError(admin, "Code");
    }

    [TestMethod]
    public void GeneratedIncludeAllRuleSets_RunsAllGroups() {
        var generated = BuildValidator();
        var model = generated.CreateModel();

        var result = generated.Validate(model, new ValidationCallOptions {
            RuleSets = ["Strict"],
            IncludeAllRuleSets = true
        });

        ValidationAssert.ErrorCount(result, 3);
    }

    [TestMethod]
    public void GeneratedIncludeRulesNotInRuleSet_RunsOnlyUngrouped() {
        var generated = BuildValidator();
        var model = generated.CreateModel();

        var result = generated.Validate(model, new ValidationCallOptions {
            RuleSets = ["default"]
        });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Always");
    }

    [TestMethod]
    public void GeneratedScalePrecision_HonorsIgnoreTrailingZeros() {
        var generated = BuildValidator();
        var model = generated.CreateModel();
        generated.Set(model, "Amount", 1.2300m);

        var trailingZeros = generated.Validate(model);
        ValidationAssert.NoErrorFor(trailingZeros, "Amount");

        generated.Set(model, "Amount", 123.456m);
        var tooPrecise = generated.Validate(model);
        ValidationAssert.HasError(tooPrecise, "Amount");
    }

    [TestMethod]
    public void GeneratedThrowOnFailures_ThrowsWithAllErrors() {
        var generated = BuildValidator();
        var model = generated.CreateModel();

        var act = () => generated.Validate(model, new ValidationCallOptions {
            ThrowOnFailures = true
        });

        var exception = act.Should().Throw<TargetInvocationException>().Which;
        var validationException = exception.InnerException as ValidationException;

        MSTestAssertions.That(() => validationException.Should().NotBeNull());
        MSTestAssertions.That(() => validationException!.Errors.Should().HaveCount(3));
    }

    [TestMethod]
    public void GeneratedDescriptorAndClientRules_ExposeRuleSets() {
        var generated = BuildValidator();
        var descriptor = generated.Describe();

        var nameRule = descriptor.RulesForMember("Name").Single();
        var codeRule = descriptor.RulesForMember("Code").Single();
        var alwaysRule = descriptor.RulesForMember("Always").Single();

        MSTestAssertions.That(() => nameRule.Kind.Should().Be("NotEmpty"));
        MSTestAssertions.That(() => nameRule.Parameters["RuleSets"].Should().Be("Strict"));
        MSTestAssertions.That(() => codeRule.Parameters["RuleSets"].Should().Be("Admin"));
        MSTestAssertions.That(() => alwaysRule.Parameters.ContainsKey("RuleSets").Should().BeFalse());

        var document = JsonNode.Parse(ClientRulesWriter.WriteToString([descriptor]))!;
        var members = document["validators"]![0]!["members"]!.AsArray();
        var nameClientRule = members
            .Single(static member => member!["name"]!.GetValue<string>() == "Name")!["rules"]!
            .AsArray()
            .Single(static rule => rule!["rule"]!.GetValue<string>() == "NotEmpty")!;

        MSTestAssertions.That(() => nameClientRule["params"]!["RuleSets"]!.GetValue<string>().Should().Be("Strict"));
    }

    private static GeneratedRuleSetValidator BuildValidator() {
        var sourceTree = CSharpSyntaxTree.ParseText(SOURCE);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            $"GeneratedRuleSetAssembly_{Guid.NewGuid():N}",
            [sourceTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();
        var errors = runResult.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();

        MSTestAssertions.That(() => errors.Should().BeEmpty(
            string.Join(Environment.NewLine, errors)));

        using var stream = new MemoryStream();
        var emit = compilation.AddSyntaxTrees(runResult.GeneratedTrees).Emit(stream);
        var problems = emit.Diagnostics.Where(static diagnostic =>
            diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error).ToList();

        MSTestAssertions.That(() => problems.Should().BeEmpty(
            string.Join(Environment.NewLine, problems)));

        var assembly = Assembly.Load(stream.ToArray());
        var modelType = assembly.GetType("AttributeRuleSets.RuleSetModel", throwOnError: true)!;
        var validatorType = assembly.GetType("AttributeRuleSets.RuleSetModelValidator", throwOnError: true)!;

        return new GeneratedRuleSetValidator(
            modelType,
            validatorType,
            Activator.CreateInstance(validatorType)!);
    }

    private sealed class GeneratedRuleSetValidator(Type modelType, Type validatorType, object validator) {
        public object CreateModel() => Activator.CreateInstance(modelType)!;

        public void Set(object model, string propertyName, object? value) =>
            modelType.GetProperty(propertyName)!.SetValue(model, value);

        public ValidationResult Validate(object model) =>
            (ValidationResult)validatorType.GetMethod("Validate", [modelType])!.Invoke(validator, [model])!;

        public ValidationResult Validate(object model, ValidationCallOptions options) =>
            (ValidationResult)validatorType
                .GetMethod("Validate", [modelType, typeof(ValidationCallOptions)])!
                .Invoke(validator, [model, options])!;

        public IValidationDescriptor Describe() =>
            ((IValidationDescriptorProvider)validator).Describe();
    }
}
