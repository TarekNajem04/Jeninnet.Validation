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

namespace Jeninnet.Validation.Sample.ErrorHandling;

public sealed record PaymentRequest(
    string CardNumber,
    string CardholderName,
    decimal Amount,
    string Currency);

public sealed class PaymentRequestValidator : Validator<PaymentRequest>
{
    private static readonly string[] _allowedCurrencies = ["USD", "EUR", "GBP"];

    protected override void Configure(ValidatorRuleBuilder<PaymentRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Error-level — blocks processing
        builder.RuleFor(static r => r.CardNumber, "CardNumber")
            .NotEmpty().WithMessage("Card number is required.")
                .WithCode("CARD_REQUIRED").WithSeverity(ValidationErrorSeverity.Error)
            .CreditCard().WithMessage("Card number is not valid.")
                .WithCode("CARD_INVALID").WithSeverity(ValidationErrorSeverity.Error);

        // Warning-level — accepted but flagged
        builder.RuleFor(static r => r.CardholderName, "CardholderName")
            .NotEmpty().WithMessage("Cardholder name is required.")
                .WithCode("NAME_REQUIRED").WithSeverity(ValidationErrorSeverity.Error)
            .MinimumLength(3).WithMessage("Cardholder name is very short.")
                .WithCode("NAME_SHORT").WithSeverity(ValidationErrorSeverity.Warning);

        // Info-level — informational observation
        builder.RuleFor(static r => r.Amount, "Amount")
            .GreaterThan(0).WithMessage("Amount must be positive.")
                .WithCode("AMOUNT_POSITIVE").WithSeverity(ValidationErrorSeverity.Error)
            .GreaterThan(0.01m).WithMessage("Amount is very small.")
                .WithCode("AMOUNT_SMALL").WithSeverity(ValidationErrorSeverity.Info);

        // CustomState — attach extra data
        builder.RuleFor(static r => r.Currency, "Currency")
            .NotEmpty().WithMessage("Currency is required.")
                .WithCode("CURRENCY_REQUIRED").WithSeverity(ValidationErrorSeverity.Error)
                .WithState(new { Currencies = _allowedCurrencies })
            .Must(currency => currency is "USD" or "EUR" or "GBP")
                .WithMessage("Currency must be USD, EUR, or GBP.")
                .WithCode("CURRENCY_UNSUPPORTED").WithSeverity(ValidationErrorSeverity.Error)
                .WithState(new { Supported = "USD, EUR, GBP" });
    }
}
