//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable CA1062

namespace Jeninnet.Validation.Sample.Mvc.Validators;

public sealed class ProductValidator : Validator<ProductViewModel> {
    private static readonly string[] _validCategories = ["Electronics", "Books", "Clothing"];

    protected override void Configure(ValidatorRuleBuilder<ProductViewModel> builder) {
        builder.RuleFor(static x => x.Name, nameof(ProductViewModel.Name))
               .NotEmpty()
               .Length(1, 200)
               .WithCode("PROD_NAME_INVALID");

        builder.RuleFor(static x => x.Sku, nameof(ProductViewModel.Sku))
               .NotEmpty()
               .Matches(@"^[A-Z]{3}-\d{4}$")
               .WithMessage("SKU must follow format XXX-####.");

        builder.RuleFor(static x => x.Price, nameof(ProductViewModel.Price))
               .GreaterThan(0m)
               .ScalePrecision(18, 2);

        builder.RuleFor(static x => x.Stock, nameof(ProductViewModel.Stock))
               .InclusiveBetween(0, 99999);

        builder.RuleFor(static x => x.Category, nameof(ProductViewModel.Category))
               .NotEmpty()
               .In(_validCategories);

        builder.RuleFor(static x => x.Tags, nameof(ProductViewModel.Tags))
               .ForEach(new TagValidator());
    }
}

file sealed class TagValidator : Validator<string> {
    protected override void Configure(ValidatorRuleBuilder<string> builder) =>
        builder.RuleFor(static x => x, "Tag").NotEmpty();
}
