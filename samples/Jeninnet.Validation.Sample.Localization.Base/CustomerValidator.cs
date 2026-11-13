//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Localization.Base;

/// <summary>
/// Represents a validator for the Customer class, defining validation rules for its properties.
/// </summary>
public sealed class CustomerValidator : Validator<Customer> {
    public static readonly CustomerValidator Instance = new();

    protected override void Configure(ValidatorRuleBuilder<Customer> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(Customer.Name)).NotEmpty();
        builder.RuleFor(static x => x.Email, nameof(Customer.Email)).NotEmpty().EmailAddress();
        builder.RuleFor(static x => x.Age, nameof(Customer.Age)).InclusiveBetween(18, 60);
    }
}
