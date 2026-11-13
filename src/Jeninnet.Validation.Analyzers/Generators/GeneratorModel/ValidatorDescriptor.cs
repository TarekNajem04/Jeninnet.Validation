//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.GeneratorModel;

/// <summary>
/// Describes a validator to be generated.
/// </summary>
/// <param name="TargetNamespace">The target namespace for the generated validator.</param>
/// <param name="TargetTypeName">The target type name for the generated validator.</param>
/// <param name="Properties">The properties of the validator.</param>
/// <param name="ModelRules">Rules that target the whole model rather than a member.</param>
/// <param name="PreValidateExpression">Optional fully qualified pre-validation hook method group.</param>
/// <param name="PreValidateHasErrors">Whether the pre-validation hook accepts a collector.</param>
/// <param name="OnFailureExpression">Optional fully qualified failure hook method group.</param>
/// <param name="Diagnostics">Metadata diagnostics to report during generation.</param>
/// <param name="Sanitizers">Preprocessing steps from <c>[Sanitize]</c> attributes. Stored separately
/// from rules so sanitization never appears in descriptors or client payloads.</param>
/// <param name="GenerateModelExtensions">Whether to emit model-level <c>Validate</c> extensions.</param>
internal sealed record ValidatorDescriptor(
    string? TargetNamespace,
    string TargetTypeName,
    IReadOnlyList<ValidatorPropertyDescriptor> Properties,
    IReadOnlyList<ValidationRuleDescriptor>? ModelRules = null,
    string? PreValidateExpression = null,
    bool PreValidateHasErrors = false,
    string? OnFailureExpression = null,
    IReadOnlyList<GeneratorDiagnostic>? Diagnostics = null,
    IReadOnlyList<SanitizeStepModel>? Sanitizers = null,
    bool GenerateModelExtensions = true
) {
    /// <summary>
    /// Gets a value indicating whether the generator can emit a validator for this target.
    /// Unsupported targets still carry diagnostics so the build fails with a clear message.
    /// </summary>
    public bool ShouldEmit { get; init; } = true;

    public IEnumerable<ValidationRuleDescriptor> Rules => Properties.SelectMany(p => p.Rules);
}

/// <summary>
/// A diagnostic produced while building validator metadata.
/// </summary>
/// <param name="Descriptor">The descriptor that identifies the diagnostic.</param>
/// <param name="Location">The source location associated with the diagnostic, when available.</param>
/// <param name="Arguments">The message format arguments for the diagnostic.</param>
internal sealed record GeneratorDiagnostic(
    DiagnosticDescriptor Descriptor,
    Location? Location,
    object?[] Arguments
);
