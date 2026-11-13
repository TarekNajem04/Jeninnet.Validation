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

namespace Jeninnet.Validation.Sample.DtoVsDomain.Validators;

public sealed class CreateUserDtoValidator : Validator<CreateUserDto>
{
    protected override void Configure(ValidatorRuleBuilder<CreateUserDto> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static d => d.Username, "Username")
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, 30).WithMessage("Username must be between 3 and 30 characters.")
            .Matches("^[a-zA-Z0-9_]+$")
            .WithMessage("Username may only contain letters, digits, and underscores.");

        builder.RuleFor(static d => d.Email, "Email")
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        builder.RuleFor(static d => d.Password, "Password")
            .NotEmpty().WithMessage("Password is required.")
            .Length(8, 128).WithMessage("Password must be between 8 and 128 characters.");

        builder.RuleFor(static d => d.ConfirmPassword, "ConfirmPassword")
            .NotEmpty().WithMessage("Please confirm your password.");

        builder.RuleFor(static d => d.Age, "Age")
            .InclusiveBetween(13, 120)
            .WithMessage("You must be at least 13 years old.");
    }
}

public sealed class UpdateUserDtoValidator : Validator<UpdateUserDto>
{
    protected override void Configure(ValidatorRuleBuilder<UpdateUserDto> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static d => d.Email, "Email")
            .EmailAddress()
            .WithMessage("A valid email is required when updating the email address.");

        builder.RuleFor(static d => d.DisplayName, "DisplayName")
            .MaximumLength(100)
            .WithMessage("Display name must be at most 100 characters.");
    }
}
