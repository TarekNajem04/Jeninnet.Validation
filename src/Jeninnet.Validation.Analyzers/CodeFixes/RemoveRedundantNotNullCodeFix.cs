//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Removes a redundant <c>[NotNull]</c> attribute from a non-nullable member.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveRedundantNotNullCodeFix))]
[Shared]
public sealed class RemoveRedundantNotNullCodeFix : CodeFixProvider {
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.RedundantNotNull.Id];

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

        var property = root.FindNode(diagnostic.Location.SourceSpan)
            .FirstAncestorOrSelf<PropertyDeclarationSyntax>();

        if(property is null) {
            return;
        }

        var attribute = property.AttributeLists
            .SelectMany(static list => list.Attributes)
            .FirstOrDefault(static candidate => IsNotNull(candidate.Name.ToString()));

        if(attribute is null) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Remove redundant [NotNull]",
                ct => RemoveAsync(context.Document, root, attribute, ct),
                nameof(RemoveRedundantNotNullCodeFix)),
            diagnostic);
    }

    private static bool IsNotNull(string name) =>
        name is "NotNull" or "NotNullAttribute" ||
        name.EndsWith(".NotNull", StringComparison.Ordinal) ||
        name.EndsWith(".NotNullAttribute", StringComparison.Ordinal);

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
