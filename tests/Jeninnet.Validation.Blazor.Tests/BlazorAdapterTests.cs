//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Blazor.Tests;

// ---------------------------------------------------------------------------
// Models and validators
// ---------------------------------------------------------------------------

public sealed class BlazorOrder {
    public string? CustomerName { get; set; }

    public BlazorAddress? Address { get; set; }

    public List<BlazorLine> Lines { get; set; } = [];

    public HashSet<BlazorLine>? Set { get; set; }
}

public sealed class BlazorAddress {
    public string? Street { get; set; }
}

public sealed class BlazorLine {
    public string? ProductName { get; set; }
}

public sealed class BlazorOrderValidator : Validator<BlazorOrder> {
    protected override void Configure(ValidatorRuleBuilder<BlazorOrder> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.CustomerName, nameof(BlazorOrder.CustomerName))
               .NotEmpty();

        builder.RuleFor(static x => x.Address, nameof(BlazorOrder.Address))
               .ChildRules(address =>
                   address.RuleFor(static x => x.Street, nameof(BlazorAddress.Street))
                          .NotEmpty());

        builder.RuleFor(static x => x.Lines, nameof(BlazorOrder.Lines))
               .ForEach(new BlazorLineValidator());
    }
}

public sealed class BlazorLineValidator : Validator<BlazorLine> {
    protected override void Configure(ValidatorRuleBuilder<BlazorLine> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.ProductName, nameof(BlazorLine.ProductName))
               .NotEmpty();
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class BlazorAdapterTests {
    [TestMethod]
    public async Task Coordinator_MapsRootNestedAndCollectionErrorsAsync() {
        var model = CreateInvalidModel();
        var context = new EditContext(model);
        using var coordinator = new EditContextValidationCoordinator<BlazorOrder>(
            context,
            new BlazorOrderValidator(),
            model);

        await context.ValidateAsync(TestContext?.CancellationToken ?? CancellationToken.None);

        MSTestAssertions.That(() => Messages(context, model, nameof(BlazorOrder.CustomerName)).Should().BeTrue());
        MSTestAssertions.That(() => Messages(context, model.Address!, nameof(BlazorAddress.Street)).Should().BeTrue());
        MSTestAssertions.That(() => Messages(context, model.Lines[0], nameof(BlazorLine.ProductName)).Should().BeTrue());
    }

    [TestMethod]
    public void Resolver_MapsNestedAndIndexedPaths() {
        var model = CreateInvalidModel();

        var addressField = FieldIdentifierResolver.Resolve(model, "Address.Street");
        var lineField = FieldIdentifierResolver.Resolve(model, "Lines[0].ProductName");

        MSTestAssertions.That(() => ReferenceEquals(addressField.Model, model.Address).Should().BeTrue());
        MSTestAssertions.That(() => addressField.FieldName.Should().Be(nameof(BlazorAddress.Street)));
        MSTestAssertions.That(() => ReferenceEquals(lineField.Model, model.Lines[0]).Should().BeTrue());
        MSTestAssertions.That(() => lineField.FieldName.Should().Be(nameof(BlazorLine.ProductName)));
    }

    [TestMethod]
    public void Resolver_FallsBackToRootForUnknownPath() {
        var model = CreateInvalidModel();

        var field = FieldIdentifierResolver.Resolve(model, "Missing.Path");

        MSTestAssertions.That(() => ReferenceEquals(field.Model, model).Should().BeTrue());
        MSTestAssertions.That(() => field.FieldName.Should().Be("Missing.Path"));
    }

    [TestMethod]
    public void Resolver_CustomIndexerPathFallsBackWithoutThrowing() {
        var model = CreateInvalidModel();

        var field = FieldIdentifierResolver.Resolve(model, "Lines[id-0].ProductName");

        MSTestAssertions.That(() => ReferenceEquals(field.Model, model).Should().BeTrue());
        MSTestAssertions.That(() => field.FieldName.Should().Be("Lines[id-0].ProductName"));
    }

    [TestMethod]
    public void Coordinator_ValidateOnFieldChangedRunsTheValidator() {
        var model = CreateInvalidModel();
        var context = new EditContext(model);
        using var coordinator = new EditContextValidationCoordinator<BlazorOrder>(
            context,
            new BlazorOrderValidator(),
            model,
            validateOnFieldChanged: true);

        context.NotifyFieldChanged(new FieldIdentifier(model, nameof(BlazorOrder.CustomerName)));

        MSTestAssertions.That(() => Messages(context, model, nameof(BlazorOrder.CustomerName)).Should().BeTrue());
    }

    [TestMethod]
    public async Task Coordinator_ClearAndDisposeStopMessagesAsync() {
        var model = CreateInvalidModel();
        var context = new EditContext(model);
        var coordinator = new EditContextValidationCoordinator<BlazorOrder>(
            context,
            new BlazorOrderValidator(),
            model);

        await context.ValidateAsync(TestContext?.CancellationToken ?? CancellationToken.None);
        MSTestAssertions.That(() => Messages(context, model, nameof(BlazorOrder.CustomerName)).Should().BeTrue());

        coordinator.Clear();
        MSTestAssertions.That(() => Messages(context, model, nameof(BlazorOrder.CustomerName)).Should().BeFalse());

        coordinator.Dispose();
        await context.ValidateAsync(TestContext?.CancellationToken ?? CancellationToken.None);
        MSTestAssertions.That(() => Messages(context, model, nameof(BlazorOrder.CustomerName)).Should().BeFalse());
    }

    [TestMethod]
    public void Resolver_FallsBackForNullNestedUnsupportedCollectionAndOutOfRangeIndex() {
        var model = new BlazorOrder {
            Address = null,
            Lines = [new BlazorLine { ProductName = "x" }],
            Set = [new BlazorLine { ProductName = "x" }]
        };

        var nullNested = FieldIdentifierResolver.Resolve(model, "Address.Street");
        var outOfRange = FieldIdentifierResolver.Resolve(model, "Lines[5].ProductName");
        var unsupported = FieldIdentifierResolver.Resolve(model, "Set[0].ProductName");
        var elementLeaf = FieldIdentifierResolver.Resolve(model, "Lines[0]");

        MSTestAssertions.That(() => ReferenceEquals(nullNested.Model, model).Should().BeTrue());
        MSTestAssertions.That(() => nullNested.FieldName.Should().Be("Address.Street"));
        MSTestAssertions.That(() => ReferenceEquals(outOfRange.Model, model).Should().BeTrue());
        MSTestAssertions.That(() => outOfRange.FieldName.Should().Be("Lines[5].ProductName"));
        MSTestAssertions.That(() => ReferenceEquals(unsupported.Model, model).Should().BeTrue());
        MSTestAssertions.That(() => unsupported.FieldName.Should().Be("Set[0].ProductName"));
        MSTestAssertions.That(() => ReferenceEquals(elementLeaf.Model, model).Should().BeTrue());
        MSTestAssertions.That(() => elementLeaf.FieldName.Should().Be("Lines"));
    }

    public TestContext? TestContext { get; set; }

    private static BlazorOrder CreateInvalidModel() => new() {
        CustomerName = "",
        Address = new BlazorAddress { Street = "" },
        Lines = [new BlazorLine { ProductName = "" }]
    };

    private static bool Messages(EditContext context, object model, string fieldName) =>
        context.GetValidationMessages(new FieldIdentifier(model, fieldName))
               .Any(static message => message.Contains("NotEmpty", StringComparison.Ordinal));
}
