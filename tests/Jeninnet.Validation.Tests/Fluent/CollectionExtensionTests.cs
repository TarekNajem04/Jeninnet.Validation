//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class CollectionChild {
    public string? Name { get; set; }
}

file sealed class CollectionChildValidator : Validator<CollectionChild> {
    protected override void Configure(ValidatorRuleBuilder<CollectionChild> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(CollectionChild.Name)).NotEmpty();
    }
}

internal sealed class CollectionShapeModel {
    public IEnumerable<CollectionChild>? Enumerable { get; set; }
    public ICollection<CollectionChild>? Collection { get; set; }
    public IList<CollectionChild>? List { get; set; }
    public List<CollectionChild>? ConcreteList { get; set; }
    public CollectionChild[]? Array { get; set; }
}

file sealed class CollectionShapeModelValidator : Validator<CollectionShapeModel> {
    protected override void Configure(ValidatorRuleBuilder<CollectionShapeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        var child = new CollectionChildValidator();

        builder.RuleFor(static x => x.Enumerable, nameof(CollectionShapeModel.Enumerable)).ForEach(child);
        builder.RuleFor(static x => x.Collection, nameof(CollectionShapeModel.Collection)).ForEach(child);
        builder.RuleFor(static x => x.List, nameof(CollectionShapeModel.List)).ForEach(child);
        builder.RuleFor(static x => x.ConcreteList, nameof(CollectionShapeModel.ConcreteList)).ForEach(child);
        builder.RuleFor(static x => x.Array, nameof(CollectionShapeModel.Array)).ForEach(child);
    }
}

internal sealed class FilteredCollectionShapeModel {
    public List<CollectionChild>? Items { get; set; }
}

file sealed class FilteredCollectionValidator : Validator<FilteredCollectionShapeModel> {
    protected override void Configure(ValidatorRuleBuilder<FilteredCollectionShapeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Items, nameof(FilteredCollectionShapeModel.Items))
               .ForEach(new CollectionChildValidator())
               .Where(static x => x.Name != "skip");

        builder.RuleFor(static x => x.Items, nameof(FilteredCollectionShapeModel.Items))
               .ForEach(new CollectionChildValidator())
               .WhereAsync(static (x, cancellationToken) => {
                   cancellationToken.ThrowIfCancellationRequested();
                   return ValueTask.FromResult(x.Name != "skip");
               });
    }
}

[TestClass]
public sealed class CollectionExtensionTests {
    [TestMethod]
    public void ForEach_ValidatesEverySupportedCollectionShape() {
        var validator = new CollectionShapeModelValidator();
        var invalid = new CollectionShapeModel {
            Enumerable = [new CollectionChild { Name = "" }],
            Collection = [new CollectionChild { Name = "" }],
            List = [new CollectionChild { Name = "" }],
            ConcreteList = [new CollectionChild { Name = "" }],
            Array = [new CollectionChild { Name = "" }]
        };

        var result = validator.Validate(invalid);

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(5));

        var valid = new CollectionShapeModel {
            Enumerable = [new CollectionChild { Name = "a" }],
            Collection = [new CollectionChild { Name = "b" }],
            List = [new CollectionChild { Name = "c" }],
            ConcreteList = [new CollectionChild { Name = "d" }],
            Array = [new CollectionChild { Name = "e" }]
        };

        MSTestAssertions.That(() => validator.Validate(valid).IsValid.Should().BeTrue());
    }

    [TestMethod]
    public void Where_FiltersElementsAndPreservesOriginalIndexes() {
        var validator = new FilteredCollectionValidator();
        var model = new FilteredCollectionShapeModel {
            Items = [new CollectionChild { Name = "skip" }, new CollectionChild { Name = "" }]
        };

        var result = validator.Validate(model);

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => result.Errors.All(static error => error.MemberName.Contains("[1]", StringComparison.Ordinal))
            .Should().BeTrue());
    }

    [TestMethod]
    public async Task WhereAsync_FiltersElementsOnTheAsyncPathAsync() {
        var validator = new FilteredCollectionValidator();
        var model = new FilteredCollectionShapeModel {
            Items = [new CollectionChild { Name = "skip" }, new CollectionChild { Name = "" }]
        };

        var result = await validator.ValidateAsync(model, TestCancellation);

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(2));
        MSTestAssertions.That(() => result.Errors.All(static error => error.MemberName.Contains("[1]", StringComparison.Ordinal))
            .Should().BeTrue());
    }

    private CancellationToken TestCancellation => TestContext?.CancellationToken ?? CancellationToken.None;

    public TestContext? TestContext { get; set; }
}
