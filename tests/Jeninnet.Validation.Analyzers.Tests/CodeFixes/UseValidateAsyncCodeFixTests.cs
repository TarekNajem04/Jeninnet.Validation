//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.CodeFixes;

[TestClass]
public sealed class UseValidateAsyncCodeFixTests {
    private const string ASYNC_CALL_SITE_SOURCE =
        """
        #nullable enable
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using Jeninnet.Validation;
        using Jeninnet.Validation.Abstractions;

        namespace TestNamespace;

        public sealed class Payment { }

        [AsyncOnlyValidator]
        public sealed class PaymentValidator : ISyncValidator<Payment>, IAsyncValidator<Payment> {
            public ValidationResult Validate(Payment instance) => ValidationResult.Success;
            public ValidationResult Validate(Payment instance, IEnumerable<string> ruleSets) => ValidationResult.Success;
            public ValueTask<ValidationResult> ValidateAsync(Payment instance, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(ValidationResult.Success);
            public ValueTask<ValidationResult> ValidateAsync(Payment instance, IEnumerable<string> ruleSets, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(ValidationResult.Success);
        }

        public static class Runner {
            public static async Task RunAsync(PaymentValidator validator, Payment payment) {
                _ = validator.Validate(payment);
            }
        }
        """;

    private const string SYNC_CALL_SITE_SOURCE =
        """
        #nullable enable
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using Jeninnet.Validation;
        using Jeninnet.Validation.Abstractions;

        namespace TestNamespace;

        public sealed class Payment { }

        [AsyncOnlyValidator]
        public sealed class PaymentValidator : ISyncValidator<Payment>, IAsyncValidator<Payment> {
            public ValidationResult Validate(Payment instance) => ValidationResult.Success;
            public ValidationResult Validate(Payment instance, IEnumerable<string> ruleSets) => ValidationResult.Success;
            public ValueTask<ValidationResult> ValidateAsync(Payment instance, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(ValidationResult.Success);
            public ValueTask<ValidationResult> ValidateAsync(Payment instance, IEnumerable<string> ruleSets, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(ValidationResult.Success);
        }

        public static class Runner {
            public static void Run(PaymentValidator validator, Payment payment) {
                _ = validator.Validate(payment);
            }
        }
        """;

    [TestMethod]
    public async Task AsyncCallSite_IsRewrittenToAwaitValidateAsyncAsync() {
        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, ASYNC_CALL_SITE_SOURCE);

        var diagnostic = await GetDiagnosticAsync(document, cancellationToken).ConfigureAwait(false);
        var actions = await RegisterFixesAsync(document, diagnostic, cancellationToken).ConfigureAwait(false);

        MSTestAssertions.That(() => actions.Should().HaveCount(1));

        var operations = await actions[0].GetOperationsAsync(cancellationToken).ConfigureAwait(false);
        var changedSolution = operations.OfType<ApplyChangesOperation>().First().ChangedSolution;
        var changedDocument = changedSolution.GetDocument(document.Id)!;
        var changedText = await changedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

        MSTestAssertions.That(() => changedText.ToString().Should().Contain("await (validator.ValidateAsync(payment))"));
    }

    [TestMethod]
    public async Task SyncCallSite_DoesNotOfferFixAsync() {
        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, SYNC_CALL_SITE_SOURCE);

        var diagnostic = await GetDiagnosticAsync(document, cancellationToken).ConfigureAwait(false);
        var actions = await RegisterFixesAsync(document, diagnostic, cancellationToken).ConfigureAwait(false);

        MSTestAssertions.That(() => actions.Should().BeEmpty());
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

    private static async Task<Diagnostic> GetDiagnosticAsync(Document document, CancellationToken cancellationToken) {
        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The test compilation is not available.");

        var diagnostics = await compilation
            .WithAnalyzers([new SyncValidateAnalyzer()])
            .GetAnalyzerDiagnosticsAsync(cancellationToken)
            .ConfigureAwait(false);

        return diagnostics.Single(diagnostic => diagnostic.Id == "VAL0016");
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

        await new UseValidateAsyncCodeFix().RegisterCodeFixesAsync(context).ConfigureAwait(false);
        return actions;
    }

    public TestContext? TestContext { get; set; }
}
