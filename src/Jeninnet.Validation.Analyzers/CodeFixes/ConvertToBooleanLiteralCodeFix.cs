//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Converts a string literal that looks like a boolean into a boolean literal expression.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertToBooleanLiteralCodeFix))]
[Shared]
public sealed class ConvertToBooleanLiteralCodeFix : CodeFixProvider {
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.InvalidRuleParameter.Id];

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

        if(!RuleParameterCodeFixHelpers.TryGetStringLiteral(root, diagnostic, out var literal, out var text)) {
            return;
        }

        if(!RuleParameterCodeFixHelpers.LooksLikeBool(text)) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                $"Convert to boolean literal ({text.ToLowerInvariant()})",
                ct => ConvertToBoolAsync(context.Document, root, literal, text, ct),
                nameof(ConvertToBooleanLiteralCodeFix)),
            diagnostic);
    }

    private static Task<Document> ConvertToBoolAsync(
        Document document,
        SyntaxNode root,
        LiteralExpressionSyntax stringLiteral,
        string text,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();

        var kind = text.Equals("true", StringComparison.OrdinalIgnoreCase)
            ? SyntaxKind.TrueLiteralExpression
            : SyntaxKind.FalseLiteralExpression;

        var boolLiteral = SyntaxFactory.LiteralExpression(kind)
            .WithTriviaFrom(stringLiteral);

        var newRoot = root.ReplaceNode(stringLiteral, boolLiteral);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
