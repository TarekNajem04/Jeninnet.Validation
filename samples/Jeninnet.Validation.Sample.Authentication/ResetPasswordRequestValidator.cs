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

public sealed class ResetPasswordRequestValidator : Validator<ResetPasswordRequest>
{
    protected override void Configure(ValidatorRuleBuilder<ResetPasswordRequest> builder)
    {
        builder.RuleSet("Default", rules =>
        {
            rules.RuleFor(static x => x.Token, "Token")
                .NotEmpty()
                .WithMessage("Reset token is required.");

            rules.RuleFor(static x => x.NewPassword, "NewPassword")
                .NotEmpty()
                .WithMessage("New password is required.")
                .Length(8, 128);
        });

        builder.RuleSet("PasswordStrength", rules =>
        {
            rules.RuleFor(static x => x.NewPassword, "NewPassword")
                .Matches(@"[A-Z]")
                .WithMessage("New password must contain an uppercase letter.");

            rules.RuleFor(static x => x.NewPassword, "NewPassword")
                .Matches(@"\d")
                .WithMessage("New password must contain a digit.");
        });

        builder.RuleSet("Confirmation", rules =>
        {
            rules.RuleFor(static x => x.ConfirmNewPassword, "ConfirmNewPassword")
                .NotEmpty()
                .WithMessage("Please confirm your new password.");
        });
    }
}
