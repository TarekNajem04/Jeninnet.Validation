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

internal sealed class SharedContextModel {
    public string? First { get; set; }
    public string? Second { get; set; }
}

file sealed class ContextProbeRule<T>(
    IPropertyAccessor<T, string?> accessor,
    Action<object?, RootContextData?, IServiceProvider?> probe)
    : SyncValidateRule<T, string?>(accessor, message: null, messageKey: null) {
    protected override void ValidateValue(string? value, in ValidationContext<T> context, ValidationErrorCollector errors) =>
        probe(context.Root, context.RootData, context.ServiceProvider);
}

file sealed class ProbeValidator : Validator<SharedContextModel> {
    public List<(object? Root, RootContextData? Bag, IServiceProvider? Services)> Captures { get; } = [];

    protected override void Configure(ValidatorRuleBuilder<SharedContextModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        var first = builder.RuleFor(static x => x.First, nameof(SharedContextModel.First));
        first.AddRule(
            new ContextProbeRule<SharedContextModel>(
                first.Accessor,
                (root, bag, services) => Captures.Add((root, bag, services))));

        var second = builder.RuleFor(static x => x.Second, nameof(SharedContextModel.Second));
        second.AddRule(
            new ContextProbeRule<SharedContextModel>(
                second.Accessor,
                (root, bag, services) => Captures.Add((root, bag, services))));
    }
}

file sealed class MemoValidator : Validator<SharedContextModel> {
    public string? Seen { get; private set; }

    protected override void Configure(ValidatorRuleBuilder<SharedContextModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        var first = builder.RuleFor(static x => x.First, nameof(SharedContextModel.First));
        first.AddRule(
            new ContextProbeRule<SharedContextModel>(
                first.Accessor,
                (_, bag, _) => bag!["shared"] = "memo"));

        var second = builder.RuleFor(static x => x.Second, nameof(SharedContextModel.Second));
        second.AddRule(
            new ContextProbeRule<SharedContextModel>(
                second.Accessor,
                (_, bag, _) => Seen = bag!.TryGetValue("shared", out var value) ? value as string : null));
    }
}

file sealed class StubServiceProvider : IServiceProvider {
    public object? GetService(Type serviceType) => null;
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class SharedContextTests {
    [TestMethod]
    public void Rules_ShareRootAndRootDataOnSyncPath() {
        var validator = new ProbeValidator();
        var model = new SharedContextModel { First = "a", Second = "b" };

        var result = validator.Validate(model);

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => validator.Captures.Should().HaveCount(2));
        MSTestAssertions.That(() => ReferenceEquals(validator.Captures[0].Root, model).Should().BeTrue());
        MSTestAssertions.That(() => validator.Captures[0].Bag.Should().NotBeNull());
        MSTestAssertions.That(() => ReferenceEquals(validator.Captures[0].Bag, validator.Captures[1].Bag).Should().BeTrue());
        MSTestAssertions.That(() => validator.Captures[0].Services.Should().BeNull());
    }

    [TestMethod]
    public async Task Rules_ShareRootDataOnAsyncPathAsync() {
        var validator = new ProbeValidator();
        var model = new SharedContextModel { First = "a", Second = "b" };

        var result = await validator.ValidateAsync(
            model,
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => validator.Captures.Should().HaveCount(2));
        MSTestAssertions.That(() => ReferenceEquals(validator.Captures[0].Root, model).Should().BeTrue());
        MSTestAssertions.That(() => ReferenceEquals(validator.Captures[0].Bag, validator.Captures[1].Bag).Should().BeTrue());
    }

    [TestMethod]
    public void RootData_MemoizationIsVisibleAcrossRules() {
        var validator = new MemoValidator();

        var result = validator.Validate(new SharedContextModel { First = "a", Second = "b" });

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => validator.Seen.Should().Be("memo"));
    }

    [TestMethod]
    public void ServiceProvider_FlowsFromCallOptions() {
        var validator = new ProbeValidator();
        var services = new StubServiceProvider();

        _ = validator.Validate(
            new SharedContextModel { First = "a", Second = "b" },
            new ValidationCallOptions { ServiceProvider = services });

        MSTestAssertions.That(() => validator.Captures.Should().HaveCount(2));
        MSTestAssertions.That(() => ReferenceEquals(validator.Captures[0].Services, services).Should().BeTrue());
    }

    [TestMethod]
    public void ServiceProvider_FlowsFromStrategy() {
        var validator = new ProbeValidator();
        var services = new StubServiceProvider();

        _ = validator.Validate(
            new SharedContextModel { First = "a", Second = "b" },
            strategy => strategy.WithServiceProvider(services));

        MSTestAssertions.That(() => validator.Captures.Should().HaveCount(2));
        MSTestAssertions.That(() => ReferenceEquals(validator.Captures[0].Services, services).Should().BeTrue());
    }

    [TestMethod]
    public void ChildContext_PreservesRootBagAndServiceProvider() {
        var root = new SharedContextModel();
        var child = new SharedContextModel();
        var bag = new RootContextData();
        var services = new StubServiceProvider();
        var context = new ValidationContext<SharedContextModel>(
            root,
            MemberPath: string.Empty,
            Root: root,
            RootData: bag,
            ServiceProvider: services);

        var childContext = context.ForChild(child, "Child");

        MSTestAssertions.That(() => ReferenceEquals(childContext.Instance, child).Should().BeTrue());
        MSTestAssertions.That(() => childContext.MemberPath.Should().Be("Child"));
        MSTestAssertions.That(() => ReferenceEquals(childContext.Root, root).Should().BeTrue());
        MSTestAssertions.That(() => ReferenceEquals(childContext.RootData, bag).Should().BeTrue());
        MSTestAssertions.That(() => ReferenceEquals(childContext.ServiceProvider, services).Should().BeTrue());
    }

    [TestMethod]
    public void RootData_LazyBagSupportsReadWriteAndMemoization() {
        var data = new RootContextData();

        MSTestAssertions.That(() => data.Count.Should().Be(0));
        MSTestAssertions.That(() => data.TryGetValue("missing", out _).Should().BeFalse());

        data["answer"] = 42;
        MSTestAssertions.That(() => data.Count.Should().Be(1));
        MSTestAssertions.That(() => data["answer"].Should().Be(42));

        var memo = data.GetOrAdd("memo", () => "created");
        MSTestAssertions.That(() => memo.Should().Be("created"));
        MSTestAssertions.That(() => data.GetOrAdd("memo", () => "other").Should().Be("created"));

        data.Clear();
        MSTestAssertions.That(() => data.Count.Should().Be(0));
    }

    public TestContext? TestContext { get; set; }
}
