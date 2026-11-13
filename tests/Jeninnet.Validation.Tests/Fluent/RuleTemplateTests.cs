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

internal sealed class RuleTemplateModel {
    public string? Text { get; set; }
    public decimal Amount { get; set; }
    public int Age { get; set; }
    public bool IsCompany { get; set; }
}

file static class Templates {
    public static RuleTemplate<string?> ExactLines() =>
        new RuleTemplate<string?>(
            "ExactLinesCount",
            static value => value is not null && CountLines(value) == 3,
            message: "Must contain exactly {Expected} lines; found {Actual}.",
            messageKey: "ExactLinesCount",
            severity: ValidationErrorSeverity.Warning,
            errorCode: "LINES_COUNT")
        .WithParameter("Expected", 3)
        .WithParameter("Actual", static value => CountLines(value));

    public static RuleTemplate<decimal> AmountLimit() =>
        new RuleTemplate<decimal>(
            "AmountLimit",
            static value => value <= 100m,
            message: "Amount {Amount:C} exceeds the limit {Limit:C}.",
            messageKey: "AmountLimit")
        .WithParameter("Limit", 100m)
        .WithParameter("Amount", static value => value);

    public static RuleTemplate<RuleTemplateModel, int> AdultByCompany() =>
        new RuleTemplate<RuleTemplateModel, int>(
            "AdultByCompany",
            static (model, age) => model.IsCompany ? age >= 18 : age >= 21,
            message: "{PropertyName} must be at least {MinAge} for a {CustomerKind}.",
            messageKey: "AdultByCompany",
            errorCode: "AGE_LIMIT")
        .WithParameter("MinAge", static (model, _) => model.IsCompany ? 18 : 21)
        .WithParameter("CustomerKind", static (model, _) => model.IsCompany ? "business" : "consumer");

    public static int CountLines(string? value) =>
        value is null ? 0 : value.Count(static character => character == '\n') + 1;
}

file sealed class RuleTemplateValidator : Validator<RuleTemplateModel> {
    protected override void Configure(ValidatorRuleBuilder<RuleTemplateModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Text, nameof(RuleTemplateModel.Text))
               .Rule(Templates.ExactLines());
        builder.RuleFor(static model => model.Amount, nameof(RuleTemplateModel.Amount))
               .Rule(Templates.AmountLimit());
        builder.RuleFor(static model => model.Age, nameof(RuleTemplateModel.Age))
               .Rule(Templates.AdultByCompany());
    }
}

file sealed class RuleTemplateOverrideValidator : Validator<RuleTemplateModel> {
    protected override void Configure(ValidatorRuleBuilder<RuleTemplateModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Text, nameof(RuleTemplateModel.Text))
               .Rule(Templates.ExactLines())
               .WithMessage("custom message")
               .WithCode("OVERRIDE")
               .WithSeverity(ValidationErrorSeverity.Error);
    }
}

file sealed class NullValueTemplateValidator : Validator<RuleTemplateModel> {
    protected override void Configure(ValidatorRuleBuilder<RuleTemplateModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Text, nameof(RuleTemplateModel.Text))
               .Rule(new RuleTemplate<string?>(
                   "NotNullTemplate",
                   static value => value is not null,
                   message: "{PropertyName} must not be null.",
                   messageKey: "NotNullTemplate"));
    }
}

file sealed class ConditionalTemplateValidator : Validator<RuleTemplateModel> {
    protected override void Configure(ValidatorRuleBuilder<RuleTemplateModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Text, nameof(RuleTemplateModel.Text))
               .Rule(Templates.ExactLines())
               .When(static model => model.IsCompany);
    }
}

file sealed class RuleSetTemplateValidator : Validator<RuleTemplateModel> {
    protected override void Configure(ValidatorRuleBuilder<RuleTemplateModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Text, nameof(RuleTemplateModel.Text))
               .Rule(Templates.ExactLines())
               .RuleSet("TextSet");
    }
}

