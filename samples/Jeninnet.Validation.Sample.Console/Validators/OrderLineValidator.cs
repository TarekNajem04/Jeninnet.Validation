//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Console.Validators;

public sealed class OrderLineValidator : Validator<OrderLine> {
    private readonly ProductValidator _productValidator = new();
    protected override void Configure(ValidatorRuleBuilder<OrderLine> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static x => x.Product, nameof(OrderLine.Product))
               .NotNull()
               .SetValidator(_productValidator)
               ;

        builder.RuleFor(static x => x.Quantity, nameof(OrderLine.Quantity))
               .GreaterThan(0, "Quantity must be greater than 0.")
               ;

        builder.RuleFor(static x => x.UnitPrice, nameof(OrderLine.UnitPrice))
               .GreaterThan(0m, "Unit price must be greater than 0.")
               ;

        builder.RuleFor(static x => x.DiscountPercentage, nameof(OrderLine.DiscountPercentage))
               .InclusiveBetween(0m, 100m, "Discount must be between 0 and 100.")
               ;
    }
}
