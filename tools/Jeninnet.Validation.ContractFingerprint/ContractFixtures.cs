//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.ContractFingerprint;

/// <summary>
/// The canonical descriptor fixtures used by the fingerprint tool. They exercise scalar parameters,
/// nested descriptors, rule sets, dependent rules, conditions, message keys, severities, and error
/// codes without loading a user assembly or using reflection.
/// </summary>
internal static class ContractFixtures {
    private const string NOT_EMPTY = "NotEmpty";
    private static readonly string[] _strictRuleSet = ["strict"];

    public static IReadOnlyList<(string Name, IValidationDescriptor Descriptor)> All { get; } = [
        ("MinimalValidator", Minimal()),
        ("OrderValidator", Order())
    ];

    internal static ValidationDescriptor MinimalWith(
        string kind = NOT_EMPTY,
        string messageKey = NOT_EMPTY,
        ValidationErrorSeverity severity = ValidationErrorSeverity.Error,
        bool hasCondition = false,
        IReadOnlyDictionary<string, object?>? parameters = null) =>
        new(
            "MinimalValidator",
            [
                new ValidationMemberDescriptor(
                    "Value",
                    [new ValidationRuleDescriptor(kind, messageKey, severity, hasCondition, parameters)])
            ],
            typeof(MinimalModel));

    internal static ValidationDescriptor Minimal() => MinimalWith();

    private static ValidationDescriptor Order() {
        var address = new ValidationDescriptor(
            "AddressValidator",
            [
                new ValidationMemberDescriptor(
                    "Street",
                    [new ValidationRuleDescriptor(NOT_EMPTY, NOT_EMPTY)])
            ],
            typeof(AddressModel));

        var line = new ValidationDescriptor(
            "LineValidator",
            [
                new ValidationMemberDescriptor(
                    "ProductName",
                    [new ValidationRuleDescriptor(NOT_EMPTY, NOT_EMPTY)])
            ],
            typeof(LineModel));

        return new ValidationDescriptor(
            "OrderValidator",
            [
                new ValidationMemberDescriptor(
                    "Address",
                    [new ValidationRuleDescriptor("Nested", "Nested", nestedDescriptor: address)]),
                new ValidationMemberDescriptor(
                    "Age",
                    [
                        new ValidationRuleDescriptor(
                            "InclusiveBetween",
                            "InclusiveBetween",
                            parameters: new Dictionary<string, object?> {
                                ["0"] = 18,
                                ["1"] = 99,
                                ["Min"] = 18,
                                ["Max"] = 99
                            })
                    ]),
                new ValidationMemberDescriptor(
                    "Code",
                    [
                        new ValidationRuleDescriptor(
                            "Length",
                            "Length",
                            parameters: new Dictionary<string, object?> {
                                ["Min"] = 3,
                                ["Max"] = 10
                            })
                    ]),
                new ValidationMemberDescriptor(
                    "Email",
                    [
                        new ValidationRuleDescriptor(
                            "Email",
                            "Email",
                            severity: ValidationErrorSeverity.Warning,
                            errorCode: "EMAIL_FORMAT",
                            displayName: "Email address")
                    ]),
                new ValidationMemberDescriptor(
                    "Items",
                    [
                        new ValidationRuleDescriptor(
                            "Count",
                            "Count",
                            parameters: new Dictionary<string, object?> {
                                ["Min"] = 1,
                                ["Max"] = 5,
                                ["RuleSets"] = _strictRuleSet,
                                ["Dependent"] = true
                            })
                    ]),
                new ValidationMemberDescriptor(
                    "Lines",
                    [new ValidationRuleDescriptor("ForEach", "ForEach", nestedDescriptor: line)]),
                new ValidationMemberDescriptor(
                    "Name",
                    [
                        new ValidationRuleDescriptor(NOT_EMPTY, NOT_EMPTY),
                        new ValidationRuleDescriptor(
                            "Custom",
                            "ConditionalName",
                            severity: ValidationErrorSeverity.Warning,
                            hasCondition: true) {
                            MessageIsAuthored = true
                        }
                    ])
            ],
            typeof(OrderModel));
    }
}

internal sealed class MinimalModel {
    public string? Value { get; set; }
}

internal sealed class OrderModel {
    public string? Name { get; set; }

    public int Age { get; set; }
}

internal sealed class AddressModel {
    public string? Street { get; set; }
}

internal sealed class LineModel {
    public string? ProductName { get; set; }
}

