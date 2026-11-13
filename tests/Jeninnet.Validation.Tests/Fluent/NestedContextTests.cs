//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class NestedContextParent {
    public NestedContextChild? Child { get; set; }
    public List<NestedContextChild> Children { get; set; } = [];
}

internal sealed class NestedContextChild {
    public string? First { get; set; }
}

file sealed class ContextProbeRule<T>(
    IPropertyAccessor<T, string?> accessor,
    Action<ValidationContext<T>> probe)
    : SyncValidateRule<T, string?>(accessor, message: null, messageKey: null) {
    protected override void ValidateValue(string? value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        probe(context);

        errors.Add(
            new ValidationError(
                memberName: context.GetFullPath(Accessor.MemberName),
                messageId: MessageId.Invalid,
                messageKey: "Probe",
                messageParameters: null,
                message: "probe-error",
                errorCode: "PROBE",
                severity: ValidationErrorSeverity.Warning,
                customState: "probe-state"));
    }
}

file sealed class NestedContextParentValidator : Validator<NestedContextParent> {
    public List<CapturedContext> Captures { get; } = [];

    protected override void Configure(ValidatorRuleBuilder<NestedContextParent> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        var childAccessor = ValidatorFactory.Accessor<NestedContextChild, string?>(
            static child => child.First,
            nameof(NestedContextChild.First));

        var childRule = new ContextProbeRule<NestedContextChild>(
            childAccessor,
            context => Captures.Add(CapturedContext.From(context)));

        var childRuleSet = new RuleSetRule<NestedContextChild>(["child"], [childRule], []);
        var childValidator = new CompositeValidator<NestedContextChild>([childRuleSet], null);

        builder.RuleSet("child", child => {
            child.RuleFor(static parent => parent.Child, nameof(NestedContextParent.Child))
                 .SetValidator(childValidator);

            child.RuleFor(static parent => parent.Children, nameof(NestedContextParent.Children))
                 .ForEach(childValidator);
        });
    }
}

internal sealed record CapturedContext(
    object? Root,
    RootContextData? RootData,
    IServiceProvider? Services,
    string MemberPath,
    IEnumerable<string>? RuleSets,
    bool FailFast) {
    public static CapturedContext From<T>(ValidationContext<T> context) =>
        new(context.Root, context.RootData, context.ServiceProvider, context.MemberPath, context.RuleSets, context.FailFast);
}

file sealed class NestedStubServiceProvider : IServiceProvider {
    public object? GetService(Type serviceType) => null;
}

[TestClass]
public sealed class NestedContextTests {
    [TestMethod]
    public void NestedValidator_PreservesRootDataServicesRuleSetsAndFailFast() {
        var validator = new NestedContextParentValidator();
        var services = new NestedStubServiceProvider();
        var parent = new NestedContextParent { Child = new NestedContextChild { First = "child" } };

        var result = validator.Validate(
            parent,
            new ValidationCallOptions {
                ServiceProvider = services,
                RuleSets = ["child"],
                FailFast = true
            });

        ValidationAssert.HasError(result, "Child.First");
        MSTestAssertions.That(() => validator.Captures.Should().HaveCount(1));

        var capture = validator.Captures[0];
        MSTestAssertions.That(() => ReferenceEquals(capture.Root, parent).Should().BeTrue());
        MSTestAssertions.That(() => capture.RootData.Should().NotBeNull());
        MSTestAssertions.That(() => ReferenceEquals(capture.Services, services).Should().BeTrue());
        MSTestAssertions.That(() => capture.MemberPath.Should().Be("Child"));
        MSTestAssertions.That(() => capture.RuleSets.Should().NotBeNull());
        MSTestAssertions.That(() => capture.FailFast.Should().BeTrue());
    }

    [TestMethod]
    public void NestedValidator_PreservesErrorFields() {
        var validator = new NestedContextParentValidator();
        var parent = new NestedContextParent { Child = new NestedContextChild { First = "child" } };

        var result = validator.Validate(parent, new ValidationCallOptions { RuleSets = ["child"] });

        ValidationAssert.HasError(result, "Child.First");
        var error = result.Errors.Single(static error => error.MemberName == "Child.First");
        MSTestAssertions.That(() => error.ErrorCode.Should().Be("PROBE"));
        MSTestAssertions.That(() => error.Severity.Should().Be(ValidationErrorSeverity.Warning));
        MSTestAssertions.That(() => error.CustomState.Should().Be("probe-state"));
        MSTestAssertions.That(() => error.Message.Should().Be("probe-error"));
    }

    [TestMethod]
    public async Task ForEachValidator_PreservesContextAndIndexedPathAsync() {
        var validator = new NestedContextParentValidator();
        var services = new NestedStubServiceProvider();
        var parent = new NestedContextParent {
            Children = [new NestedContextChild { First = "first" }]
        };

        var result = await validator.ValidateAsync(
            parent,
            new ValidationCallOptions {
                ServiceProvider = services,
                RuleSets = ["child"],
                FailFast = true
            },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.HasError(result, "Children[0].First");
        var error = result.Errors.Single(static error => error.MemberName == "Children[0].First");
        MSTestAssertions.That(() => error.ErrorCode.Should().Be("PROBE"));
        MSTestAssertions.That(() => error.CustomState.Should().Be("probe-state"));
        MSTestAssertions.That(() => validator.Captures.Should().HaveCount(1));

        var capture = validator.Captures[0];
        MSTestAssertions.That(() => ReferenceEquals(capture.Root, parent).Should().BeTrue());
        MSTestAssertions.That(() => capture.RootData.Should().NotBeNull());
        MSTestAssertions.That(() => ReferenceEquals(capture.Services, services).Should().BeTrue());
        MSTestAssertions.That(() => capture.MemberPath.Should().Be("Children[0]"));
        MSTestAssertions.That(() => capture.FailFast.Should().BeTrue());
    }

    public TestContext? TestContext { get; set; }
}
