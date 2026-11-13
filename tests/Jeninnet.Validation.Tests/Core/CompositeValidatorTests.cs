//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Core;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

file sealed class Product {
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
}

file sealed class ProductValidator : Validator<Product> {
    protected override void Configure(ValidatorRuleBuilder<Product> builder) {
        builder.RuleFor(static x => x.Name, nameof(Product.Name))
               .NotEmpty("Name is required.");

        builder.RuleFor(static x => x.Price, nameof(Product.Price))
               .GreaterThan(0m, "Price must be positive.");

        builder.RuleFor(static x => x.Stock, nameof(Product.Stock))
               .GreaterThanOrEqual(0, "Stock cannot be negative.");
    }
}

// ---------------------------------------------------------------------------
// CompositeValidator integration
// ---------------------------------------------------------------------------

[TestClass]
public sealed class CompositeValidatorTests {
    // -------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------

    [TestMethod]
    public void ValidProduct_Passes() {
        var validator = new ProductValidator();
        var result = validator.Validate(new Product {
            Id = Guid.NewGuid(),
            Name = "Widget",
            Price = 9.99m,
            Stock = 100
        });

        ValidationAssert.IsValid(result);
    }

    // -------------------------------------------------------------------------
    // Multiple failures are all reported
    // -------------------------------------------------------------------------

    [TestMethod]
    public void AllInvalidFields_AllErrorsReported() {
        var validator = new ProductValidator();
        var result = validator.Validate(new Product {
            Id = Guid.Empty,
            Name = "",
            Price = -1m,
            Stock = -5
        });

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
        ValidationAssert.HasError(result, nameof(Product.Name));
        ValidationAssert.HasError(result, nameof(Product.Price));
        ValidationAssert.HasError(result, nameof(Product.Stock));
    }

    // -------------------------------------------------------------------------
    // Async path mirrors sync results
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ValidateAsync_ValidProduct_PassesAsync() {
        var validator = new ProductValidator();
        var result = await validator.ValidateAsync(
            new Product {
                Id = Guid.NewGuid(),
                Name = "Gadget",
                Price = 1m,
                Stock = 0
            },
            TestContext?.CancellationToken ?? CancellationToken.None
        );

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public async Task ValidateAsync_InvalidProduct_ReturnsErrorsAsync() {
        var validator = new ProductValidator();
        var result = await validator.ValidateAsync(
            new Product {
                Name = "",
                Price = 0m,
                Stock = -1
            },
            TestContext?.CancellationToken ?? CancellationToken.None
        );

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
        ValidationAssert.HasError(result, nameof(Product.Name));
    }

    // -------------------------------------------------------------------------
    // Ruleset selection
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RuleSet_OnlyAppropriateRulesRun() {
        // A validator with a billing rule set.
        var services = new List<ISyncValidationRule<Product>>();
        var asyncServices = new List<IAsyncValidationRule<Product>>();
        var builder = new ValidatorRuleBuilder<Product>(services, asyncServices);

        builder.RuleFor(static x => x.Name, nameof(Product.Name))
               .NotEmpty();

        builder.RuleSet("Billing", b =>
            b.RuleFor(static x => x.Price, nameof(Product.Price))
             .GreaterThan(0m)
        );

        var composite = new CompositeValidator<Product>(services, asyncServices);

        // Without ruleset: only Name rule runs.
        var resultAll = composite.Validate(new Product { Name = "", Price = -1m });
        ValidationAssert.HasError(resultAll, nameof(Product.Name));
        ValidationAssert.HasError(resultAll, nameof(Product.Price), "default validation should also run non-ruleset rules");

        // With "Billing" ruleset: only Billing rules run.
        var resultBilling = composite.Validate(
            new Product { Name = "", Price = -1m },
            ["Billing"]
        );
        ValidationAssert.HasError(resultBilling, nameof(Product.Price));
        ValidationAssert.NoErrorFor(resultBilling, nameof(Product.Name), "Name rule is not in 'Billing' ruleset");
    }

    [TestMethod]
    public void ValidateWithOptions_ThrowOnFailures_ThrowsWithAllErrors() {
        var composite = new ProductValidator().Build();

        var act = () => composite.Validate(
            new Product { Name = "", Price = 0m, Stock = -1 },
            new ValidationCallOptions { ThrowOnFailures = true });

        var exception = act.Should().ThrowExactly<ValidationException>().Which;
        MSTestAssertions.That(() => exception.Errors.Should().HaveCount(3));
    }

    [TestMethod]
    public async Task ValidateAsyncWithOptions_ThrowOnFailures_ThrowsWithAllErrorsAsync() {
        var composite = new ProductValidator().Build();

        var act = async () => await composite.ValidateAsync(
            new Product { Name = "", Price = 0m, Stock = -1 },
            new ValidationCallOptions { ThrowOnFailures = true },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        MSTestAssertions.That(() => exception.Which.Errors.Should().HaveCount(3));
    }

    // -------------------------------------------------------------------------
    // Empty rule set — returns Success immediately
    // -------------------------------------------------------------------------

    [TestMethod]
    public void EmptyRuleSet_AlwaysReturnsSuccess() {
        var composite = new CompositeValidator<Product>();
        var result = composite.Validate(new Product { Name = "" });

        ValidationAssert.IsValid(result, "A composite with no rules must always return Success.");
    }

    // -------------------------------------------------------------------------
    // Cascade mode — Stop on first failure
    // -------------------------------------------------------------------------

    [TestMethod]
    public void CascadeStop_StopsAfterFirstFailureOnSameMember() {
        var services = new List<ISyncValidationRule<Product>>();
        var asyncServices = new List<IAsyncValidationRule<Product>>();
        var builder = new ValidatorRuleBuilder<Product>(services, asyncServices);

        // Two rules on Name; cascade is Stop so only one error should surface.
        builder.RuleFor(static x => x.Name, nameof(Product.Name))
               .NotEmpty()
               .NotNull()
               .Cascade(CascadeMode.Stop);

        var composite = new CompositeValidator<Product>(services, asyncServices);
        var result = composite.Validate(new Product { Name = "" });

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1, "CascadeMode.Stop must emit at most one error per member group."));
    }

    public TestContext? TestContext { get; set; }
}
