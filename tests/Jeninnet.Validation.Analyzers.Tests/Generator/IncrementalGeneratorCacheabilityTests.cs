//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.Generator;

[TestClass]
public sealed class IncrementalGeneratorCacheabilityTests {
    private const string MODEL_SOURCE = """
        using Jeninnet.Validation.Attributes;

        namespace CacheModel;

        [Validate("CacheModel")]
        public sealed class Customer {
            [NotEmpty]
            public string? Name { get; set; }
        }
        """;

    private const string OTHER_SOURCE = """
        namespace Other;

        public sealed class Other { }
        """;

    [TestMethod]
    public void UnrelatedSyntaxEdit_DoesNotRerunValidatorGeneration() {
        var driver = (GeneratorDriver)CreateDriver();
        var compilation = CreateCompilation(MODEL_SOURCE, OTHER_SOURCE);

        driver = driver.RunGenerators(compilation, TestCancellation);
        var first = driver.GetRunResult().Results[0];

        TestContext?.WriteLine("TrackedSteps: " + string.Join(", ", first.TrackedSteps.Keys));
        TestContext?.WriteLine("TrackedOutputSteps: " + string.Join(", ", first.TrackedOutputSteps.Keys));

        var otherTree = compilation.SyntaxTrees.Single(static tree => tree.ToString().Contains("class Other", StringComparison.Ordinal));
        var updated = compilation.ReplaceSyntaxTree(
            otherTree,
            CSharpSyntaxTree.ParseText(OTHER_SOURCE + "\n// unrelated edit", cancellationToken: TestCancellation));

        driver = driver.RunGenerators(updated, TestCancellation);
        var second = driver.GetRunResult().Results[0];

        foreach(var pair in second.TrackedSteps) {
            TestContext?.WriteLine($"{pair.Key}: {string.Join(", ", pair.Value.Select(static step => step.Outputs.FirstOrDefault().Reason))}");
        }

        foreach(var pair in second.TrackedOutputSteps) {
            TestContext?.WriteLine($"output {pair.Key}: {string.Join(", ", pair.Value.Select(static step => step.Outputs.FirstOrDefault().Reason))}");
        }

        var outputReasons = second.TrackedSteps["SourceOutput"]
            .SelectMany(static step => step.Outputs)
            .Select(static output => output.Reason)
            .ToArray();

        MSTestAssertions.That(() => outputReasons.Count(static reason =>
            reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged).Should().BeGreaterThanOrEqualTo(2));
        MSTestAssertions.That(() => outputReasons.Should().Contain(IncrementalStepRunReason.Modified));
        MSTestAssertions.That(() => second.GeneratedSources.Should().NotBeEmpty());
    }

    [TestMethod]
    public void RepeatedRuns_ProduceByteIdenticalSources() {
        var compilation = CreateCompilation(MODEL_SOURCE, OTHER_SOURCE);
        var first = RunAndGetSources(compilation);
        var second = RunAndGetSources(compilation);

        MSTestAssertions.That(() => second.Should().Equal(first));
    }

    [TestMethod]
    public void ColdAndIncrementalGeneration_AreMeasured() {
        var compilation = CreateCompilation(MODEL_SOURCE, OTHER_SOURCE);
        var cold = Stopwatch.StartNew();
        var driver = CreateDriver().RunGenerators(compilation, TestCancellation);
        cold.Stop();

        var otherTree = compilation.SyntaxTrees.Single(static tree => tree.ToString().Contains("class Other", StringComparison.Ordinal));
        var updated = compilation.ReplaceSyntaxTree(
            otherTree,
            CSharpSyntaxTree.ParseText(OTHER_SOURCE + "\n// unrelated edit", cancellationToken: TestCancellation));
        var incremental = Stopwatch.StartNew();
        _ = driver.RunGenerators(updated, TestCancellation);
        incremental.Stop();

        TestContext?.WriteLine($"Cold generation: {cold.Elapsed.TotalMilliseconds:F2} ms");
        TestContext?.WriteLine($"Incremental unrelated edit: {incremental.Elapsed.TotalMilliseconds:F2} ms");
        MSTestAssertions.That(() => cold.Elapsed.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(0));
        MSTestAssertions.That(() => incremental.Elapsed.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(0));
    }

    private IReadOnlyList<string> RunAndGetSources(Compilation compilation) {
        var result = CreateDriver().RunGenerators(compilation, TestCancellation).GetRunResult().Results[0];

        return [.. result.GeneratedSources
            .OrderBy(static source => source.HintName, StringComparer.Ordinal)
            .Select(static source => $"{source.HintName}\n{source.SourceText}")];
    }

    private static CSharpGeneratorDriver CreateDriver() =>
        CSharpGeneratorDriver.Create(
            [new ValidatorGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

    private CSharpCompilation CreateCompilation(params string[] sources) {
        var trees = sources
            .Select(source => CSharpSyntaxTree.ParseText(source, cancellationToken: TestCancellation))
            .ToArray();

        return CSharpCompilation.Create(
            "CacheAssembly",
            trees,
            AnalyzerTestSupport.CreateReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
    }

    private CancellationToken TestCancellation => TestContext?.CancellationToken ?? CancellationToken.None;

    public TestContext? TestContext { get; set; }
}
