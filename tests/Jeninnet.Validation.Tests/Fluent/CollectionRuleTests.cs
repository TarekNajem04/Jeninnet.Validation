//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Fluent;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

internal sealed class CollectionModel {
    public List<string>? Tags { get; set; }
    public IEnumerable<string>? Sequence { get; set; }
    public List<Line>? Lines { get; set; }
    public Line[]? LineArray { get; set; }
}

internal sealed class Line {
    public bool IsActive { get; set; } = true;
    public int Quantity { get; set; }
}

file sealed class LineValidator : Validator<Line> {
    protected override void Configure(ValidatorRuleBuilder<Line> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Quantity, nameof(Line.Quantity))
               .GreaterThan(0);
    }
}

file sealed class CountValidator : Validator<CollectionModel> {
    protected override void Configure(ValidatorRuleBuilder<CollectionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Tags, nameof(CollectionModel.Tags))
               .Count(1, 3);

        builder.RuleFor(static x => x.Sequence, nameof(CollectionModel.Sequence))
               .MinCount(2)
               .MaxCount(4);
    }
}

file sealed class FilteredLineValidator : Validator<CollectionModel> {
    protected override void Configure(ValidatorRuleBuilder<CollectionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Lines, nameof(CollectionModel.Lines))
               .ForEach(new LineValidator())
               .Where(static line => line.IsActive);

        builder.RuleFor(static x => x.LineArray, nameof(CollectionModel.LineArray))
               .ForEach(new LineValidator())
               .Where(static line => line.IsActive);
    }
}

file sealed class AsyncFilteredLineValidator : Validator<CollectionModel> {
    public int FilterCalls { get; private set; }

    protected override void Configure(ValidatorRuleBuilder<CollectionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Tags, nameof(CollectionModel.Tags))
               .MaxCount(10);

        builder.RuleFor(static x => x.Lines, nameof(CollectionModel.Lines))
               .ForEach(new LineValidator())
               .WhereAsync(async (line, cancellationToken) => {
                   FilterCalls++;
                   await Task.Yield();
                   cancellationToken.ThrowIfCancellationRequested();
                   return line.IsActive;
               });
    }
}

file sealed class CombinedFilterValidator : Validator<CollectionModel> {
    public int AsyncFilterCalls { get; private set; }

    protected override void Configure(ValidatorRuleBuilder<CollectionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Lines, nameof(CollectionModel.Lines))
               .ForEach(new MinimumQuantityValidator())
               .Where(static line => line.IsActive)
               .WhereAsync(async (line, cancellationToken) => {
                   AsyncFilterCalls++;
                   await Task.Yield();
                   cancellationToken.ThrowIfCancellationRequested();
                   return line.Quantity >= 5;
               });
    }
}

file sealed class MinimumQuantityValidator : Validator<Line> {
    protected override void Configure(ValidatorRuleBuilder<Line> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Quantity, nameof(Line.Quantity))
               .GreaterThan(5);
    }
}

file sealed class MissingForEachValidator : Validator<CollectionModel> {
    protected override void Configure(ValidatorRuleBuilder<CollectionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Tags, nameof(CollectionModel.Tags))
               .Where(static _ => true);
    }
}

