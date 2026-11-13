//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Wraps a date string literal in a DateOnly.Parse(...) expression.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertToDateOnlyLiteralCodeFix))]
[Shared]
public sealed class ConvertToDateOnlyLiteralCodeFix : CodeFixProvider {
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

        if(!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Wrap in DateOnly.Parse(...)",
                ct => WrapInDateOnlyParseAsync(context.Document, root, literal, ct),
                nameof(ConvertToDateOnlyLiteralCodeFix)),
            diagnostic);
    }

    private static Task<Document> WrapInDateOnlyParseAsync(
        Document document,
        SyntaxNode root,
        LiteralExpressionSyntax stringLiteral,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();
        var raw = stringLiteral.Token.Text; // includes quotes

        var invocation = SyntaxFactory.ParseExpression($"DateOnly.Parse({raw})")
            .WithTriviaFrom(stringLiteral);

        var newRoot = root.ReplaceNode(stringLiteral, invocation);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
