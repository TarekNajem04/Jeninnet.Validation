//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Metadata.Builders;

internal static class PropertyRuleBuilder {
    public static RuleModel CreatePropertyRule(string propertyName, RuleKind kind, AttributeData attribute, string propertyTypeName) {
        var param = MetadataHelper.GetParam(attribute, 0);

        return new RuleModel(
            kind,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(param.Value, param.TypeName),
            Message: MetadataHelper.GetMessageArgument(attribute)
        );
    }
}
