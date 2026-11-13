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

internal sealed class CustomModel {
    public string? Password { get; set; }
    public string? Confirm { get; set; }
}

internal sealed class CustomParentModel {
    public CustomModel? Child { get; set; }
}

file sealed class CustomValidator : Validator<CustomModel> {
    public object? ObservedRoot { get; private set; }
    public RootContextData? ObservedBag { get; private set; }

    protected override void Configure(ValidatorRuleBuilder<CustomModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Password, nameof(CustomModel.Password))
               .Custom((password, context, errors) => {
                   ObservedRoot = context.Root;
                   ObservedBag = context.RootData;

                   if(password?.Any(char.IsDigit) is not true) {
                       AddError(errors, "Password needs a digit.", "PWD_DIGIT");
                   }

                   if(password?.Any(char.IsUpper) is not true) {
                       AddError(errors, "Password needs an uppercase letter.", "PWD_UPPER");
                   }
               });
    }

    private static void AddError(ValidationErrorCollector errors, string message, string errorCode) =>
        errors.Add(
            nameof(CustomModel.Password),
            new MessageId("Password"),
            messageKey: null,
            messageParameters: null,
            message,
            errorCode,
            ValidationErrorSeverity.Error);
}

file sealed class CustomAsyncValidator : Validator<CustomModel> {
    public bool AsyncRan { get; private set; }

    protected override void Configure(ValidatorRuleBuilder<CustomModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Password, nameof(CustomModel.Password))
               .Custom(static (password, _, errors) => {
                   if(string.IsNullOrEmpty(password)) {
                       errors.Add(
                           nameof(CustomModel.Password),
                           new MessageId("Password"),
                           messageKey: null,
                           messageParameters: null,
                           message: "Password is required.",
                           errorCode: "SYNC_REQUIRED",
                           severity: ValidationErrorSeverity.Error);
                   }
               });

        builder.RuleFor(static x => x.Confirm, nameof(CustomModel.Confirm))
               .CustomAsync(async (value, _, errors, cancellationToken) => {
                   AsyncRan = true;
                   await Task.Yield();
                   cancellationToken.ThrowIfCancellationRequested();

                   if(value != "match") {
                       errors.Add(
                           nameof(CustomModel.Confirm),
                           new MessageId("Confirm"),
                           messageKey: null,
                           messageParameters: null,
                           message: "Confirmation does not match.",
                           errorCode: "CONFIRM_MISMATCH",
                           severity: ValidationErrorSeverity.Error);
                   }
               });
    }
}

file sealed class CancelAsyncValidator : Validator<CustomModel> {
    protected override void Configure(ValidatorRuleBuilder<CustomModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Confirm, nameof(CustomModel.Confirm))
               .CustomAsync(async (_, _, _, cancellationToken) =>
                   await Task.Delay(10, cancellationToken).ConfigureAwait(false));
    }
}

file sealed class ThrowingCustomValidator : Validator<CustomModel> {
    protected override void Configure(ValidatorRuleBuilder<CustomModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Password, nameof(CustomModel.Password))
               .Custom(static (_, _, _) =>
                   throw new InvalidOperationException("boom"));
    }
}

file sealed class ChildCustomValidator : Validator<CustomModel> {
    protected override void Configure(ValidatorRuleBuilder<CustomModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Password, nameof(CustomModel.Password))
               .Custom(static (_, _, errors) =>
                   errors.Add(
                       nameof(CustomModel.Password),
                       new MessageId("Password"),
                       messageKey: null,
                       messageParameters: null,
                       message: "Child password is invalid.",
                       errorCode: "CHILD_PWD",
                       severity: ValidationErrorSeverity.Error));
    }
}

