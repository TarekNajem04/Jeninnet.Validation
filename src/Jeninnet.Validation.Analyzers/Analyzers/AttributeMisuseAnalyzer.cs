//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
namespace Jeninnet.Validation.Analyzers;

/// <summary>
/// Analyzes validation attribute usage and reports diagnostics for incompatible types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AttributeMisuseAnalyzer : DiagnosticAnalyzer {
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.RuleAppliedToIncompatibleType];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        if(context is null) {
            throw new ArgumentNullException(nameof(context));
        }

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context) {
        var attributeSyntax = (AttributeSyntax)context.Node;
        var attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax, context.CancellationToken).Symbol as IMethodSymbol;

        if(attributeSymbol?.ContainingType is not INamedTypeSymbol attributeType) {
            return;
        }

        // We care only about our validation attributes
        if(!IsValidationAttribute(attributeType)) {
            return;
        }

        // Find the property this attribute is applied to
        if(attributeSyntax.Parent?.Parent is not PropertyDeclarationSyntax propertySyntax) {
            return;
        }

        if(context.SemanticModel.GetDeclaredSymbol(propertySyntax, context.CancellationToken) is not IPropertySymbol propertySymbol) {
            return;
        }

        const string ATTRIBUTE_SUFFIX = "Attribute";
        var propertyType = propertySymbol.Type;
        // Map attribute to rule name (without "Attribute" suffix)
        var attributeName = attributeType.Name;
        var ruleName = attributeName.EndsWith(ATTRIBUTE_SUFFIX, StringComparison.Ordinal)
            ? attributeName.Substring(0, attributeName.Length - ATTRIBUTE_SUFFIX.Length)
            : attributeName;

        if(IsMisused(ruleName, propertyType, context.Compilation, out var reason)) {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.RuleAppliedToIncompatibleType,
                attributeSyntax.GetLocation(),
                ruleName,
                propertySymbol.Name,
                propertyType.ToDisplayString(),
                string.IsNullOrEmpty(reason) ? ", [Misuse]" : reason
            );

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsValidationAttribute(INamedTypeSymbol attributeType) {
        // Name-based check for backward compatibility and tests
        if(IsValidationAttributeName(attributeType.Name)) {
            return true;
        }

        // Hierarchy-based check
        var baseType = attributeType.BaseType;
        while(baseType is not null) {
            if(baseType.Name == "ValidationAttribute" && baseType.ContainingNamespace?.ToDisplayString() == "Jeninnet.Validation.Attributes") {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool IsValidationAttributeName(string attributeName) =>
        Enum.GetValues(typeof(RuleKind))
            .OfType<RuleKind>()
            .Any(x => x.GetAttributeName(removeSuffix: false)
            .Contains(attributeName, StringComparison.OrdinalIgnoreCase)) ||
        attributeName.Equals(nameof(RuleKind.MaximumLength), StringComparison.OrdinalIgnoreCase) ||
        attributeName.Equals(nameof(RuleKind.MinimumLength), StringComparison.OrdinalIgnoreCase);

    private static bool IsMisused(string ruleName, ITypeSymbol propertyType, Compilation compilation, out string reason) {
        if(CommonMisuseValidator.IsMisusedNotEmpty(propertyType, compilation, ruleName, out reason)) {
            return true;
        }

        if(CommonMisuseValidator.IsMisusedNotNull(propertyType, ruleName, out reason)) {
            return true;
        }

        if(StringMisuseValidator.IsMisusedLength(propertyType, ruleName, out reason)) {
            return true;
        }

        if(CommonMisuseValidator.IsMisusedValidateEach(propertyType, compilation, ruleName, out reason)) {
            return true;
        }

        if(NestedMisuseValidator.IsMisused(propertyType, ruleName, out reason)) {
            return true;
        }

        if(ComparisonMisuseValidator.IsMisused(propertyType, compilation, ruleName, out reason)) {
            return true;
        }

        if(DateTimeMisuseValidator.IsMisused(propertyType, ruleName, out reason)) {
            return true;
        }

        return false;
    }
}
