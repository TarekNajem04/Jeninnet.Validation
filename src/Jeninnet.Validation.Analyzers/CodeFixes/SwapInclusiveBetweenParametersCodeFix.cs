//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Swaps the parameters of an [InclusiveBetween] attribute to fix an out-of-range parameter order.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SwapInclusiveBetweenParametersCodeFix))]
[Shared]
public sealed class SwapInclusiveBetweenParametersCodeFix : CodeFixProvider {
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.RuleParameterOutOfRange.Id];

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
        var attribute = node.AncestorsAndSelf().OfType<AttributeSyntax>().FirstOrDefault();

        if(attribute?.ArgumentList?.Arguments.Count != 2) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Swap InclusiveBetween parameters",
                ct => SwapParametersAsync(context.Document, root, attribute, ct),
                nameof(SwapInclusiveBetweenParametersCodeFix)),
            diagnostic);
    }

    private static Task<Document> SwapParametersAsync(
        Document document,
        SyntaxNode root,
        AttributeSyntax attribute,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();

        var args = attribute.ArgumentList!.Arguments;
        var swapped = SyntaxFactory.AttributeArgumentList(
            SyntaxFactory.SeparatedList(
                [
                    args[1],
                    args[0]
                ]
            )
        );

        var newAttribute = attribute.WithArgumentList(swapped);
        var newRoot = root.ReplaceNode(attribute, newAttribute);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
