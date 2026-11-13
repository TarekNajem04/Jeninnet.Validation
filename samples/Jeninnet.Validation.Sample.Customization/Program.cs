//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Customization;

public sealed record UserProfile(string Username, string InternalCode, int Age);

public sealed class UserProfileValidator : Validator<UserProfile> {
    protected override void Configure(ValidatorRuleBuilder<UserProfile> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(x => x.Username, nameof(UserProfile.Username))
               .NotEmpty()
               .UsernameAvailable();

        builder.RuleFor(x => x.InternalCode, nameof(UserProfile.InternalCode))
               .NotEmpty()
               .StartsWith("INT-");

        builder.RuleFor(x => x.Age, nameof(UserProfile.Age))
               .InclusiveBetween(18, 100)
               .Must(age => age % 2 == 0, "Age must be an even number.");
    }
}

public static class Program {
    public static async Task Main() {
        Console.WriteLine("=== Jeninnet.Validation Customization Sample ===");

        var validator = new UserProfileValidator();

        var profile = new UserProfile(
            Username: "admin",      // Should fail (reserved)
            InternalCode: "ABC-123", // Should fail (wrong prefix)
            Age: 25                 // Should fail (not even)
        );

        Console.WriteLine($"Validating: {profile}");

        var result = await validator.ValidateAsync(profile).ConfigureAwait(false);

        if(result.IsValid) {
            Console.WriteLine("Validation Succeeded!");
        } else {
            Console.WriteLine("Validation Failed with errors:");
            foreach(var error in result.Errors) {
                // Errors carry the member path, message, optional key/code, and custom state.
                Console.WriteLine($"- {error.MemberName}: {error.Message}");
            }
        }

        Console.WriteLine("\nRun Other Test: Testing successful validation...");
        var validProfile = new UserProfile("johndoe", "INT-999", 30);
        var validResult = await validator.ValidateAsync(validProfile).ConfigureAwait(false);
        Console.WriteLine($"Is valid: {validResult.IsValid}");
    }
}
