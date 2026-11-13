//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Metadata.Builders;

internal static class CollectionRuleBuilder {
    public static RuleModel CreateForEachRule(string propertyName, IPropertySymbol propertySymbol) {
        var collectionType = propertySymbol.Type;
        var elementType = collectionType.GetElementType() ?? throw new NotSupportedException($"[ValidateEach] can only be applied to array/collection types. Property: {propertyName}");
        var propertyTypeName = collectionType.GetFullTypeName();
        var targetTypeName = elementType.GetFullTypeName();

        return new RuleModel(
            Kind: RuleKind.ForEach,
            PropertyName: propertyName,
            PropertyTypeName: propertyTypeName,
            TargetTypeName: targetTypeName);
    }

    public static RuleModel CreateCountRule(string propertyName, AttributeData attribute, string propertyTypeName) {
        var min = MetadataHelper.GetParam(attribute, 0);
        var max = MetadataHelper.GetParam(attribute, 1);

        return new RuleModel(
            RuleKind.Count,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(min.Value, min.TypeName),
            Parameter2: new ParameterInfo(max.Value, max.TypeName),
            Message: GetMessage(attribute, messageIndex: 2));
    }

    public static RuleModel CreateMinCountRule(string propertyName, AttributeData attribute, string propertyTypeName) {
        var min = MetadataHelper.GetParam(attribute, 0);

        return new RuleModel(
            RuleKind.MinCount,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(min.Value, min.TypeName),
            Message: GetMessage(attribute, messageIndex: 1));
    }

    public static RuleModel CreateMaxCountRule(string propertyName, AttributeData attribute, string propertyTypeName) {
        var max = MetadataHelper.GetParam(attribute, 0);

        return new RuleModel(
            RuleKind.MaxCount,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(max.Value, max.TypeName),
            Message: GetMessage(attribute, messageIndex: 1));
    }

    public static RuleModel CreateUniqueItemsRule(
        string propertyName,
        AttributeData attribute,
        IPropertySymbol propertySymbol) {
        var elementType = propertySymbol.Type.GetElementType()
            ?? throw new NotSupportedException($"[UniqueItems] can only be applied to array/collection types. Property: {propertyName}");

        return new RuleModel(
            Kind: RuleKind.UniqueItems,
            PropertyName: propertyName,
            PropertyTypeName: propertySymbol.Type.GetFullTypeName(),
            Message: GetMessage(attribute, messageIndex: 0),
            TargetTypeName: elementType.GetFullTypeName());
    }

    private static string? GetMessage(AttributeData attribute, int messageIndex) =>
        MetadataHelper.GetNamedArgument<string>(attribute, "Message")
        ?? MetadataHelper.GetConstructorArgument<string>(attribute, messageIndex);
}
