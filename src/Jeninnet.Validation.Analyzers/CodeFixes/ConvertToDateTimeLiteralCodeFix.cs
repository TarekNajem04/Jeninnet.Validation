//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Converts a date-time string literal into a DateTime.Parse(...) expression or a new DateTime(...) constructor.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertToDateTimeLiteralCodeFix))]
[Shared]
public sealed class ConvertToDateTimeLiteralCodeFix : CodeFixProvider {
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
                "Wrap in DateTime.Parse(...)",
                ct => WrapInDateTimeParseAsync(context.Document, root, literal, ct),
                "DateTimeParse"
            ),
            diagnostic
        );

        context.RegisterCodeFix(
            CodeAction.Create(
                "Convert to new DateTime(...) constructor",
                ct => ConvertToDateTimeCtorAsync(context.Document, root, literal, text, ct),
                "DateTimeCtor"),
            diagnostic);
    }

    private static Task<Document> WrapInDateTimeParseAsync(
        Document document,
        SyntaxNode root,
        LiteralExpressionSyntax stringLiteral,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();
        var raw = stringLiteral.Token.Text; // includes quotes

        var invocation = SyntaxFactory.ParseExpression($"DateTime.Parse({raw})")
            .WithTriviaFrom(stringLiteral);

        var newRoot = root.ReplaceNode(stringLiteral, invocation);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static Task<Document> ConvertToDateTimeCtorAsync(
        Document document,
        SyntaxNode root,
        LiteralExpressionSyntax stringLiteral,
        string text,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();
        var dt = DateTime.Parse(text, CultureInfo.InvariantCulture);

        var ctor = SyntaxFactory.ParseExpression(
            $"new DateTime({dt.Year}, {dt.Month}, {dt.Day}, {dt.Hour}, {dt.Minute}, {dt.Second})")
            .WithTriviaFrom(stringLiteral
        );

        var newRoot = root.ReplaceNode(stringLiteral, ctor);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
