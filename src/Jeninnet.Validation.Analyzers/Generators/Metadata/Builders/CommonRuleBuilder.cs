//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Metadata.Builders;

internal static class CommonRuleBuilder {
    public static RuleModel CreateCommonNotXRule(string propertyName, RuleKind kind, AttributeData attribute, string propertyTypeName) =>
        new(kind, propertyName, propertyTypeName, Message: MetadataHelper.GetMessageArgument(attribute));

    public static RuleModel CreateInRule(string propertyName, RuleKind kind, AttributeData attribute, string propertyTypeName) {
        var values = attribute.ConstructorArguments[0];
        var valuesText = string.Join(", ", values.Values.Select(v => MetadataHelper.FormatConstant(v.Value) ?? "null"));

        return new RuleModel(
            kind,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(valuesText, typeof(string).FullName),
            Parameter2: new ParameterInfo("array", typeof(Array).FullName),
            Message: MetadataHelper.GetMessageArgument(attribute)
        );
    }
}
