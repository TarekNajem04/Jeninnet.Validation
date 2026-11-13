//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Console.Validators;

public sealed class SupplierValidator : Validator<Supplier> {
    private readonly AddressValidator _addressValidator = new();

    protected override void Configure(ValidatorRuleBuilder<Supplier> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(x => x.Id, nameof(Supplier.Id))
               .NotEmpty()
               ;

        builder.RuleFor(x => x.Name, nameof(Supplier.Name))
               .NotNull("name is required.")
               .NotEmpty()
               ;

        builder.RuleFor(x => x.CompanyName, nameof(Supplier.CompanyName))
               .NotEmpty("Company name is required.")
               .Equal("Microsoft")
               .Length(2, 100)
               ;

        builder.RuleFor(static x => x.ContactPerson, nameof(Supplier.ContactPerson))
               .Length(2, 100)
               ;

        builder.RuleFor(static x => x.Email, nameof(Supplier.Email))
               .EmailAddress("Supplier email is invalid.");

        builder.RuleFor(static x => x.PhoneNumber, nameof(Supplier.PhoneNumber))
               .Length(7, 20)
               ;
        // ======================================================================================================
        // ===== Age =====
        // ======================================================================================================
        builder.RuleFor(static x => x.Age, nameof(Supplier.Age))
               .GreaterThan(18)
               ;

        builder.RuleFor(static x => x.AgeOptional, nameof(Supplier.AgeOptional))
               .NotNull()
               .GreaterThan(18)
               ;

        builder.RuleFor(static x => x.AgeValue_RS, nameof(Supplier.AgeValue_RS))
               .GreaterThan(18)
               ;

        // The null value of the nullable value type is always converted to the default value,
        // so you should be careful to use function NotNull() followed by function Cascade(CascadeMode.Stop)
        // to prevent messages from being displayed after the null rule validation.
        builder.RuleFor(static x => x.AgeValue_RS_Optional, nameof(Supplier.AgeValue_RS_Optional))
               .NotNull()
               .GreaterThan(18)
               .Cascade(CascadeMode.Stop)
               ;

        builder.RuleFor(static x => x.AgeValue_RC, nameof(Supplier.AgeValue_RC))
               .GreaterThan(18)
               ;
        builder.RuleFor(static x => x.AgeValue_RC_Optional, nameof(Supplier.AgeValue_RC_Optional))
               .NotNull()
               // The null value of the reference type is always true.
               .GreaterThan(18)
               .Cascade(CascadeMode.Stop)
               ;

        // ======================================================================================================

        builder.RuleFor(static x => x.VatNumber, nameof(Supplier.VatNumber))
               .Length(5, 20)
               ;

        builder.RuleFor(static x => x.Rating, nameof(Supplier.Rating))
               .InclusiveBetween(0m, 5m, "Rating must be between 0 and 5.")
               ;

        builder.RuleFor(static x => x.PhoneNumber, nameof(Supplier.PhoneNumber))
               .NotEqual("055066")
               ;

        builder.RuleFor(static x => x.Address, nameof(Supplier.Address))
               .NotNull("Supplier address is required.")
               .SetValidator(_addressValidator)
               ;
    }
}