file sealed class SingleTemplateValidator(RuleTemplate<string?> template) : Validator<RuleTemplateModel> {
    protected override void Configure(ValidatorRuleBuilder<RuleTemplateModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Text, nameof(RuleTemplateModel.Text))
               .Rule(template);
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class RuleTemplateTests {
    [TestMethod]
    public void RuleTemplate_ValidModel_Passes() {
        var result = new RuleTemplateValidator().Validate(new RuleTemplateModel {
            Text = "one\ntwo\nthree",
            Amount = 50m,
            Age = 25
        });

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void RuleTemplate_InvalidValue_EmitsMessageKeySeverityCodeAndParameters() {
        var model = new RuleTemplateModel {
            Text = "one\ntwo",
            Amount = 50m,
            Age = 25
        };

        var result = new RuleTemplateValidator().Validate(model);
        var error = result.Errors.Single();

        MSTestAssertions.That(() => error.MemberName.Should().Be(nameof(RuleTemplateModel.Text)));
        MSTestAssertions.That(() => error.MessageKey.Should().Be("ExactLinesCount"));
        MSTestAssertions.That(() => error.Message.Should().Be("Must contain exactly {Expected} lines; found {Actual}."));
        MSTestAssertions.That(() => error.Severity.Should().Be(ValidationErrorSeverity.Warning));
        MSTestAssertions.That(() => error.ErrorCode.Should().Be("LINES_COUNT"));
        MSTestAssertions.That(() => error.MessageId.Should().Be(new MessageId("ExactLinesCount")));
        MSTestAssertions.That(() => error.MessageParameters!["Expected"].Should().Be(3));
        MSTestAssertions.That(() => error.MessageParameters!["Actual"].Should().Be(2));
        MSTestAssertions.That(() => error.MessageParameters!["PropertyName"].Should().Be(nameof(RuleTemplateModel.Text)));
        MSTestAssertions.That(() => error.MessageParameters!["PropertyPath"].Should().Be(nameof(RuleTemplateModel.Text)));
        MSTestAssertions.That(() => error.MessageParameters!["PropertyValue"].Should().Be(model.Text));
    }

    [TestMethod]
    public void RuleTemplate_InstanceAwarePredicate_ReceivesTheModel() {
        var consumer = new RuleTemplateModel {
            Text = "one\ntwo\nthree",
            Amount = 50m,
            Age = 19,
            IsCompany = false
        };

        var consumerResult = new RuleTemplateValidator().Validate(consumer);
        var error = consumerResult.Errors.Single();

        MSTestAssertions.That(() => error.MemberName.Should().Be(nameof(RuleTemplateModel.Age)));
        MSTestAssertions.That(() => error.MessageParameters!["MinAge"].Should().Be(21));
        MSTestAssertions.That(() => error.MessageParameters!["CustomerKind"].Should().Be("consumer"));

        var business = new RuleTemplateModel {
            Text = "one\ntwo\nthree",
            Amount = 50m,
            Age = 19,
            IsCompany = true
        };

        ValidationAssert.IsValid(new RuleTemplateValidator().Validate(business));
    }

    [TestMethod]
    public void RuleTemplate_WithMessageCodeAndSeverity_OverridesTheTemplateDefaults() {
        var result = new RuleTemplateOverrideValidator().Validate(new RuleTemplateModel {
            Text = "one\ntwo",
            Amount = 50m,
            Age = 25
        });

        var error = result.Errors.Single();

        MSTestAssertions.That(() => error.Message.Should().Be("custom message"));
        MSTestAssertions.That(() => error.ErrorCode.Should().Be("OVERRIDE"));
        MSTestAssertions.That(() => error.Severity.Should().Be(ValidationErrorSeverity.Error));
    }

    [TestMethod]
    public void RuleTemplate_NullValueIsPassedToThePredicate() {
        var result = new NullValueTemplateValidator().Validate(new RuleTemplateModel {
            Text = null,
            Amount = 50m,
            Age = 25
        });

        ValidationAssert.HasError(result, nameof(RuleTemplateModel.Text));
    }

    [TestMethod]
    public void RuleTemplate_IsImmutableAndReusable() {
        var original = new RuleTemplate<string>("Immutable", static _ => true, message: "original")
            .WithParameter("Value", 1);

        var changed = original
            .WithParameter("Value", 2)
            .WithMessage("changed");

        MSTestAssertions.That(() => original.Message.Should().Be("original"));
        MSTestAssertions.That(() => original.Parameters["Value"].Should().Be(1));
        MSTestAssertions.That(() => changed.Message.Should().Be("changed"));
        MSTestAssertions.That(() => changed.Parameters["Value"].Should().Be(2));
    }

    [TestMethod]
    public void RuleTemplate_BuilderMethodsReturnUpdatedCopies() {
        var original = new RuleTemplate<int>(
            "Builder",
            static _ => false,
            message: "original",
            messageKey: "OriginalKey",
            severity: ValidationErrorSeverity.Error,
            errorCode: "ORIGINAL");

        var changed = original
            .WithMessage("changed")
            .WithMessageKey("ChangedKey")
            .WithSeverity(ValidationErrorSeverity.Warning)
            .WithErrorCode("CHANGED")
            .WithParameters([
                new KeyValuePair<string, object?>("Min", 1),
                new KeyValuePair<string, object?>("Max", 10)
            ]);

        MSTestAssertions.That(() => original.Message.Should().Be("original"));
        MSTestAssertions.That(() => original.MessageKey.Should().Be("OriginalKey"));
        MSTestAssertions.That(() => original.Severity.Should().Be(ValidationErrorSeverity.Error));
        MSTestAssertions.That(() => original.ErrorCode.Should().Be("ORIGINAL"));
        MSTestAssertions.That(() => original.Parameters.Should().BeEmpty());

        MSTestAssertions.That(() => changed.Message.Should().Be("changed"));
        MSTestAssertions.That(() => changed.MessageKey.Should().Be("ChangedKey"));
        MSTestAssertions.That(() => changed.Severity.Should().Be(ValidationErrorSeverity.Warning));
        MSTestAssertions.That(() => changed.ErrorCode.Should().Be("CHANGED"));
        MSTestAssertions.That(() => changed.Parameters["Min"].Should().Be(1));
        MSTestAssertions.That(() => changed.Parameters["Max"].Should().Be(10));
    }

    [TestMethod]
    public void RuleTemplate_DynamicParameterReplacesAStaticParameterWithTheSameName() {
        var template = new RuleTemplate<string?>(
            "ParameterReplacement",
            static value => value == "expected",
            message: "{Value}",
            messageKey: "ParameterReplacement")
            .WithParameter("Value", "static")
            .WithParameter("Value", static value => value);

        var result = new SingleTemplateValidator(template)
            .Validate(new RuleTemplateModel { Text = "actual" });
        var error = result.Errors.Single();

        MSTestAssertions.That(() => template.Parameters.ContainsKey("Value").Should().BeFalse());
        MSTestAssertions.That(() => error.MessageParameters!["Value"].Should().Be("actual"));
    }

    [TestMethod]
    public void RuleTemplate_DescriptorExposesTheTemplateNameAndParameters() {
        var descriptor = new RuleTemplateValidator().Describe();
        var rule = descriptor.RulesForMember(nameof(RuleTemplateModel.Text)).Single();

        MSTestAssertions.That(() => rule.Kind.Should().Be("ExactLinesCount"));
        MSTestAssertions.That(() => rule.MessageKey.Should().Be("ExactLinesCount"));
        MSTestAssertions.That(() => rule.Severity.Should().Be(ValidationErrorSeverity.Warning));
        MSTestAssertions.That(() => rule.ErrorCode.Should().Be("LINES_COUNT"));
        MSTestAssertions.That(() => rule.Parameters["Expected"].Should().Be(3));
        MSTestAssertions.That(() => rule.Parameters.ContainsKey("Actual").Should().BeTrue());
        MSTestAssertions.That(() => rule.Parameters["Actual"].Should().BeNull());
    }

    [TestMethod]
    public void RuleTemplate_ClientRulesAreServerOnlyAndKeepTheParameters() {
        var descriptor = new RuleTemplateValidator().Describe();
        var document = JsonNode.Parse(ClientRulesWriter.WriteToString([descriptor]))!;
        var rules = document["validators"]![0]!["members"]!.AsArray()
            .Single(static member => member!["name"]!.GetValue<string>() == nameof(RuleTemplateModel.Text))!["rules"]!
            .AsArray();
        var rule = rules.Single(static node => node!["rule"]!.GetValue<string>() == "ExactLinesCount")!;

        MSTestAssertions.That(() => rule["serverOnly"]!.GetValue<bool>().Should().BeTrue());
        MSTestAssertions.That(() => rule["params"]!["Expected"]!.GetValue<int>().Should().Be(3));
        MSTestAssertions.That(() => rule["params"]!["Actual"].Should().BeNull());
    }

    [TestMethod]
    public void RuleTemplate_ParameterValueRetainsItsTypeAndFormattingMetadata() {
        var result = new RuleTemplateValidator().Validate(new RuleTemplateModel {
            Text = "one\ntwo\nthree",
            Amount = 150m,
            Age = 25
        });

        var error = result.Errors.Single();
        var amount = error.MessageParameters!["Amount"];

        MSTestAssertions.That(() => (amount is decimal).Should().BeTrue());
        MSTestAssertions.That(() => ((IFormattable)amount).ToString("C", CultureInfo.GetCultureInfo("nl-NL"))
            .Should().Contain("€"));
        MSTestAssertions.That(() => ((IFormattable)amount).ToString("C", CultureInfo.GetCultureInfo("en-US"))
            .Should().Contain("$"));
    }

    [TestMethod]
    public void RuleTemplate_WhenCondition_SkipsTheTemplate() {
        var validator = new ConditionalTemplateValidator();
        var skipped = new RuleTemplateModel {
            Text = "one\ntwo",
            Amount = 50m,
            Age = 25,
            IsCompany = false
        };
        var applied = new RuleTemplateModel {
            Text = "one\ntwo",
            Amount = 50m,
            Age = 25,
            IsCompany = true
        };

        ValidationAssert.IsValid(validator.Validate(skipped));
        ValidationAssert.ErrorCount(validator.Validate(applied), 1);
    }

    [TestMethod]
    public void RuleTemplate_RuleSet_FiltersTheTemplateAndDescribesTheGroup() {
        var validator = new RuleSetTemplateValidator();
        var model = new RuleTemplateModel {
            Text = "one\ntwo",
            Amount = 50m,
            Age = 25
        };

        var selected = validator.Validate(
            model,
            new ValidationCallOptions { RuleSets = ["TextSet"] });
        var other = validator.Validate(
            model,
            new ValidationCallOptions { RuleSets = ["Other"] });

        ValidationAssert.ErrorCount(selected, 1);
        ValidationAssert.IsValid(other);

        var rule = validator.Describe().RulesForMember(nameof(RuleTemplateModel.Text)).Single();

        MSTestAssertions.That(() => rule.Kind.Should().Be("ExactLinesCount"));
        MSTestAssertions.That(() => rule.Parameters["RuleSets"].Should().Be("TextSet"));
    }

    [TestMethod]
    public void RuleTemplate_Constructor_ValidatesItsArguments() {
        MSTestAssertions.That(() => Throws<ArgumentException>(() =>
            _ = new RuleTemplate<string>(" ", static _ => true)));
        MSTestAssertions.That(() => Throws<ArgumentNullException>(() =>
            _ = new RuleTemplate<string>("Name", null!)));
        MSTestAssertions.That(() => Throws<ArgumentException>(() =>
            _ = new RuleTemplate<string>("Name", static _ => true, messageKey: " ")));
        MSTestAssertions.That(() => Throws<ArgumentException>(() =>
            _ = new RuleTemplate<string>("Name", static _ => true, errorCode: " ")));
    }

    private static void Throws<TException>(Action action) where TException : Exception {
        try {
            action();
        }
        catch(TException) {
            return;
        }

        throw new AssertFailedException($"Expected {typeof(TException).Name}.");
    }
}
