//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Wraps a GUID string literal in a Guid.Parse(...) expression.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertToGuidLiteralCodeFix))]
[Shared]
public sealed class ConvertToGuidLiteralCodeFix : CodeFixProvider {
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

        if(!Guid.TryParse(text, out _)) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Wrap in Guid.Parse(...)",
                ct => WrapInGuidParseAsync(context.Document, root, literal, ct),
                nameof(ConvertToGuidLiteralCodeFix)),
            diagnostic);
    }

    private static Task<Document> WrapInGuidParseAsync(
        Document document,
        SyntaxNode root,
        LiteralExpressionSyntax stringLiteral,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();

        var raw = stringLiteral.Token.Text; // with quotes

        var invocation = SyntaxFactory.ParseExpression($"Guid.Parse({raw})")
            .WithTriviaFrom(stringLiteral);

        var newRoot = root.ReplaceNode(stringLiteral, invocation);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
