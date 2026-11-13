//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace JeninnetValidationTemplate;

/// <summary>
/// Validates <see cref="Customer"/> with the fluent API.
/// </summary>
public sealed class CustomerValidator : Validator<Customer> {
    /// <inheritdoc />
    protected override void Configure(ValidatorRuleBuilder<Customer> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(Customer.Name))
               .NotEmpty()
               .MaximumLength(100);

        builder.RuleFor(static model => model.Email, nameof(Customer.Email))
               .NotEmpty()
               .EmailAddress();

        builder.RuleFor(static model => model.Age, nameof(Customer.Age))
               .InclusiveBetween(18, 120);
    }
}
