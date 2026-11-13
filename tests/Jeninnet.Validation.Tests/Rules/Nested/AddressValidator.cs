//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Nested;

/// <summary>
/// A hand-written mini-validator for <see cref="Address"/> used inside nested tests.
/// This avoids a dependency on the source generator while still exercising the
/// <see cref="NestedValidatorRule{T,TProperty}"/> plumbing.
/// </summary>
internal sealed class AddressValidator : Validator<Address> {
    protected override void Configure(ValidatorRuleBuilder<Address> builder) {
        builder.RuleFor(static x => x.Street, nameof(Address.Street))
               .NotEmpty("Street is required.");
        builder.RuleFor(static x => x.City, nameof(Address.City))
               .NotEmpty("City is required.");
    }
}
