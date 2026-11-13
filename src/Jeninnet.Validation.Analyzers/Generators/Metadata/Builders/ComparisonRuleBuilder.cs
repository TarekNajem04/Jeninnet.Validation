//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Metadata.Builders;

internal static class ComparisonRuleBuilder {
    public static RuleModel CreateComparisonRule(string propertyName, RuleKind kind, AttributeData attribute, string propertyTypeName) {
        var param = MetadataHelper.GetParam(attribute, 0);
        return new RuleModel(
            kind,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(param.Value, param.TypeName),
            Message: MetadataHelper.GetMessageArgument(attribute)
        );
    }

    public static RuleModel CreateComparisonBetweenRule(string propertyName, RuleKind kind, AttributeData attribute, string propertyTypeName) {
        var min = MetadataHelper.GetParam(attribute, 0);
        var max = MetadataHelper.GetParam(attribute, 1);

        return new RuleModel(
            kind,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(min.Value, min.TypeName),
            Parameter2: new ParameterInfo(max.Value, max.TypeName),
            Message: MetadataHelper.GetMessageArgument(attribute)
        );
    }

    public static RuleModel? CreateMultipleOfRule(
        string propertyName,
        AttributeData attribute,
        string propertyTypeName,
        List<GeneratorDiagnostic> diagnostics) {
        var divisor = MetadataHelper.GetParam(attribute, 0);

        if(divisor.Value is null ||
           !decimal.TryParse(divisor.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericDivisor) ||
           numericDivisor <= 0m) {
            diagnostics.Add(new GeneratorDiagnostic(
                DiagnosticDescriptors.InvalidMultipleOfDivisor,
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                [divisor.Value ?? "null", propertyName]));
            return null;
        }

        return new RuleModel(
            RuleKind.MultipleOf,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(
                numericDivisor.ToString(CultureInfo.InvariantCulture),
                typeof(decimal).FullName),
            Message: MetadataHelper.GetNamedArgument<string>(attribute, "Message")
                     ?? MetadataHelper.GetConstructorArgument<string>(attribute, 1)
        );
    }

    public static RuleModel CreateScalePrecisionRule(string propertyName, AttributeData attribute, string propertyTypeName) {
        var precision = MetadataHelper.GetParam(attribute, 0);
        var scale = MetadataHelper.GetParam(attribute, 1);
        var ignoreTrailingZeros = MetadataHelper.GetNamedArgument<bool>(attribute, "IgnoreTrailingZeros");

        return new RuleModel(
            RuleKind.ScalePrecision,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(precision.Value, precision.TypeName),
            Parameter2: new ParameterInfo(scale.Value, scale.TypeName),
            Parameter3: new ParameterInfo(ignoreTrailingZeros ? "true" : "false", typeof(bool).FullName),
            Message: MetadataHelper.GetMessageArgument(attribute)
        );
    }
}
