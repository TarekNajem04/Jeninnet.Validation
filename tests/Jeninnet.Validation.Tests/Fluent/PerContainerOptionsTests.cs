//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class PerContainerModel {
    public string? Name { get; set; }
    public string? Code { get; set; }
}

file sealed class PerContainerValidator : Validator<PerContainerModel> {
    protected override void Configure(ValidatorRuleBuilder<PerContainerModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(PerContainerModel.Name))
               .NotEmpty()
               .MinimumLength(5);

        builder.RuleFor(static x => x.Code, nameof(PerContainerModel.Code))
               .NotEmpty();
    }
}

[TestClass]
public sealed class PerContainerOptionsTests {
    [TestMethod]
    public void ContainerOptions_OverrideDisplayNameWithoutStaticState() {
        using var first = CreateProvider(displayName: "First name");
        using var second = CreateProvider(displayName: "Second name");
        var validator = new PerContainerValidator();

        var firstError = validator.Validate(
            new PerContainerModel { Name = string.Empty, Code = "x" },
            new ValidationCallOptions { ServiceProvider = first }).Errors[0];
        var secondError = validator.Validate(
            new PerContainerModel { Name = string.Empty, Code = "x" },
            new ValidationCallOptions { ServiceProvider = second }).Errors[0];

        MSTestAssertions.That(() => firstError.MessageParameters!["PropertyName"].Should().Be("First name"));
        MSTestAssertions.That(() => secondError.MessageParameters!["PropertyName"].Should().Be("Second name"));
        MSTestAssertions.That(() => firstError.MemberName.Should().Be(nameof(PerContainerModel.Name)));
    }

    [TestMethod]
    public void ContainerOptions_OverrideRuleLevelCascade() {
        using var stop = CreateProvider(defaultRuleLevelCascadeMode: CascadeMode.Stop);
        using var @continue = CreateProvider(defaultRuleLevelCascadeMode: CascadeMode.Continue);
        var validator = new PerContainerValidator();

        var stopResult = validator.Validate(
            new PerContainerModel { Name = string.Empty, Code = string.Empty },
            new ValidationCallOptions { ServiceProvider = stop });
        var continueResult = validator.Validate(
            new PerContainerModel { Name = string.Empty, Code = string.Empty },
            new ValidationCallOptions { ServiceProvider = @continue });

        MSTestAssertions.That(() => stopResult.Errors.Should().HaveCount(2));
        MSTestAssertions.That(() => continueResult.Errors.Should().HaveCount(3));
    }

    [TestMethod]
    public void ContainerOptions_OverrideClassLevelCascade() {
        using var stop = CreateProvider(defaultClassLevelCascadeMode: CascadeMode.Stop);
        using var @continue = CreateProvider(defaultClassLevelCascadeMode: CascadeMode.Continue);
        var validator = new PerContainerValidator();

        var stopResult = validator.Validate(
            new PerContainerModel { Name = string.Empty, Code = string.Empty },
            new ValidationCallOptions { ServiceProvider = stop });
        var continueResult = validator.Validate(
            new PerContainerModel { Name = string.Empty, Code = string.Empty },
            new ValidationCallOptions { ServiceProvider = @continue });

        MSTestAssertions.That(() => stopResult.Errors.Should().HaveCount(2));
        MSTestAssertions.That(() => continueResult.Errors.Should().HaveCount(3));
    }

    private static ServiceProvider CreateProvider(
        string? displayName = null,
        CascadeMode? defaultRuleLevelCascadeMode = null,
        CascadeMode? defaultClassLevelCascadeMode = null) {
        var services = new ServiceCollection();

        services.AddValidationCore(options => options with {
            DisplayNameResolver = displayName is null ? null : (_, _) => displayName,
            DefaultRuleLevelCascadeMode = defaultRuleLevelCascadeMode ?? options.DefaultRuleLevelCascadeMode,
            DefaultClassLevelCascadeMode = defaultClassLevelCascadeMode ?? options.DefaultClassLevelCascadeMode
        });

        return services.BuildServiceProvider();
    }
}
