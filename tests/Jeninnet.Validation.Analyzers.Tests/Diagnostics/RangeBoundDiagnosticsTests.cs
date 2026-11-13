//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.Diagnostics;

[TestClass]
public sealed class RangeBoundDiagnosticsTests {
    [TestMethod]
    public void InclusiveBetween_ReversedBounds_ReportsReversedRange() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [InclusiveBetween(10, 1)]
                public int Score { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        MSTestAssertions.That(() => diagnostics.Should().Contain(d =>
            d.Id == "VAL0010" && d.Severity == DiagnosticSeverity.Error));
    }

    [TestMethod]
    public void ExclusiveBetween_EqualBounds_ReportsVal0033() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [ExclusiveBetween(5, 5)]
                public int Score { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        MSTestAssertions.That(() => diagnostics.Should().Contain(d =>
            d.Id == "VAL0033" &&
            d.Severity == DiagnosticSeverity.Error &&
            d.GetMessage(CultureInfo.InvariantCulture).Contains("Score", StringComparison.Ordinal) &&
            d.GetMessage(CultureInfo.InvariantCulture).Contains('5')));
    }

    [TestMethod]
    public void ConditionalRange_ReversedBounds_ReportsReversedRange() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                public string? Mode { get; set; }

                [ConditionalRange(nameof(Mode), "Business", 10, 1)]
                public int Amount { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        MSTestAssertions.That(() => diagnostics.Should().Contain(d =>
            d.Id == "VAL0010" && d.Severity == DiagnosticSeverity.Error));
    }

    [TestMethod]
    public void ValidRanges_ReportNoRangeDiagnostics() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                public string? Mode { get; set; }

                [InclusiveBetween(18, 99)]
                public int Age { get; set; }

                [ExclusiveBetween(1, 2)]
                public int Rank { get; set; }

                [ConditionalRange(nameof(Mode), "Business", 1, 10)]
                public int Amount { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        MSTestAssertions.That(() => diagnostics.Any(static diagnostic =>
            diagnostic.Id is "VAL0010" or "VAL0033").Should().BeFalse());
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
