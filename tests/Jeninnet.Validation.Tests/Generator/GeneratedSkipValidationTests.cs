//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Covers [SkipValidation] in the source generator: property exclusion, type exclusion, nested
/// suppression, and the VAL0028 conflict warning.
/// </summary>
[TestClass]
public sealed class GeneratedSkipValidationTests {
    private const string SOURCE = """
        using Jeninnet.Validation.Attributes;

        namespace SkipModels;

        [Validate("SkipModels")]
        public sealed class SkipModel {
            [SkipValidation, NotEmpty]
            public string? Ignored { get; set; }

            [NotEmpty]
            public string? Name { get; set; }

            [SkipValidation]
            public string? AlsoIgnored { get; set; }

            [Nested]
            public SkippedChild? Child { get; set; }
        }

        [Validate("SkipModels"), SkipValidation]
        public sealed class SkippedChild {
            [NotEmpty]
            public string? Value { get; set; }
        }
        """;

    [TestMethod]
    public void Generator_SkipsPropertyAndSkippedNestedType() {
        var run = RunGenerator(SOURCE, "SkipValidationTestAssembly");
        var generatedText = string.Join(
            Environment.NewLine,
            run.GeneratedTrees.Select(static tree => tree.ToString()));

        MSTestAssertions.That(() => generatedText.Should().NotContain("SkippedChildValidator"));
        MSTestAssertions.That(() => generatedText.Should().NotContain("Ignored"));
        MSTestAssertions.That(() => generatedText.Should().NotContain("AlsoIgnored"));
        MSTestAssertions.That(() => generatedText.Should().NotContain("Child"));
        MSTestAssertions.That(() => run.Diagnostics.Any(static diagnostic => diagnostic.Id == "VAL0028").Should().BeTrue());
        MSTestAssertions.That(() => run.Diagnostics
            .Where(static diagnostic => diagnostic.Id == "VAL0028")
            .Should().HaveCount(1));
    }

    [TestMethod]
    public void GeneratedValidator_IgnoresSkippedMembersAtRuntime() {
        var assembly = CompileGeneratedAssembly(SOURCE, "SkipValidationRuntimeTestAssembly");
        var modelType = assembly.GetType("SkipModels.SkipModel", throwOnError: true)!;
        var childType = assembly.GetType("SkipModels.SkippedChild", throwOnError: true)!;
        var validatorType = assembly.GetType("SkipModels.SkipModelValidator", throwOnError: true)!;

        var model = Activator.CreateInstance(modelType)!;
        var child = Activator.CreateInstance(childType)!;
        childType.GetProperty("Value")!.SetValue(child, string.Empty);
        modelType.GetProperty("Ignored")!.SetValue(model, string.Empty);
        modelType.GetProperty("Name")!.SetValue(model, string.Empty);
        modelType.GetProperty("AlsoIgnored")!.SetValue(model, string.Empty);
        modelType.GetProperty("Child")!.SetValue(model, child);

        var validator = Activator.CreateInstance(validatorType)!;
        var result = (ValidationResult)validatorType
            .GetMethod("Validate", [modelType])!
            .Invoke(validator, [model])!;

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("Name"));
    }

    private static GeneratorRun RunGenerator(string source, string assemblyName) {
        var compilation = CreateCompilation(source, assemblyName);
        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var result = driver.GetRunResult();

        return new GeneratorRun(
            result.GeneratedTrees,
            result.Diagnostics);
    }

    private static Assembly CompileGeneratedAssembly(string source, string assemblyName) {
        var compilation = CreateCompilation(source, assemblyName);
        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var result = driver.GetRunResult();
        var errors = result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();

        MSTestAssertions.That(() => errors.Should().BeEmpty(
            string.Join(Environment.NewLine, errors)));

        using var stream = new MemoryStream();
        var emit = compilation.AddSyntaxTrees(result.GeneratedTrees).Emit(stream);
        var problems = emit.Diagnostics.Where(static diagnostic =>
            diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error).ToList();

        MSTestAssertions.That(() => problems.Should().BeEmpty(
            string.Join(Environment.NewLine, problems)));

        return Assembly.Load(stream.ToArray());
    }

    private static CSharpCompilation CreateCompilation(string source, string assemblyName) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        return CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
    }

    private sealed record GeneratorRun(
        IEnumerable<SyntaxTree> GeneratedTrees,
        IEnumerable<Diagnostic> Diagnostics);
}
