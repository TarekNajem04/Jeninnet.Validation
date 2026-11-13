//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
namespace Jeninnet.Validation.Analyzers;

/// <summary>
/// Reports fluent <c>RuleFor(x => x.Member, "OtherMember")</c> calls whose textual member name
/// identifies a different member of the model. Only provable mismatches are reported: the string
/// must be a compile-time constant and must match an actual property or field on the model.
/// Custom paths and free-form names are left alone.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RuleForMemberNameAnalyzer : DiagnosticAnalyzer {
    private const string RULE_BUILDER_NAMESPACE = "Jeninnet.Validation.Fluent";
    private const string RULE_BUILDER_NAME = "ValidatorRuleBuilder";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.RuleForMemberNameMismatch];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        if(context is null) {
            throw new ArgumentNullException(nameof(context));
        }

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if(invocation.Expression is not MemberAccessExpressionSyntax ||
           invocation.ArgumentList.Arguments.Count is not 2) {
            return;
        }

        if(context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method ||
           !IsRuleFor(method) ||
           method.ContainingType.TypeArguments.Length is not 1) {
            return;
        }

        var modelType = method.ContainingType.TypeArguments[0];

        if(modelType is not INamedTypeSymbol namedModel) {
            return;
        }

        var lambda = TryGetSingleParameterLambda(invocation.ArgumentList.Arguments[0].Expression);

        if(lambda is null) {
            return;
        }

        var parameterName = GetParameterName(lambda);

        if(parameterName is null ||
           Unwrap(lambda.Body) is not ExpressionSyntax body ||
           Unwrap(body) is not MemberAccessExpressionSyntax memberAccess ||
           memberAccess.Expression is not IdentifierNameSyntax receiver ||
           !string.Equals(receiver.Identifier.ValueText, parameterName, StringComparison.Ordinal)) {
            return;
        }

        var selectedMember = context.SemanticModel
            .GetSymbolInfo(memberAccess, context.CancellationToken)
            .Symbol;

        if(selectedMember is not IPropertySymbol and not IFieldSymbol) {
            return;
        }

        var constant = context.SemanticModel.GetConstantValue(
            invocation.ArgumentList.Arguments[1].Expression,
            context.CancellationToken);

        if(!constant.HasValue || constant.Value is not string literal || !IsSimpleMemberPath(literal)) {
            return;
        }

        if(string.Equals(literal, selectedMember.Name, StringComparison.Ordinal) ||
           !MemberExists(namedModel, literal)) {
            return;
        }

        var modelTypeName = namedModel.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var properties = ImmutableDictionary<string, string?>.Empty
            .Add("MemberName", selectedMember.Name)
            .Add("ModelType", modelTypeName);

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.RuleForMemberNameMismatch,
                invocation.ArgumentList.Arguments[1].Expression.GetLocation(),
                properties,
                literal,
                selectedMember.Name,
                modelTypeName));
    }

    private static bool IsRuleFor(IMethodSymbol method) =>
        method.Name is "RuleFor" &&
        method.ContainingType is { Name: RULE_BUILDER_NAME } containingType &&
        containingType.ContainingNamespace.ToDisplayString() is RULE_BUILDER_NAMESPACE;

    private static LambdaExpressionSyntax? TryGetSingleParameterLambda(ExpressionSyntax expression) =>
        Unwrap(expression) switch {
            SimpleLambdaExpressionSyntax simple => simple,
            ParenthesizedLambdaExpressionSyntax parenthesized when parenthesized.ParameterList.Parameters.Count is 1
                => parenthesized,
            _ => null
        };

    private static string? GetParameterName(LambdaExpressionSyntax lambda) =>
        lambda switch {
            SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.ValueText,
            ParenthesizedLambdaExpressionSyntax parenthesized
                when parenthesized.ParameterList.Parameters[0].Identifier.ValueText is { Length: > 0 } name => name,
            _ => null
        };

    private static bool IsSimpleMemberPath(string value) =>
        value.Length > 0 &&
        value.IndexOf('.') < 0 &&
        value.IndexOf('[') < 0 &&
        !value.Any(char.IsWhiteSpace);

    private static bool MemberExists(INamedTypeSymbol modelType, string name) {
        for(var current = modelType; current is not null; current = current.BaseType) {
            if(current.GetMembers(name).Any(static member =>
                   member.Kind is SymbolKind.Property or SymbolKind.Field)) {
                return true;
            }
        }

        return false;
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression) {
        while(true) {
            switch(expression) {
                case ParenthesizedExpressionSyntax parenthesized:
                    expression = parenthesized.Expression;
                    continue;

                case PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppress:
                    expression = suppress.Operand;
                    continue;

                default:
                    return expression;
            }
        }
    }

    private static CSharpSyntaxNode Unwrap(CSharpSyntaxNode node) =>
        node is ExpressionSyntax expression ? Unwrap(expression) : node;
}
