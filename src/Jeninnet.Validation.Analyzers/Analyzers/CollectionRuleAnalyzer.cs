//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
namespace Jeninnet.Validation.Analyzers;

/// <summary>
/// Reports <c>Count</c>, <c>MinCount</c>, and <c>MaxCount</c> rules applied to targets that are
/// statically known to be non-collections. Strings, numeric values, and other scalar types cannot
/// satisfy collection cardinality rules. Unknown targets are ignored to avoid false positives.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CollectionRuleAnalyzer : DiagnosticAnalyzer {
    private const string COUNT_EXTENSIONS = "Jeninnet.Validation.Fluent.RuleBuilderCountExtensions";
    private const string RULE_BUILDER_INTERFACE = "Jeninnet.Validation.Fluent.IRuleBuilder`2";
    private const string ATTRIBUTE_NAMESPACE = "Jeninnet.Validation.Attributes";

    private static readonly ImmutableHashSet<string> _ruleNames =
        ImmutableHashSet.Create(StringComparer.Ordinal, "Count", "MinCount", "MaxCount");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.CollectionRuleRequiresCollection];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        if(context is null) {
            throw new ArgumentNullException(nameof(context));
        }

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if(invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
           context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method ||
           !_ruleNames.Contains(method.Name) ||
           method.ContainingType.ToDisplayString() is not COUNT_EXTENSIONS) {
            return;
        }

        var receiverType = context.SemanticModel.GetTypeInfo(
            memberAccess.Expression,
            context.CancellationToken).Type;

        if(receiverType is null) {
            return;
        }

        var propertyType = GetRuleBuilderPropertyType(receiverType, context.Compilation);

        if(propertyType is null || CollectionTypeHelper.IsCollection(propertyType, context.Compilation) is not false) {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.CollectionRuleRequiresCollection,
                invocation.GetLocation(),
                method.Name,
                propertyType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context) {
        var attribute = (AttributeSyntax)context.Node;
        var attributeType = context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken)
            .Symbol?
            .ContainingType;

        if(attributeType?.ContainingNamespace.ToDisplayString().StartsWith(ATTRIBUTE_NAMESPACE, StringComparison.Ordinal) != true) {
            return;
        }

        var ruleName = attributeType.Name.EndsWith("Attribute", StringComparison.Ordinal)
            ? attributeType.Name.Substring(0, attributeType.Name.Length - "Attribute".Length)
            : attributeType.Name;

        if(!_ruleNames.Contains(ruleName) ||
           attribute.Parent?.Parent is not PropertyDeclarationSyntax propertySyntax ||
           context.SemanticModel.GetDeclaredSymbol(propertySyntax, context.CancellationToken) is not IPropertySymbol property) {
            return;
        }

        if(CollectionTypeHelper.IsCollection(property.Type, context.Compilation) is not false) {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.CollectionRuleRequiresCollection,
                attribute.GetLocation(),
                ruleName,
                property.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static ITypeSymbol? GetRuleBuilderPropertyType(ITypeSymbol receiverType, Compilation compilation) {
        var ruleBuilder = compilation.GetTypeByMetadataName(RULE_BUILDER_INTERFACE);

        if(ruleBuilder is null || receiverType is not INamedTypeSymbol named) {
            return null;
        }

        if(SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, ruleBuilder)) {
            return named.TypeArguments[1];
        }

        var implemented = named.AllInterfaces.FirstOrDefault(interfaceType =>
            SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, ruleBuilder));

        return implemented?.TypeArguments[1];
    }
}