file sealed class ParentValidator : Validator<CustomParentModel> {
    protected override void Configure(ValidatorRuleBuilder<CustomParentModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Child, nameof(CustomParentModel.Child))
               .SetValidator(new ChildCustomValidator());
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class CustomRuleTests {
    [TestMethod]
    public void Custom_EmitsMultipleErrorsWithDistinctCodes() {
        var result = new CustomValidator().Validate(new CustomModel { Password = "short" });

        ValidationAssert.ErrorCount(result, 2);
        ValidationAssert.HasError(result, nameof(CustomModel.Password));
        MSTestAssertions.That(() => result.Errors.Any(static error => error.ErrorCode == "PWD_DIGIT").Should().BeTrue());
        MSTestAssertions.That(() => result.Errors.Any(static error => error.ErrorCode == "PWD_UPPER").Should().BeTrue());
    }

    [TestMethod]
    public void Custom_ReceivesRootAndSharedBag() {
        var validator = new CustomValidator();
        var model = new CustomModel { Password = "short" };

        _ = validator.Validate(model);

        MSTestAssertions.That(() => ReferenceEquals(validator.ObservedRoot, model).Should().BeTrue());
        MSTestAssertions.That(() => validator.ObservedBag.Should().NotBeNull());
    }

    [TestMethod]
    public void Custom_ExceptionBecomesInvalidFailure() {
        var result = new ThrowingCustomValidator().Validate(new CustomModel { Password = "short" });

        ValidationAssert.ErrorCount(result, 1);
        MSTestAssertions.That(() => result.Errors[0].MessageId.Should().Be(MessageId.Invalid));
        MSTestAssertions.That(() => result.Errors[0].ErrorCode.Should().Be("CustomValidationException"));
        MSTestAssertions.That(() => result.Errors[0].Message.Should().Contain("InvalidOperationException"));
    }

    [TestMethod]
    public void Custom_CanEmitNestedPath() {
        var rule = new CustomRule<CustomModel, string?>(
            ValidatorFactory.Accessor<CustomModel, string?>(
                static x => x.Password,
                nameof(CustomModel.Password)),
            static (_, context, errors) =>
                errors.Add(
                    context.GetFullPath(nameof(CustomModel.Password)),
                    new MessageId("Password"),
                    messageKey: null,
                    messageParameters: null,
                    message: "Nested failure.",
                    errorCode: "NESTED",
                    severity: ValidationErrorSeverity.Error));

        var errors = new ValidationErrorCollector();
        var context = new ValidationContext<CustomModel>(new CustomModel(), MemberPath: "Parent");
        rule.Validate(in context, errors);
        var result = ValidationResult.FromErrors(errors);

        ValidationAssert.HasError(result, "Parent.Password");
    }

    [TestMethod]
    public void Custom_NestedValidator_PrefixesMemberPath() {
        var result = new ParentValidator().Validate(
            new CustomParentModel { Child = new CustomModel { Password = "" } });

        ValidationAssert.HasError(result, "Child.Password");
    }

    [TestMethod]
    public void CustomAsync_RunsOnAsyncPathOnly() {
        var validator = new CustomAsyncValidator();
        var model = new CustomModel { Password = "valid", Confirm = "other" };

        var syncResult = validator.Validate(model);
        ValidationAssert.IsValid(syncResult);
        MSTestAssertions.That(() => validator.AsyncRan.Should().BeFalse());
    }

    [TestMethod]
    public async Task CustomAsync_EmitsErrorsOnAsyncPathAsync() {
        var validator = new CustomAsyncValidator();
        var model = new CustomModel { Password = "valid", Confirm = "other" };

        var result = await validator.ValidateAsync(
            model,
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, nameof(CustomModel.Confirm));
        MSTestAssertions.That(() => validator.AsyncRan.Should().BeTrue());
    }

    [TestMethod]
    public async Task CustomAsync_HonorsCancellationAsync() {
        var validator = new CancelAsyncValidator();

        Exception? thrown = null;
        try {
            await validator.ValidateAsync(
                new CustomModel(),
                new CancellationToken(canceled: true));
        }
        catch(Exception exception) {
            thrown = exception;
        }

        MSTestAssertions.That(() => (thrown is OperationCanceledException).Should().BeTrue());
    }

    public TestContext? TestContext { get; set; }
}
