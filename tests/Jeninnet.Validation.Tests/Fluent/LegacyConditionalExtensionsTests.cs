//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class ConditionalModel {
    public string? Country { get; set; }
    public string? Code { get; set; }
}

file sealed class SyncConditionalValidator : Validator<ConditionalModel> {
    protected override void Configure(ValidatorRuleBuilder<ConditionalModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.When(
            static x => x.Country == "US",
            conditional => conditional.RuleFor(static x => x.Code, nameof(ConditionalModel.Code)).NotEmpty());

        builder.Unless(
            static x => x.Country == "US",
            conditional => conditional.RuleFor(static x => x.Code, nameof(ConditionalModel.Code)).MaximumLength(3));
    }
}

file sealed class AsyncConditionalValidator : Validator<ConditionalModel> {
    protected override void Configure(ValidatorRuleBuilder<ConditionalModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WhenAsyncRule(
            static async (x, cancellationToken) => {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return x.Country == "US";
            },
            conditional => conditional.RuleFor(static x => x.Code, nameof(ConditionalModel.Code)).NotEmpty());

        builder.UnlessAsyncRule(
            static async (x, cancellationToken) => {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return x.Country == "US";
            },
            conditional => conditional.RuleFor(static x => x.Code, nameof(ConditionalModel.Code)).NotEmpty());
    }
}

[TestClass]
public sealed class LegacyConditionalExtensionsTests {
    [TestMethod]
    public void When_And_Unless_GateSyncRules() {
        var validator = new SyncConditionalValidator();

        MSTestAssertions.That(() => validator.Validate(new ConditionalModel { Country = "US", Code = "" })
            .Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => validator.Validate(new ConditionalModel { Country = "US", Code = "ABC" })
            .IsValid.Should().BeTrue());
        MSTestAssertions.That(() => validator.Validate(new ConditionalModel { Country = "CA", Code = "ABCD" })
            .Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => validator.Validate(new ConditionalModel { Country = "CA", Code = "AB" })
            .IsValid.Should().BeTrue());
    }

    [TestMethod]
    public async Task WhenAsyncRule_And_UnlessAsyncRule_GateAsyncRulesAsync() {
        var validator = new AsyncConditionalValidator();

        var usEmpty = await validator.ValidateAsync(new ConditionalModel { Country = "US", Code = "" }, TestCancellation);
        var usValid = await validator.ValidateAsync(new ConditionalModel { Country = "US", Code = "ABC" }, TestCancellation);
        var caEmpty = await validator.ValidateAsync(new ConditionalModel { Country = "CA", Code = "" }, TestCancellation);
        var caValid = await validator.ValidateAsync(new ConditionalModel { Country = "CA", Code = "ABC" }, TestCancellation);

        MSTestAssertions.That(() => usEmpty.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => usValid.IsValid.Should().BeTrue());
        MSTestAssertions.That(() => caEmpty.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => caValid.IsValid.Should().BeTrue());
    }

    private CancellationToken TestCancellation => TestContext?.CancellationToken ?? CancellationToken.None;

    public TestContext? TestContext { get; set; }
}
