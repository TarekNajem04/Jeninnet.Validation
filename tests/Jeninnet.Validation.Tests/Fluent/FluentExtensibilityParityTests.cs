//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Fluent;

public sealed class ExtensibilityModel {
    public string? Name { get; set; }

    public string? Code { get; set; }
}

public sealed class ContextStateValidator : Validator<ExtensibilityModel> {
    protected override void Configure(ValidatorRuleBuilder<ExtensibilityModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ExtensibilityModel.Name))
               .NotEmpty()
               .WithState(static (model, value, context) =>
                   $"{model.Code}:{value}:{context.GetFullPath(nameof(ExtensibilityModel.Name))}");
    }
}

public sealed class SimpleStateValidator : Validator<ExtensibilityModel> {
    protected override void Configure(ValidatorRuleBuilder<ExtensibilityModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ExtensibilityModel.Name))
               .NotEmpty()
               .WithState(static (model, value) => $"{model.Code}:{value}");
    }
}

public sealed class HookedValidator : Validator<ExtensibilityModel> {
    public int AddedRules { get; private set; }

    protected override void Configure(ValidatorRuleBuilder<ExtensibilityModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ExtensibilityModel.Name)).NotEmpty();
        builder.RuleFor(static x => x.Code, nameof(ExtensibilityModel.Code)).NotEmpty();
    }

    protected override void OnRuleAdded(IRuleDefinition rule) {
        ArgumentNullException.ThrowIfNull(rule);

        AddedRules++;

        if(rule is IRuleDefinitionOptions options) {
            options.Severity = ValidationErrorSeverity.Warning;
            options.ErrorCode ??= "HOOK";
        }
    }
}

[TestClass]
public sealed class FluentExtensibilityParityTests {
    public TestContext TestContext { get; set; } = null!;
    [TestMethod]
    public void WithState_ContextFactoryRunsOnTheSyncPath() {
        var validator = new ContextStateValidator();
        var result = validator.Validate(new ExtensibilityModel { Name = string.Empty, Code = "C" });

        ValidationAssert.HasError(result, nameof(ExtensibilityModel.Name));
        MSTestAssertions.That(() => result.Errors[0].CustomState.Should().Be($"C::{nameof(ExtensibilityModel.Name)}"));
    }

    [TestMethod]
    public async Task WithState_FactoriesRunOnTheAsyncPathAsync() {
        var contextValidator = new ContextStateValidator();
        var simpleValidator = new SimpleStateValidator();
        var model = new ExtensibilityModel { Name = string.Empty, Code = "C" };

        var contextResult = await contextValidator.ValidateAsync(model, TestContext.CancellationToken);
        var simpleResult = await simpleValidator.ValidateAsync(model, TestContext.CancellationToken);

        MSTestAssertions.That(() => contextResult.Errors[0].CustomState.Should().Be($"C::{nameof(ExtensibilityModel.Name)}"));
        MSTestAssertions.That(() => simpleResult.Errors[0].CustomState.Should().Be("C:"));
    }

    [TestMethod]
    public void OnRuleAdded_RunsOncePerLeafRuleAndChangesDefaults() {
        var validator = new HookedValidator();
        var result = validator.Validate(new ExtensibilityModel { Name = string.Empty, Code = string.Empty });

        MSTestAssertions.That(() => validator.AddedRules.Should().Be(2));
        ValidationAssert.ErrorCount(result, 2);

        foreach(var error in result.Errors) {
            MSTestAssertions.That(() => error.Severity.Should().Be(ValidationErrorSeverity.Warning));
            MSTestAssertions.That(() => error.ErrorCode.Should().Be("HOOK"));
        }
    }

    [TestMethod]
    public void ValidationPath_HandlesMembersAndIndexers() {
        MSTestAssertions.That(() => ValidationPath.GetParent("A.B[0].C").Should().Be("A.B[0]"));
        MSTestAssertions.That(() => ValidationPath.GetParent("Items[0]").Should().Be("Items"));
        MSTestAssertions.That(() => ValidationPath.GetParent("A.B").Should().Be("A"));
        MSTestAssertions.That(() => ValidationPath.GetParent("A").Should().BeNull());
        MSTestAssertions.That(() => ValidationPath.GetParent("[0]").Should().BeNull());
        MSTestAssertions.That(() => ValidationPath.GetParent(null).Should().BeNull());
        MSTestAssertions.That(() => ValidationPath.GetParent(string.Empty).Should().BeNull());

        MSTestAssertions.That(() => ValidationPath.IsChildOf("A", "A.B").Should().BeTrue());
        MSTestAssertions.That(() => ValidationPath.IsChildOf("A", "A[0].B").Should().BeTrue());
        MSTestAssertions.That(() => ValidationPath.IsChildOf("Items[0]", "Items[0].Name").Should().BeTrue());
        MSTestAssertions.That(() => ValidationPath.IsChildOf("A.B", "A").Should().BeFalse());
        MSTestAssertions.That(() => ValidationPath.IsChildOf("A", "AB.C").Should().BeFalse());
        MSTestAssertions.That(() => ValidationPath.IsChildOf("A", "A").Should().BeFalse());
        MSTestAssertions.That(() => ValidationPath.IsChildOf(string.Empty, "A").Should().BeFalse());
    }
}
