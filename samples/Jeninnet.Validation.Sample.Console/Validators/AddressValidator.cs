//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Console.Validators;

public sealed class AddressValidator : Validator<Address> {
    protected override void Configure(ValidatorRuleBuilder<Address> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static x => x.Street, nameof(Address.Street))
               .NotEmpty("Street is required.")
               .Length(2, 100, "Street must be between 2 and 100 characters.")
               ;

        builder.RuleFor(static x => x.City, nameof(Address.City))
               .NotEmpty("City is required.")
               .Length(2, 50);

        builder.RuleFor(static x => x.PostalCode, nameof(Address.PostalCode))
               .NotEmpty("Postal code is required.")
               .Length(3, 10)
               ;

        builder.RuleFor(static x => x.Country, nameof(Address.Country))
               .NotEmpty("Country is required.")
               .Length(2, 50)
               ;
    }
}
