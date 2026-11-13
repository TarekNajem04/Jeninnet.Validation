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

public sealed class OrderLineValidator : Validator<OrderLine>
{
    protected override void Configure(ValidatorRuleBuilder<OrderLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static l => l.ProductName, "ProductName")
            .NotEmpty()
            .Length(2, 200);

        builder.RuleFor(static l => l.Quantity, "Quantity")
            .GreaterThan(0)
            .WithMessage("{PropertyName} must be at least 1.");

        builder.RuleFor(static l => l.UnitPrice, "UnitPrice")
            .GreaterThan(0m)
            .WithMessage("{PropertyName} must be greater than zero.");
    }
}
