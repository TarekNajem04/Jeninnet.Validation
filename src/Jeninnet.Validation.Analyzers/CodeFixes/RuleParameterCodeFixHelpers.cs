//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

internal static class RuleParameterCodeFixHelpers {
    public static bool TryGetStringLiteral(SyntaxNode root, Diagnostic diagnostic, out LiteralExpressionSyntax literal, out string text) {
        literal = null!;
        text = string.Empty;

        // A literal argument often has the same full span as its AttributeArgumentSyntax; request
        // the innermost node so the literal itself is returned instead of its wrapper.
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        if(node is not LiteralExpressionSyntax stringLiteral ||
            !stringLiteral.IsKind(SyntaxKind.StringLiteralExpression)) {
            return false;
        }

        literal = stringLiteral;
        text = stringLiteral.Token.ValueText; // unescaped
        return true;
    }

    public static bool IsNumeric(string text) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _);

    public static bool LooksLikeDate(string text) =>
        DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    public static bool LooksLikeGuid(string text) =>
        Guid.TryParse(text, out _);

    public static bool LooksLikeBool(string text) =>
        text.Equals("true", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("false", StringComparison.OrdinalIgnoreCase);
}
