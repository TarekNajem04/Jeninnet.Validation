//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class ElementModel {
    public List<string?>? Tags { get; set; }
    public string?[]? Values { get; set; }
    public List<ElementLine>? Lines { get; set; }
    public ElementLine? Primary { get; set; }
}

internal sealed class ElementLine {
    public bool Active { get; set; } = true;
    public string? Name { get; set; }
    public int Quantity { get; set; }
}

file sealed class ElementLineValidator : Validator<ElementLine> {
    protected override void Configure(ValidatorRuleBuilder<ElementLine> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ElementLine.Name)).NotEmpty();
        builder.RuleFor(static x => x.Quantity, nameof(ElementLine.Quantity)).GreaterThan(0);
    }
}

file sealed class ElementRuleValidator : Validator<ElementModel> {
    protected override void Configure(ValidatorRuleBuilder<ElementModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Tags, nameof(ElementModel.Tags))
               .ForEachElement(static element => element.NotNull());

        builder.RuleFor(static x => x.Values, nameof(ElementModel.Values))
               .ForEachElement(static element => element.NotNull());

        builder.RuleFor(static x => x.Lines, nameof(ElementModel.Lines))
               .ForEachElement(static line => line.Must(static value => value.Quantity > 0))
               .Where(static line => line.Active)
               .OverrideIndexer(static (_, index) => $"id-{index}");
    }
}

file sealed class AsyncElementRuleValidator : Validator<ElementModel> {
    protected override void Configure(ValidatorRuleBuilder<ElementModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Tags, nameof(ElementModel.Tags))
               .ForEachElement(static element => element.Must(static (value, _) =>
                   new ValueTask<bool>(value != "bad")));
    }
}

file sealed class AsyncFilteredElementValidator : Validator<ElementModel> {
    protected override void Configure(ValidatorRuleBuilder<ElementModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Tags, nameof(ElementModel.Tags))
               .ForEachElement(static element => element.NotEmpty())
               .WhereAsync(static async (element, cancellationToken) => {
                   await Task.Yield();
                   cancellationToken.ThrowIfCancellationRequested();
                   return element != "skip";
               });
    }
}

file sealed class IndexedLineCollectionValidator : Validator<ElementModel> {
    protected override void Configure(ValidatorRuleBuilder<ElementModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Lines, nameof(ElementModel.Lines))
               .ForEach(new ElementLineValidator())
               .OverrideIndexer(static (_, index) => $"n{index}");
    }
}

file sealed class LineCollectionValidator : Validator<ElementModel> {
    protected override void Configure(ValidatorRuleBuilder<ElementModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Lines, nameof(ElementModel.Lines))
               .ForEach(new ElementLineValidator());

        builder.RuleFor(static x => x.Primary, nameof(ElementModel.Primary))
               .SetValidator(new ElementLineValidator());
    }
}

[TestClass]
public sealed class CollectionElementRuleTests {
    [TestMethod]
    public void ForEachElement_ValidatesNullElementsInsteadOfSkippingThem() {
        var validator = new ElementRuleValidator();

        var result = validator.Validate(
            new ElementModel { Tags = ["ok", null], Values = [null] });

        ValidationAssert.ErrorCount(result, 2);
        ValidationAssert.HasError(result, "Tags[1]");
        ValidationAssert.HasError(result, "Values[0]");

        var tagError = result.Errors.Single(static error => error.MemberName == "Tags[1]");
        MSTestAssertions.That(() => tagError.MessageParameters.Should().NotBeNull());
        MSTestAssertions.That(() => tagError.MessageParameters!["CollectionIndex"].Should().Be(1));

        var valueError = result.Errors.Single(static error => error.MemberName == "Values[0]");
        MSTestAssertions.That(() => valueError.MessageParameters!["CollectionIndex"].Should().Be(0));
    }

    [TestMethod]
    public void ForEachElement_FilterPreservesOriginalIndexAndOverridesIndexer() {
        var validator = new ElementRuleValidator();

        var result = validator.Validate(
            new ElementModel {
                Lines = [
                    new ElementLine { Active = false, Quantity = 0 },
                    new ElementLine { Active = true, Quantity = 0 },
                    new ElementLine { Active = true, Quantity = 1 }
                ]
            });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Lines[id-1]");

        var error = result.Errors[0];
        MSTestAssertions.That(() => error.MessageParameters!["CollectionIndex"].Should().Be(1));
    }

    [TestMethod]
    public async Task ForEachElement_AsyncRulesRunOnTheAsyncPathAsync() {
        var validator = new AsyncElementRuleValidator();

        var result = await validator.ValidateAsync(
            new ElementModel { Tags = ["ok", "bad"] },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Tags[1]");
        MSTestAssertions.That(() => result.Errors[0].MessageParameters!["CollectionIndex"].Should().Be(1));
    }

    [TestMethod]
    public void ForEachChildValidator_OverrideIndexerChangesThePath() {
        var validator = new IndexedLineCollectionValidator();

        var result = validator.Validate(
            new ElementModel {
                Lines = [new ElementLine { Name = string.Empty, Quantity = 0 }]
            });

        ValidationAssert.ErrorCount(result, 2);
        ValidationAssert.HasError(result, "Lines[n0].Name");
        ValidationAssert.HasError(result, "Lines[n0].Quantity");
        MSTestAssertions.That(() => result.Errors[0].MessageParameters!["CollectionIndex"].Should().Be(0));
    }

    [TestMethod]
    public async Task ForEachElement_WhereAsyncFiltersOnTheAsyncPathAsync() {
        var validator = new AsyncFilteredElementValidator();

        var result = await validator.ValidateAsync(
            new ElementModel { Tags = ["skip", string.Empty] },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Tags[1]");
    }

    [TestMethod]
    public void IncludeProperties_SupportsCollectionWildcardSubPaths() {
        var validator = new LineCollectionValidator();
        var model = new ElementModel {
            Lines = [new ElementLine { Name = string.Empty, Quantity = 0 }]
        };

        var result = validator.Validate(
            model,
            new ValidationCallOptions { IncludeProperties = ["Lines[].Quantity"] });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Lines[0].Quantity");
        ValidationAssert.NoErrorFor(result, "Lines[0].Name");
    }

    [TestMethod]
    public void ExcludeProperties_SupportsStarCollectionWildcardSubPaths() {
        var validator = new LineCollectionValidator();
        var model = new ElementModel {
            Lines = [new ElementLine { Name = string.Empty, Quantity = 0 }]
        };

        var result = validator.Validate(
            model,
            new ValidationCallOptions { ExcludeProperties = ["Lines[*].Name"] });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Lines[0].Quantity");
        ValidationAssert.NoErrorFor(result, "Lines[0].Name");
    }

    [TestMethod]
    public void IncludeProperties_SupportsNestedObjectSubPaths() {
        var validator = new LineCollectionValidator();
        var model = new ElementModel {
            Primary = new ElementLine { Name = string.Empty, Quantity = 0 }
        };

        var result = validator.Validate(
            model,
            new ValidationCallOptions { IncludeProperties = ["Primary.Quantity"] });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Primary.Quantity");
        ValidationAssert.NoErrorFor(result, "Primary.Name");
    }

    public TestContext? TestContext { get; set; }
}
