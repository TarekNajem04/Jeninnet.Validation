//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Console;

internal static class OrderValidatorTask {
    public static void Run(IServiceProvider serviceProvider) {
        // Resolve the OrderValidator
        var validator = serviceProvider.GetRequiredService<OrderValidator>().Build();

        // Create a sample invalid order
        var order = SampleData.CreateInvalidOrder();

        // Validate using all rules
        var result = validator.Validate(order);

        System.Console.WriteLine("=== VALIDATION [ValidatiorBasedValidatior] RESULTS ===");

        for(var i = 0; i < result.Errors.Count; i++) {
            var error = result.Errors[i];
            System.Console.WriteLine($"{error.MemberName}: {error.Message}");
        }

        System.Console.WriteLine();
        System.Console.WriteLine("=== RULESET: [ValidatiorBasedValidatior] Shipping ===");

        // Validate using a specific RuleSet (demonstrates RuleSetExtensions)
        var ruleSetResult = validator.Validate(instance: order, ruleSets: ["Shipping"]);

        for(var i = 0; i < ruleSetResult.Errors.Count; i++) {
            var error = result.Errors[i];
            System.Console.WriteLine($"{error.MemberName}: {error.Message}");
        }

        System.Console.WriteLine();
    }
}
