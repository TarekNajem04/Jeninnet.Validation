//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Tests.Diagnostics;

[TestClass]
public sealed class UniqueItemsDiagnosticsTests {
    public TestContext TestContext { get; set; } = null!;
    [TestMethod]
    public void UniqueItems_OnNonCollection_ReportsIncompatibleType() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [UniqueItems]
                public string? Name { get; set; }
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.CancellationToken);
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
        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var _,
            out var diagnostics,
            TestContext.CancellationToken);

        MSTestAssertions.That(() => diagnostics.Should().Contain(d =>
            d.Id == "VAL0002" &&
            d.Severity == DiagnosticSeverity.Error &&
            d.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains("UniqueItems", StringComparison.Ordinal)));
    }
}
