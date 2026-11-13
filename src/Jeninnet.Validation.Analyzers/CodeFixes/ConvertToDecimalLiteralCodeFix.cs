//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Converts a numeric string literal into a decimal literal (suffix 'm').
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertToDecimalLiteralCodeFix))]
[Shared]
public sealed class ConvertToDecimalLiteralCodeFix : CodeFixProvider {
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

        if(!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _)) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Convert to decimal literal (10m)",
                ct => ConvertToDecimalAsync(context.Document, root, literal, ct),
                nameof(ConvertToDecimalLiteralCodeFix)),
            diagnostic);
    }

    private static Task<Document> ConvertToDecimalAsync(
        Document document,
        SyntaxNode root,
        LiteralExpressionSyntax stringLiteral,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();

        var numericText = stringLiteral.Token.ValueText;
        var parsed = decimal.Parse(numericText, CultureInfo.InvariantCulture);
        var decimalLiteral = SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(numericText + "m", parsed)
        );

        var newRoot = root.ReplaceNode(stringLiteral, decimalLiteral);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
