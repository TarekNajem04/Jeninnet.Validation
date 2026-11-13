//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Localization.Base;

public sealed class UserValidator : Validator<User> {
    public static readonly UserValidator Instance = new();

    protected override void Configure(ValidatorRuleBuilder<User> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(User.Name)).NotEmpty();
        builder.RuleFor(static x => x.Email, nameof(User.Email)).NotEmpty().EmailAddress();
        builder.RuleFor(static x => x.Password, nameof(User.Password)).NotEmpty().Length(8, 64);
        builder.RuleFor(static x => x.Age, nameof(User.Age)).InclusiveBetween(18, 60);
    }
}
