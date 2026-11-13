//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class OptionChild {
    public string? Name { get; set; }
    public int Age { get; set; }
}

internal sealed class OptionParent {
    public OptionChild? Child { get; set; }
    public List<OptionChild> Items { get; set; } = [];
}

file sealed class OptionChildValidator : Validator<OptionChild> {
    protected override void Configure(ValidatorRuleBuilder<OptionChild> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static child => child.Name, nameof(OptionChild.Name))
               .NotEmpty();

        builder.RuleFor(static child => child.Age, nameof(OptionChild.Age))
               .InclusiveBetween(18, 99);
    }
}

file sealed class SetValidatorOptionValidator : Validator<OptionParent> {
    protected override void Configure(ValidatorRuleBuilder<OptionParent> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static parent => parent.Child, nameof(OptionParent.Child))
               .SetValidator(new OptionChildValidator())
               .WithMessage("child-message")
               .WithCode("CHILD")
               .WithSeverity(ValidationErrorSeverity.Warning)
               .WithState("child-state")
               .WithName("RenamedChild");
    }
}

file sealed class ForEachOptionValidator : Validator<OptionParent> {
    protected override void Configure(ValidatorRuleBuilder<OptionParent> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static parent => parent.Items, nameof(OptionParent.Items))
               .ForEach(new OptionChildValidator())
               .WithMessage("item-message")
               .WithCode("ITEM")
               .WithSeverity(ValidationErrorSeverity.Info)
               .WithState("item-state")
               .WithName("Item");
    }
}

file sealed class ChildRulesOptionValidator : Validator<OptionParent> {
    protected override void Configure(ValidatorRuleBuilder<OptionParent> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static parent => parent.Child, nameof(OptionParent.Child))
               .ChildRules(child => {
                   child.RuleFor(static value => value.Name, nameof(OptionChild.Name)).NotEmpty();
                   child.RuleFor(static value => value.Age, nameof(OptionChild.Age)).InclusiveBetween(18, 99);
               })
               .WithMessage("inline-message")
               .WithCode("INLINE")
               .WithSeverity(ValidationErrorSeverity.Warning)
               .WithState("inline-state")
               .WithName("InlineChild");
    }
}

file sealed class LateOptionValidator : Validator<OptionChild> {
    protected override void Configure(ValidatorRuleBuilder<OptionChild> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static child => child.Name, nameof(OptionChild.Name))
               .NotEmpty()
               .When(static _ => true)
               .WithMessage("late");
    }
}

[TestClass]
public sealed class RuleOptionsOnCompositeRulesTests {
    private static readonly OptionChild _invalidChild = new() { Name = "", Age = 10 };

    [TestMethod]
    public void SetValidatorOptions_ApplyToEveryChildError() {
        var result = new SetValidatorOptionValidator().Validate(new OptionParent { Child = _invalidChild });

        ValidationAssert.ErrorCount(result, 2);
        foreach(var error in result.Errors) {
            MSTestAssertions.That(() => error.MemberName.Should().Be("RenamedChild"));
            MSTestAssertions.That(() => error.Message.Should().Be("child-message"));
            MSTestAssertions.That(() => error.ErrorCode.Should().Be("CHILD"));
            MSTestAssertions.That(() => error.Severity.Should().Be(ValidationErrorSeverity.Warning));
            MSTestAssertions.That(() => error.CustomState.Should().Be("child-state"));
        }
    }

    [TestMethod]
    public void ForEachOptions_ApplyToEveryElementError() {
        var result = new ForEachOptionValidator().Validate(new OptionParent { Items = [_invalidChild] });

        ValidationAssert.ErrorCount(result, 2);
        foreach(var error in result.Errors) {
            MSTestAssertions.That(() => error.MemberName.Should().Be("Item"));
            MSTestAssertions.That(() => error.Message.Should().Be("item-message"));
            MSTestAssertions.That(() => error.ErrorCode.Should().Be("ITEM"));
            MSTestAssertions.That(() => error.Severity.Should().Be(ValidationErrorSeverity.Info));
            MSTestAssertions.That(() => error.CustomState.Should().Be("item-state"));
        }
    }

    [TestMethod]
    public void ChildRulesOptions_ApplyToEveryInlineError() {
        var result = new ChildRulesOptionValidator().Validate(new OptionParent { Child = _invalidChild });

        ValidationAssert.ErrorCount(result, 2);
        foreach(var error in result.Errors) {
            MSTestAssertions.That(() => error.MemberName.Should().Be("InlineChild"));
            MSTestAssertions.That(() => error.Message.Should().Be("inline-message"));
            MSTestAssertions.That(() => error.ErrorCode.Should().Be("INLINE"));
            MSTestAssertions.That(() => error.Severity.Should().Be(ValidationErrorSeverity.Warning));
            MSTestAssertions.That(() => error.CustomState.Should().Be("inline-state"));
        }
    }

    [TestMethod]
    public void OptionsAfterCondition_ThrowInsteadOfSilentNoOp() {
        try {
            _ = new LateOptionValidator();
            MSTestAssertions.That(() => false.Should().BeTrue("Expected construction to throw."));
        }
        catch(InvalidOperationException exception) {
            MSTestAssertions.That(() => exception.Message.Should().Contain("directly follow"));
        }
    }
}
