//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Console.Validators;

public sealed class CustomerValidator : Validator<Customer> {
    private readonly AddressValidator _addressValidator = new();

    protected override void Configure(ValidatorRuleBuilder<Customer> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static x => x.Id, nameof(Customer.Id))
               .NotEmpty()
               ;
        builder.RuleFor(static x => x.FirstName, nameof(Customer.FirstName))
               .NotEmpty()
               .Length(2, 50)
               ;

        builder.RuleFor(static x => x.LastName, nameof(Customer.LastName))
               .NotEmpty()
               .Length(2, 50)
               ;
        builder.RuleFor(static x => x.DateOfBirth, nameof(Customer.DateOfBirth))
               .After(DateTime.Now)
               ;

        builder.RuleFor(static x => x.Email, nameof(Customer.Email))
               .NotEmpty()
               .EmailAddress()
               ;

        builder.RuleFor(static x => x.PhoneNumber, nameof(Customer.PhoneNumber))
               .NotNull()
               .Length(7, 20)
               ;

        builder.RuleFor(static x => x.Age, nameof(Customer.Age))
               .InclusiveBetween(18, 35);

        builder.RuleFor(static x => x.BillingAddress, nameof(Customer.BillingAddress))
               .SetValidator(_addressValidator)
               ;

        builder.RuleFor(static x => x.ShippingAddress, nameof(Customer.ShippingAddress))
               .NotNull()
               .SetValidator(_addressValidator)
               ;
    }
}
