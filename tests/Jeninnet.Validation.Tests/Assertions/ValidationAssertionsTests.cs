//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Assertions;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

internal sealed class AssertionModel {
    public string? Email { get; set; }
    public int Age { get; set; }
}

internal sealed class AssertionModelValidator : Validator<AssertionModel> {
    protected override void Configure(ValidatorRuleBuilder<AssertionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Email, nameof(AssertionModel.Email))
               .NotEmpty("Email is required.")
               .EmailAddress("Email is invalid.");

        builder.RuleFor(static x => x.Age, nameof(AssertionModel.Age))
               .GreaterThan(17, "Age must be at least 18.")
               .WithCode("AGE_MIN")
               .WithSeverity(ValidationErrorSeverity.Warning)
               .WithState("adult");
    }
}

internal sealed class AsyncAssertionValidator : Validator<AssertionModel> {
    protected override void Configure(ValidatorRuleBuilder<AssertionModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Email, nameof(AssertionModel.Email))
               .Must(
                   static (email, _) => new ValueTask<bool>(email?.Contains('@', StringComparison.Ordinal) is true),
                   "Email must contain '@'.");
    }
}

// ---------------------------------------------------------------------------
// Assertions
// ---------------------------------------------------------------------------

[TestClass]
public sealed class ValidationAssertionsTests {
    private readonly AssertionModelValidator _validator = new();

    [TestMethod]
    public void ShouldHaveValidationErrors_WhenErrorsExist_Chains() {
        var result = CreateResult(CreateEmailError());

        var assertions = result.ShouldHaveValidationErrors()
                               .WithErrorCount(1)
                               .ContainingMessage("invalid")
                               .WithErrorCode("E01")
                               .WithSeverity(ValidationErrorSeverity.Warning);

        MSTestAssertions.That(() => assertions.Should().NotBeNull());
    }

    [TestMethod]
    public void ShouldHaveValidationErrors_WhenResultIsValid_Throws() {
        var act = () => ValidationResult.Success.ShouldHaveValidationErrors();

        MSTestAssertions.That(() => act.Should().ThrowExactly<ValidationAssertionException>()
            .Which.Message.Should().Contain("Actual errors: none"));
    }

    [TestMethod]
    public void ShouldNotHaveValidationErrors_WhenValid_Chains() {
        var assertions = ValidationResult.Success.ShouldNotHaveValidationErrors();

        MSTestAssertions.That(() => assertions.WithErrorCount(0).Should().NotBeNull());
    }

    [TestMethod]
    public void ShouldNotHaveValidationErrors_WhenInvalid_Throws() {
        var act = () => CreateResult(CreateEmailError()).ShouldNotHaveValidationErrors();

        MSTestAssertions.That(() => act.Should().ThrowExactly<ValidationAssertionException>()
            .Which.Message.Should().Contain("Expected the validation result to be valid"));
    }

    [TestMethod]
    public void WithErrorCount_WhenMismatched_Throws() {
        var act = () => CreateResult(CreateEmailError()).ShouldHaveValidationErrors().WithErrorCount(2);

        MSTestAssertions.That(() => act.Should().ThrowExactly<ValidationAssertionException>()
            .Which.Message.Should().Contain("found 1"));
    }

    [TestMethod]
    public void ForMember_WhenMemberHasNoErrors_Throws() {
        var act = () => CreateResult(CreateEmailError()).ShouldHaveValidationErrors().ForMember("Age");

        MSTestAssertions.That(() => act.Should().ThrowExactly<ValidationAssertionException>()
            .Which.Message.Should().Contain("member 'Age'"));
    }

    [TestMethod]
    public void MemberMatchers_WithMatchingError_Chain() {
        var result = CreateResult(
            CreateEmailError(),
            new ValidationError(
                memberName: "Email",
                messageId: new MessageId("NotEmpty"),
                messageKey: ValidationMessageKeys.NOT_EMPTY,
                message: "Email is required."));

        var member = result.ShouldHaveValidationErrors()
                           .ForMember(nameof(AssertionModel.Email))
                           .WithCount(2);

        member.ContainsRule(ValidationMessageKeys.EMAIL)
              .ContainingMessage("invalid")
              .WithErrorCode("E01")
              .WithSeverity(ValidationErrorSeverity.Warning)
              .WithCustomState("state");

        MSTestAssertions.That(() => member.Should().NotBeNull());
    }

