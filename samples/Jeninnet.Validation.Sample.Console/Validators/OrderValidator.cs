//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Console.Validators;

public sealed class OrderValidator : ValidatorDefinition<Order> {
    private readonly CustomerValidator _customerValidator = new();
    private readonly OrderLineValidator _lineValidator = new();

    protected override void Configure(ValidatorRuleBuilder<Order> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static x => x.OrderNumber, nameof(Order.OrderNumber))
               .NotEmpty("Order number is required.")
               .Length(3, 50)
               ;

        builder.RuleFor(static x => x.OrderDate, nameof(Order.OrderDate))
               .Must(static date => date <= DateTime.UtcNow, "Order date cannot be in the future.")
               .Before(DateTime.UtcNow)
               ;

        builder.RuleFor(static x => x.DeliveryDate, nameof(Order.DeliveryDate))
               .GreaterThan(right: ValidatorFactory.Accessor<Order, DateTime?>(x => x.OrderDate, nameof(Order.OrderDate)))
               ;

        builder.RuleFor(static x => x.Customer, nameof(Order.Customer))
               .NotNull("Customer is required.")
               .SetValidator(_customerValidator)
               ;

        builder.RuleFor(static x => x.Lines, nameof(Order.Lines))
               .Must(static lines => lines?.Count is not 0, "Order must contain at least one line.")
               ;

        builder.RuleFor(static x => x.Lines, nameof(Order.Lines))
               .NotEmpty()
               .ForEach(validator: _lineValidator)
               ;

        builder.RuleFor(static x => x.TotalAmount, nameof(Order.TotalAmount))
               .GreaterThan(0m, "Total amount must be greater than 0.")
               ;
    }
}
