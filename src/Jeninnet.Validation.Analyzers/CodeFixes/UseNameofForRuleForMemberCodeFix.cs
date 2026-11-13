//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Rewrites a mismatched fluent <c>RuleFor</c> member name to <c>nameof(Model.Member)</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseNameofForRuleForMemberCodeFix))]
[Shared]
public sealed class UseNameofForRuleForMemberCodeFix : CodeFixProvider {
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.RuleForMemberNameMismatch.Id];

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var diagnostic = context.Diagnostics[0];

        if(!diagnostic.Properties.TryGetValue("MemberName", out var memberName) ||
           !diagnostic.Properties.TryGetValue("ModelType", out var modelType) ||
           string.IsNullOrWhiteSpace(memberName) ||
           string.IsNullOrWhiteSpace(modelType)) {
            return;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if(root is null) {
            return;
        }

        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var argument = node.AncestorsAndSelf().OfType<ArgumentSyntax>().FirstOrDefault();

        if(argument is null) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use nameof(...)",
                ct => ReplaceWithNameofAsync(context.Document, root, argument, modelType!, memberName!, ct),
                nameof(UseNameofForRuleForMemberCodeFix)),
            diagnostic);
    }

    private static Task<Document> ReplaceWithNameofAsync(
        Document document,
        SyntaxNode root,
        ArgumentSyntax argument,
        string modelType,
        string memberName,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        var replacement = SyntaxFactory
            .ParseExpression($"nameof({modelType}.{memberName})")
            .WithTriviaFrom(argument.Expression);

        var newRoot = root.ReplaceNode(argument.Expression, replacement);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
