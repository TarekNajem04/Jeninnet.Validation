//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
namespace Jeninnet.Validation.Analyzers;

/// <summary>
/// Analyzes validation rule declarations and reports diagnostics for invalid rule configurations.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValidationRuleAnalyzer : DiagnosticAnalyzer {
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.InvalidRuleParameter,
            DiagnosticDescriptors.RuleAppliedToIncompatibleType,
            DiagnosticDescriptors.RuleParameterTypeMismatch,
            DiagnosticDescriptors.NestedValidatorNotFound,
            DiagnosticDescriptors.CollectionElementValidatorNotFound,
            DiagnosticDescriptors.RuleNotAllowedOnNullableType,
            DiagnosticDescriptors.ConflictingRules,
            DiagnosticDescriptors.UnsupportedAttributeArgumentType,
            DiagnosticDescriptors.InvalidRuleCombination,
            DiagnosticDescriptors.RuleParameterOutOfRange,
            DiagnosticDescriptors.SuggestNotEmptyForString,
            DiagnosticDescriptors.SuggestNotNullForNullable
        );

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        if(context is null) {
            throw new ArgumentNullException(nameof(context));
        }
        // For now, we just enable concurrent execution and configure analysis.
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // You can register syntax or symbol actions here later.
        // e.g., context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute)
    }
}
