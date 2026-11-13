//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Verifies that generated validators honor <c>[Sanitize]</c>: values are cleaned before rules
/// run, the model is mutated in place, and sanitization never appears in descriptors.
/// </summary>
[TestClass]
// Generator tests share process-wide metadata fixtures.
[DoNotParallelize]
public sealed class GeneratedSanitizeTests {
    private const string SOURCE = """
        using System.Collections.Generic;
        using Jeninnet.Validation.Attributes;

        namespace GeneratedSanitize;

        [Validate("GeneratedSanitize")]
        public sealed class SanitizeGenerationModel {
            [Sanitize(Trim = true)]
            [NotEmpty]
            public string? Name { get; set; }

            [Sanitize(ToUpper = true, Culture = "tr-TR")]
            [NotEmpty]
            public string? Code { get; set; }

            [Sanitize(Trim = true)]
            public IList<string?>? Tags { get; set; }

            [NotEmpty]
            public string? Plain { get; set; }
        }
        """;

    [TestMethod]
    public void GeneratedSanitize_CleansValuesBeforeRulesRun() {
        var generated = BuildGenerated();
        var model = generated.CreateModel("  Ada  ", "i", ["  a  ", null], "  Plain  ");

        var result = generated.Validate(model);

        MSTestAssertions.That(() => result.IsValid.Should().BeTrue());
        MSTestAssertions.That(() => generated.GetProperty(model, "Name").Should().Be("Ada"));
        MSTestAssertions.That(() => generated.GetProperty(model, "Code").Should().Be("İ"));
        MSTestAssertions.That(() => generated.GetProperty(model, "Plain").Should().Be("  Plain  "));

        var tags = (IList<string?>)generated.GetProperty(model, "Tags")!;

        MSTestAssertions.That(() => tags[0].Should().Be("a"));
        MSTestAssertions.That(() => tags[1].Should().BeNull());
    }

    [TestMethod]
    public void GeneratedSanitize_StillFailsUnfixableValues() {
        var generated = BuildGenerated();
        var model = generated.CreateModel("   ", "i", null, "ok");

        var result = generated.Validate(model);

        ValidationAssert.HasError(result, "Name");
        MSTestAssertions.That(() => generated.GetProperty(model, "Name").Should().Be(string.Empty));
    }

    [TestMethod]
    public void GeneratedSanitize_DoesNotAppearInDescriptors() {
        var generated = BuildGenerated();
        var descriptor = generated.Describe();
        var names = descriptor.RulesForMember("Name").Single();

        MSTestAssertions.That(() => names.Kind.Should().Be("NotEmpty"));

        var kinds = descriptor.Members
            .SelectMany(static member => member.Rules)
            .Select(static rule => rule.Kind)
            .ToList();

        MSTestAssertions.That(() => kinds.Should().NotContain("Sanitize"));
    }

    private GeneratedSanitizeValidator BuildGenerated() {
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
            $"GeneratedSanitizeAssembly_{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, cancellationToken);
        var runResult = driver.GetRunResult();

        MSTestAssertions.That(() => runResult.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Should().BeEmpty(
            string.Join(Environment.NewLine, runResult.Diagnostics)));

        MSTestAssertions.That(() => (runResult.GeneratedTrees.Length > 0).Should().BeTrue(
            string.Join(Environment.NewLine, runResult.Diagnostics)));

        var generatedCompilation = compilation.AddSyntaxTrees(runResult.GeneratedTrees);

        using var stream = new MemoryStream();
        var emit = generatedCompilation.Emit(stream, cancellationToken: cancellationToken);

        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        var assembly = Assembly.Load(stream.ToArray());
        var modelType = assembly.GetType("GeneratedSanitize.SanitizeGenerationModel", throwOnError: true)!;
        var validatorType = assembly.GetType("GeneratedSanitize.SanitizeGenerationModelValidator", throwOnError: true)!;

        return new GeneratedSanitizeValidator(modelType, validatorType, Activator.CreateInstance(validatorType)!);
    }

    private sealed class GeneratedSanitizeValidator(Type modelType, Type validatorType, object validator) {
        public object CreateModel(string? name, string? code, IList<string?>? tags, string? plain) {
            var model = Activator.CreateInstance(modelType)!;

            modelType.GetProperty("Name")!.SetValue(model, name);
            modelType.GetProperty("Code")!.SetValue(model, code);
            modelType.GetProperty("Tags")!.SetValue(model, tags);
            modelType.GetProperty("Plain")!.SetValue(model, plain);
            return model;
        }

        public object? GetProperty(object model, string name) =>
            modelType.GetProperty(name)!.GetValue(model);

        public ValidationResult Validate(object model) =>
            (ValidationResult)validatorType.GetMethod("Validate", [modelType])!.Invoke(validator, [model])!;

        public IValidationDescriptor Describe() =>
            (IValidationDescriptor)validatorType.GetMethod("Describe")!.Invoke(validator, null)!;
    }

    public TestContext? TestContext { get; set; }
}
