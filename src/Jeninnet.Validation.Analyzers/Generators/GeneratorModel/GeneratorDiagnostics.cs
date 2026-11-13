//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.GeneratorModel;

/// <summary>
/// Provides helper methods for reporting diagnostics from the source generator.
/// </summary>
public static class GeneratorDiagnostics {
    /// <summary>
    /// Reports a VAL0001 diagnostic when a rule parameter value cannot be parsed to the expected type.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="compilation">The compilation being analyzed.</param>
    /// <param name="location">The location of the diagnostic.</param>
    /// <param name="rawValue">The raw value that could not be parsed.</param>
    /// <param name="targetType">The target type that the value could not be parsed to.</param>
    /// <param name="ruleName">The name of the rule.</param>
    public static void ReportInvalidRuleParameter(
        SourceProductionContext context,
        Compilation compilation,
        Location location,
        string rawValue,
        ITypeSymbol targetType,
        string ruleName
    ) {
        if(compilation is null) {
            throw new ArgumentNullException(nameof(compilation));
        }

        if(targetType is null) {
            throw new ArgumentNullException(nameof(targetType));
        }

        if(ruleName is null) {
            throw new ArgumentNullException(nameof(ruleName));
        }

        if(location is null ||
            location.SourceTree is null ||
            !compilation.ContainsSyntaxTree(location.SourceTree)) {
            // Fall back to a diagnostic with no location
            location = Location.None;
        }

        var props = ImmutableDictionary<string, string?>
                        .Empty
                        .Add("RawValue", rawValue)
                        .Add("TargetType", targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                        .Add("RuleName", ruleName);
        var diag = Diagnostic.Create(
            DiagnosticDescriptors.InvalidRuleParameter,
            location,
            props,
            rawValue,
            targetType.ToDisplayString(),
            ruleName);

        context.ReportDiagnostic(diag);
    }
}
