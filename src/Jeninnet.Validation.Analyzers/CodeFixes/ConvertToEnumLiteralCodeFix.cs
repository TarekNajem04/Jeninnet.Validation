//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Converts a string literal that matches an enum member name into a qualified enum expression.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertToEnumLiteralCodeFix))]
[Shared]
public sealed class ConvertToEnumLiteralCodeFix : CodeFixProvider {
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

        if(!RuleParameterCodeFixHelpers.TryGetStringLiteral(root, diagnostic, out var literal, out var value)) {
            return;
        }

        // Very basic heuristic: looks like an identifier
        if(!IsIdentifierLike(value)) {
            return;
        }

        // Extract type name from the diagnostic message: "The value "Red" cannot be parsed as type "MyNamespace.Color" for rule "X"."
        if(!diagnostic.Properties.TryGetValue("TargetType", out var targetType) && string.IsNullOrEmpty(targetType)) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                $"Convert to enum literal ({targetType}.{value})",
                ct => ConvertToEnumAsync(context.Document, root, literal, targetType!, value, ct),
                nameof(ConvertToEnumLiteralCodeFix)
            ),
            diagnostic
        );
    }

    private static bool IsIdentifierLike(string value) {
        if(string.IsNullOrEmpty(value)) {
            return false;
        }

        if(!(char.IsLetter(value[0]) || value[0] == '_')) {
            return false;
        }

        return value.All(ch => char.IsLetterOrDigit(ch) || ch == '_');
    }

    private static Task<Document> ConvertToEnumAsync(
        Document document,
        SyntaxNode root,
        LiteralExpressionSyntax stringLiteral,
        string typeName,
        string memberName,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();
        var expr = SyntaxFactory.ParseExpression($"{typeName}.{memberName}")
            .WithTriviaFrom(stringLiteral);

        var newRoot = root.ReplaceNode(stringLiteral, expr);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
