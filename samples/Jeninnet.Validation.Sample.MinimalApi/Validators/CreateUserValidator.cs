//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable CA1062

namespace Jeninnet.Validation.Sample.MinimalApi.Validators;

public sealed class CreateUserValidator : Validator<CreateUserRequest> {
    protected override void Configure(ValidatorRuleBuilder<CreateUserRequest> builder) {
        builder.RuleFor(static x => x.Username, "Username")
            .NotEmpty().WithMessage("Username is required.");

        builder.RuleFor(static x => x.Email, "Email")
            .NotEmpty().EmailAddress();

        builder.RuleFor(static x => x.Age, "Age")
            .InclusiveBetween(18, 120);

        builder.RuleFor(static x => x.HomeAddress, "HomeAddress")
            .ChildRules(address => {
                address.RuleFor(static a => a.Street, "Street").NotEmpty();
                address.RuleFor(static a => a.City, "City").NotEmpty();
            });
    }
}
