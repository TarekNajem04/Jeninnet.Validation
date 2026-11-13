//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Console.Validators;

public sealed class UserValidator : ValidatorDefinition<User> {
    protected override void Configure(ValidatorRuleBuilder<User> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static x => x.Id, nameof(User.Id))
               .NotEmpty()
               ;
        builder.RuleFor(static x => x.Username, nameof(User.Username))
               .NotEmpty()
               .Length(3, 50)
               ;

        builder.RuleFor(static x => x.Password, nameof(User.Password))
               .NotNull()
               .NotEmpty()
               .MinimumLength(8, "Password must be at least 8 characters.")
               .MaximumLength(20, "Password must not exceed 20 characters.")
               .Must(static p => p?.Any(char.IsDigit) is true, "Password must contain at least one digit.")
               .Must(static p => p?.Any(char.IsUpper) is true, "Password must contain at least one uppercase letter.")
               ;

        builder.RuleFor(static x => x.Email, nameof(User.Email))
               .EmailAddress()
               ;

        builder.RuleFor(static x => x.Role, nameof(User.Role))
               .NotEmpty()
               ;

        builder.RuleFor(static x => x.CreatedAt, nameof(User.CreatedAt))
               .After(DateTime.Now)
               ;
    }
}
