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
namespace Jeninnet.Validation.Sample.ECommerce;

public sealed class AddressValidator : Validator<Address>
{
    protected override void Configure(ValidatorRuleBuilder<Address> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static a => a.Street, "Street")
            .NotEmpty()
            .Length(3, 100);

        builder.RuleFor(static a => a.City, "City")
            .NotEmpty()
            .Length(2, 50);

        builder.RuleFor(static a => a.ZipCode, "ZipCode")
            .NotEmpty()
            .Matches(@"^\d{4,10}$")
            .WithMessage("{PropertyName} must be a valid postal code.");

        builder.RuleFor(static a => a.Country, "Country")
            .NotEmpty()
            .Length(2, 60);
    }
}
