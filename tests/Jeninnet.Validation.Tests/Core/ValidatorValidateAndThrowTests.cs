//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Core;

sealed class ValidateAndThrowModel {
    public string? Name { get; set; }
}

sealed class ValidateAndThrowValidator : Validator<ValidateAndThrowModel> {
    protected override void Configure(ValidatorRuleBuilder<ValidateAndThrowModel> builder) => builder.RuleFor(static x => x.Name, nameof(ValidateAndThrowModel.Name))
               .NotEmpty("Name is required.");
}

[TestClass]
public sealed class ValidatorValidateAndThrowTests {
    private readonly ValidateAndThrowValidator _validator = new();

    [TestMethod]
    public void ValidateAndThrow_WhenValid_DoesNotThrow() {
        var act = () => _validator.ValidateAndThrow(new ValidateAndThrowModel { Name = "Valid" });
        MSTestAssertions.That(() => act.Should().NotThrow());
    }

    [TestMethod]
    public void ValidateAndThrow_WhenInvalid_ThrowsValidationException() {
        var act = () => _validator.ValidateAndThrow(new ValidateAndThrowModel { Name = "" });
        MSTestAssertions.That(() => act.Should().Throw<ValidationException>()
           .Which.Errors.Should().HaveCount(1));
    }

    [TestMethod]
    public void ValidateAndThrow_WithRuleSets_WhenInvalid_Throws() {
        var act = () => _validator.ValidateAndThrow(new ValidateAndThrowModel { Name = "" }, ["Default"]);
        MSTestAssertions.That(() => act.Should().Throw<ValidationException>()
           .Which.Errors.Should().HaveCount(1));
    }

    [TestMethod]
    public async Task ValidateAndThrowAsync_WhenValid_DoesNotThrowAsync() {
        var act = async () => await _validator.ValidateAndThrowAsync(new ValidateAndThrowModel { Name = "Valid" }, TestContext?.CancellationToken ?? CancellationToken.None);
        await MSTestAssertions.ThatAsync(async () => await act.Should().NotThrowAsync());
    }

    [TestMethod]
    public async Task ValidateAndThrowAsync_WhenInvalid_ThrowsValidationExceptionAsync() {
        var act = async () => await _validator.ValidateAndThrowAsync(new ValidateAndThrowModel { Name = "" }, TestContext?.CancellationToken ?? CancellationToken.None);
        await MSTestAssertions.ThatAsync(async () => (await act.Should().ThrowAsync<ValidationException>()).Where(ex => ex.Errors.Count == 1));
    }

    [TestMethod]
    public async Task ValidateAndThrowAsync_WithRuleSets_WhenInvalid_ThrowsAsync() {
        var act = async () => await _validator.ValidateAndThrowAsync(
            new ValidateAndThrowModel { Name = "" }, ["Default"], TestContext?.CancellationToken ?? CancellationToken.None);
        await MSTestAssertions.ThatAsync(async () => (await act.Should().ThrowAsync<ValidationException>()).Where(ex => ex.Errors.Count == 1));
    }

    [TestMethod]
    public void ValidateAndThrow_ThrownException_HasCorrectErrors() {
        try {
            _validator.ValidateAndThrow(new ValidateAndThrowModel { Name = "" });
        }
        catch(ValidationException ex) {
            MSTestAssertions.That(() => ex.Errors.Should().HaveCount(1));
            MSTestAssertions.That(() => ex.Errors[0].MemberName.Should().Be(nameof(ValidateAndThrowModel.Name)));
        }
    }

    [TestMethod]
    public void Validate_WithCallOptions_ReturnsResultAndThrowsWhenRequested() {
        var valid = _validator.Validate(new ValidateAndThrowModel { Name = "Valid" }, new ValidationCallOptions());
        MSTestAssertions.That(() => valid.IsValid.Should().BeTrue());

        var act = () => _validator.Validate(
            new ValidateAndThrowModel { Name = "" },
            new ValidationCallOptions { ThrowOnFailures = true });

        MSTestAssertions.That(() => act.Should().Throw<ValidationException>()
           .Which.Errors.Should().HaveCount(1));
    }

    [TestMethod]
    public void Validate_WithStrategy_ReturnsResultAndThrowsWhenRequested() {
        var valid = _validator.Validate(
            new ValidateAndThrowModel { Name = "Valid" },
            static strategy => strategy.IncludeProperties(nameof(ValidateAndThrowModel.Name)));
        MSTestAssertions.That(() => valid.IsValid.Should().BeTrue());

        var act = () => _validator.Validate(
            new ValidateAndThrowModel { Name = "" },
            static strategy => strategy.ThrowOnFailures());

        MSTestAssertions.That(() => act.Should().Throw<ValidationException>());
    }

    [TestMethod]
    public async Task ValidateAsync_WithCallOptions_ReturnsResultAndThrowsWhenRequestedAsync() {
        var token = TestContext?.CancellationToken ?? CancellationToken.None;

        var valid = await _validator.ValidateAsync(
            new ValidateAndThrowModel { Name = "Valid" },
            new ValidationCallOptions(),
            token);
        MSTestAssertions.That(() => valid.IsValid.Should().BeTrue());

        var act = async () => await _validator.ValidateAsync(
            new ValidateAndThrowModel { Name = "" },
            new ValidationCallOptions { ThrowOnFailures = true },
            token);

        await MSTestAssertions.ThatAsync(async () =>
            (await act.Should().ThrowAsync<ValidationException>()).Where(ex => ex.Errors.Count == 1));
    }

    [TestMethod]
    public async Task ValidateAsync_WithStrategy_ReturnsResultAndThrowsWhenRequestedAsync() {
        var token = TestContext?.CancellationToken ?? CancellationToken.None;

        var valid = await _validator.ValidateAsync(
            new ValidateAndThrowModel { Name = "Valid" },
            static strategy => strategy.IncludeProperties(nameof(ValidateAndThrowModel.Name)),
            token);
        MSTestAssertions.That(() => valid.IsValid.Should().BeTrue());

        var act = async () => await _validator.ValidateAsync(
            new ValidateAndThrowModel { Name = "" },
            static strategy => strategy.ThrowOnFailures(),
            token);

        await MSTestAssertions.ThatAsync(async () =>
            (await act.Should().ThrowAsync<ValidationException>()).Where(ex => ex.Errors.Count == 1));
    }

    [TestMethod]
    public void ValidateAndThrow_WithCallOptions_WhenInvalid_Throws() {
        var act = () => _validator.ValidateAndThrow(
            new ValidateAndThrowModel { Name = "" },
            new ValidationCallOptions());

        MSTestAssertions.That(() => act.Should().Throw<ValidationException>());
    }

    [TestMethod]
    public async Task ValidateAndThrowAsync_WithCallOptions_WhenInvalid_ThrowsAsync() {
        var act = async () => await _validator.ValidateAndThrowAsync(
            new ValidateAndThrowModel { Name = "" },
            new ValidationCallOptions(),
            TestContext?.CancellationToken ?? CancellationToken.None);

        await MSTestAssertions.ThatAsync(async () =>
            (await act.Should().ThrowAsync<ValidationException>()).Where(ex => ex.Errors.Count == 1));
    }

    public TestContext? TestContext { get; set; }
}
