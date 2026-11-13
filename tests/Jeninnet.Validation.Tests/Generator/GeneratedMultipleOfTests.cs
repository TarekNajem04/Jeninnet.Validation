//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Verifies the generated <c>[MultipleOf]</c> rule, including decimal divisors and diagnostics.
/// </summary>
[TestClass]
public sealed class GeneratedMultipleOfTests {
    private const string SOURCE = """
        using Jeninnet.Validation.Attributes;

        namespace MultipleOfParity;

        [Validate("MultipleOfParity")]
        public sealed class Basket {
            [MultipleOf(3)]
            public int Quantity { get; set; }

            [MultipleOf(0.05)]
            public decimal Price { get; set; }
        }
        """;

    [TestMethod]
    public void GeneratedMultipleOf_ValidValues_Pass() {
        var (validator, model, modelType) = Build();

        modelType.GetProperty("Quantity")!.SetValue(model, 9);
        modelType.GetProperty("Price")!.SetValue(model, 0.10m);

        MSTestAssertions.That(() => Validate(validator, model).IsValid.Should().BeTrue());
    }

    [TestMethod]
    public void GeneratedMultipleOf_InvalidValues_FailWithMember() {
        var (validator, model, modelType) = Build();

        modelType.GetProperty("Quantity")!.SetValue(model, 10);
        modelType.GetProperty("Price")!.SetValue(model, 0.11m);

        var result = Validate(validator, model);

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(2));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("Quantity"));
        MSTestAssertions.That(() => result.Errors[0].MessageKey.Should().Be("MultipleOf"));
    }

    [TestMethod]
    public void GeneratedMultipleOf_InvalidDivisor_ReportsVal0034() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace MultipleOfInvalid;

            [Validate]
            public sealed class Basket {
                [MultipleOf(0)]
                public int Quantity { get; set; }
            }
            """;

        var diagnostics = RunGenerator(source).Diagnostics;

        MSTestAssertions.That(() => diagnostics.Should().Contain(d =>
            d.Id == "VAL0034" && d.Severity == DiagnosticSeverity.Error));
    }

    private static ValidationResult Validate(object validator, object model) {
        var method = validator.GetType().GetMethod("Validate", [model.GetType()])!;
        return (ValidationResult)method.Invoke(validator, [model])!;
    }

    private (object Validator, object Model, Type ModelType) Build() {
        var assembly = BuildAssembly(SOURCE).Assembly;
        var modelType = assembly.GetType("MultipleOfParity.Basket", throwOnError: true)!;
        var validatorType = assembly.GetType("MultipleOfParity.BasketValidator", throwOnError: true)!;

        return (Activator.CreateInstance(validatorType)!, Activator.CreateInstance(modelType)!, modelType);
    }

    private (Assembly Assembly, string[] Sources) BuildAssembly(string source) {
        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            $"GeneratedMultipleOfAssembly_{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, cancellationToken);
        var runResult = driver.GetRunResult();

        MSTestAssertions.That(() => (runResult.GeneratedTrees.Length > 0).Should().BeTrue(
            string.Join(Environment.NewLine, runResult.Diagnostics)));

        var sources = runResult.GeneratedTrees.Select(tree => tree.GetText(cancellationToken).ToString()).ToArray();
        var generatedCompilation = compilation.AddSyntaxTrees(runResult.GeneratedTrees);

        using var stream = new MemoryStream();
        var emit = generatedCompilation.Emit(stream, cancellationToken: cancellationToken);

        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        return (Assembly.Load(stream.ToArray()), sources);
    }

    private GeneratorRun RunGenerator(string source) {
        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            $"GeneratedMultipleOfDiagnosticsAssembly_{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, cancellationToken);
        var result = driver.GetRunResult();

        return new GeneratorRun(result.GeneratedTrees, [.. result.Diagnostics]);
    }

    private sealed record GeneratorRun(
        ImmutableArray<SyntaxTree> GeneratedTrees,
        ImmutableArray<Diagnostic> Diagnostics);

    public TestContext? TestContext { get; set; }
}
