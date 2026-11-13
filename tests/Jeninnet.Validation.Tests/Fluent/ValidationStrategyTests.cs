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

internal sealed class StrategyModel {
    public string? Email { get; set; }
    public string? Name { get; set; }
    public int Age { get; set; }
}

file sealed class PerRuleSetValidator : Validator<StrategyModel> {
    protected override void Configure(ValidatorRuleBuilder<StrategyModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Email, nameof(StrategyModel.Email))
               .NotEmpty("email-required")
               .RuleSet("EmailSet");

        builder.RuleFor(static x => x.Name, nameof(StrategyModel.Name))
               .NotEmpty("name-required")
               .RuleSet("NameSet");

        builder.RuleFor(static x => x.Age, nameof(StrategyModel.Age))
               .InclusiveBetween(18, 99, "age-range");
    }
}

file sealed class StrategyValidator : Validator<StrategyModel> {
    protected override void Configure(ValidatorRuleBuilder<StrategyModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Email, nameof(StrategyModel.Email))
               .NotEmpty("email-required")
               .EmailAddress("email-format");

        builder.RuleFor(static x => x.Name, nameof(StrategyModel.Name))
               .NotEmpty("name-required")
               .Must(
                   static async (value, cancellationToken) => {
                       await Task.Yield();
                       cancellationToken.ThrowIfCancellationRequested();
                       return value is not { Length: > 10 };
                   },
                   "name-too-long");

        builder.RuleFor(static x => x.Age, nameof(StrategyModel.Age))
               .InclusiveBetween(18, 99, "age-range");

        builder.RuleSet(
            "strict",
            strict => strict.RuleFor(static x => x.Name, nameof(StrategyModel.Name))
                            .MinimumLength(5, "name-strict"));
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class ValidationStrategyTests {
    private static StrategyModel InvalidModel => new() { Email = "bad", Name = "", Age = 10 };

    [TestMethod]
    public void IncludeProperties_Email_RunsOnlyEmailRules() {
        var result = new StrategyValidator().Validate(
            InvalidModel,
            new ValidationCallOptions { IncludeProperties = ["Email"] });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, nameof(StrategyModel.Email));
        MSTestAssertions.That(() => result.Errors[0].Message.Should().Be("email-format"));
    }

    [TestMethod]
    public void ExcludeProperties_Age_SkipsAgeRules() {
        var result = new StrategyValidator().Validate(
            InvalidModel,
            new ValidationCallOptions { ExcludeProperties = ["Age"] });

        // The named "strict" rule set runs by default (its Name rule also fails here).
        ValidationAssert.ErrorCount(result, 3);
        ValidationAssert.HasError(result, nameof(StrategyModel.Email));
        ValidationAssert.HasError(result, nameof(StrategyModel.Name));
        ValidationAssert.NoErrorFor(result, nameof(StrategyModel.Age));
    }

    [TestMethod]
    public void IncludeProperties_UnknownMember_RunsNothing() {
        var result = new StrategyValidator().Validate(
            InvalidModel,
            new ValidationCallOptions { IncludeProperties = ["Missing"] });

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void IncludeProperties_IsOrdinal() {
        var result = new StrategyValidator().Validate(
            InvalidModel,
            new ValidationCallOptions { IncludeProperties = ["email"] });

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void ExcludeProperties_WinsOverInclude() {
        var result = new StrategyValidator().Validate(
            InvalidModel,
            new ValidationCallOptions {
                IncludeProperties = ["Email", "Name"],
                ExcludeProperties = ["Name"]
            });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, nameof(StrategyModel.Email));
    }

    [TestMethod]
    public void IncludeRuleSets_Strict_RunsOnlyStrictRules() {
        var model = new StrategyModel { Email = "user@example.com", Name = "abc", Age = 20 };

        var result = new StrategyValidator().Validate(
            model,
            new ValidationCallOptions { RuleSets = ["strict"] });

        ValidationAssert.ErrorCount(result, 1);
        MSTestAssertions.That(() => result.Errors[0].Message.Should().Be("name-strict"));
    }

    [TestMethod]
    public void ExcludeRuleSets_Strict_RemovesStrictRules() {
        var model = new StrategyModel { Email = "user@example.com", Name = "abc", Age = 20 };

        var result = new StrategyValidator().Validate(
            model,
            new ValidationCallOptions { RuleSets = ["strict"], ExcludeRuleSets = ["strict"] });

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void ExcludeRuleSets_Default_RemovesUngroupedRules() {
        var model = new StrategyModel { Email = "bad", Name = "abcde", Age = 20 };

        var result = new StrategyValidator().Validate(
            model,
            new ValidationCallOptions { ExcludeRuleSets = ["default"] });

        // Ungrouped rules (Email) are removed; the named "strict" rule set still runs and passes.
        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void StrategyApi_IncludesProperties() {
        var result = new StrategyValidator().Validate(
            InvalidModel,
            strategy => strategy.IncludeProperties("Email"));

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, nameof(StrategyModel.Email));
    }

    [TestMethod]
    public void StrategyApi_ComposesMemberAndRuleSetFilters() {
        var model = new StrategyModel { Email = "bad", Name = "", Age = 20 };

        var result = new StrategyValidator().Validate(
            model,
            strategy => strategy
                        .IncludeProperties("Email", "Name")
                        .ExcludeRuleSets("strict"));

        ValidationAssert.ErrorCount(result, 2);
        ValidationAssert.HasError(result, nameof(StrategyModel.Email));
        ValidationAssert.HasError(result, nameof(StrategyModel.Name));
    }

    [TestMethod]
    public async Task AsyncSelection_MatchesSyncRulesAsync() {
        var model = new StrategyModel { Email = "bad", Name = "abcdefghijkl", Age = 20 };

        var result = await new StrategyValidator().ValidateAsync(
            model,
            new ValidationCallOptions { IncludeProperties = ["Name"] },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.ErrorCount(result, 1);
        MSTestAssertions.That(() => result.Errors[0].Message.Should().Be("name-too-long"));
    }

    [TestMethod]
    public void DefaultBehavior_IsUnchanged() {
        var result = new StrategyValidator().Validate(InvalidModel);

        // Default validation runs ungrouped rules and named rule sets (the "strict" Name rule).
        ValidationAssert.ErrorCount(result, 4);
        ValidationAssert.HasError(result, nameof(StrategyModel.Email));
        ValidationAssert.HasError(result, nameof(StrategyModel.Name));
        ValidationAssert.HasError(result, nameof(StrategyModel.Age));
    }

    [TestMethod]
    public void Strategy_Null_Throws() {
        var validator = new StrategyValidator();

        var act = () => validator.Validate(InvalidModel, (Action<ValidationStrategy>)null!);

        MSTestAssertions.That(() => act.Should().ThrowExactly<ArgumentNullException>());
    }

    [TestMethod]
    public void IncludeAllRuleSets_OverridesExplicitSelection() {
        var result = new StrategyValidator().Validate(
            InvalidModel,
            strategy => strategy
                        .IncludeRuleSets("strict")
                        .IncludeAllRuleSets());

        ValidationAssert.ErrorCount(result, 4);
    }

    [TestMethod]
    public void IncludeRulesNotInRuleSet_ExcludesNamedSets() {
        var result = new StrategyValidator().Validate(
            InvalidModel,
            strategy => strategy.IncludeRulesNotInRuleSet());

        ValidationAssert.ErrorCount(result, 3);
        ValidationAssert.HasError(result, nameof(StrategyModel.Email));
        ValidationAssert.HasError(result, nameof(StrategyModel.Name));
        ValidationAssert.HasError(result, nameof(StrategyModel.Age));
    }

    [TestMethod]
    public void RuleSetsNullOrEmpty_RunAllRules() {
        var validator = new StrategyValidator();

        var nullResult = validator.Validate(
            InvalidModel,
            new ValidationCallOptions { RuleSets = null });
        var emptyResult = validator.Validate(
            InvalidModel,
            new ValidationCallOptions { RuleSets = [] });

        ValidationAssert.ErrorCount(nullResult, 4);
        ValidationAssert.ErrorCount(emptyResult, 4);
    }

    [TestMethod]
    public void ThrowOnFailures_ThrowsWithAllErrors() {
        var validator = new StrategyValidator();

        var viaStrategy = () => validator.Validate(
            InvalidModel,
            strategy => strategy.ThrowOnFailures());
        var viaOptions = () => validator.Validate(
            InvalidModel,
            new ValidationCallOptions { ThrowOnFailures = true });

        MSTestAssertions.That(() => viaStrategy.Should()
            .Throw<ValidationException>()
            .Which.Errors.Should().HaveCount(4));
        MSTestAssertions.That(() => viaOptions.Should()
            .Throw<ValidationException>()
            .Which.Errors.Should().HaveCount(4));
    }

    [TestMethod]
    public async Task ThrowOnFailuresAsync_ThrowsWithAllErrorsAsync() {
        var model = new StrategyModel { Email = "bad", Name = "abcdefghijkl", Age = 20 };
        var act = async () => await new StrategyValidator().ValidateAsync(
            model,
            new ValidationCallOptions { ThrowOnFailures = true },
            TestContext?.CancellationToken ?? CancellationToken.None);

        await MSTestAssertions.ThatAsync(async () => {
            var exception = await act.Should().ThrowAsync<ValidationException>();
            exception.Which.Errors.Should().HaveCount(2);
        });
    }

    [TestMethod]
    public void PerRuleRuleSet_SelectsOnlyWrappedRuleAndDescribesGroup() {
        var validator = new PerRuleSetValidator();
        var model = new StrategyModel { Email = "", Name = "", Age = 10 };

        var emailOnly = validator.Validate(
            model,
            new ValidationCallOptions { RuleSets = ["EmailSet"] });
        var ungrouped = validator.Validate(
            model,
            strategy => strategy.IncludeRulesNotInRuleSet());
        var all = validator.Validate(
            model,
            strategy => strategy.IncludeAllRuleSets());

        ValidationAssert.ErrorCount(emailOnly, 1);
        ValidationAssert.HasError(emailOnly, nameof(StrategyModel.Email));
        ValidationAssert.ErrorCount(ungrouped, 1);
        ValidationAssert.HasError(ungrouped, nameof(StrategyModel.Age));
        ValidationAssert.ErrorCount(all, 3);

        var emailRule = validator.Describe().RulesForMember(nameof(StrategyModel.Email)).Single();
        MSTestAssertions.That(() => emailRule.Kind.Should().Be("NotEmpty"));
        MSTestAssertions.That(() => emailRule.Parameters["RuleSets"].Should().Be("EmailSet"));
    }

    public TestContext? TestContext { get; set; }
}
