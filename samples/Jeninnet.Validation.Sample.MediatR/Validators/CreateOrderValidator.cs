//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Sample.MediatR.Validators;

public sealed class CreateOrderValidator : Validator<CreateOrderRequest> {
    protected override void Configure(ValidatorRuleBuilder<CreateOrderRequest> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static x => x.CustomerName, nameof(CreateOrderRequest.CustomerName))
               .NotEmpty().Length(1, 200);

        builder.RuleFor(static x => x.Email, nameof(CreateOrderRequest.Email))
               .NotEmpty().EmailAddress();

        builder.RuleFor(static x => x.Lines, nameof(CreateOrderRequest.Lines))
               .NotEmpty()
               .ForEach(new OrderLineNameValidator())
               .ForEach(new OrderLineQuantityValidator())
               .ForEach(new OrderLinePriceValidator());
    }
}

public sealed class CreateOrderCommandValidator : Validator<CreateOrderCommand> {
    protected override void Configure(ValidatorRuleBuilder<CreateOrderCommand> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static x => x.Request, nameof(CreateOrderCommand.Request))
               .SetValidator(new CreateOrderValidator());
    }
}

file sealed class OrderLineNameValidator : Validator<OrderLineDto> {
    protected override void Configure(ValidatorRuleBuilder<OrderLineDto> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static l => l.ProductName, nameof(OrderLineDto.ProductName))
               .NotEmpty();
    }
}

file sealed class OrderLineQuantityValidator : Validator<OrderLineDto> {
    protected override void Configure(ValidatorRuleBuilder<OrderLineDto> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static l => l.Quantity, nameof(OrderLineDto.Quantity))
               .InclusiveBetween(1, 9999);
    }
}

file sealed class OrderLinePriceValidator : Validator<OrderLineDto> {
    protected override void Configure(ValidatorRuleBuilder<OrderLineDto> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static l => l.UnitPrice, nameof(OrderLineDto.UnitPrice))
               .GreaterThan(0m);
    }
}
