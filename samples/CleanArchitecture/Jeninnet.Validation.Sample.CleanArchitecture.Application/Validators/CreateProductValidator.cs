//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.CleanArchitecture.Application.Validators;

public sealed class CreateProductValidator : Validator<CreateProductRequest> {
    protected override void Configure(ValidatorRuleBuilder<CreateProductRequest> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static x => x.Name, "Name")
            .NotEmpty().Length(1, 200);

        builder.RuleFor(static x => x.Sku, "Sku")
            .NotEmpty().Matches(@"^[A-Z]{2,4}-\d{4,6}$");

        builder.RuleFor(static x => x.Price, "Price")
            .GreaterThan(0m).ScalePrecision(18, 2);

        builder.RuleFor(static x => x.Stock, "Stock")
            .InclusiveBetween(0, 99999);
    }
}
