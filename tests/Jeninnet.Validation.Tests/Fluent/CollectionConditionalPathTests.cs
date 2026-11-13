//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class ConditionalLine {
    public string? Name { get; set; }
}

internal sealed class ConditionalOrder {
    public bool Enabled { get; set; }

    public List<ConditionalLine> Lines { get; set; } = [];
}

file sealed class ConditionalLineValidator : Validator<ConditionalLine> {
    protected override void Configure(ValidatorRuleBuilder<ConditionalLine> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static line => line.Name, nameof(ConditionalLine.Name))
               .NotEmpty()
               .WithCode("NAME_REQUIRED");
    }
}

file sealed class ConditionalForEachValidator : Validator<ConditionalOrder> {
    protected override void Configure(ValidatorRuleBuilder<ConditionalOrder> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static order => order.Lines, nameof(ConditionalOrder.Lines))
               .ForEach(new ConditionalLineValidator())
               .When(static order => order.Enabled);
    }
}

file sealed class DualConditionalForEachValidator : Validator<ConditionalOrder> {
    protected override void Configure(ValidatorRuleBuilder<ConditionalOrder> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static order => order.Lines, nameof(ConditionalOrder.Lines))
               .ForEach(new ConditionalLineValidator(), new ConditionalLineValidator())
               .When(static order => order.Enabled);
    }
}

file sealed class FilteredConditionalForEachValidator : Validator<ConditionalOrder> {
    protected override void Configure(ValidatorRuleBuilder<ConditionalOrder> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static order => order.Lines, nameof(ConditionalOrder.Lines))
               .ForEachElement(static element => element.Must(static line => line.Name is not null))
               .Where(static line => line.Name != "skip")
               .When(static order => order.Enabled);
    }
}

file sealed class IndexedConditionalForEachValidator : Validator<ConditionalOrder> {
    protected override void Configure(ValidatorRuleBuilder<ConditionalOrder> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static order => order.Lines, nameof(ConditionalOrder.Lines))
               .ForEach(new ConditionalLineValidator())
               .OverrideIndexer(static (_, index) => $"key{index}")
               .When(static order => order.Enabled);
    }
}

[TestClass]
public sealed class CollectionConditionalPathTests {
    [TestMethod]
    public void ForEach_WhenFalse_SkipsBothPaths() {
        var validator = new ConditionalForEachValidator();
        var order = CreateOrder(enabled: false);

        MSTestAssertions.That(() => validator.Validate(order).IsValid.Should().BeTrue());
    }

    [TestMethod]
    public async Task ForEach_WhenFalse_SkipsAsyncPathAsync() {
        var validator = new ConditionalForEachValidator();
        var order = CreateOrder(enabled: false);

        var result = await validator.ValidateAsync(order, CancellationToken.None);

        MSTestAssertions.That(() => result.IsValid.Should().BeTrue());
    }

    [TestMethod]
    public void ForEach_WhenTrue_ReportsElementPath() {
        var validator = new ConditionalForEachValidator();
        var order = CreateOrder(enabled: true);

        var result = validator.Validate(order);

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("Lines[0].Name"));
        MSTestAssertions.That(() => result.Errors[0].ErrorCode.Should().Be("NAME_REQUIRED"));
    }

    [TestMethod]
    public async Task ForEach_WhenTrue_ReportsElementPathAsync() {
        var validator = new ConditionalForEachValidator();
        var order = CreateOrder(enabled: true);

        var result = await validator.ValidateAsync(order, CancellationToken.None);

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("Lines[0].Name"));
    }

    [TestMethod]
    public void DualForEach_WhenFalse_SkipsSyncPath() {
        var validator = new DualConditionalForEachValidator();
        var order = CreateOrder(enabled: false);

        MSTestAssertions.That(() => validator.Validate(order).IsValid.Should().BeTrue());
    }

    [TestMethod]
    public async Task DualForEach_WhenFalse_SkipsAsyncPathAsync() {
        var validator = new DualConditionalForEachValidator();
        var order = CreateOrder(enabled: false);

        var result = await validator.ValidateAsync(order, CancellationToken.None);

        MSTestAssertions.That(() => result.IsValid.Should().BeTrue());
    }

    [TestMethod]
    public void ForEachElement_WhenAndWhere_PreservesOriginalIndex() {
        var validator = new FilteredConditionalForEachValidator();
        var order = new ConditionalOrder {
            Enabled = true,
            Lines = [
                new ConditionalLine { Name = "skip" },
                new ConditionalLine { Name = null }
            ]
        };

        var result = validator.Validate(order);

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("Lines[1]"));
    }

    [TestMethod]
    public async Task ForEachElement_WhenAndWhere_PreservesOriginalIndexAsync() {
        var validator = new FilteredConditionalForEachValidator();
        var order = new ConditionalOrder {
            Enabled = true,
            Lines = [
                new ConditionalLine { Name = "skip" },
                new ConditionalLine { Name = null }
            ]
        };

        var result = await validator.ValidateAsync(order, CancellationToken.None);

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("Lines[1]"));
    }

    [TestMethod]
    public void ForEach_OverrideIndexer_WithWhen_PreservesCustomPath() {
        var validator = new IndexedConditionalForEachValidator();
        var order = CreateOrder(enabled: true);

        var result = validator.Validate(order);

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("Lines[key0].Name"));
    }

    private static ConditionalOrder CreateOrder(bool enabled) =>
        new() {
            Enabled = enabled,
            Lines = [new ConditionalLine { Name = null }]
        };
}
