//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Metadata.Builders;

internal static class NestedRuleBuilder {
    public static RuleModel CreateNestedRule(string propertyName, AttributeData attribute, string propertyTypeName) {
        var param = MetadataHelper.GetParam(attribute, 0);
        var targetTypeName = propertyTypeName + ValidatorEmitter.CLASS_SUFFIX;

        return new RuleModel(
            Kind: RuleKind.Nested,
            PropertyName: propertyName,
            PropertyTypeName: propertyTypeName,
            TargetTypeName: string.IsNullOrEmpty(param.Value) ? targetTypeName : param.Value
        );
    }
}
