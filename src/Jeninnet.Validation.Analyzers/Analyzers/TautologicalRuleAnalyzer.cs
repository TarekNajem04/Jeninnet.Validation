//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
namespace Jeninnet.Validation.Analyzers;

/// <summary>
/// Reports validation rules whose bounds cover the member's entire value range, making the rule a
/// tautology (always satisfied). Only the provable subset is reported: length lower/upper extremes
/// and inclusive/comparison bounds at the numeric type's extremes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TautologicalRuleAnalyzer : DiagnosticAnalyzer {
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.TautologicalRule];

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
        if(context.SemanticModel.GetDeclaredSymbol(propertySyntax, context.CancellationToken) is not IPropertySymbol property) {
            return;
        }

        var propertyType = UnwrapNullable(property.Type);

        foreach(var attribute in property.GetAttributes()) {
            if(!IsValidationAttribute(attribute.AttributeClass)) {
                continue;
            }

            if(!IsTautological(attribute, propertyType, out var ruleName)) {
                continue;
            }

            var attributeSyntax = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken) as AttributeSyntax;
            var location = attributeSyntax?.GetLocation() ?? propertySyntax.Identifier.GetLocation();

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.TautologicalRule,
                    location,
                    ruleName,
                    property.Name));
        }
    }

    private static bool IsTautological(AttributeData attribute, ITypeSymbol propertyType, out string ruleName) {
        ruleName = attribute.AttributeClass?.Name.Replace("Attribute", string.Empty) ?? "rule";

        return attribute.AttributeClass?.Name switch {
            "MinimumLengthAttribute" => IsString(propertyType) &&
                                        TryGetInt(attribute, 0, out var minimum) &&
                                        minimum == 0,
            "MaximumLengthAttribute" => IsString(propertyType) &&
                                        TryGetInt(attribute, 0, out var maximum) &&
                                        maximum == int.MaxValue,
            "LengthAttribute" => IsString(propertyType) &&
                                 TryGetInt(attribute, 0, out var lengthMinimum) &&
                                 TryGetInt(attribute, 1, out var lengthMaximum) &&
                                 lengthMinimum == 0 &&
                                 lengthMaximum == int.MaxValue,
            "InclusiveBetweenAttribute" => IsFullRange(attribute, propertyType, FullRangeRule.InclusiveBetween),
            "GreaterThanOrEqualAttribute" => IsFullRange(attribute, propertyType, FullRangeRule.GreaterThanOrEqual),
            "LessThanOrEqualAttribute" => IsFullRange(attribute, propertyType, FullRangeRule.LessThanOrEqual),
            _ => false
        };
    }

    private static bool IsFullRange(AttributeData attribute, ITypeSymbol propertyType, FullRangeRule rule) {
        if(IsIntegral(propertyType.SpecialType)) {
            return IsIntegralFullRange(attribute, propertyType.SpecialType, rule);
        }

        if(propertyType.SpecialType is SpecialType.System_Single or SpecialType.System_Double) {
            return IsFloatingPointFullRange(attribute, propertyType.SpecialType, rule);
        }

        return false;
    }

    private static bool IsFullRange(FullRangeRule rule, bool firstAtMinimum, bool firstAtMaximum, bool secondAtMaximum) =>
        rule switch {
            FullRangeRule.GreaterThanOrEqual => firstAtMinimum,
            FullRangeRule.LessThanOrEqual => firstAtMaximum,
            FullRangeRule.InclusiveBetween => firstAtMinimum && secondAtMaximum,
            _ => false
        };

    private static bool IsIntegralFullRange(AttributeData attribute, SpecialType propertyType, FullRangeRule rule) {
        if(!TryGetDecimal(attribute, 0, out var first)) {
            return false;
        }

        var second = 0m;
        if(rule == FullRangeRule.InclusiveBetween && !TryGetDecimal(attribute, 1, out second)) {
            return false;
        }

        var (typeMinimum, typeMaximum) = IntegralBounds(propertyType);
        return IsFullRange(rule, first <= typeMinimum, first >= typeMaximum, second >= typeMaximum);
    }

    private static bool IsFloatingPointFullRange(AttributeData attribute, SpecialType propertyType, FullRangeRule rule) {
        if(!TryGetDouble(attribute, 0, out var first)) {
            return false;
        }

        var second = 0d;
        if(rule == FullRangeRule.InclusiveBetween && !TryGetDouble(attribute, 1, out second)) {
            return false;
        }

        var (typeMinimum, typeMaximum) = propertyType == SpecialType.System_Single
            ? ((double)float.MinValue, (double)float.MaxValue)
            : (double.MinValue, double.MaxValue);

        return IsFullRange(rule, first <= typeMinimum, first >= typeMaximum, second >= typeMaximum);
    }

    private static bool IsValidationAttribute(INamedTypeSymbol? attributeType) =>
        attributeType?.ContainingNamespace.ToDisplayString()
                     .StartsWith("Jeninnet.Validation.Attributes", StringComparison.Ordinal) == true;

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            ? nullable.TypeArguments[0]
            : type;

    private static bool IsString(ITypeSymbol type) => type.SpecialType == SpecialType.System_String;

    private static bool IsIntegral(SpecialType type) =>
        type is SpecialType.System_SByte or SpecialType.System_Byte
             or SpecialType.System_Int16 or SpecialType.System_UInt16
             or SpecialType.System_Int32 or SpecialType.System_UInt32
             or SpecialType.System_Int64 or SpecialType.System_UInt64;

    private static (decimal Minimum, decimal Maximum) IntegralBounds(SpecialType type) =>
        type switch {
            SpecialType.System_SByte => (sbyte.MinValue, sbyte.MaxValue),
            SpecialType.System_Byte => (byte.MinValue, byte.MaxValue),
            SpecialType.System_Int16 => (short.MinValue, short.MaxValue),
            SpecialType.System_UInt16 => (ushort.MinValue, ushort.MaxValue),
            SpecialType.System_Int32 => (int.MinValue, int.MaxValue),
            SpecialType.System_UInt32 => (uint.MinValue, uint.MaxValue),
            SpecialType.System_Int64 => (long.MinValue, long.MaxValue),
            SpecialType.System_UInt64 => (ulong.MinValue, ulong.MaxValue),
            _ => (0m, 0m)
        };

    private static bool TryGetInt(AttributeData attribute, int index, out int value) {
        value = 0;
        var arguments = attribute.ConstructorArguments;

        if(index >= arguments.Length || arguments[index].Value is not int candidate) {
            return false;
        }

        value = candidate;
        return true;
    }

    private static bool TryGetDecimal(AttributeData attribute, int index, out decimal value) {
        value = 0m;
        var arguments = attribute.ConstructorArguments;

        if(index >= arguments.Length || arguments[index].Value is null) {
            return false;
        }

        try {
            value = Convert.ToDecimal(arguments[index].Value, CultureInfo.InvariantCulture);
            return true;
        }
        catch(Exception exception) when(exception is InvalidCastException or FormatException or OverflowException) {
            return false;
        }
    }

    private static bool TryGetDouble(AttributeData attribute, int index, out double value) {
        value = 0d;
        var arguments = attribute.ConstructorArguments;

        if(index >= arguments.Length || arguments[index].Value is null) {
            return false;
        }

        try {
            value = Convert.ToDouble(arguments[index].Value, CultureInfo.InvariantCulture);
            return true;
        }
        catch(Exception exception) when(exception is InvalidCastException or FormatException or OverflowException) {
            return false;
        }
    }

    private enum FullRangeRule {
        InclusiveBetween = 0,
        GreaterThanOrEqual = 1,
        LessThanOrEqual = 2
    }
}
