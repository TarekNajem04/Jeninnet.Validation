//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Wraps a time-span string literal in a TimeSpan.Parse(...) expression.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertToTimeSpanLiteralCodeFix))]
[Shared]
public sealed class ConvertToTimeSpanLiteralCodeFix : CodeFixProvider {
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

        if(!TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out _)) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Wrap in TimeSpan.Parse(...)",
                ct => WrapInTimeSpanParseAsync(context.Document, root, literal, ct),
                nameof(ConvertToTimeSpanLiteralCodeFix)),
            diagnostic);
    }

    private static Task<Document> WrapInTimeSpanParseAsync(
        Document document,
        SyntaxNode root,
        LiteralExpressionSyntax stringLiteral,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();
        var raw = stringLiteral.Token.Text;

        var invocation = SyntaxFactory.ParseExpression($"TimeSpan.Parse({raw})")
            .WithTriviaFrom(stringLiteral);

        var newRoot = root.ReplaceNode(stringLiteral, invocation);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
