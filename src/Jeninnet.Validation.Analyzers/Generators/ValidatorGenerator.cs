//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
namespace Jeninnet.Validation.Generators;

/// <summary>
/// Roslyn incremental source generator that produces validators
/// based on validation attributes.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ValidatorGenerator : IIncrementalGenerator {
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var validatorDescriptors =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeNames.VALIDATE,
                    static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax or InterfaceDeclarationSyntax,
                    static (ctx, _) => ValidatorMetadataBuilder.TryBuildValidatorDescriptor((INamedTypeSymbol)ctx.TargetSymbol)
                )
                .Where(static d => d is not null)
                .WithTrackingName("ValidatorDescriptors");

        var emitClientRules = context.AnalyzerConfigOptionsProvider.Select(
            static (provider, _) =>
                provider.GlobalOptions.TryGetValue("build_property.JeninnetEmitClientRules", out var value) &&
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

        // Per-validator generation depends only on the validator descriptor. Rule-combination
        // diagnostics and assembly aggregation still need the compilation, so they run in separate
        // outputs and do not invalidate unrelated validator sources.
        context.RegisterSourceOutput(
            validatorDescriptors,
            static (ctx, descriptor) => EmitValidator(ctx, descriptor));

        var compilationAndValidators = context.CompilationProvider
            .Combine(validatorDescriptors.Collect())
            .Combine(emitClientRules);

        context.RegisterSourceOutput(
            compilationAndValidators,
            static (ctx, pair) => ValidateRuleCombinations(ctx, pair.Left.Left, pair.Left.Right));

        context.RegisterSourceOutput(
            compilationAndValidators,
            static (ctx, pair) => EmitAssemblySources(ctx, pair.Left.Left, pair.Left.Right, pair.Right));
    }

    private static void EmitValidator(
        SourceProductionContext context,
        ValidatorDescriptor? descriptor
    ) {
        if(descriptor is null) {
            return;
        }

        ReportDiagnostics(context, descriptor);

        if(!descriptor.ShouldEmit) {
            return;
        }

        EmitValidatorSources(context, descriptor);
    }

    private static void ValidateRuleCombinations(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ValidatorDescriptor?> descriptors
    ) {
        foreach(var descriptor in descriptors) {
            if(descriptor?.ShouldEmit != true) {
                continue;
            }

            foreach(var prop in descriptor.Properties) {
                RuleCombinationValidator.ValidatePropertyRules(
                    context,
                    compilation,
                    prop.Name,
                    prop.Type,
                    prop.Rules,
                    prop.Location,
                    prop.Property
                );
            }
        }
    }

    private static void ReportDiagnostics(SourceProductionContext context, ValidatorDescriptor descriptor) {
        if(descriptor.Diagnostics is not { Count: > 0 }) {
            return;
        }

        foreach(var diagnostic in descriptor.Diagnostics) {
            context.ReportDiagnostic(Diagnostic.Create(
                diagnostic.Descriptor,
                diagnostic.Location,
                diagnostic.Arguments));
        }
    }

    private static void EmitValidatorSources(
        SourceProductionContext context,
        ValidatorDescriptor descriptor) {
        var className = descriptor.TargetTypeName.Replace("global::", "").Replace(".", "_") + "Validator";
        context.AddSource($"{className}.g.cs", ValidatorEmitter.Emit(descriptor));

        if(descriptor.GenerateModelExtensions &&
           InjectedValidatorCollector.Collect(descriptor).Count is 0) {
            context.AddSource($"{className}.Extensions.g.cs", ModelExtensionsEmitter.Emit(descriptor));
        }
    }

    private static void EmitAssemblySources(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ValidatorDescriptor?> descriptors,
        bool emitClientRules) {
        var validDescriptors = new List<ValidatorDescriptor>(descriptors.Length);

        foreach(var descriptor in descriptors) {
            if(descriptor?.ShouldEmit == true) {
                validDescriptors.Add(descriptor);
            }
        }

        if(validDescriptors.Count > 0 &&
           compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection") is not null) {
            var assemblyName = compilation.AssemblyName ?? "Generated";
            context.AddSource(
                "GeneratedValidatorRegistry.g.cs",
                GeneratedValidatorRegistryEmitter.Emit(validDescriptors, assemblyName));
        }

        if(emitClientRules) {
            context.AddSource("ClientRulesCatalog.g.cs", ClientRulesCatalogEmitter.Emit(validDescriptors));
        }
    }
}
