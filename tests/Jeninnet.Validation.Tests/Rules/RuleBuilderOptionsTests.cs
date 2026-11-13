//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules;

[TestClass]
public sealed class RuleBuilderOptionsTests {
    // -------------------------------------------------------------------------
    // WithMessage
    // -------------------------------------------------------------------------

    [TestMethod]
    public void WithMessage_OverridesDefaultErrorMessage() {
        var result = RunRule(
            b => b.RuleFor(static x => x.Name, nameof(OptionsModel.Name))
                  .NotNull()
                  .WithMessage("Custom message"),
            new OptionsModel());
        MSTestAssertions.That(() => result.Errors[0].Message.Should().Be("Custom message"));
    }

    // -------------------------------------------------------------------------
    // WithCode
    // -------------------------------------------------------------------------

    [TestMethod]
    public void WithCode_SetsErrorCode() {
        var result = RunRule(
            b => b.RuleFor(static x => x.Name, nameof(OptionsModel.Name))
                  .NotNull()
                  .WithCode("ERR_REQUIRED"),
            new OptionsModel());
        MSTestAssertions.That(() => result.Errors[0].ErrorCode.Should().Be("ERR_REQUIRED"));
    }

    // -------------------------------------------------------------------------
    // WithSeverity
    // -------------------------------------------------------------------------

    [TestMethod]
    public void WithSeverity_SetsWarningSeverity() {
        var result = RunRule(
            b => b.RuleFor(static x => x.Name, nameof(OptionsModel.Name))
                  .NotNull()
                  .WithSeverity(ValidationErrorSeverity.Warning),
            new OptionsModel());
        MSTestAssertions.That(() => result.Errors[0].Severity.Should().Be(ValidationErrorSeverity.Warning));
    }

    [TestMethod]
    public void WithSeverity_SetsInfoSeverity() {
        var result = RunRule(
            b => b.RuleFor(static x => x.Name, nameof(OptionsModel.Name))
                  .NotNull()
                  .WithSeverity(ValidationErrorSeverity.Info),
            new OptionsModel());
        MSTestAssertions.That(() => result.Errors[0].Severity.Should().Be(ValidationErrorSeverity.Info));
    }

    [TestMethod]
    public void WithSeverity_SetsErrorSeverity() {
        var result = RunRule(
            b => b.RuleFor(static x => x.Name, nameof(OptionsModel.Name))
                  .NotNull()
                  .WithSeverity(ValidationErrorSeverity.Error),
            new OptionsModel());
        MSTestAssertions.That(() => result.Errors[0].Severity.Should().Be(ValidationErrorSeverity.Error));
    }

    [TestMethod]
    public void DefaultSeverity_IsError() {
        var result = RunRule(
            b => b.RuleFor(static x => x.Name, nameof(OptionsModel.Name))
                  .NotNull(),
            new OptionsModel());
        MSTestAssertions.That(() => result.Errors[0].Severity.Should().Be(ValidationErrorSeverity.Error));
    }

    // -------------------------------------------------------------------------
    // OverridePropertyName
    // -------------------------------------------------------------------------

    [TestMethod]
    public void OverridePropertyName_ChangesMemberName() {
        var result = RunRule(
            b => b.RuleFor(static x => x.Name, nameof(OptionsModel.Name))
                  .NotNull()
                  .OverridePropertyName("CustomField"),
            new OptionsModel());
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("CustomField"));
    }

    // -------------------------------------------------------------------------
    // WithName (alias for OverridePropertyName)
    // -------------------------------------------------------------------------

    [TestMethod]
    public void WithName_ChangesMemberName() {
        var result = RunRule(
            b => b.RuleFor(static x => x.Name, nameof(OptionsModel.Name))
                  .NotNull()
                  .WithName("RenamedField"),
            new OptionsModel());
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("RenamedField"));
    }

    // -------------------------------------------------------------------------
    // WithState
    // -------------------------------------------------------------------------

    [TestMethod]
    public void WithState_SetsCustomStateOnError() {
        var state = new { Id = 42 };
        var result = RunRule(
            b => b.RuleFor(static x => x.Name, nameof(OptionsModel.Name))
                  .NotNull()
                  .WithState(state),
            new OptionsModel());
        MSTestAssertions.That(() => result.Errors[0].CustomState.Should().BeSameAs(state));
    }

    [TestMethod]
    public void WithState_NullState_CustomStateIsNull() {
        var result = RunRule(
            b => b.RuleFor(static x => x.Name, nameof(OptionsModel.Name))
                  .NotNull()
                  .WithState(null),
            new OptionsModel());
        MSTestAssertions.That(() => result.Errors[0].CustomState.Should().BeNull());
    }

    // -------------------------------------------------------------------------
    // Chaining multiple options
    // -------------------------------------------------------------------------

    [TestMethod]
    public void CanChainMessageCodeAndSeverity() {
        var state = new { Info = "test" };
        var result = RunRule(
            b => b.RuleFor(static x => x.Name, nameof(OptionsModel.Name))
                  .NotNull()
                  .WithMessage("Custom message")
                  .WithCode("ERR_CUSTOM")
                  .WithSeverity(ValidationErrorSeverity.Warning)
                  .WithState(state),
            new OptionsModel());

        var error = result.Errors[0];
        MSTestAssertions.That(() => error.Message.Should().Be("Custom message"));
        MSTestAssertions.That(() => error.ErrorCode.Should().Be("ERR_CUSTOM"));
        MSTestAssertions.That(() => error.Severity.Should().Be(ValidationErrorSeverity.Warning));
        MSTestAssertions.That(() => error.CustomState.Should().BeSameAs(state));
    }

    // -------------------------------------------------------------------------
    // Options applied to non-NotNull rule (NotEmpty)
    // -------------------------------------------------------------------------

    [TestMethod]
    public void WithMessage_WorksWithNotEmpty() {
        var result = RunRule(
            b => b.RuleFor(static x => x.Name, nameof(OptionsModel.Name))
                  .NotEmpty()
                  .WithMessage("Must not be empty"),
            new OptionsModel { Name = "" });
        MSTestAssertions.That(() => result.Errors[0].Message.Should().Be("Must not be empty"));
    }

    [TestMethod]
    public void WithCode_WorksWithInclusiveBetween() {
        var result = RunRule(
            b => b.RuleFor(static x => x.Age, nameof(OptionsModel.Age))
                  .InclusiveBetween(18, 99)
                  .WithCode("AGE_RANGE"),
            new OptionsModel { Age = 10 });
        MSTestAssertions.That(() => result.Errors[0].ErrorCode.Should().Be("AGE_RANGE"));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static ValidationResult RunRule<T>(
        Action<ValidatorRuleBuilder<T>> configure,
        T instance) {
        var syncRules = new List<ISyncValidationRule<T>>();
        var asyncRules = new List<IAsyncValidationRule<T>>();
        var builder = new ValidatorRuleBuilder<T>(syncRules, asyncRules);
        configure(builder);
        var composite = new CompositeValidator<T>(syncRules, asyncRules);
        return composite.Validate(instance);
    }

    private sealed class OptionsModel {
        public string? Name { get; set; }
        public int Age { get; set; }
    }
}
