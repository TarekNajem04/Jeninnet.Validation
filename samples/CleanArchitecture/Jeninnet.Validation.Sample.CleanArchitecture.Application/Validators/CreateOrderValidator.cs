//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.CleanArchitecture.Application.Validators;

public sealed class CreateOrderValidator : Validator<CreateOrderRequest> {
    protected override void Configure(ValidatorRuleBuilder<CreateOrderRequest> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static x => x.CustomerName, "CustomerName")
            .NotEmpty().Length(1, 200);

        builder.RuleFor(static x => x.Email, "Email")
            .NotEmpty().EmailAddress();

        builder.RuleFor(static x => x.Lines, "Lines")
            .NotEmpty()
            .ForEach(new OrderLineNameValidator())
            .ForEach(new OrderLineQuantityValidator())
            .ForEach(new OrderLinePriceValidator());
    }
}

file sealed class OrderLineNameValidator : Validator<OrderLineDto> {
    protected override void Configure(ValidatorRuleBuilder<OrderLineDto> builder) =>
        builder.RuleFor(static l => l.ProductName, "ProductName").NotEmpty();
}

file sealed class OrderLineQuantityValidator : Validator<OrderLineDto> {
    protected override void Configure(ValidatorRuleBuilder<OrderLineDto> builder) =>
        builder.RuleFor(static l => l.Quantity, "Quantity").InclusiveBetween(1, 999);
}

file sealed class OrderLinePriceValidator : Validator<OrderLineDto> {
    protected override void Configure(ValidatorRuleBuilder<OrderLineDto> builder) =>
        builder.RuleFor(static l => l.UnitPrice, "UnitPrice").GreaterThan(0m);
}
