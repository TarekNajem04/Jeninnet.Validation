//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Sample.Console;

public static class UserValidatorTask {
    public static async Task RunAsync(IServiceProvider provider) {
        var validator = provider.GetRequiredService<IValidator<User>>();
        var user = SampleData.CreateInvalidUser();

        System.Console.WriteLine("=== [User] RESULTS ===");
        var result = await validator.ValidateAsync(user);
        System.Console.WriteLine($"Is User Valid: {result.IsValid}");
        foreach(var error in result.Errors) {
            System.Console.WriteLine($"{error.MemberName}: {error.Message}");
        }

        System.Console.WriteLine("============================================================");
    }
}
