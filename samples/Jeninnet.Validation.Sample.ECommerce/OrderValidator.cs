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

public sealed class OrderValidator : Validator<Order>
{
    protected override void Configure(ValidatorRuleBuilder<Order> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static o => o.OrderNumber, "OrderNumber")
            .NotEmpty()
            .Matches(@"^ORD-\d{6}$")
            .WithMessage("{PropertyName} must match ORD-###### format.");

        // Include: import rules from another Order validator inline
        builder.Include(rules =>
        {
            rules.RuleFor(static o => o.OrderNumber, "OrderNumber")
                .MinimumLength(5);
        });

        // SetValidator: delegates Customer validation
        builder.RuleFor(static o => o.Customer, "Customer")
            .SetValidator(new CustomerValidator());

        // RuleForEach: validates each item in the Lines collection
        builder.RuleFor(static o => o.Lines, "Lines")
            .ForEach(new OrderLineValidator());

        // ChildRules: inline nested rules
        builder.RuleFor(static o => o.Payment, "Payment")
            .ChildRules(payment =>
            {
                payment.RuleFor(static p => p.Method, "Method")
                    .NotEmpty()
                    .WithMessage("Payment method is required.");

                payment.RuleFor(static p => p.TransactionId, "TransactionId")
                    .NotEmpty();

                payment.RuleFor(static p => p.Amount, "Amount")
                    .GreaterThan(0m);
            });
    }
}
