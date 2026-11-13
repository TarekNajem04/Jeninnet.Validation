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

public sealed class CustomerValidator : Validator<Customer>
{
    protected override void Configure(ValidatorRuleBuilder<Customer> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static c => c.Name, "Name")
            .NotEmpty()
            .Length(2, 100);

        builder.RuleFor(static c => c.Email, "Email")
            .NotEmpty()
            .EmailAddress();

        // SetValidator: delegates Address validation to a separate validator
        builder.RuleFor(static c => c.Address, "Address")
            .SetValidator(new AddressValidator());
    }
}
