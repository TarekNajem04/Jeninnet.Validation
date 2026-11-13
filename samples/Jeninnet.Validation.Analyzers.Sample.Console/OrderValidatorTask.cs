//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Sample.Console;

internal static class OrderValidatorTask {
    public static async Task RunAsync(IServiceProvider serviceProvider) {
        var order = SampleData.CreateInvalidOrder();

        // var validator = serviceProvider.GetRequiredService<OrderValidator>()
        // var validator = serviceProvider.GetRequiredService<ISyncValidator<Order>>()
        // var validator = serviceProvider.GetRequiredService<IAsyncValidator<Order>>()
        var validator = serviceProvider.GetRequiredService<IValidator<Order>>();
        var result = validator.Validate(order);

        System.Console.WriteLine();
        System.Console.WriteLine("=== [ValidateSync] [AttributeBasedValidatior] RESULTS ===");

        System.Console.WriteLine($"Is [ValidateSync] Order Valid: {result.IsValid}");

        for(var i = 0; i < result.Errors.Count; i++) {
            var error = result.Errors[i];
            System.Console.WriteLine($"{error.MemberName}: {error.Message}");
        }

        result = await validator.ValidateAsync(order);

        System.Console.WriteLine();
        System.Console.WriteLine("=== [ValidateAsync] [AttributeBasedValidatior] RESULTS ===");

        System.Console.WriteLine($"Is [ValidateAsync] Order Valid: {result.IsValid}");

        for(var i = 0; i < result.Errors.Count; i++) {
            var error = result.Errors[i];
            System.Console.WriteLine($"{error.MemberName}: {error.Message}");
        }

        System.Console.WriteLine();

        // Validate with rule set
        var billingResult = validator.Validate(order, ["Billing"]);

        System.Console.WriteLine($"Is Order [Billing] rule sets Valid: {result.IsValid}");

        for(var i = 0; i < billingResult.Errors.Count; i++) {
            var error = billingResult.Errors[i];
            System.Console.WriteLine($"{error.MemberName}: {error.Message}");
        }

        System.Console.WriteLine();

        var asyncResult = validator.Validate(order);

        System.Console.WriteLine($"Is [Async] Order Valid: {result.IsValid}");

        for(var i = 0; i < asyncResult.Errors.Count; i++) {
            var error = asyncResult.Errors[i];
            System.Console.WriteLine($"{error.MemberName}: {error.Message}");
        }

        System.Console.WriteLine();
        System.Console.WriteLine(new string('=', 60));
        System.Console.WriteLine();
    }
}
