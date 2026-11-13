//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class GlobalOptionsModel {
    public string? Name { get; set; }
}

file sealed class GlobalOptionsValidator : Validator<GlobalOptionsModel> {
    protected override void Configure(ValidatorRuleBuilder<GlobalOptionsModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(GlobalOptionsModel.Name))
               .NotEmpty()
               .MinimumLength(5);
    }
}

file sealed class ContinueRuleLevelValidator : Validator<GlobalOptionsModel> {
    public ContinueRuleLevelValidator() : base(CascadeMode.Continue) {
    }

    protected override void Configure(ValidatorRuleBuilder<GlobalOptionsModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(GlobalOptionsModel.Name))
               .NotEmpty()
               .MinimumLength(5);
    }
}

file sealed class ContinueRuleCascadeValidator : Validator<GlobalOptionsModel> {
    protected override void Configure(ValidatorRuleBuilder<GlobalOptionsModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(GlobalOptionsModel.Name))
               .NotEmpty()
               .MinimumLength(5)
               .Cascade(CascadeMode.Continue);
    }
}

file sealed class OverriddenNameValidator : Validator<GlobalOptionsModel> {
    protected override void Configure(ValidatorRuleBuilder<GlobalOptionsModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(GlobalOptionsModel.Name))
               .NotEmpty()
               .OverridePropertyName("Login");
    }
}

[TestClass]
// Mutates the process-wide validation options and restores them.
[DoNotParallelize]
public sealed class GlobalValidatorOptionsTests {
    [TestMethod]
    public void PropertyNameResolver_RenamesTheMemberNameAndPropertyName() =>
        WithOptions(
            static options => options with {
                PropertyNameResolver = static (type, name) =>
                    type == typeof(GlobalOptionsModel) && name == nameof(GlobalOptionsModel.Name)
                        ? "customer_name"
                        : name
            },
            () => {
                var error = new GlobalOptionsValidator().Validate(new GlobalOptionsModel { Name = "" }).Errors[0];

                MSTestAssertions.That(() => error.MemberName.Should().Be("customer_name"));
                MSTestAssertions.That(() => error.MessageParameters!["PropertyName"].Should().Be("customer_name"));
                MSTestAssertions.That(() => error.MessageParameters!["PropertyPath"].Should().Be("customer_name"));
            });

    [TestMethod]
    public void PropertyNameResolver_RuleOverrideWins() =>
        WithOptions(
            static options => options with { PropertyNameResolver = static (_, _) => "global_name" },
            () => {
                var error = new OverriddenNameValidator().Validate(new GlobalOptionsModel { Name = "" }).Errors[0];

                MSTestAssertions.That(() => error.MemberName.Should().Be("Login"));
                MSTestAssertions.That(() => error.MessageParameters!["PropertyName"].Should().Be("Login"));
            });

    [TestMethod]
    public void PropertyNameResolver_DisplayNameStillControlsPropertyName() =>
        WithOptions(
            static options => options with {
                PropertyNameResolver = static (_, _) => "customer_name",
                DisplayNameResolver = static (_, _) => "Customer name"
            },
            () => {
                var error = new GlobalOptionsValidator().Validate(new GlobalOptionsModel { Name = "" }).Errors[0];

                MSTestAssertions.That(() => error.MemberName.Should().Be("customer_name"));
                MSTestAssertions.That(() => error.MessageParameters!["PropertyName"].Should().Be("Customer name"));
            });

    [TestMethod]
    public void PropertyNameResolver_BlankResultKeepsTheOriginalName() =>
        WithOptions(
            static options => options with { PropertyNameResolver = static (_, _) => " " },
            () => {
                var error = new GlobalOptionsValidator().Validate(new GlobalOptionsModel { Name = "" }).Errors[0];

                MSTestAssertions.That(() => error.MemberName.Should().Be(nameof(GlobalOptionsModel.Name)));
            });

    [TestMethod]
    public void DefaultRuleLevelCascadeMode_AppliesToRuleGroups() =>
        WithOptions(
            static options => options with { DefaultRuleLevelCascadeMode = CascadeMode.Stop },
            () => {
                var result = new GlobalOptionsValidator().Validate(new GlobalOptionsModel { Name = "" });

                MSTestAssertions.That(() => result.Errors.Count.Should().Be(1));
            });

    [TestMethod]
    public void DefaultCascadeModeAlias_AppliesToRuleGroups() =>
        WithOptions(
            static options => {
                var updated = options with { DefaultCascadeMode = CascadeMode.Stop };
                MSTestAssertions.That(() => updated.DefaultRuleLevelCascadeMode.Should().Be(CascadeMode.Stop));
                return updated;
            },
            () => {
                var result = new GlobalOptionsValidator().Validate(new GlobalOptionsModel { Name = "" });

                MSTestAssertions.That(() => result.Errors.Count.Should().Be(1));
            });

    [TestMethod]
    public void ValidatorRuleLevelCascadeMode_OverridesGlobal() =>
        WithOptions(
            static options => options with { DefaultRuleLevelCascadeMode = CascadeMode.Stop },
            () => {
                var result = new ContinueRuleLevelValidator().Validate(new GlobalOptionsModel { Name = "" });

                MSTestAssertions.That(() => result.Errors.Count.Should().Be(2));
            });

    [TestMethod]
    public void RuleCascadeOverride_WinsOverGlobalAndValidator() =>
        WithOptions(
            static options => options with { DefaultRuleLevelCascadeMode = CascadeMode.Stop },
            () => {
                var result = new ContinueRuleCascadeValidator().Validate(new GlobalOptionsModel { Name = "" });

                MSTestAssertions.That(() => result.Errors.Count.Should().Be(2));
            });

    [TestMethod]
    public void CreateValidator_UsesConfiguredFactoryWhenServiceIsMissing() => WithOptions(
            static options => options with {
                ValidatorFactory = (type, _) =>
                    type == typeof(GlobalOptionsModel) ? new GlobalOptionsValidator() : null
            },
            () => {
                var created = ValidationConfiguration.CreateValidator<GlobalOptionsModel>(serviceProvider: null);
                var untyped = ValidationConfiguration.CreateValidator(typeof(GlobalOptionsModel), serviceProvider: null);

                MSTestAssertions.That(() => created.Should().BeOfType<GlobalOptionsValidator>());
                MSTestAssertions.That(() => untyped.Should().BeOfType<GlobalOptionsValidator>());
            });

    [TestMethod]
    public void CreateValidator_ReturnsNullWithoutAFactory() =>
        WithOptions(
            static options => options with { ValidatorFactory = null },
            () => {
                MSTestAssertions.That(() =>
                    ValidationConfiguration.CreateValidator<GlobalOptionsModel>(serviceProvider: null).Should().BeNull());
                MSTestAssertions.That(() =>
                    ValidationConfiguration.CreateValidator(typeof(GlobalOptionsModel), serviceProvider: null).Should().BeNull());
            });

    private static void WithOptions(Func<ValidationOptions, ValidationOptions> configure, Action assertion) {
        var previous = ValidationConfiguration.Options;

        try {
            ValidationConfiguration.Configure(configure);
            assertion();
        }
        finally {
            ValidationConfiguration.Configure(_ => previous);
        }
    }
}
