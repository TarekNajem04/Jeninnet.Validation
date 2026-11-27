//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Removes a validation attribute whose bounds cover the member's entire value range.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveTautologicalRuleCodeFix))]
[Shared]
public sealed class RemoveTautologicalRuleCodeFix : CodeFixProvider {
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.TautologicalRule.Id];

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var diagnostic = context.Diagnostics[0];
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if(root is null) {
            return;
        }

        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var attribute = node.FirstAncestorOrSelf<AttributeSyntax>() ??
            node.DescendantNodesAndSelf().OfType<AttributeSyntax>().FirstOrDefault();

        if(attribute is null) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                $"Remove tautological [{attribute.Name}]",
                ct => RemoveAsync(context.Document, root, attribute, ct),
                nameof(RemoveTautologicalRuleCodeFix)),
            diagnostic);
    }

    private static Task<Document> RemoveAsync(
        Document document,
        SyntaxNode root,
        AttributeSyntax attribute,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        var newRoot = AttributeRemoval.Remove(root, attribute);
        return Task.FromResult(newRoot is null ? document : document.WithSyntaxRoot(newRoot));
    }
}
