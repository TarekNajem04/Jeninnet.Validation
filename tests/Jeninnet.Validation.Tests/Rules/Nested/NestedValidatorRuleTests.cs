//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Nested;

[TestClass]
public sealed class NestedValidatorRuleTests {
    [TestMethod]
    public void ValidNestedObject_Passes() {
        var rule = MakeNestedRule();
        var result = RuleRunner.Run(rule, new Customer {
            Address = new Address { Street = "Main St", City = "Amsterdam" }
        });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void InvalidNestedObject_ErrorsIncludeNestedPath() {
        var rule = MakeNestedRule();
        var result = RuleRunner.Run(rule, new Customer {
            Address = new Address { Street = "", City = "" }
        });

        // Errors must be prefixed with the parent property name.
        MSTestAssertions.That(() => result.Errors.Should().Contain(
            e => e.MemberName.StartsWith("Address.", StringComparison.Ordinal),
            "Expected errors prefixed with 'Address.' but got: {0}",
            string.Join(", ", result.Errors.Select(e => e.MemberName))));
    }

    [TestMethod]
    public void NullNestedObject_Skipped() {
        var rule = MakeNestedRule();
        var result = RuleRunner.Run(rule, new Customer { Address = null });
        ValidationAssert.IsValid(result,
            "NestedValidatorRule must not fire when the nested object is null.");
    }

    [TestMethod]
    public void DeepPath_WhenRunUnderParentContext_IncludesFullPath() {
        var rule = MakeNestedRule();
        // Simulate this nested rule running inside an "Order" parent.
        var result = RuleRunner.RunWithPath(rule,
            new Customer { Address = new Address { Street = "", City = "Amsterdam" } },
            "Order.Customer");

        MSTestAssertions.That(() => result.Errors.Should().Contain(
            e => e.MemberName == "Order.Customer.Address.Street",
            "Expected 'Order.Customer.Address.Street' but got: {0}",
            string.Join(", ", result.Errors.Select(e => e.MemberName))));
    }

    private static NestedValidatorRule<Customer, Address> MakeNestedRule() {
        var addressValidator = new AddressValidator();
        return new NestedValidatorRule<Customer, Address>(
            ValidatorFactory.Accessor<Customer, Address?>(
                x => x.Address, nameof(Customer.Address)),
            addressValidator);
    }
}
