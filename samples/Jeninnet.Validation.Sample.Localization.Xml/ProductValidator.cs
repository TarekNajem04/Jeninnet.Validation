//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Localization.Xml;

public sealed class ProductValidator : Validator<Product> {
    protected override void Configure(ValidatorRuleBuilder<Product> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static x => x.Name, "Name").NotEmpty();
        builder.RuleFor(static x => x.Price, "Price").GreaterThan(0);
        builder.RuleFor(static x => x.Quantity, "Quantity").InclusiveBetween(1, 100);
    }
}
