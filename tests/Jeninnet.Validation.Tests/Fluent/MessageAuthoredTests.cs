//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class AuthoredMessageModel {
    public string? Name { get; set; }

    public string? Code { get; set; }
}

file sealed class AuthoredMessageValidator : Validator<AuthoredMessageModel> {
    protected override void Configure(ValidatorRuleBuilder<AuthoredMessageModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(AuthoredMessageModel.Name))
               .NotEmpty()
               .WithMessage("authored message");

        builder.RuleFor(static model => model.Code, nameof(AuthoredMessageModel.Code))
               .NotEmpty();
    }
}

file sealed class DynamicAuthoredMessageValidator : Validator<AuthoredMessageModel> {
    protected override void Configure(ValidatorRuleBuilder<AuthoredMessageModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(AuthoredMessageModel.Name))
               .NotEmpty()
               .WithMessage(static model => $"dynamic:{model.Name}");
    }
}

[TestClass]
public sealed class MessageAuthoredTests {
    [TestMethod]
    public void WithMessage_MarksTheErrorAsAuthored() {
        var validator = new AuthoredMessageValidator();
        var result = validator.Validate(new AuthoredMessageModel { Name = string.Empty, Code = string.Empty });

        var authored = result.Errors.Single(static error => error.MemberName == nameof(AuthoredMessageModel.Name));
        var generated = result.Errors.Single(static error => error.MemberName == nameof(AuthoredMessageModel.Code));

        MSTestAssertions.That(() => authored.Message.Should().Be("authored message"));
        MSTestAssertions.That(() => authored.MessageIsAuthored.Should().BeTrue());
        MSTestAssertions.That(() => generated.MessageIsAuthored.Should().BeFalse());
    }

    [TestMethod]
    public void DynamicWithMessage_MarksTheErrorAsAuthored() {
        var validator = new DynamicAuthoredMessageValidator();
        var result = validator.Validate(new AuthoredMessageModel { Name = string.Empty, Code = null });

        var error = result.Errors.Single(static error => error.MemberName == nameof(AuthoredMessageModel.Name));

        MSTestAssertions.That(() => error.Message.Should().Be("dynamic:"));
        MSTestAssertions.That(() => error.MessageIsAuthored.Should().BeTrue());
    }

    [TestMethod]
    public void Descriptor_CarriesAuthoredMessageFlag() {
        var descriptor = new AuthoredMessageValidator().Describe();
        var name = descriptor.Members.Single(static member => member.Name == nameof(AuthoredMessageModel.Name));
        var code = descriptor.Members.Single(static member => member.Name == nameof(AuthoredMessageModel.Code));

        MSTestAssertions.That(() => name.Rules.Single().MessageIsAuthored.Should().BeTrue());
        MSTestAssertions.That(() => code.Rules.Single().MessageIsAuthored.Should().BeFalse());
    }

    [TestMethod]
    public void Collector_OverloadMarksTheMessageAsAuthored() {
        var collector = new ValidationErrorCollector();
        var error = new ValidationError("Name", new MessageId(ValidationMessageKeys.NOT_EMPTY), ValidationMessageKeys.NOT_EMPTY);

        collector.Add(error, messageIsAuthored: true);

        MSTestAssertions.That(() => collector.Count.Should().Be(1));
        MSTestAssertions.That(() => collector[0].MessageIsAuthored.Should().BeTrue());
    }

    [TestMethod]
    public void ValidateAndThrow_PreservesAuthoredMessage() {
        var validator = new AuthoredMessageValidator();

        try {
            validator.ValidateAndThrow(new AuthoredMessageModel { Name = string.Empty });
            MSTestAssertions.That(static () => false.Should().BeTrue("ValidateAndThrow must throw"));
        }
        catch(ValidationException exception) {
            var error = exception.Errors.Single(static error => error.MemberName == nameof(AuthoredMessageModel.Name));
            MSTestAssertions.That(() => error.Message.Should().Be("authored message"));
            MSTestAssertions.That(() => error.MessageIsAuthored.Should().BeTrue());
        }
    }
}
