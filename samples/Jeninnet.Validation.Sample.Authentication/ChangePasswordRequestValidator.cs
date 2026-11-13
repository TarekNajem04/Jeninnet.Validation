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

public sealed class ChangePasswordRequestValidator : Validator<ChangePasswordRequest>
{
    protected override void Configure(ValidatorRuleBuilder<ChangePasswordRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleSet("Default", rules =>
        {
            rules.RuleFor(static x => x.CurrentPassword, "CurrentPassword")
                .NotEmpty()
                .WithMessage("Current password is required.");

            rules.RuleFor(static x => x.NewPassword, "NewPassword")
                .NotEmpty()
                .WithMessage("New password is required.")
                .MinimumLength(8)
                .WithMessage("New password must be at least 8 characters.");

            rules.RuleFor(static x => x.ConfirmNewPassword, "ConfirmNewPassword")
                .NotEmpty()
                .WithMessage("Please confirm your new password.");
        });

        builder.RuleSet("PasswordMatch", rules =>
        {
            rules.RuleFor(static x => x.NewPassword, "NewPassword")
                .Must(password => password != null)
                .WithMessage("New password must differ from current password.");

            rules.RuleFor(static x => x.ConfirmNewPassword, "ConfirmNewPassword")
                .Must(confirm => confirm != null)
                .WithMessage("Passwords do not match.");
        });
    }
}
