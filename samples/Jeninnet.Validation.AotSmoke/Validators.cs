//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.AotSmoke.Validators;

/// <summary>
/// Adds a custom rule to the generated <see cref="Order"/> validator.
/// </summary>
public sealed partial class OrderValidator {
    partial void AddCustomValidators() =>
        _syncRules.Add(
            new MustRule<Order, string?>(
                Core.ValidatorFactory.Accessor<Order, string?>(
                    static x => x.Reference,
                    nameof(Order.Reference)),
                static reference => reference?.Contains('9', StringComparison.Ordinal) != false,
                "Reference must contain a digit."));
}

/// <summary>
/// Fluent validator with a synchronous and an asynchronous rule.
/// </summary>
public sealed class ShipmentValidator : Validator<Shipment> {
    /// <inheritdoc />
    protected override void Configure(ValidatorRuleBuilder<Shipment> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Code, nameof(Shipment.Code))
               .NotEmpty("Code is required.")
               .Must(
                   static (code, _) => new ValueTask<bool>(code is { Length: >= 3 }),
                   "Code must have at least 3 characters.");
    }
}

/// <summary>
/// Fluent validator whose only rule is asynchronous.
/// </summary>
public sealed class AsyncOnlyRefundValidator : Validator<AsyncOnlyRefund> {
    /// <inheritdoc />
    protected override void Configure(ValidatorRuleBuilder<AsyncOnlyRefund> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Reason, nameof(AsyncOnlyRefund.Reason))
               .Must(
                   static (reason, _) => new ValueTask<bool>(
                       reason?.Contains("ok", StringComparison.Ordinal) is true),
                   "Reason must contain 'ok'.");
    }
}
