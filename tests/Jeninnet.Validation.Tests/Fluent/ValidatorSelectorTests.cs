//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class SelectorModel {
    public string? Name { get; set; }

    public string? Email { get; set; }
}

file sealed class SelectorValidator : Validator<SelectorModel> {
    protected override void Configure(ValidatorRuleBuilder<SelectorModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(SelectorModel.Name))
               .NotEmpty();

        builder.RuleFor(static model => model.Email, nameof(SelectorModel.Email))
               .NotEmpty();
    }
}

file sealed class RuleSetSelectorValidator : Validator<SelectorModel> {
    protected override void Configure(ValidatorRuleBuilder<SelectorModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleSet("special", special => special
            .RuleFor(static model => model.Name, nameof(SelectorModel.Name))
            .NotEmpty());

        builder.RuleFor(static model => model.Email, nameof(SelectorModel.Email))
               .NotEmpty();
    }
}

file sealed class MemberSelector(params string[] members) : IValidatorSelector {
    public bool CanExecute<T>(IValidationRuleDescriptor rule, string memberName, ValidationContext<T> context) {
        _ = rule;
        _ = context;

        return members.Contains(memberName, StringComparer.Ordinal);
    }
}

file sealed class ExcludingSelector(string excludedMember) : IValidatorSelector {
    public List<string> SeenMembers { get; } = [];

    public bool CanExecute<T>(IValidationRuleDescriptor rule, string memberName, ValidationContext<T> context) {
        _ = rule;
        _ = context;

        SeenMembers.Add(memberName);
        return !string.Equals(memberName, excludedMember, StringComparison.Ordinal);
    }
}

[TestClass]
public sealed class ValidatorSelectorTests {
    [TestMethod]
    public void CustomSelector_ExcludesMatchingMemberOnTheSyncPath() {
        var selector = new ExcludingSelector(nameof(SelectorModel.Name));
        var options = new ValidationCallOptions { Selector = selector };

        var result = new SelectorValidator().Validate(new SelectorModel { Name = "", Email = "" }, options);

        MSTestAssertions.That(() => result.Errors.Count.Should().Be(1));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be(nameof(SelectorModel.Email)));
        MSTestAssertions.That(() => selector.SeenMembers.SequenceEqual([nameof(SelectorModel.Name), nameof(SelectorModel.Email)]).Should().BeTrue());
    }

    [TestMethod]
    public async Task CustomSelector_ExcludesMatchingMemberOnTheAsyncPathAsync() {
        var selector = new ExcludingSelector(nameof(SelectorModel.Email));
        var options = new ValidationCallOptions { Selector = selector };

        var result = await new SelectorValidator().ValidateAsync(
            new SelectorModel { Name = "", Email = "" },
            options,
            TestContext?.CancellationToken ?? CancellationToken.None);

        MSTestAssertions.That(() => result.Errors.Count.Should().Be(1));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be(nameof(SelectorModel.Name)));
    }

    [TestMethod]
    public void RulesetValidatorSelector_SelectsMatchingRuleSet() {
        var options = new ValidationCallOptions {
            Selector = new RulesetValidatorSelector("special")
        };

        var result = new RuleSetSelectorValidator().Validate(new SelectorModel { Name = "", Email = "" }, options);

        MSTestAssertions.That(() => result.Errors.Count.Should().Be(1));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be(nameof(SelectorModel.Name)));
    }

    [TestMethod]
    public void Selector_CombinesWithIncludeProperties() {
        var options = new ValidationCallOptions {
            IncludeProperties = [nameof(SelectorModel.Email)],
            Selector = new MemberSelector(nameof(SelectorModel.Name))
        };

        var result = new SelectorValidator().Validate(new SelectorModel { Name = "", Email = "" }, options);

        MSTestAssertions.That(() => result.Errors.Should().BeEmpty());
    }

    [TestMethod]
    public void DefaultSelector_RunsEveryRule() {
        var options = new ValidationCallOptions { Selector = DefaultValidatorSelector.Instance };

        var result = new SelectorValidator().Validate(new SelectorModel { Name = "", Email = "" }, options);

        MSTestAssertions.That(() => result.Errors.Count.Should().Be(2));
    }

    [TestMethod]
    public void CompositeValidatorSelector_SelectsWhenAnySelectorMatches() {
        var options = new ValidationCallOptions {
            Selector = new CompositeValidatorSelector(
                new MemberSelector(nameof(SelectorModel.Email)),
                new RulesetValidatorSelector("special"))
        };

        var result = new RuleSetSelectorValidator().Validate(new SelectorModel { Name = "", Email = "" }, options);

        MSTestAssertions.That(() => result.Errors.Count.Should().Be(2));
    }

    [TestMethod]
    public void CompositeValidatorSelector_WithNoEffectiveSelectors_RunsNothing() {
        var options = new ValidationCallOptions {
            Selector = new CompositeValidatorSelector()
        };

        var result = new SelectorValidator().Validate(new SelectorModel { Name = "", Email = "" }, options);

        MSTestAssertions.That(() => result.Errors.Should().BeEmpty());
    }

    public TestContext? TestContext { get; set; }
}
