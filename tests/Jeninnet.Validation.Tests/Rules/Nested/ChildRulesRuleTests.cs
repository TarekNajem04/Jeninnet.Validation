//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Nested;

[TestClass]
public sealed class ChildRulesRuleTests {
    [TestMethod]
    public void ValidChildObject_Passes() {
        var rule = MakeChildRule();
        var result = RuleRunner.Run(rule, new Customer {
            Address = new Address { Street = "Main St", City = "Amsterdam" }
        });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void InvalidChildObject_ErrorsIncludeNestedPath() {
        var rule = MakeChildRule();
        var result = RuleRunner.Run(rule, new Customer {
            Address = new Address { Street = "", City = "" }
        });

        MSTestAssertions.That(() => result.Errors.Should().Contain(
            e => e.MemberName.StartsWith("Address.", StringComparison.Ordinal),
            "Expected errors prefixed with 'Address.' but got: {0}",
            string.Join(", ", result.Errors.Select(e => e.MemberName))));
    }

    [TestMethod]
    public void NullChildObject_Skipped() {
        var rule = MakeChildRule();
        var result = RuleRunner.Run(rule, new Customer { Address = null });
        ValidationAssert.IsValid(result,
            "ChildRulesRule must not fire when the nested object is null.");
    }

    [TestMethod]
    public void DeepPath_WhenRunUnderParentContext_IncludesFullPath() {
        var rule = MakeChildRule();
        var result = RuleRunner.RunWithPath(rule,
            new Customer { Address = new Address { Street = "", City = "Amsterdam" } },
            "Order.Customer");

        MSTestAssertions.That(() => result.Errors.Should().Contain(
            e => e.MemberName == "Order.Customer.Address.Street",
            "Expected 'Order.Customer.Address.Street' but got: {0}",
            string.Join(", ", result.Errors.Select(e => e.MemberName))));
    }

    [TestMethod]
    public void MultipleChildRules_AllEvaluated() {
        var syncRules = new List<ISyncValidationRule<Address>>();
        var asyncRules = new List<IAsyncValidationRule<Address>>();
        var inner = new ValidatorRuleBuilder<Address>(syncRules, asyncRules);
        inner.RuleFor(x => x.Street, "Street").NotEmpty();
        inner.RuleFor(x => x.City, "City").NotEmpty();

        var rule = new ChildRulesRule<Customer, Address>(
            ValidatorFactory.Accessor<Customer, Address?>(x => x.Address, nameof(Customer.Address)),
            syncRules, asyncRules);

        var result = RuleRunner.Run(rule, new Customer {
            Address = new Address { Street = "", City = "" }
        });

        ValidationAssert.ErrorCount(result, 2);
    }

    private static ChildRulesRule<Customer, Address> MakeChildRule() {
        var syncRules = new List<ISyncValidationRule<Address>>();
        var asyncRules = new List<IAsyncValidationRule<Address>>();
        var inner = new ValidatorRuleBuilder<Address>(syncRules, asyncRules);
        inner.RuleFor(x => x.Street, "Street").NotEmpty();
        inner.RuleFor(x => x.City, "City").NotEmpty();

        return new ChildRulesRule<Customer, Address>(
            ValidatorFactory.Accessor<Customer, Address?>(x => x.Address, nameof(Customer.Address)),
            syncRules, asyncRules);
    }
}
