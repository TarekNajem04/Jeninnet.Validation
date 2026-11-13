//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Adds a [NotNull] attribute before a numeric rule on a nullable property to resolve type incompatibility.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddNotNullBeforeNumericRuleCodeFix))]
[Shared]
public sealed class AddNotNullBeforeNumericRuleCodeFix : CodeFixProvider {
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.RuleNotAllowedOnNullableType.Id];

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

        if(!diagnostic.Properties.TryGetValue("PropertyName", out var _)) {
            return;
        }

        var attributeList = root.FindNode(diagnostic.Location.SourceSpan)
            .AncestorsAndSelf()
            .OfType<AttributeListSyntax>()
            .FirstOrDefault();

        if(attributeList is null) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Add [NotNull] before numeric rule",
                ct => AddNotNullAsync(context.Document, root, attributeList, ct),
                nameof(AddNotNullBeforeNumericRuleCodeFix)),
            diagnostic);
    }

    private static Task<Document> AddNotNullAsync(
        Document document,
        SyntaxNode root,
        AttributeListSyntax attributeList,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();

        var notNullAttr = SyntaxFactory.Attribute(SyntaxFactory.ParseName("NotNull"));
        var newList = attributeList.AddAttributes(notNullAttr);

        var newRoot = root.ReplaceNode(attributeList, newList);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
