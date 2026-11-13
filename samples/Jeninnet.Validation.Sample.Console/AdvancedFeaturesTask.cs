//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Console;

file sealed class Address {
    public string? Street { get; set; }
    public string? City { get; set; }
}

file sealed class Registration {
    public string? Username { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public string? MiddleName { get; set; }
    public List<string>? Tags { get; set; }
    public string? Status { get; set; }
    public Address? HomeAddress { get; set; }
}

file sealed class RegistrationValidator : Validator<Registration> {
    protected override void Configure(ValidatorRuleBuilder<Registration> builder) {
        builder.RuleFor(static x => x.Username, "Username")
               .NotEmpty()
               .WithMessage("A username is required.")
               .WithCode("USERNAME_REQUIRED")
               .WithSeverity(ValidationErrorSeverity.Error);

        builder.RuleFor(static x => x.Email, "Email")
               .NotEmpty()
               .WithMessage("Email address is required.")
               .WithCode("EMAIL_REQUIRED")
               .OverridePropertyName("EmailAddress");

        builder.RuleFor(static x => x.Age, "Age")
               .InclusiveBetween(18, 99)
               .WithMessage("You must be at least 18 years old.")
               .WithCode("AGE_MINIMUM")
               .WithState(new { MinAge = 18, MaxAge = 99 });

        builder.RuleFor(static x => x.MiddleName, "MiddleName")
               .Null()
               .WithMessage("Middle name must be null.");

        builder.RuleFor(static x => x.Tags, "Tags")
               .Empty()
               .WithMessage("Tags collection must be empty.");

        builder.RuleFor(static x => x.Status, "Status")
               .IsEnumName(typeof(OrderStatus))
               .WithMessage("Status must be a valid order status.");

        builder.RuleFor(static x => x.HomeAddress, "HomeAddress")
               .ChildRules(address => {
                   address.RuleFor(static a => a.Street, "Street").NotEmpty();
                   address.RuleFor(static a => a.City, "City").NotEmpty();
               });

        builder.Include(b => b.RuleFor(static x => x.Username, "Username").NotNull());
    }
}

file enum OrderStatus {
    Pending = 0,
    Shipped = 1,
    Delivered = 2
}

internal static class AdvancedFeaturesTask {
    public static void Run() {
        var validator = new RegistrationValidator();

        System.Console.WriteLine("=== ADVANCED FEATURES (Phase 4-B & 4-C) ===");
        System.Console.WriteLine();

        // 1. ValidateAndThrow — throws when invalid
        var invalid = new Registration {
            Username = "",
            Email = "bad",
            Age = 15,
            MiddleName = "John",
            Tags = ["a", "b", "c"],
            Status = "Unknown",
            HomeAddress = new Address { Street = "", City = "" }
        };
        try {
            validator.ValidateAndThrow(invalid);
        }
        catch(ValidationException ex) {
            System.Console.WriteLine("ValidateAndThrow caught ValidationException with {0} error(s):", ex.Errors.Count);
            foreach(var error in ex.Errors) {
                System.Console.WriteLine("  [{0}] {1} (code: {2}, severity: {3})",
                    error.MemberName, error.Message, error.ErrorCode, error.Severity);
                if(error.CustomState is not null) {
                    System.Console.WriteLine("    CustomState: {0}", error.CustomState);
                }
            }
        }

        System.Console.WriteLine();

        // 2. Normal validation with options — no throw
        var result = validator.Validate(invalid);
        System.Console.WriteLine("Normal validation (no throw) — errors: {0}", result.Errors.Count);
        foreach(var error in result.Errors) {
            System.Console.WriteLine("  [{0}] {1} (code: {2}, severity: {3})",
                error.MemberName, error.Message, error.ErrorCode, error.Severity);
            if(error.CustomState is not null) {
                System.Console.WriteLine("    CustomState: {0}", error.CustomState);
            }
        }

        System.Console.WriteLine();
    }
}
