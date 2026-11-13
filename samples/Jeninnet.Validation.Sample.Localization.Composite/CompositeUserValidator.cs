//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Localization.Composite;

public sealed class CompositeUserValidator : Validator<Customer> {
    protected override void Configure(ValidatorRuleBuilder<Customer> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(Customer.Name)).NotEmpty();
        builder.RuleFor(static x => x.Email, nameof(Customer.Email)).NotEmpty().EmailAddress();
        builder.RuleFor(static x => x.Age, nameof(Customer.Age)).GreaterThan(0);
    }
}