file sealed class InvalidCountValidator : Validator<CollectionModel> {
    protected override void Configure(ValidatorRuleBuilder<CollectionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Tags, nameof(CollectionModel.Tags))
               .Count(3, 1);
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class CollectionRuleTests {
    [TestMethod]
    public void Count_SkipsNullCollectionsAndValidatesBoundaries() {
        var validator = new CountValidator();

        ValidationAssert.IsValid(validator.Validate(new CollectionModel()));

        var empty = validator.Validate(new CollectionModel { Tags = [], Sequence = ["a"] });
        ValidationAssert.ErrorCount(empty, 2);
        ValidationAssert.HasError(empty, nameof(CollectionModel.Tags));
        ValidationAssert.HasError(empty, nameof(CollectionModel.Sequence));

        var aboveMax = validator.Validate(
            new CollectionModel { Tags = ["a", "b", "c", "d"], Sequence = ["a", "b", "c", "d", "e"] });
        ValidationAssert.ErrorCount(aboveMax, 2);

        var boundaries = validator.Validate(
            new CollectionModel { Tags = ["a", "b", "c"], Sequence = ["a", "b", "c", "d"] });
        ValidationAssert.IsValid(boundaries);
    }

    [TestMethod]
    public void Count_ExposesMessageKeysAndParameters() {
        var result = new CountValidator().Validate(
            new CollectionModel { Tags = [], Sequence = ["a"] });

        ValidationAssert.ErrorCount(result, 2);

        var countError = result.Errors.Single(static error => error.MemberName == nameof(CollectionModel.Tags));
        MSTestAssertions.That(() => countError.MessageKey.Should().Be(ValidationMessageKeys.COUNT));
        MSTestAssertions.That(() => countError.MessageParameters.Should().NotBeNull());
        MSTestAssertions.That(() => countError.MessageParameters!["Min"].Should().Be(1));
        MSTestAssertions.That(() => countError.MessageParameters!["Max"].Should().Be(3));
        MSTestAssertions.That(() => countError.MessageParameters!["Count"].Should().Be(0));
        MSTestAssertions.That(() => countError.MessageParameters!["PropertyValue.Count"].Should().Be(0));

        var minError = result.Errors.Single(static error => error.MemberName == nameof(CollectionModel.Sequence));
        MSTestAssertions.That(() => minError.MessageKey.Should().Be(ValidationMessageKeys.MIN_COUNT));
        MSTestAssertions.That(() => minError.MessageParameters!["Min"].Should().Be(2));
        MSTestAssertions.That(() => minError.MessageParameters!["Count"].Should().Be(1));
    }

    [TestMethod]
    public void Count_RejectsInvalidBounds() {
        Action act = static () => _ = new InvalidCountValidator();

        MSTestAssertions.That(() => act.Should().ThrowExactly<ArgumentOutOfRangeException>());
    }

    [TestMethod]
    public void Where_FiltersElementsAndPreservesOriginalIndices() {
        var result = new FilteredLineValidator().Validate(new CollectionModel {
            Lines =
            [
                new Line { IsActive = true, Quantity = 0 },
                new Line { IsActive = false, Quantity = 0 },
                new Line { IsActive = true, Quantity = 0 }
            ],
            LineArray =
            [
                new Line { IsActive = true, Quantity = 0 },
                new Line { IsActive = false, Quantity = 0 },
                new Line { IsActive = true, Quantity = 0 }
            ]
        });

        ValidationAssert.ErrorCount(result, 4);
        ValidationAssert.HasError(result, "Lines[0].Quantity");
        ValidationAssert.HasError(result, "Lines[2].Quantity");
        ValidationAssert.NoErrorFor(result, "Lines[1].Quantity");
        ValidationAssert.HasError(result, "LineArray[0].Quantity");
        ValidationAssert.HasError(result, "LineArray[2].Quantity");
        ValidationAssert.NoErrorFor(result, "LineArray[1].Quantity");
    }

    [TestMethod]
    public void Where_PreservesIndexAfterNullElement() {
        var result = new FilteredLineValidator().Validate(new CollectionModel {
            Lines = [null!, new Line { IsActive = true, Quantity = 0 }]
        });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Lines[1].Quantity");
    }

    [TestMethod]
    public void WhereAsync_RunsOnlyOnAsyncPath() {
        var validator = new AsyncFilteredLineValidator();
        var model = new CollectionModel {
            Lines =
            [
                new Line { IsActive = true, Quantity = 0 },
                new Line { IsActive = false, Quantity = 0 }
            ]
        };

        var syncResult = validator.Validate(model);
        ValidationAssert.IsValid(syncResult);
        MSTestAssertions.That(() => validator.FilterCalls.Should().Be(0));

        var asyncResult = validator.ValidateAsync(
            model,
            TestContext?.CancellationToken ?? CancellationToken.None).AsTask().GetAwaiter().GetResult();

        ValidationAssert.ErrorCount(asyncResult, 1);
        ValidationAssert.HasError(asyncResult, "Lines[0].Quantity");
        ValidationAssert.NoErrorFor(asyncResult, "Lines[1].Quantity");
        MSTestAssertions.That(() => validator.FilterCalls.Should().Be(2));
    }

    [TestMethod]
    public async Task WhereAndWhereAsync_CombineWithAndAsync() {
        var validator = new CombinedFilterValidator();
        var model = new CollectionModel {
            Lines =
            [
                new Line { IsActive = true, Quantity = 0 },   // fails the async filter
                new Line { IsActive = true, Quantity = 5 },   // passes filters, fails the child rule
                new Line { IsActive = false, Quantity = 5 }   // fails the sync filter
            ]
        };

        var result = await validator.ValidateAsync(
            model,
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Lines[1].Quantity");
        MSTestAssertions.That(() => validator.AsyncFilterCalls.Should().Be(2));
    }

    [TestMethod]
    public void Where_WithoutForEachThrows() {
        Action act = static () => _ = new MissingForEachValidator();

        MSTestAssertions.That(() => act.Should().ThrowExactly<InvalidOperationException>());
    }

    public TestContext? TestContext { get; set; }
}
