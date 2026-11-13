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
                .Where(static d => d is not null);

        var emitClientRules = context.AnalyzerConfigOptionsProvider.Select(
            static (provider, _) =>
                provider.GlobalOptions.TryGetValue("build_property.JeninnetEmitClientRules", out var value) &&
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

        var compilationAndValidators = context.CompilationProvider
            .Combine(validatorDescriptors.Collect())
            .Combine(emitClientRules);

        context.RegisterSourceOutput(
            compilationAndValidators,
            static (ctx, pair) => EmitValidator(ctx, pair.Left.Left, pair.Left.Right, pair.Right));
    }

    private static void EmitValidator(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ValidatorDescriptor?> descriptors,
        bool emitClientRules
    ) {
        if(descriptors.Length == 0) {
            return;
        }

        var validDescriptors = new List<ValidatorDescriptor>(descriptors.Length);

        foreach(var descriptor in descriptors) {
            if(descriptor is null) {
                continue;
            }

            ReportDiagnostics(context, descriptor);

            if(!descriptor.ShouldEmit) {
                continue;
            }

            validDescriptors.Add(descriptor);
            EmitValidatorSources(context, compilation, descriptor);
        }

        EmitAssemblySources(context, compilation, validDescriptors, emitClientRules);
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
        Compilation compilation,
        ValidatorDescriptor descriptor) {
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
        List<ValidatorDescriptor> validDescriptors,
        bool emitClientRules) {
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
