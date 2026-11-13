//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Rewrites a synchronous <c>Validate</c> call on an <c>[AsyncOnlyValidator]</c> to
/// <c>await ValidateAsync</c>. Registered only when the enclosing method or local function is
/// already <c>async</c>; broader method-shape rewrites are intentionally out of scope.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseValidateAsyncCodeFix))]
[Shared]
public sealed class UseValidateAsyncCodeFix : CodeFixProvider {
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.SyncValidateOnAsyncOnlyValidator.Id];

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

        var invocation = root.FindNode(diagnostic.Location.SourceSpan)
                              .FirstAncestorOrSelf<InvocationExpressionSyntax>();

        if(invocation?.Expression is not MemberAccessExpressionSyntax) {
            return;
        }

        if(invocation.Parent is AwaitExpressionSyntax) {
            return;
        }

        if(!IsInAsyncContext(invocation)) {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if(semanticModel is null) {
            return;
        }

        if(semanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol { Name: "Validate" }) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use ValidateAsync",
                cancellationToken => RewriteAsync(context.Document, invocation, cancellationToken),
                equivalenceKey: nameof(UseValidateAsyncCodeFix)),
            diagnostic);
    }

    private static bool IsInAsyncContext(SyntaxNode node) {
        foreach(var ancestor in node.Ancestors()) {
            switch(ancestor) {
                case MethodDeclarationSyntax method:
                    return method.Modifiers.Any(SyntaxKind.AsyncKeyword);
                case LocalFunctionStatementSyntax localFunction:
                    return localFunction.Modifiers.Any(SyntaxKind.AsyncKeyword);
                case AnonymousFunctionExpressionSyntax:
                case AccessorDeclarationSyntax:
                    return false;
            }
        }

        return false;
    }

    private static async Task<Document> RewriteAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken) {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if(root is null || invocation.Expression is not MemberAccessExpressionSyntax memberAccess) {
            return document;
        }

        var renamedName = SyntaxFactory.IdentifierName("ValidateAsync").WithTriviaFrom(memberAccess.Name);
        var renamedInvocation = invocation
            .WithExpression(memberAccess.WithName(renamedName))
            .WithoutLeadingTrivia()
            .WithoutTrailingTrivia();

        var awaitKeyword = SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
                                        .WithLeadingTrivia(invocation.GetLeadingTrivia())
                                        .WithTrailingTrivia(SyntaxFactory.Space);

        var awaited = SyntaxFactory.AwaitExpression(
                                  awaitKeyword,
                                  SyntaxFactory.ParenthesizedExpression(renamedInvocation))
                                  .WithTrailingTrivia(invocation.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(invocation, awaited);
        return document.WithSyntaxRoot(newRoot);
    }
}
