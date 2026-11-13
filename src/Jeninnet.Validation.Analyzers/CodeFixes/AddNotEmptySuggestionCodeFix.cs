//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Adds a [NotEmpty] attribute to a string property that participates in validation but lacks it.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddNotEmptySuggestionCodeFix))]
[Shared]
public sealed class AddNotEmptySuggestionCodeFix : CodeFixProvider {
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.SuggestNotEmptyForString.Id];

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
        var property = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
        if(property is null) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Add [NotEmpty]",
                ct => AddNotEmptyAsync(context.Document, root, property, ct),
                nameof(AddNotEmptySuggestionCodeFix)),
            diagnostic);
    }

    private static Task<Document> AddNotEmptyAsync(
        Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax property,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();
        var notEmptyAttr = SyntaxFactory.Attribute(SyntaxFactory.ParseName("NotEmpty"));

        AttributeListSyntax newList;
        if(property.AttributeLists.Any()) {
            var firstList = property.AttributeLists.First();
            newList = firstList.AddAttributes(notEmptyAttr);
            var newProperty = property.WithAttributeLists(
                property.AttributeLists.Replace(firstList, newList));
            var newRoot = root.ReplaceNode(property, newProperty);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        } else {
            newList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(notEmptyAttr));

            var newProperty = property.AddAttributeLists(newList);
            var newRoot = root.ReplaceNode(property, newProperty);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}
