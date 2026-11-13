//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
namespace Jeninnet.Validation.Analyzers;

/// <summary>
/// Analyzes validated properties and suggests additional validation rules
/// (e.g., adding [NotEmpty] for strings or [NotNull] for nullable types).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RuleSuggestionAnalyzer : DiagnosticAnalyzer {
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.SuggestNotEmptyForString, DiagnosticDescriptors.SuggestNotNullForNullable
];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        if(context is null) {
            throw new ArgumentNullException(nameof(context));
        }

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeProperty(SyntaxNodeAnalysisContext context) {
        var propertySyntax = (PropertyDeclarationSyntax)context.Node;
        if(context.SemanticModel.GetDeclaredSymbol(propertySyntax, context.CancellationToken) is not IPropertySymbol propertySymbol) {
            return;
        }

        var propertyType = propertySymbol.Type;

        var attributes = propertySymbol.GetAttributes();
        var hasValidationAttributes = attributes.Any(a => IsValidationAttribute(a.AttributeClass));

        if(!hasValidationAttributes) {
            return;
        }

        // Suggest NotEmpty for string
        if(propertyType.SpecialType == SpecialType.System_String) {
            var hasNotEmpty = attributes.Any(a => a.AttributeClass?.Name is (AttributeNames.SIMPLE_NOT_EMPTY + "Attribute") or "NotBlankAttribute");
            if(!hasNotEmpty) {
                var diag = Diagnostic.Create(
                    DiagnosticDescriptors.SuggestNotEmptyForString,
                    propertySyntax.Identifier.GetLocation(),
                    propertySymbol.Name);

                context.ReportDiagnostic(diag);
            }
        }

        // Suggest NotNull for nullable types with other rules
        if(IsNullable(propertyType)) {
            var hasNotNull = attributes.Any(a => a.AttributeClass?.Name is (AttributeNames.SIMPLE_NOT_NULL + "Attribute"));
            if(!hasNotNull) {
                var diag = Diagnostic.Create(
                    DiagnosticDescriptors.SuggestNotNullForNullable,
                    propertySyntax.Identifier.GetLocation(),
                    propertySymbol.Name);

                context.ReportDiagnostic(diag);
            }
        }
    }

    private static bool IsValidationAttribute(INamedTypeSymbol? attrType) {
        if(attrType is null) {
            return false;
        }

        var name = attrType.Name;
        return name.EndsWith("Attribute", StringComparison.Ordinal) &&
               attrType.ContainingNamespace.ToDisplayString().Contains("Validation", StringComparison.Ordinal);
    }

    private static bool IsNullable(ITypeSymbol type) =>
        (type is INamedTypeSymbol named &&
        named.IsGenericType &&
        named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        || (type.IsReferenceType && type.NullableAnnotation is NullableAnnotation.Annotated);
}
