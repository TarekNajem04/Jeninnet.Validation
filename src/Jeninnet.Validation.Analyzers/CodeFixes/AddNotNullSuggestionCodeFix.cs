//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Adds a [NotNull] attribute to a nullable property that has validation rules but lacks it.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddNotNullSuggestionCodeFix))]
[Shared]
public sealed class AddNotNullSuggestionCodeFix : CodeFixProvider {
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.SuggestNotNullForNullable.Id];

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
                "Add [NotNull]",
                ct => AddNotNullAsync(context.Document, root, property, ct),
                nameof(AddNotNullSuggestionCodeFix)),
            diagnostic);
    }

    private static Task<Document> AddNotNullAsync(
        Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax property,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();
        var notNullAttr = SyntaxFactory.Attribute(SyntaxFactory.ParseName("NotNull"));

        AttributeListSyntax newList;
        if(property.AttributeLists.Any()) {
            var firstList = property.AttributeLists.First();
            newList = firstList.AddAttributes(notNullAttr);
            var newProperty = property.WithAttributeLists(
                property.AttributeLists.Replace(firstList, newList));
            var newRoot = root.ReplaceNode(property, newProperty);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        } else {
            newList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(notNullAttr));

            var newProperty = property.AddAttributeLists(newList);
            var newRoot = root.ReplaceNode(property, newProperty);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}
