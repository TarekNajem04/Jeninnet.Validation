//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
using System.Text;
using Jeninnet.Validation.Metadata;

namespace Jeninnet.Validation.ContractFingerprint;

internal static class FingerprintDocument {
    private const string NEW_LINE = "\r\n";

    public static string CreateJson() {
        var validators = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach(var (name, descriptor) in ContractFixtures.All) {
            validators[name] = ValidationContractFingerprint.Compute(descriptor);
        }

        var builder = new StringBuilder();
        builder.Append('{').Append(NEW_LINE)
               .Append("  \"version\": 1,").Append(NEW_LINE)
               .Append("  \"algorithm\": \"sha256\",").Append(NEW_LINE)
               .Append("  \"validators\": {").Append(NEW_LINE);

        var index = 0;

        foreach(var pair in validators) {
            builder.Append("    \"").Append(pair.Key).Append("\": \"").Append(pair.Value).Append('"');

            if(++index < validators.Count) {
                builder.Append(',');
            }

            builder.Append(NEW_LINE);
        }

        builder.Append("  }").Append(NEW_LINE).Append('}').Append(NEW_LINE);

        return builder.ToString();
    }
}

internal static class SelfTest {
    public static int Run() {
        var baseline = ContractFixtures.Minimal();

        AssertStable("deterministic", baseline);
        AssertChanged("rule kind", baseline, ContractFixtures.MinimalWith(kind: "Email"));
        AssertChanged("message key", baseline, ContractFixtures.MinimalWith(messageKey: "Required"));
        AssertChanged("severity", baseline, ContractFixtures.MinimalWith(severity: ValidationErrorSeverity.Warning));
        AssertChanged("condition", baseline, ContractFixtures.MinimalWith(hasCondition: true));
        AssertChanged(
            "parameter",
            baseline,
            ContractFixtures.MinimalWith(
                parameters: new Dictionary<string, object?> { ["Min"] = 1, ["Max"] = 10 }));

        var order = ContractFixtures.All.Single(static fixture => fixture.Name == "OrderValidator").Descriptor;
        var nestedChange = new ValidationDescriptor(
            "OrderValidator",
            order.Members.Select(static member => member.Name == "Address"
                ? new ValidationMemberDescriptor(
                    member.Name,
                    [
                        new ValidationRuleDescriptor(
                            "Nested",
                            "Nested",
                            nestedDescriptor: new ValidationDescriptor(
                                "AddressValidator",
                                [
                                    new ValidationMemberDescriptor(
                                        "Street",
                                        [new ValidationRuleDescriptor("Length", "Length")])
                                ],
                                typeof(AddressModel)))
                    ])
                : member),
            typeof(OrderModel));
        AssertChanged("nested rule", order, nestedChange);

        Console.WriteLine("PASS: 7 sensitivity checks; deterministic and no-op rebuilds are stable.");
        return 0;
    }

    private static void AssertStable(string name, IValidationDescriptor descriptor) {
        var first = ValidationContractFingerprint.Compute(descriptor);
        var second = ValidationContractFingerprint.Compute(descriptor);

        if(!string.Equals(first, second, StringComparison.Ordinal)) {
            throw new InvalidOperationException($"Fingerprint is not stable for {name}.");
        }

        Console.WriteLine($"PASS: {name} is stable");
    }

    private static void AssertChanged(
        string name,
        IValidationDescriptor original,
        IValidationDescriptor changed) {
        var before = ValidationContractFingerprint.Compute(original);
        var after = ValidationContractFingerprint.Compute(changed);

        if(string.Equals(before, after, StringComparison.Ordinal)) {
            throw new InvalidOperationException($"Fingerprint did not change for {name}.");
        }

        Console.WriteLine($"PASS: {name} changes the fingerprint");
    }
}
