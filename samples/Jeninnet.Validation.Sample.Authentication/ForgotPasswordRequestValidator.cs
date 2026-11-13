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

public sealed class ForgotPasswordRequestValidator : Validator<ForgotPasswordRequest>
{
    protected override void Configure(ValidatorRuleBuilder<ForgotPasswordRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static x => x.Email, "Email")
            .NotEmpty()
            .WithMessage("An email address is required to reset your password.")
            .EmailAddress()
            .WithMessage("Provide a valid email address.");
    }
}
