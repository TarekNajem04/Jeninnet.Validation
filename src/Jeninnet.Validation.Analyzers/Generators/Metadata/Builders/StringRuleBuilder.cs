//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Metadata.Builders;

internal static class StringRuleBuilder {
    public static RuleModel CreateStringParamRule(string propertyName, RuleKind kind, AttributeData attribute, string propertyTypeName) {
        var param = MetadataHelper.GetParam(attribute, 0);
        return new RuleModel(
            kind,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(param.Value, param.TypeName),
            Message: MetadataHelper.GetMessageArgument(attribute)
        );
    }

    public static RuleModel CreateCreditCardRule(string propertyName, AttributeData attribute, string propertyTypeName) =>
        new(RuleKind.CreditCard, propertyName, propertyTypeName, Message: MetadataHelper.GetMessageArgument(attribute));

    public static RuleModel CreateEmailRule(string propertyName, AttributeData attribute, string propertyTypeName) =>
        new(RuleKind.Email, propertyName, propertyTypeName, Message: MetadataHelper.GetMessageArgument(attribute));

    public static RuleModel CreateLengthRule(string propertyName, AttributeData attribute, string propertyTypeName) {
        var min = MetadataHelper.GetParam(attribute, 0);
        var max = MetadataHelper.GetParam(attribute, 1);

        return new RuleModel(
            RuleKind.Length,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(min.Value, min.TypeName),
            Parameter2: new ParameterInfo(max.Value, max.TypeName),
            Message: MetadataHelper.GetMessageArgument(attribute)
        );
    }

    public static RuleModel CreateMaxLengthRule(string propertyName, AttributeData attribute, string propertyTypeName) {
        var max = MetadataHelper.GetParam(attribute, 0);

        return new RuleModel(
            RuleKind.MaximumLength,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo("0", typeof(int).FullName),
            Parameter2: new ParameterInfo(max.Value, max.TypeName),
            Message: MetadataHelper.GetMessageArgument(attribute)
        );
    }

    public static RuleModel CreateMinLengthRule(string propertyName, AttributeData attribute, string propertyTypeName) {
        var min = MetadataHelper.GetParam(attribute, 0);

        return new RuleModel(
            RuleKind.MinimumLength,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(min.Value, min.TypeName),
            Parameter2: new ParameterInfo(int.MaxValue.ToString(CultureInfo.InvariantCulture), typeof(int).FullName),
            Message: MetadataHelper.GetMessageArgument(attribute)
        );
    }

    public static RuleModel CreateRegexRule(string propertyName, AttributeData attribute, string propertyTypeName) {
        var param = MetadataHelper.GetParam(attribute, 0);

        return new RuleModel(
            RuleKind.Regex,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(param.Value, param.TypeName),
            Message: MetadataHelper.GetMessageArgument(attribute)
        );
    }

    public static RuleModel CreateStringOnlyRule(string propertyName, RuleKind kind, AttributeData attribute, string propertyTypeName) =>
        new(kind, propertyName, propertyTypeName, Message: MetadataHelper.GetMessageArgument(attribute));

    public static RuleModel CreateExactLengthRule(string propertyName, AttributeData attribute, string propertyTypeName) {
        var length = MetadataHelper.GetParam(attribute, 0);

        return new RuleModel(
            RuleKind.ExactLength,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(length.Value, length.TypeName),
            Message: GetMessage(attribute, messageIndex: 1));
    }

    public static RuleModel CreateBase64StringRule(string propertyName, AttributeData attribute, string propertyTypeName) =>
        new(RuleKind.Base64String, propertyName, propertyTypeName, Message: GetMessage(attribute, messageIndex: 0));

    public static RuleModel CreateFileExtensionsRule(string propertyName, AttributeData attribute, string propertyTypeName) {
        var extensions = MetadataHelper.GetArrayParam(attribute, 0)
                                        .Where(static extension => !string.IsNullOrWhiteSpace(extension));

        var ignoreCase = MetadataHelper.GetNamedArgument<bool?>(attribute, "IgnoreCase") ?? true;
        var allowNonStandard = MetadataHelper.GetNamedArgument<bool?>(attribute, "AllowNonStandard") ?? false;

        return new RuleModel(
            RuleKind.FileExtensions,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(string.Join(",", extensions), typeof(string).FullName),
            Parameter2: new ParameterInfo(ignoreCase ? "true" : "false", typeof(bool).FullName),
            Parameter3: new ParameterInfo(allowNonStandard ? "true" : "false", typeof(bool).FullName),
            Message: MetadataHelper.GetNamedArgument<string>(attribute, "Message"));
    }

    private static string? GetMessage(AttributeData attribute, int messageIndex) =>
        MetadataHelper.GetNamedArgument<string>(attribute, "Message")
        ?? MetadataHelper.GetConstructorArgument<string>(attribute, messageIndex);
}
