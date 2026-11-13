//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Removes a conflicting validation rule from a property to resolve a rule conflict diagnostic.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveConflictingRuleCodeFix))]
[Shared]
public sealed class RemoveConflictingRuleCodeFix : CodeFixProvider {
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.InvalidRuleCombination.Id, DiagnosticDescriptors.ConflictingRules.Id];

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

        var rule1 = string.Empty;
        var rulesCsv = string.Empty;

        if(!diagnostic.Properties.TryGetValue("Rule1", out rule1) &&
            !diagnostic.Properties.TryGetValue("Rules", out rulesCsv)) {
            return;
        }

        foreach(var rule in rulesCsv?.Split(',') ?? new[] { rule1 }) {
            if(rule is null) {
                continue;
            }

            var attribute = root.FindNode(diagnostic.Location.SourceSpan)
                .AncestorsAndSelf()
                .OfType<AttributeSyntax>()
                .FirstOrDefault(candidate => candidate.Name.ToString().Contains(rule));

            if(attribute is null) {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Remove [{rule}]",
                    ct => RemoveAttributeAsync(context.Document, root, attribute, ct),
                    rule),
                diagnostic);
        }
    }

    private static Task<Document> RemoveAttributeAsync(
        Document document,
        SyntaxNode root,
        AttributeSyntax attribute,
        CancellationToken cancellationToken
    ) {
        cancellationToken.ThrowIfCancellationRequested();

        var newRoot = root.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia);
        return Task.FromResult(newRoot is null ? document : document.WithSyntaxRoot(newRoot));
    }
}
