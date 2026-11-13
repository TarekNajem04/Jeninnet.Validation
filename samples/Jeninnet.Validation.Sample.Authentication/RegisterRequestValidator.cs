//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Platform/
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Authentication;

public sealed class RegisterRequestValidator : Validator<RegisterRequest>
{
#pragma warning disable IDE1006 // Naming rule violation
    public const string PasswordStrengthRuleSet = "PasswordStrength";
#pragma warning restore IDE1006

    protected override void Configure(ValidatorRuleBuilder<RegisterRequest> builder)
    {
        builder.RuleSet("Default", rules =>
        {
            rules.RuleFor(static x => x.Username, "Username")
                .NotEmpty()
                .Length(3, 30);

            rules.RuleFor(static x => x.Email, "Email")
                .NotEmpty()
                .EmailAddress();

            rules.RuleFor(static x => x.Password, "Password")
                .NotEmpty()
                .WithMessage("A password is required.");

            rules.RuleFor(static x => x.ConfirmPassword, "ConfirmPassword")
                .NotEmpty()
                .WithMessage("You must confirm your password.");

            rules.RuleFor(static x => x.Age, "Age")
                .NotEmpty()
                .InclusiveBetween(13, 120)
                .WithMessage("You must be at least 13 years old.");
        });

        builder.RuleSet(PasswordStrengthRuleSet, rules =>
        {
            rules.RuleFor(static x => x.Password, "Password")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters.");

            rules.RuleFor(static x => x.Password, "Password")
                .Matches(@"[A-Z]")
                .WithMessage("Password must contain an uppercase letter.");

            rules.RuleFor(static x => x.Password, "Password")
                .Matches(@"[a-z]")
                .WithMessage("Password must contain a lowercase letter.");

            rules.RuleFor(static x => x.Password, "Password")
                .Matches(@"\d")
                .WithMessage("Password must contain a digit.");
        });
    }
}
