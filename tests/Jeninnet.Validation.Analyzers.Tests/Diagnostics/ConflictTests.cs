//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Tests.Diagnostics;

[TestClass]
public class ConflictTests {
    [TestMethod]
    public void GreaterThan_And_LessThan_Conflict_ShouldReportError() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [GreaterThan(100)]
                [LessThan(50)]
                public int Age { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        MSTestAssertions.That(() => diagnostics.Should().Contain(d => d.Id == "VAL0007" && d.Severity == DiagnosticSeverity.Error));
    }

    [TestMethod]
    public void InclusiveBetween_And_ExclusiveBetween_Conflict_ShouldReportError() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [InclusiveBetween(10, 20)]
                [ExclusiveBetween(5, 15)]
                public int Score { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        MSTestAssertions.That(() => diagnostics.Should().Contain(d => d.Id == "VAL0007" && d.Severity == DiagnosticSeverity.Error));
    }

    private static ImmutableArray<Diagnostic> GetDiagnostics(string source) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attributes.ValidateAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "System.Runtime").Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var _, out var diagnostics);

        return diagnostics;
    }
}
