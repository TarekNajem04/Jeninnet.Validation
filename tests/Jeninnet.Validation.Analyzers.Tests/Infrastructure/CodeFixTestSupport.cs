//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.Infrastructure;

/// <summary>
/// Shared helpers for exercising Roslyn code-fix providers without rebuilding the full analyzer
/// pipeline. Analyzer detection is covered separately; these tests exercise the fix itself.
/// </summary>
internal static class CodeFixTestSupport {
    internal static Document CreateDocument(AdhocWorkspace workspace, string source) {
        var project = workspace.AddProject("TestAssembly", LanguageNames.CSharp)
                               .WithCompilationOptions(
                                   new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                       .WithNullableContextOptions(NullableContextOptions.Enable))
                               .WithParseOptions(new CSharpParseOptions(LanguageVersion.Latest))
                               .AddMetadataReferences(AnalyzerTestSupport.CreateReferences());

        return project.AddDocument("Test.cs", SourceText.From(source));
    }

    internal static Diagnostic CreateDiagnostic(
        string id,
        Location location,
        params (string Key, string Value)[] properties) {
        var descriptor = new DiagnosticDescriptor(
            id,
            title: "Test diagnostic",
            messageFormat: "Test diagnostic",
            category: "Test",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        var propertyBag = properties.ToImmutableDictionary(
            static pair => pair.Key,
            static pair => (string?)pair.Value,
            StringComparer.Ordinal);

        return Diagnostic.Create(descriptor, location, properties: propertyBag);
    }

    internal static async Task<Document?> ApplyFixAsync(
        Document document,
        Diagnostic diagnostic,
        CodeFixProvider provider,
        int actionIndex = 0,
        CancellationToken cancellationToken = default) {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            cancellationToken);

        await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

        if(actions.Count <= actionIndex) {
            return null;
        }

        var operations = await actions[actionIndex]
            .GetOperationsAsync(cancellationToken)
            .ConfigureAwait(false);
        var changedSolution = operations
            .OfType<ApplyChangesOperation>()
            .Single()
            .ChangedSolution;

        return changedSolution.GetDocument(document.Id);
    }

    internal static async Task<string?> ApplyFixAndGetTextAsync(
        Document document,
        Diagnostic diagnostic,
        CodeFixProvider provider,
        int actionIndex = 0,
        CancellationToken cancellationToken = default) {
        var changedDocument = await ApplyFixAsync(
            document,
            diagnostic,
            provider,
            actionIndex,
            cancellationToken).ConfigureAwait(false);

        if(changedDocument is null) {
            return null;
        }

        var text = await changedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return text.ToString();
    }

    internal static async Task<ISet<string>> GetFixActionTitlesAsync(
        Document document,
        Diagnostic diagnostic,
        CodeFixProvider provider,
        CancellationToken cancellationToken = default) {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            cancellationToken);

        await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        return actions.Select(static action => action.Title).ToHashSet(StringComparer.Ordinal);
    }
}
