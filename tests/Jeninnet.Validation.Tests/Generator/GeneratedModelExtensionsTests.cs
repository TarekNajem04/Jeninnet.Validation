//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Covers the model-level <c>Validate</c>/<c>ValidateAsync</c> extensions emitted by the generator.
/// </summary>
[TestClass]
public sealed class GeneratedModelExtensionsTests {
    private const string SOURCE = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using Jeninnet.Validation;
        using Jeninnet.Validation.Attributes;

        namespace ModelExtensionsModels;

        [Validate]
        public sealed class ClassModel {
            [NotEmpty]
            public string? Name { get; set; }

            [Length(2, 5)]
            public string? Code { get; set; }
        }

        [Validate]
        public struct StructModel {
            [NotEmpty]
            public string? Name { get; set; }
        }

        [Validate]
        public readonly record struct RecordStructModel([property: NotEmpty] string? Name);

        [Validate(GenerateModelExtensions = false)]
        public sealed class DisabledModel {
            [NotEmpty]
            public string? Name { get; set; }
        }

        public static class Harness {
            public static ValidationResult RunClass(ClassModel model) => model.Validate();

            public static ValidationResult RunStruct(StructModel model) => model.Validate();

            public static ValidationResult RunRecord(RecordStructModel model) => model.Validate();

            public static ValidationResult RunOptions(ClassModel model) =>
                model.Validate(new ValidationCallOptions { FailFast = true });

            public static ValueTask<ValidationResult> RunAsync(ClassModel model, CancellationToken cancellationToken) =>
                model.ValidateAsync(cancellationToken);

            public static ValueTask<ValidationResult> RunAsyncOptions(ClassModel model, CancellationToken cancellationToken) =>
                model.ValidateAsync(new ValidationCallOptions { IncludeProperties = new[] { "Name" } }, cancellationToken);
        }
        """;

    [TestMethod]
    public void GeneratedSource_EmitsExtensionsForClassStructAndRecordStruct() {
        var run = RunGenerator(SOURCE);
        var text = string.Join(Environment.NewLine, run.GeneratedTrees.Select(static tree => tree.ToString()));

        MSTestAssertions.That(() => text.Should().Contain("ClassModelValidationExtensions"));
        MSTestAssertions.That(() => text.Should().Contain("StructModelValidationExtensions"));
        MSTestAssertions.That(() => text.Should().Contain("RecordStructModelValidationExtensions"));
        MSTestAssertions.That(() => text.Should().Contain("ValidateAsync("));
        MSTestAssertions.That(() => text.Should().NotContain("DisabledModelValidationExtensions"));
    }

    [TestMethod]
    public void ModelExtensions_MatchTheGeneratedValidatorForClassesAndStructs() {
        var generated = BuildGenerated(SOURCE);
        var classModel = generated.Create("ModelExtensionsModels.ClassModel");
        classModel.GetType().GetProperty("Name")!.SetValue(classModel, string.Empty);
        classModel.GetType().GetProperty("Code")!.SetValue(classModel, "x");

        var classResult = generated.InvokeResult("RunClass", classModel);
        ValidationAssert.HasError(classResult, "Name");
        ValidationAssert.HasError(classResult, "Code");

        var structModel = generated.Create("ModelExtensionsModels.StructModel");
        structModel.GetType().GetProperty("Name")!.SetValue(structModel, string.Empty);
        var structResult = generated.InvokeResult("RunStruct", structModel);
        ValidationAssert.HasError(structResult, "Name");

        var recordModel = generated.Create(
            "ModelExtensionsModels.RecordStructModel",
            string.Empty);
        var recordResult = generated.InvokeResult("RunRecord", recordModel);
        ValidationAssert.HasError(recordResult, "Name");
    }

    [TestMethod]
    public async Task ModelExtensions_HonorOptionsAndCancellationAsync() {
        var generated = BuildGenerated(SOURCE);
        var model = generated.Create("ModelExtensionsModels.ClassModel");
        model.GetType().GetProperty("Name")!.SetValue(model, string.Empty);
        model.GetType().GetProperty("Code")!.SetValue(model, "x");

        var failFast = generated.InvokeResult("RunOptions", model);
        ValidationAssert.ErrorCount(failFast, 1);

        var asyncResult = await generated.InvokeAsync("RunAsync", model);
        ValidationAssert.HasError(asyncResult, "Name");

        var filtered = await generated.InvokeAsync("RunAsyncOptions", model);
        ValidationAssert.HasError(filtered, "Name");
        ValidationAssert.NoErrorFor(filtered, "Code");
    }

    private static GeneratorRun RunGenerator(string source) {
        var compilation = CreateCompilation(source, $"ModelExtensionsAssembly_{Guid.NewGuid():N}");
        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var result = driver.GetRunResult();

        return new GeneratorRun(result.GeneratedTrees, [.. result.Diagnostics]);
    }

    private static GeneratedExtensions BuildGenerated(string source) {
        var compilation = CreateCompilation(source, $"ModelExtensionsAssembly_{Guid.NewGuid():N}");
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

        return new GeneratedExtensions(Assembly.Load(stream.ToArray()));
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

    private sealed class GeneratedExtensions(Assembly assembly) {
        public object Create(string typeName, params object?[] arguments) =>
            Activator.CreateInstance(assembly.GetType(typeName, throwOnError: true)!, arguments)!;

        public ValidationResult InvokeResult(string methodName, object model) =>
            (ValidationResult)assembly.GetType("ModelExtensionsModels.Harness", throwOnError: true)!
                .GetMethod(methodName)!
                .Invoke(null, [model])!;

        public async Task<ValidationResult> InvokeAsync(string methodName, object model) {
            var valueTask = (ValueTask<ValidationResult>)assembly
                .GetType("ModelExtensionsModels.Harness", throwOnError: true)!
                .GetMethod(methodName)!
                .Invoke(null, [model, CancellationToken.None])!;

            return await valueTask;
        }
    }

    private sealed record GeneratorRun(
        IEnumerable<SyntaxTree> GeneratedTrees,
        IReadOnlyList<Diagnostic> Diagnostics);
}
