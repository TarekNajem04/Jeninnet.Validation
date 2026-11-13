//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Nested;

[TestClass]
public sealed class ForEachRuleTests {
    [TestMethod]
    public void AllElementsValid_Passes() {
        var rule = AddressForEachRule();
        var result = RuleRunner.Run(rule, new Order {
            Lines =
            [
                new Address { Street = "Main St",   City = "Utrecht" },
                new Address { Street = "Side Lane",  City = "Leiden" }
            ]
        });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void SecondElementInvalid_ErrorHasIndexedPath() {
        var rule = AddressForEachRule();
        var result = RuleRunner.Run(rule, new Order {
            Lines =
            [
                new Address { Street = "Main St",   City = "Utrecht" },
                new Address { Street = "",           City = "Leiden" }    // invalid street
            ]
        });

        // We expect an error at "Lines[1].Street".
        MSTestAssertions.That(() => result.Errors.Should().Contain(e => e.MemberName == "Lines[1].Street"));
    }

    [TestMethod]
    public void NullCollection_Skipped() {
        var rule = AddressForEachRule();
        var result = RuleRunner.Run(rule, new Order { Lines = null });
        ValidationAssert.IsValid(result, "ForEachRule must skip null collections.");
    }

    [TestMethod]
    public void EmptyCollection_Passes() {
        var rule = AddressForEachRule();
        var result = RuleRunner.Run(rule, new Order { Lines = [] });
        ValidationAssert.IsValid(result, "ForEachRule must pass on empty collections.");
    }

    [TestMethod]
    public void MultipleInvalidElements_AllIndexesReported() {
        var rule = AddressForEachRule();
        var result = RuleRunner.Run(rule, new Order {
            Lines =
            [
                new Address { Street = "",  City = ""  },   // [0] - two errors
                new Address { Street = "X", City = "Y" },   // [1] - valid
                new Address { Street = "",  City = ""  }    // [2] - two errors
            ]
        });

        MSTestAssertions.That(() => result.Errors.Should().Contain(e => e.MemberName.StartsWith("Lines[0].", StringComparison.Ordinal)));
        MSTestAssertions.That(() => result.Errors.Should().Contain(e => e.MemberName.StartsWith("Lines[2].", StringComparison.Ordinal)));
        MSTestAssertions.That(() => result.Errors.Should().NotContain(e => e.MemberName.StartsWith("Lines[1].", StringComparison.Ordinal)));
    }

    private static ForEachRule<Order, Address> AddressForEachRule() {
        var addressValidator = new AddressValidator();
        return new ForEachRule<Order, Address>(
            accessor: ValidatorFactory.Accessor<Order, List<Address>?>(x => x.Lines, nameof(Order.Lines)),
            syncValidator: addressValidator,
            asyncValidator: addressValidator
        );
    }
}
