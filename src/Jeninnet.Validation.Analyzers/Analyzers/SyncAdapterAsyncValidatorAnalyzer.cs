//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
namespace Jeninnet.Validation.Analyzers;

/// <summary>
/// Reports async-only validators wired into the synchronous DataAnnotations adapter through
/// <c>[JeninnetValidatable]</c> or <c>[JeninnetValidateProperty]</c>. These attributes carry the
/// validator type, so the mismatch is provable without whole-program analysis. Blazor, Observable,
/// and DI-resolved paths do not expose the validator type statically and remain covered by
/// <c>VAL0016</c> and the runtime guard.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SyncAdapterAsyncValidatorAnalyzer : DiagnosticAnalyzer {
    private const string DATA_ANNOTATIONS_NAMESPACE = "Jeninnet.Validation.DataAnnotations";
    private const string VALIDATABLE_ATTRIBUTE = "JeninnetValidatableAttribute";
    private const string PROPERTY_ATTRIBUTE = "JeninnetValidatePropertyAttribute";
    private const string ASYNC_ONLY_ATTRIBUTE = "Jeninnet.Validation.AsyncOnlyValidatorAttribute";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.AsyncOnlyValidatorInSyncAdapter];

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
        var attribute = (AttributeSyntax)context.Node;
        var attributeType = context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken)
            .Symbol?
            .ContainingType;

        if(attributeType?.ContainingNamespace.ToDisplayString()
               .Equals(DATA_ANNOTATIONS_NAMESPACE, StringComparison.Ordinal) != true) {
            return;
        }

        var validatorType = GetValidatorType(attribute, attributeType, context.SemanticModel, context.CancellationToken);

        if(validatorType is null || !IsAsyncOnly(validatorType, context.Compilation)) {
            return;
        }

        var adapterName = attributeType.Name is VALIDATABLE_ATTRIBUTE
            ? "JeninnetValidatable"
            : "JeninnetValidateProperty";

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.AsyncOnlyValidatorInSyncAdapter,
                attribute.GetLocation(),
                validatorType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                adapterName));
    }

    private static ITypeSymbol? GetValidatorType(
        AttributeSyntax attribute,
        INamedTypeSymbol attributeType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken) {
        if(attributeType.Arity is 1 &&
           string.Equals(attributeType.Name, PROPERTY_ATTRIBUTE, StringComparison.Ordinal)) {
            return attributeType.TypeArguments[0];
        }

        if(!string.Equals(attributeType.Name, VALIDATABLE_ATTRIBUTE, StringComparison.Ordinal) &&
           !string.Equals(attributeType.Name, PROPERTY_ATTRIBUTE, StringComparison.Ordinal)) {
            return null;
        }

        if(attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is not TypeOfExpressionSyntax typeOf) {
            return null;
        }

        return semanticModel.GetTypeInfo(typeOf.Type, cancellationToken).Type;
    }

    private static bool IsAsyncOnly(ITypeSymbol validatorType, Compilation compilation) {
        var marker = compilation.GetTypeByMetadataName(ASYNC_ONLY_ATTRIBUTE);

        return marker is not null &&
               validatorType.GetAttributes().Any(attribute =>
                   SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, marker));
    }
}
