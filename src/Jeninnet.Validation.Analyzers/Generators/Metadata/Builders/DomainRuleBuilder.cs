//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Metadata.Builders;

internal static class DomainRuleBuilder {
    public static RuleModel CreateCountryRule(
        string propertyName,
        RuleKind kind,
        AttributeData attribute,
        string propertyTypeName) {
        var country = MetadataHelper.GetParam(attribute, 0);

        return new RuleModel(
            kind,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(country.Value, country.TypeName),
            Message: GetMessage(attribute, messageIndex: 1));
    }

    public static RuleModel CreatePasswordStrengthRule(
        string propertyName,
        AttributeData attribute,
        string propertyTypeName) {
        var preset = MetadataHelper.GetParam(attribute, 0).Value;
        var (name, minLength) = preset switch {
            "0" => ("Basic", 8),
            "2" => ("Strict", 16),
            _ => ("Strong", 12)
        };

        return new RuleModel(
            RuleKind.PasswordStrength,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(name, typeof(string).FullName),
            Parameter2: new ParameterInfo(minLength.ToString(CultureInfo.InvariantCulture), typeof(int).FullName),
            Message: GetMessage(attribute, messageIndex: 1));
    }

    public static RuleModel CreateSimpleRule(
        string propertyName,
        RuleKind kind,
        AttributeData attribute,
        string propertyTypeName) =>
        new(kind, propertyName, propertyTypeName, Message: GetMessage(attribute, messageIndex: 0));

    private static string? GetMessage(AttributeData attribute, int messageIndex) =>
        MetadataHelper.GetNamedArgument<string>(attribute, "Message")
        ?? MetadataHelper.GetConstructorArgument<string>(attribute, messageIndex);
}
