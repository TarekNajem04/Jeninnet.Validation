//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Sanitization;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

internal sealed class SanitizeModel {
    public string? Name { get; set; }
    public string? Code { get; set; }
    public IList<string?>? Tags { get; set; }
    public int Age { get; set; }
}

internal sealed class SanitizeInitModel {
    public string? Name { get; init; }
}

file sealed class TrimValidator : Validator<SanitizeModel> {
    protected override void Configure(ValidatorRuleBuilder<SanitizeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(
                static model => model.Name,
                nameof(SanitizeModel.Name),
                static (model, value) => model.Name = value)
               .Sanitize(Sanitize.Trim)
               .NotEmpty("name-required");
    }
}

file sealed class OptionsValidator(SanitizeOptions options) : Validator<SanitizeModel> {
    protected override void Configure(ValidatorRuleBuilder<SanitizeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(
                static model => model.Name,
                nameof(SanitizeModel.Name),
                static (model, value) => model.Name = value)
               .Sanitize(options)
               .NotEmpty("name-required");
    }
}

file sealed class NoSanitizeValidator : Validator<SanitizeModel> {
    protected override void Configure(ValidatorRuleBuilder<SanitizeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(SanitizeModel.Name))
               .NotEmpty("name-required");
    }
}

file sealed class WholeModelValidator(Action<SanitizeModel> sanitize) : Validator<SanitizeModel> {
    protected override void Configure(ValidatorRuleBuilder<SanitizeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Sanitize(sanitize);

        builder.RuleFor(static model => model.Name, nameof(SanitizeModel.Name))
               .NotEmpty("name-required");
    }
}

file sealed class CustomSanitizer : ISanitizer<SanitizeModel> {
    public void Sanitize(SanitizeModel instance) =>
        instance.Name = instance.Name?.Trim();
}

file sealed class CustomSanitizerValidator : Validator<SanitizeModel> {
    protected override void Configure(ValidatorRuleBuilder<SanitizeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Sanitize(new CustomSanitizer());

        builder.RuleFor(static model => model.Name, nameof(SanitizeModel.Name))
               .NotEmpty("name-required");
    }
}

file sealed class RuleSetSanitizeValidator : Validator<SanitizeModel> {
    protected override void Configure(ValidatorRuleBuilder<SanitizeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(
                static model => model.Name,
                nameof(SanitizeModel.Name),
                static (model, value) => model.Name = value)
               .Sanitize(Sanitize.Trim)
               .NotEmpty("name-required")
               .RuleSet("NameSet");

        builder.RuleFor(static model => model.Age, nameof(SanitizeModel.Age))
               .InclusiveBetween(18, 99, "age-range");
    }
}

file sealed class ThrowingSanitizeValidator : Validator<SanitizeModel> {
    protected override void Configure(ValidatorRuleBuilder<SanitizeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(
                static model => model.Name,
                nameof(SanitizeModel.Name),
                static (model, value) => model.Name = value)
               .Sanitize(static _ => throw new InvalidOperationException("boom"))
               .NotEmpty("name-required");
    }
}

