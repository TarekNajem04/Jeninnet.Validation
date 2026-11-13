//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
namespace Jeninnet.Validation.Analyzers;

/// <summary>
/// Reports synchronous invocations of the sync-validation API
/// (<c>Jeninnet.Validation.Abstractions.ISyncValidator&lt;T&gt;.Validate</c>) on validator types
/// marked with <c>Jeninnet.Validation.AsyncOnlyValidatorAttribute</c>, which the runtime
/// guard rejects with <c>Jeninnet.Validation.AsyncValidatorInvokedSynchronouslyException</c>.
/// </summary>
/// <remarks>
/// The analyzer never diverges from the runtime contract: it warns exactly where
/// <c>CompositeValidator</c> would throw. Validators whose rule composition is not
/// statically known (fluent-authored, custom-composed) rely on the runtime guard.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SyncValidateAnalyzer : DiagnosticAnalyzer {
    private const string SYNC_VALIDATOR_METADATA_NAME = "Jeninnet.Validation.Abstractions.ISyncValidator`1";
    private const string ASYNC_ONLY_VALIDATOR_METADATA_NAME = "Jeninnet.Validation.AsyncOnlyValidatorAttribute";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.SyncValidateOnAsyncOnlyValidator];

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

        if(invocation.Expression is not MemberAccessExpressionSyntax memberAccess) {
            return;
        }

        var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
        if(receiverType is null) {
            return;
        }

        var markerType = context.Compilation.GetTypeByMetadataName(ASYNC_ONLY_VALIDATOR_METADATA_NAME);
        if(markerType is null || !IsMarkedAsyncOnly(receiverType, markerType)) {
            return;
        }

        if(context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
           is not { } symbol
        ) {
            return;
        }

        if(symbol is not IMethodSymbol method || !IsSyncValidatorValidate(context.Compilation, method)) {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.SyncValidateOnAsyncOnlyValidator,
                invocation.GetLocation(),
                receiverType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            )
        );
    }

    private static bool IsMarkedAsyncOnly(ITypeSymbol receiverType, INamedTypeSymbol markerType) =>
        receiverType.GetAttributes()
                    .Any(
                        attribute =>
                            SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, markerType)
                    );

    private static bool IsSyncValidatorValidate(Compilation compilation, IMethodSymbol method) {
        var syncValidator =
            compilation.GetTypeByMetadataName(SYNC_VALIDATOR_METADATA_NAME);
        if(syncValidator is null) {
            return false;
        }

        var declaring =
            method.OriginalDefinition.ContainingType ??
            method.ContainingType.OriginalDefinition;
        if(SymbolEqualityComparer.Default.Equals(declaring, syncValidator)) {
            return true;
        }

        // Invocations through a concrete class resolve to the implemented method;
        // the contract member is still ISyncValidator<T>.Validate.
        return method.ContainingType
                     .AllInterfaces
                     .Any(
                         implemented =>
                             SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, syncValidator)
                     ) &&
             method.OriginalDefinition.Name is "Validate";
    }
}
