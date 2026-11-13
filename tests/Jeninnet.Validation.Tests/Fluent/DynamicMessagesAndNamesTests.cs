//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class DynamicMessageModel {
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DynamicMessageChild? Child { get; set; }
}

internal sealed class DynamicMessageChild {
    public string? Value { get; set; }
}

file sealed class DynamicMessageValidator : Validator<DynamicMessageModel> {
    protected override void Configure(ValidatorRuleBuilder<DynamicMessageModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(DynamicMessageModel.Name))
               .NotEmpty()
               .WithMessage(static x => $"Name '{x.Name}' is required.");
    }
}

file sealed class DynamicContextMessageValidator : Validator<DynamicMessageModel> {
    protected override void Configure(ValidatorRuleBuilder<DynamicMessageModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Email, nameof(DynamicMessageModel.Email))
               .NotEmpty()
               .WithMessage(static (x, context) =>
                   $"{context.GetFullPath(nameof(DynamicMessageModel.Email))}:{x.Email}");
    }
}

file sealed class DynamicNameValidator : Validator<DynamicMessageModel> {
    protected override void Configure(ValidatorRuleBuilder<DynamicMessageModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(DynamicMessageModel.Name))
               .NotEmpty()
               .WithName(static x => $"Name for {x.Email}");
    }
}

file sealed class DynamicMemberNameValidator : Validator<DynamicMessageModel> {
    protected override void Configure(ValidatorRuleBuilder<DynamicMessageModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(DynamicMessageModel.Name))
               .NotEmpty()
               .OverridePropertyName(static x => $"field-{x.Email}");
    }
}

file sealed class StaticAfterDynamicMessageValidator : Validator<DynamicMessageModel> {
    protected override void Configure(ValidatorRuleBuilder<DynamicMessageModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(DynamicMessageModel.Name))
               .NotEmpty()
               .WithMessage(static _ => "dynamic")
               .WithMessage("static");
    }
}

file sealed class BlankDynamicMessageValidator : Validator<DynamicMessageModel> {
    protected override void Configure(ValidatorRuleBuilder<DynamicMessageModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(DynamicMessageModel.Name))
               .NotEmpty()
               .WithMessage(static _ => "   ");
    }
}

file sealed class BlankDynamicNameValidator : Validator<DynamicMessageModel> {
    protected override void Configure(ValidatorRuleBuilder<DynamicMessageModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(DynamicMessageModel.Name))
               .NotEmpty()
               .WithName(static _ => " ");
    }
}

file sealed class DynamicChildValidator : Validator<DynamicMessageChild> {
    protected override void Configure(ValidatorRuleBuilder<DynamicMessageChild> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Value, nameof(DynamicMessageChild.Value)).NotEmpty();
    }
}

file sealed class CompositeDynamicOptionsValidator : Validator<DynamicMessageModel> {
    protected override void Configure(ValidatorRuleBuilder<DynamicMessageModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Child, nameof(DynamicMessageModel.Child))
               .SetValidator(new DynamicChildValidator())
               .WithMessage(static parent => $"child of {parent.Name}")
               .WithName(static parent => $"Child of {parent.Name}");
    }
}

[TestClass]
public sealed class DynamicMessagesAndNamesTests {
    [TestMethod]
    public void WithMessage_UsesTheInstance() {
        var result = new DynamicMessageValidator().Validate(
            new DynamicMessageModel { Name = string.Empty, Email = "a@b.c" });

        var error = result.Errors.Single();
        MSTestAssertions.That(() => error.Message.Should().Be("Name '' is required."));
        MSTestAssertions.That(() => error.MessageKey.Should().Be(ValidationMessageKeys.NOT_EMPTY));
        MSTestAssertions.That(() => error.MemberName.Should().Be(nameof(DynamicMessageModel.Name)));
    }

    [TestMethod]
    public void WithMessage_CanUseTheValidationContext() {
        var result = new DynamicContextMessageValidator().Validate(
            new DynamicMessageModel { Email = string.Empty });

        MSTestAssertions.That(() => result.Errors.Single().Message.Should().Be("Email:"));
    }

    [TestMethod]
    public void WithName_ChangesPropertyNameButNotMemberName() {
        var result = new DynamicNameValidator().Validate(
            new DynamicMessageModel { Name = string.Empty, Email = "a@b.c" });

        var error = result.Errors.Single();
        MSTestAssertions.That(() => error.MemberName.Should().Be(nameof(DynamicMessageModel.Name)));
        MSTestAssertions.That(() => error.MessageParameters!["PropertyName"].Should().Be("Name for a@b.c"));
    }

    [TestMethod]
    public void OverridePropertyName_WithDelegateChangesMemberName() {
        var result = new DynamicMemberNameValidator().Validate(
            new DynamicMessageModel { Name = string.Empty, Email = "a@b.c" });

        MSTestAssertions.That(() => result.Errors.Single().MemberName.Should().Be("field-a@b.c"));
    }

    [TestMethod]
    public void StaticMessage_AfterDynamicMessageWins() {
        var result = new StaticAfterDynamicMessageValidator().Validate(
            new DynamicMessageModel { Name = string.Empty });

        MSTestAssertions.That(() => result.Errors.Single().Message.Should().Be("static"));
    }

    [TestMethod]
    public void BlankDynamicMessage_FallsBackToTheRuleDefault() {
        var result = new BlankDynamicMessageValidator().Validate(
            new DynamicMessageModel { Name = string.Empty });

        var error = result.Errors.Single();
        MSTestAssertions.That(() => error.Message.Should().BeNull());
        MSTestAssertions.That(() => error.MessageKey.Should().Be(ValidationMessageKeys.NOT_EMPTY));
    }

    [TestMethod]
    public void BlankDynamicName_FallsBackWithoutChangingMemberName() {
        var result = new BlankDynamicNameValidator().Validate(
            new DynamicMessageModel { Name = string.Empty });

        var error = result.Errors.Single();
        MSTestAssertions.That(() => error.MemberName.Should().Be(nameof(DynamicMessageModel.Name)));
        MSTestAssertions.That(() => error.MessageParameters!["PropertyName"].Should().Be(nameof(DynamicMessageModel.Name)));
        MSTestAssertions.That(() => error.MessageParameters!["PropertyPath"].Should().Be(nameof(DynamicMessageModel.Name)));
    }

    [TestMethod]
    public void CompositeDynamicOptions_UseTheParentInstance() {
        var result = new CompositeDynamicOptionsValidator().Validate(
            new DynamicMessageModel {
                Name = "parent",
                Child = new DynamicMessageChild { Value = string.Empty }
            });

        var error = result.Errors.Single();
        MSTestAssertions.That(() => error.Message.Should().Be("child of parent"));
        MSTestAssertions.That(() => error.MemberName.Should().Be("Child.Value"));
        MSTestAssertions.That(() => error.MessageParameters!["PropertyName"].Should().Be("Child of parent"));
    }
}
