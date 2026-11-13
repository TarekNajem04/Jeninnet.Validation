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

public sealed class PaymentValidator : Validator<Payment>
{
    protected override void Configure(ValidatorRuleBuilder<Payment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static p => p.Method, "Method")
            .NotEmpty()
            .Must(method => method is "CreditCard" or "PayPal" or "BankTransfer")
            .WithMessage("{PropertyName} must be CreditCard, PayPal, or BankTransfer.");

        builder.RuleFor(static p => p.TransactionId, "TransactionId")
            .NotEmpty()
            .WithMessage("A transaction identifier is required.");

        builder.RuleFor(static p => p.Amount, "Amount")
            .GreaterThan(0m);
    }
}
