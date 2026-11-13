//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Tests.Diagnostics;

[TestClass]
public class TypeMismatchTests {
    [TestMethod]
    public void Email_On_Int_ShouldReportError() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [Email]
                public int Id { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        MSTestAssertions.That(() => diagnostics.Should().Contain(d => d.Id == "VAL0002" && d.Severity == DiagnosticSeverity.Error));
    }

    [TestMethod]
    public void GreaterThan_On_String_ShouldReportError() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [GreaterThan(10)]
                public string Name { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        MSTestAssertions.That(() => diagnostics.Should().Contain(d => d.Id == "VAL0002" && d.Severity == DiagnosticSeverity.Error));
    }

    [TestMethod]
    public void NotNull_On_Int_ShouldReportWarning() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [NotNull]
                public int Age { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        MSTestAssertions.That(() => diagnostics.Should().Contain(d => d.Id == "VAL0014" && d.Severity == DiagnosticSeverity.Warning));
    }

    private static ImmutableArray<Diagnostic> GetDiagnostics(string source) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(int).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attributes.ValidateAttribute).Assembly.Location)
        };

        // Add System.Runtime and others needed for basic types like int, string
        if(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedAssembliesStr) {
            var trustedAssemblies = trustedAssembliesStr.Split(Path.PathSeparator);
            var needed = new[] { "System.Runtime", "System.Collections", "mscorlib", "System.Private.CoreLib" };

            foreach(var path in trustedAssemblies) {
                var name = Path.GetFileNameWithoutExtension(path);
                if(needed.Contains(name)) {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        return diagnostics;
    }
}
