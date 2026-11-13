//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Covers generated enum rules for <c>[Flags]</c> enums: declared combinations pass,
/// undefined bits fail, and the generated source keeps using <c>EnumRule</c>.
/// </summary>
[TestClass]
public sealed class GeneratedEnumFlagsTests {
    private const string SOURCE = """
        using System;
        using Jeninnet.Validation.Attributes;
        using Jeninnet.Validation.Attributes.Common;

        namespace EnumFlagsModels;

        [Flags]
        public enum Access {
            None = 0,
            Read = 1,
            Write = 2,
            Delete = 4,
            All = Read | Write | Delete
        }

        [Validate("EnumFlagsModels")]
        public sealed class Grant {
            [Enum]
            public Access Access { get; set; }
        }
        """;

    [TestMethod]
    public void GeneratedFlagsEnum_AcceptsDeclaredCombinations() {
        var generated = BuildGenerated();

        ValidationAssert.IsValid(generated.Validate(0));
        ValidationAssert.IsValid(generated.Validate(3));
        ValidationAssert.IsValid(generated.Validate(7));
    }

    [TestMethod]
    public void GeneratedFlagsEnum_RejectsUndefinedBits() {
        var generated = BuildGenerated();
        var result = generated.Validate(8);

        ValidationAssert.HasError(result, "Access");
    }

    [TestMethod]
    public void GeneratedSource_UsesEnumRuleWithTheFlagsType() {
        var run = RunGenerator(SOURCE);
        var text = string.Join(Environment.NewLine, run.GeneratedTrees.Select(static tree => tree.ToString()));

        MSTestAssertions.That(() => text.Should().Contain("EnumRule"));
        MSTestAssertions.That(() => text.Should().Contain("typeof(Access)"));
        MSTestAssertions.That(() => text.Should().Contain("typeof"));
    }

    private static GeneratorRun RunGenerator(string source) {
        var compilation = CreateCompilation(source, $"EnumFlagsAssembly_{Guid.NewGuid():N}");
        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var result = driver.GetRunResult();

        return new GeneratorRun(result.GeneratedTrees, [.. result.Diagnostics]);
    }

    private static GeneratedGrant BuildGenerated() {
        var compilation = CreateCompilation(SOURCE, $"EnumFlagsAssembly_{Guid.NewGuid():N}");
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

        return new GeneratedGrant(Assembly.Load(stream.ToArray()));
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

    private sealed class GeneratedGrant(Assembly assembly) {
        private readonly Type _accessType = assembly.GetType("EnumFlagsModels.Access", throwOnError: true)!;
        private readonly Type _modelType = assembly.GetType("EnumFlagsModels.Grant", throwOnError: true)!;
        private readonly object _validator = Activator.CreateInstance(
            assembly.GetType("EnumFlagsModels.GrantValidator", throwOnError: true)!)!;

        public ValidationResult Validate(int access) {
            var model = Activator.CreateInstance(_modelType)!;
            _modelType.GetProperty("Access")!.SetValue(model, Enum.ToObject(_accessType, access));

            return (ValidationResult)_validator.GetType()
                                                .GetMethod("Validate", [_modelType])!
                                                .Invoke(_validator, [model])!;
        }
    }

    private sealed record GeneratorRun(
        IEnumerable<SyntaxTree> GeneratedTrees,
        IReadOnlyList<Diagnostic> Diagnostics);
}