file sealed class RecordingValidator(Action<SanitizeModel, string?> onWrite) : Validator<SanitizeModel> {
    protected override void Configure(ValidatorRuleBuilder<SanitizeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(
                static model => model.Name,
                nameof(SanitizeModel.Name),
                (model, value) => {
                    onWrite(model, value);
                    model.Name = value;
                })
               .Sanitize(Sanitize.Trim)
               .NotEmpty("name-required");
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class SanitizationTests {
    public TestContext TestContext { get; set; } = null!;
    [TestMethod]
    public void Trim_BeforeNotEmpty_PassesAndMutatesTheModel() {
        var model = new SanitizeModel { Name = "  Ada  " };

        var result = new TrimValidator().Validate(model);

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => model.Name.Should().Be("Ada"));
    }

    [TestMethod]
    public void WithoutSanitizers_TheModelIsUntouched() {
        var model = new SanitizeModel { Name = "  Ada  " };

        var result = new NoSanitizeValidator().Validate(model);

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => model.Name.Should().Be("  Ada  "));
    }

    [TestMethod]
    public void SanitizeOptions_ToUpperUsesTheConfiguredCulture() {
        var turkish = new SanitizeOptions { ToUpper = true, Culture = CultureInfo.GetCultureInfo("tr-TR") };
        var model = new SanitizeModel { Name = "i" };

        ValidationAssert.IsValid(new OptionsValidator(turkish).Validate(model));

        MSTestAssertions.That(() => model.Name.Should().Be("İ"));
    }

    [TestMethod]
    public void SanitizeOptions_ToLowerUsesTheInvariantCultureByDefault() {
        var options = new SanitizeOptions { ToLower = true };
        var model = new SanitizeModel { Name = "ADA" };

        ValidationAssert.IsValid(new OptionsValidator(options).Validate(model));

        MSTestAssertions.That(() => model.Name.Should().Be("ada"));
    }

    [TestMethod]
    public void SanitizeOptions_ToUpperWinsOverToLower() {
        var options = new SanitizeOptions {
            ToUpper = true,
            ToLower = true,
            Culture = CultureInfo.InvariantCulture
        };

        MSTestAssertions.That(() => options.Build().Should().HaveCount(1));
    }

    [TestMethod]
    public void NormalizeWhitespace_CollapsesRunsAndTrims() {
        MSTestAssertions.That(() => Sanitize.NormalizeWhitespace("  Ada\t\n  Lovelace  ").Should().Be("Ada Lovelace"));
        MSTestAssertions.That(() => Sanitize.NormalizeWhitespace(null).Should().BeNull());
    }

    [TestMethod]
    public void RemoveControlChars_RemovesControlsButKeepsLineBreaks() {
        MSTestAssertions.That(() => Sanitize.RemoveControlChars("A\0B" + "\x01" + "C\tD\nE\rF").Should().Be("ABC\tD\nE\rF"));
        MSTestAssertions.That(() => Sanitize.RemoveControlChars(null).Should().BeNull());
    }

    [TestMethod]
    public void EachElement_SanitizesListItemsInPlace() {
        var tags = new List<string?> { "  a  ", null, "b " };
        var sanitize = Sanitize.EachElement(Sanitize.Trim);

        var result = sanitize(tags);

        MSTestAssertions.That(() => ReferenceEquals(result, tags).Should().BeTrue());
        MSTestAssertions.That(() => tags[0].Should().Be("a"));
        MSTestAssertions.That(() => tags[1].Should().BeNull());
        MSTestAssertions.That(() => tags[2].Should().Be("b"));
        MSTestAssertions.That(() => ((object?)sanitize(null)).Should().BeNull());
    }

    [TestMethod]
    public void Sanitize_WithoutSetter_ThrowsInvalidOperation() {
        var builder = new ValidatorRuleBuilder<SanitizeModel>([], []);
        var ruleBuilder = builder.RuleFor(static model => model.Name, nameof(SanitizeModel.Name));

        MSTestAssertions.That(() => Throws<InvalidOperationException>(() => ruleBuilder.Sanitize(Sanitize.Trim)));
    }

    [TestMethod]
    public void SanitizeOptions_OnNonStringMember_ThrowsInvalidOperation() {
        var builder = new ValidatorRuleBuilder<SanitizeModel>([], []);
        var ruleBuilder = builder.RuleFor(static model => model.Age, nameof(SanitizeModel.Age));

        MSTestAssertions.That(() => Throws<InvalidOperationException>(() => ruleBuilder.Sanitize(new SanitizeOptions { Trim = true })));
    }

    [TestMethod]
    public void WholeModelSanitizer_RunsBeforeRules() {
        var model = new SanitizeModel { Name = "  Ada  " };

        var result = new WholeModelValidator(static model => model.Name = model.Name?.Trim()).Validate(model);

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => model.Name.Should().Be("Ada"));
    }

    [TestMethod]
    public void WholeModelCustomSanitizer_RunsBeforeRules() {
        var model = new SanitizeModel { Name = "  Ada  " };

        var result = new CustomSanitizerValidator().Validate(model);

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => model.Name.Should().Be("Ada"));
    }

    [TestMethod]
    public void Sanitize_WritesOnlyWhenTheValueChanged() {
        var writes = 0;
        var clean = new SanitizeModel { Name = "Ada" };
        var dirty = new SanitizeModel { Name = "  Ada  " };

        ValidationAssert.IsValid(new RecordingValidator((_, _) => writes++).Validate(clean));
        MSTestAssertions.That(() => writes.Should().Be(0));

        ValidationAssert.IsValid(new RecordingValidator((_, _) => writes++).Validate(dirty));
        MSTestAssertions.That(() => writes.Should().Be(1));
        MSTestAssertions.That(() => dirty.Name.Should().Be("Ada"));
    }

    [TestMethod]
    public void Sanitize_RunsEvenWhenRuleSetsSelectOtherRules() {
        var model = new SanitizeModel { Name = "  Ada  ", Age = 25 };
        var validator = new RuleSetSanitizeValidator();

        var result = validator.Validate(
            model,
            new ValidationCallOptions { RuleSets = ["Other"] });

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => model.Name.Should().Be("Ada"));
    }

    [TestMethod]
    public void Sanitize_NullValue_SkipsTheWriteAndRulesStillApply() {
        var model = new SanitizeModel { Name = null };

        var result = new TrimValidator().Validate(model);

        ValidationAssert.HasError(result, nameof(SanitizeModel.Name));
    }

    [TestMethod]
    public async Task Sanitize_RunsOnTheAsyncPathAsync() {
        var model = new SanitizeModel { Name = "  Ada  " };

        var result = await new TrimValidator().ValidateAsync(model, TestContext.CancellationToken);

        ValidationAssert.IsValid(result);
        MSTestAssertions.That(() => model.Name.Should().Be("Ada"));
    }

    [TestMethod]
    public void Sanitize_ThrowingTransform_WrapsInValidationException() => MSTestAssertions.That(() => Throws<ValidationException>(() =>
            new ThrowingSanitizeValidator().Validate(new SanitizeModel { Name = "Ada" })));

    [TestMethod]
    public void Sanitize_DoesNotAppearInDescriptorsOrClientRules() {
        var descriptor = new TrimValidator().Describe();
        var rule = descriptor.RulesForMember(nameof(SanitizeModel.Name)).Single();

        MSTestAssertions.That(() => rule.Kind.Should().Be("NotEmpty"));

        var document = JsonNode.Parse(ClientRulesWriter.WriteToString([descriptor]))!;
        var rules = document["validators"]![0]!["members"]!.AsArray()
            .Single(static member => member!["name"]!.GetValue<string>() == nameof(SanitizeModel.Name))!["rules"]!
            .AsArray();

        MSTestAssertions.That(() => rules.Should().HaveCount(1));
        MSTestAssertions.That(() => rules[0]!["rule"]!.GetValue<string>().Should().Be("NotEmpty"));
    }

    [TestMethod]
    public void ElementSanitize_IsExplicitlyRejected() {
        var builder = new ValidatorRuleBuilder<SanitizeModel>([], []);

        MSTestAssertions.That(() => Throws<InvalidOperationException>(() =>
            builder.RuleFor(static model => model.Tags, nameof(SanitizeModel.Tags))
                   .ForEachElement(static element => element
                       .Sanitize(Sanitize.Trim)
                       .NotEmpty())));
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
