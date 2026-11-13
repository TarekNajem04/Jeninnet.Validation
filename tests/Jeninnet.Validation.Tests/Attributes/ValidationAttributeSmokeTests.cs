//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
using System.Reflection;
using ValidationAttributeBase = Jeninnet.Validation.Attributes.ValidationAttribute;

namespace Jeninnet.Validation.Tests.Attributes;

/// <summary>Smoke coverage for every validation attribute type in the product assemblies.</summary>
[TestClass]
public sealed class ValidationAttributeSmokeTests {
    [TestMethod]
    public void EveryAttributeType_CanBeConstructedWithMinimalArguments() {
        var assemblies = new[] {
            typeof(ValidationAttributeBase).Assembly,
            typeof(PasswordPolicy).Assembly
        };

        var attributeTypes = assemblies
            .SelectMany(static assembly => assembly.GetTypes())
            .Where(static type =>
                type.IsPublic &&
                !type.IsAbstract &&
                !type.ContainsGenericParameters &&
                typeof(Attribute).IsAssignableFrom(type) &&
                type.Namespace?.Contains(".Attributes", StringComparison.Ordinal) == true)
            .Distinct()
            .ToArray();

        MSTestAssertions.That(() => attributeTypes.Should().NotBeEmpty());

        foreach(var type in attributeTypes) {
            var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(static candidate => candidate.GetParameters().Length)
                .First();
            var arguments = constructor.GetParameters().Select(CreateArgument).ToArray();
            var instance = constructor.Invoke(arguments);

            MSTestAssertions.That(() => instance.Should().NotBeNull(
                $"attribute '{type.Name}' must be constructible with minimal arguments"));

            if(instance is ValidationAttributeBase validationAttribute) {
                _ = validationAttribute.Message;
                _ = validationAttribute.Severity;
                _ = validationAttribute.ErrorCode;
                _ = validationAttribute.DisplayName;
                _ = validationAttribute.OverridePropertyName;
                _ = validationAttribute.CustomState;
                _ = validationAttribute.RuleSet;
            }
        }
    }

    private static object? CreateArgument(ParameterInfo parameter) {
        if(parameter.HasDefaultValue) {
            return parameter.DefaultValue;
        }

        if(parameter.ParameterType == typeof(string)) {
            return "Probe";
        }

        if(parameter.ParameterType == typeof(Type)) {
            return typeof(string);
        }

        if(parameter.ParameterType.IsArray) {
            return Array.CreateInstance(parameter.ParameterType.GetElementType()!, 0);
        }

        if(parameter.ParameterType.IsValueType) {
            return Activator.CreateInstance(parameter.ParameterType);
        }

        return null;
    }
}
