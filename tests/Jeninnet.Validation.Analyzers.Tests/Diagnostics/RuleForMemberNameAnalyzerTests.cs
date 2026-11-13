//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.Diagnostics;

[TestClass]
public sealed class RuleForMemberNameAnalyzerTests {
    private const string MISMATCH_SOURCE =
        """
        #nullable enable
        using Jeninnet.Validation.Fluent;

        namespace TestNamespace;

        public sealed class Model {
            public string? Name { get; set; }
            public string? Email { get; set; }
        }

        public sealed class ModelValidator : Validator<Model> {
            protected override void Configure(ValidatorRuleBuilder<Model> builder) {
                builder.RuleFor(static x => x.Name, "Email");
                builder.RuleFor(static x => x.Name, nameof(Model.Email));
            }
        }
        """;

    private const string SAFE_SOURCE =
        """
        #nullable enable
        using Jeninnet.Validation.Fluent;

        namespace TestNamespace;

        public sealed class Model {
            public string? Name { get; set; }
            public string? Email { get; set; }
        }

        public sealed class ModelValidator : Validator<Model> {
            protected override void Configure(ValidatorRuleBuilder<Model> builder) {
                builder.RuleFor(static x => x.Name, nameof(Model.Name));
                builder.RuleFor(static x => x.Email, nameof(Model.Email));
                builder.RuleFor(static x => x.Name, "Contact.Email");
                builder.RuleFor(static x => x.Name, "Custom label");
                builder.RuleFor(static x => x.Name, "MissingMember");
            }
        }
        """;

    [TestMethod]
    public async Task RuleForMemberNameMismatch_IsReportedTwiceAsync() {
        var diagnostics = await AnalyzerTestSupport.GetDiagnosticsAsync(
            MISMATCH_SOURCE,
            new RuleForMemberNameAnalyzer(),
            TestContext?.CancellationToken ?? CancellationToken.None);

        var mismatches = diagnostics.Where(diagnostic => diagnostic.Id == "VAL0023").ToList();

        MSTestAssertions.That(() => mismatches.Should().HaveCount(2));
        MSTestAssertions.That(() => mismatches.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).Should().BeTrue());
        MSTestAssertions.That(() => mismatches.Any(diagnostic =>
            diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("Name", StringComparison.Ordinal)).Should().BeTrue());
    }

    [TestMethod]
    public async Task SafeMemberNames_AreNotReportedAsync() {
        var diagnostics = await AnalyzerTestSupport.GetDiagnosticsAsync(
            SAFE_SOURCE,
            new RuleForMemberNameAnalyzer(),
            TestContext?.CancellationToken ?? CancellationToken.None);

        MSTestAssertions.That(() => diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "VAL0023"));
    }

    [TestMethod]
    public async Task CodeFix_RewritesMismatchToNameofAsync() {
        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, MISMATCH_SOURCE);

        var diagnostic = await GetDiagnosticAsync(document, cancellationToken).ConfigureAwait(false);
        var actions = await RegisterFixesAsync(document, diagnostic, cancellationToken).ConfigureAwait(false);

        MSTestAssertions.That(() => actions.Should().HaveCount(1));

        var operations = await actions[0].GetOperationsAsync(cancellationToken).ConfigureAwait(false);
        var changedSolution = operations.OfType<ApplyChangesOperation>().First().ChangedSolution;
        var changedDocument = changedSolution.GetDocument(document.Id)!;
        var changedText = await changedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

        MSTestAssertions.That(() => changedText.ToString()
            .Should().Contain("nameof(global::TestNamespace.Model.Name)"));
        MSTestAssertions.That(() => changedText.ToString().Should().NotContain("\"Email\""));
    }

    private static Document CreateDocument(AdhocWorkspace workspace, string source) {
        var project = workspace.AddProject("TestAssembly", LanguageNames.CSharp)
                               .WithCompilationOptions(
                                   new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                       .WithNullableContextOptions(NullableContextOptions.Enable))
                               .WithParseOptions(new CSharpParseOptions(LanguageVersion.Latest))
                               .AddMetadataReferences(AnalyzerTestSupport.CreateReferences());

        return project.AddDocument("Test.cs", SourceText.From(source));
    }

    private static async Task<Diagnostic> GetDiagnosticAsync(
        Document document,
        CancellationToken cancellationToken) {
        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The test compilation is not available.");

        var diagnostics = await compilation
            .WithAnalyzers([new RuleForMemberNameAnalyzer()])
            .GetAnalyzerDiagnosticsAsync(cancellationToken)
            .ConfigureAwait(false);

        return diagnostics.First(diagnostic => diagnostic.Id == "VAL0023");
    }

    private static async Task<List<CodeAction>> RegisterFixesAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken) {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            cancellationToken);

        var provider = new UseNameofForRuleForMemberCodeFix();
        await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        return actions;
    }

    public TestContext? TestContext { get; set; }
}
