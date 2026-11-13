//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Nested;

[TestClass]
public sealed class IncludeRuleTests {
    [TestMethod]
    public void IncludeRule_AsSyncRule_ExecutesFromValidator() {
        var validator = new CustomerValidator();
        var rule = new IncludeRule<Customer>(validator);
        var result = RuleRunner.Run(rule, new Customer {
            Address = new Address { Street = "", City = "Amsterdam" }
        });
        ValidationAssert.HasError(result, "Address.Street");
    }

    [TestMethod]
    public void IncludeRule_AsSyncRule_WhenValid_Passes() {
        var validator = new CustomerValidator();
        var rule = new IncludeRule<Customer>(validator);
        var result = RuleRunner.Run(rule, new Customer {
            Address = new Address { Street = "Main St", City = "Amsterdam" }
        });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void IncludeExtension_WithValidator_WhenInvalid_Fails() {
        var syncRules = new List<ISyncValidationRule<Customer>>();
        var asyncRules = new List<IAsyncValidationRule<Customer>>();
        var builder = new ValidatorRuleBuilder<Customer>(syncRules, asyncRules);
        builder.Include(new CustomerValidator());
        var validator = new CompositeValidator<Customer>(syncRules, asyncRules);
        var result = validator.Validate(new Customer {
            Address = new Address { Street = "", City = "" }
        });
        ValidationAssert.HasError(result, "Address.Street");
        ValidationAssert.HasError(result, "Address.City");
    }

    [TestMethod]
    public void IncludeExtension_WithAction_WhenValid_Passes() {
        var syncRules = new List<ISyncValidationRule<Model>>();
        var asyncRules = new List<IAsyncValidationRule<Model>>();
        var builder = new ValidatorRuleBuilder<Model>(syncRules, asyncRules);
        builder.Include(b =>
            b.RuleFor(x => x.Name, "Name").NotEmpty()
        );
        var validator = new CompositeValidator<Model>(syncRules, asyncRules);
        var result = validator.Validate(new Model { Name = "Valid" });
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void IncludeExtension_WithAction_WhenInvalid_Fails() {
        var syncRules = new List<ISyncValidationRule<Model>>();
        var asyncRules = new List<IAsyncValidationRule<Model>>();
        var builder = new ValidatorRuleBuilder<Model>(syncRules, asyncRules);
        builder.Include(b =>
            b.RuleFor(x => x.Name, "Name").NotEmpty()
        );
        var validator = new CompositeValidator<Model>(syncRules, asyncRules);
        var result = validator.Validate(new Model { Name = "" });
        ValidationAssert.HasError(result, "Name");
    }

    private sealed class Model {
        public string? Name { get; init; }
    }

    private sealed class CustomerValidator : Validator<Customer> {
        protected override void Configure(ValidatorRuleBuilder<Customer> builder) => builder.RuleFor(x => x.Address, nameof(Customer.Address))
                   .SetValidator(new AddressValidator());
    }
}
