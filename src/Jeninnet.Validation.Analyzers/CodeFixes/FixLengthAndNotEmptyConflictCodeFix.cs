//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Resolves a conflict between [Length] and [NotEmpty] by removing [NotEmpty] or adjusting [Length] minimum to 1.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FixLengthAndNotEmptyConflictCodeFix))]
[Shared]
public sealed class FixLengthAndNotEmptyConflictCodeFix : CodeFixProvider {
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.InvalidRuleCombination.Id];

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

        if(!diagnostic.Properties.TryGetValue("Rule1", out var rule1) ||
            !diagnostic.Properties.TryGetValue("Rule2", out var rule2)) {
            return;
        }

        var rules = new[] { rule1, rule2 };

        if(!rules.Contains("Length") || !rules.Contains("NotEmpty")) {
            return;
        }

        var property = root.FindNode(diagnostic.Location.SourceSpan)
            .FirstAncestorOrSelf<PropertyDeclarationSyntax>();

        if(property is null) {
            return;
        }

        var attributes = property.AttributeLists.SelectMany(static list => list.Attributes).ToList();
        var notEmptyAttribute = attributes.FirstOrDefault(
            static attribute => attribute.Name.ToString() == "NotEmpty");
        var lengthAttribute = attributes.FirstOrDefault(
            static attribute => attribute.Name.ToString() == "Length");

        // Option 1: Remove NotEmpty
        if(notEmptyAttribute is not null) {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Remove [NotEmpty]",
                    ct => RemoveAttributeAsync(context.Document, root, notEmptyAttribute, ct),
                    "RemoveNotEmpty"),
                diagnostic);
        }

        // Option 2: Change Length(0, X) → Length(1, X)
        if(lengthAttribute?.ArgumentList?.Arguments.Count >= 2) {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Change Length(min=0) to Length(min=1)",
                    ct => FixLengthMinAsync(context.Document, root, lengthAttribute, ct),
                    "FixLengthMin"),
                diagnostic);
        }
    }

    private static Task<Document> RemoveAttributeAsync(
        Document document,
        SyntaxNode root,
        AttributeSyntax attribute,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();

        var newRoot = root.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia);
        return Task.FromResult(newRoot is null ? document : document.WithSyntaxRoot(newRoot));
    }

    private static Task<Document> FixLengthMinAsync(
        Document document,
        SyntaxNode root,
        AttributeSyntax attribute,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();

        var args = attribute.ArgumentList!.Arguments;
        var minArg = args[0];

        var newMin = SyntaxFactory.AttributeArgument(
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));

        var newArgs = args.Replace(minArg, newMin);
        var newList = SyntaxFactory.AttributeArgumentList(newArgs);

        var newAttribute = attribute.WithArgumentList(newList);
        var newRoot = root.ReplaceNode(attribute, newAttribute);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
