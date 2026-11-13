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

public sealed class LoginRequestValidator : Validator<LoginRequest>
{
    protected override void Configure(ValidatorRuleBuilder<LoginRequest> builder)
    {
        builder.RuleSet("Default", rules =>
        {
            rules.RuleFor(static x => x.Username, "Username")
                .NotEmpty()
                .WithMessage("Username is required.")
                .OverridePropertyName("LoginName");

            rules.RuleFor(static x => x.Password, "Password")
                .NotEmpty()
                .WithMessage("Password is required.");
        });
    }
}