    [TestMethod]
    public void MemberMatchers_WhenRuleIsMissing_Throws() {
        var act = () => CreateResult(CreateEmailError())
            .ShouldHaveValidationErrors()
            .ForMember(nameof(AssertionModel.Email))
            .ContainsRule(ValidationMessageKeys.NOT_EMPTY);

        MSTestAssertions.That(() => act.Should().ThrowExactly<ValidationAssertionException>()
            .Which.Message.Should().Contain(ValidationMessageKeys.NOT_EMPTY));
    }

    [TestMethod]
    public void TestValidate_WithInvalidModel_BindsMemberAssertions() {
        var test = _validator.TestValidate(new AssertionModel { Email = "not-an-email", Age = 10 });

        test.ShouldHaveValidationErrors().WithErrorCount(2);
        test.ShouldHaveValidationErrorFor(nameof(AssertionModel.Email))
            .WithCount(1)
            .ContainsRule(ValidationMessageKeys.EMAIL);
        test.ShouldHaveValidationErrorFor(nameof(AssertionModel.Age))
            .WithErrorCode("AGE_MIN")
            .WithSeverity(ValidationErrorSeverity.Warning)
            .WithCustomState("adult");
    }

    [TestMethod]
    public void TestValidate_WithValidModel_HasNoErrors() {
        var test = _validator.TestValidate(new AssertionModel { Email = "user@example.com", Age = 20 });

        test.ShouldNotHaveValidationErrors().Result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public void ShouldNotHaveValidationErrorFor_WhenMemberIsValid_Chains() {
        var test = _validator.TestValidate(new AssertionModel { Email = "not-an-email", Age = 20 });

        var chained = test.ShouldNotHaveValidationErrorFor(nameof(AssertionModel.Age));

        MSTestAssertions.That(() => chained.Should().NotBeNull());
    }

    [TestMethod]
    public void ShouldNotHaveValidationErrorFor_WhenMemberIsInvalid_Throws() {
        var act = () => _validator
            .TestValidate(new AssertionModel { Email = "not-an-email", Age = 20 })
            .ShouldNotHaveValidationErrorFor(nameof(AssertionModel.Email));

        MSTestAssertions.That(() => act.Should().ThrowExactly<ValidationAssertionException>()
            .Which.Message.Should().Contain("member 'Email'"));
    }

    [TestMethod]
    public async Task TestValidateAsync_WithInvalidModel_BindsResultAsync() {
        var validator = new AsyncAssertionValidator();
        var test = await validator.TestValidateAsync(
            new AssertionModel { Email = "invalid" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        test.ShouldHaveValidationErrorFor(nameof(AssertionModel.Email))
            .ContainingMessage("'@'");
    }

    [TestMethod]
    public void TestValidate_WithNullValidator_Throws() {
        ISyncValidator<AssertionModel> validator = null!;
        var act = () => validator.TestValidate(new AssertionModel());

        MSTestAssertions.That(() => act.Should().ThrowExactly<ArgumentNullException>());
    }

    [TestMethod]
    public void TestValidate_WithNullModel_Throws() {
        var act = () => _validator.TestValidate(null!);

        MSTestAssertions.That(() => act.Should().ThrowExactly<ArgumentNullException>());
    }

    private static ValidationError CreateEmailError() =>
        new(
            memberName: "Email",
            messageId: new MessageId("Email"),
            messageKey: ValidationMessageKeys.EMAIL,
            message: "Email is invalid.",
            errorCode: "E01",
            severity: ValidationErrorSeverity.Warning,
            customState: "state");

    private static ValidationResult CreateResult(params ValidationError[] errors) {
        var collector = new ValidationErrorCollector();

        collector.AddRange(errors);

        return ValidationResult.FromErrors(collector);
    }

    public TestContext? TestContext { get; set; }
}
