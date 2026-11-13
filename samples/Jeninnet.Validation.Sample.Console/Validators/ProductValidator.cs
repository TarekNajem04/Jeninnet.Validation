//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Console.Validators;

public sealed class ProductValidator : Validator<Product> {
    private readonly SupplierValidator _supplierValidator = new();
    protected override void Configure(ValidatorRuleBuilder<Product> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static x => x.Id, nameof(Product.Id))
               .NotEmpty()
               ;
        builder.RuleFor(static x => x.Name, nameof(Product.Name))
               .NotNull()
               .NotEmpty()
               .Length(3, 100)
               ;

        builder.RuleFor(static x => x.Code, nameof(Product.Code))
               .NotNull()
               .NotEmpty()
               .Length(2, 50)
               ;

        builder.RuleFor(static x => x.Supplier, nameof(Product.Supplier))
               .NotNull("Supplier is required.")
               .SetValidator(_supplierValidator)
               ;
    }
}
