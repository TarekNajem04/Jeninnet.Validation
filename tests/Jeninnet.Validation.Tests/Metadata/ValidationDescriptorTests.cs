//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Metadata;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

internal sealed class DescriptorModel {
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public List<string>? Tags { get; set; }
}

file sealed class DescriptorValidator : Validator<DescriptorModel> {
    protected override void Configure(ValidatorRuleBuilder<DescriptorModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(DescriptorModel.Name))
               .NotEmpty();

        builder.RuleFor(static x => x.Email, nameof(DescriptorModel.Email))
               .EmailAddress();

        builder.RuleFor(static x => x.Age, nameof(DescriptorModel.Age))
               .GreaterThan(18);

        builder.RuleFor(static x => x.Tags, nameof(DescriptorModel.Tags))
               .Count(1, 5);
    }
}

file sealed class IncludeDescriptorValidator : Validator<DescriptorModel> {
    protected override void Configure(ValidatorRuleBuilder<DescriptorModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Include(new DescriptorValidator());
    }
}

file sealed class ConditionalDescriptorValidator : Validator<DescriptorModel> {
    protected override void Configure(ValidatorRuleBuilder<DescriptorModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(DescriptorModel.Name))
               .NotEmpty()
               .When(static x => x.Age > 0);
    }
}

file sealed class RichDescriptorValidator : Validator<DescriptorModel> {
    protected override void Configure(ValidatorRuleBuilder<DescriptorModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(DescriptorModel.Name))
               .NotEmpty()
               .WithCode("name_required")
               .WithSeverity(ValidationErrorSeverity.Warning)
               .WithState("name-state")
               .RuleSet("Create");

        builder.RuleFor(static x => x.Age, nameof(DescriptorModel.Age))
               .GreaterThan(18)
               .DependentRules(static dependent =>
                   dependent.RuleFor(static x => x.Age, nameof(DescriptorModel.Age))
                            .LessThan(65));
    }
}

internal sealed class UndescribedRule<T> : ISyncValidationRule<T> {
    public void Validate(in ValidationContext<T> context, ValidationErrorCollector errors) {
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class ValidationDescriptorTests {
    [TestMethod]
    public void FluentValidator_DescribesMembersAndParameters() {
        var validator = new DescriptorValidator();

        var descriptor = validator.Describe();

        MSTestAssertions.That(() => descriptor.ValidatorName.Should().Contain(nameof(DescriptorValidator)));

        var nameRules = descriptor.RulesForMember(nameof(DescriptorModel.Name));
        MSTestAssertions.That(() => nameRules.Should().HaveCount(1));
        MSTestAssertions.That(() => nameRules[0].Kind.Should().Be("NotEmpty"));
        MSTestAssertions.That(() => nameRules[0].MessageKey.Should().Be("NotEmpty"));
        MSTestAssertions.That(() => nameRules[0].Parameters.Should().BeEmpty());

        var emailRules = descriptor.RulesForMember(nameof(DescriptorModel.Email));
        MSTestAssertions.That(() => emailRules.Should().HaveCount(1));
        MSTestAssertions.That(() => emailRules[0].Kind.Should().Be("Email"));

        var ageRules = descriptor.RulesForMember(nameof(DescriptorModel.Age));
        MSTestAssertions.That(() => ageRules.Should().HaveCount(1));
        MSTestAssertions.That(() => ageRules[0].Kind.Should().Be("GreaterThan"));
        MSTestAssertions.That(() => ageRules[0].Parameters["Threshold"].Should().Be(18));

        var tagRules = descriptor.RulesForMember(nameof(DescriptorModel.Tags));
        MSTestAssertions.That(() => tagRules.Should().HaveCount(1));
        MSTestAssertions.That(() => tagRules[0].Kind.Should().Be("Count"));
        MSTestAssertions.That(() => tagRules[0].Parameters["Min"].Should().Be(1));
        MSTestAssertions.That(() => tagRules[0].Parameters["Max"].Should().Be(5));
    }

    [TestMethod]
    public void FluentValidator_CachesTheDescriptorAndHandlesUnknownMembers() {
        var validator = new DescriptorValidator();

        var first = validator.Describe();
        var second = validator.Describe();

        MSTestAssertions.That(() => ReferenceEquals(first, second).Should().BeTrue());
        MSTestAssertions.That(() => first.RulesForMember("Unknown").Should().BeEmpty());
    }

    [TestMethod]
    public void FluentValidator_DescribesIncludedRuleOnce() {
        var descriptor = new IncludeDescriptorValidator().Describe();

        MSTestAssertions.That(() => descriptor.RulesForMember("*").Should().HaveCount(1));
    }

    [TestMethod]
    public void FluentValidator_DescribesConditionalRules() {
        var descriptor = new ConditionalDescriptorValidator().Describe();

        var rules = descriptor.RulesForMember(nameof(DescriptorModel.Name));

        MSTestAssertions.That(() => rules.Should().HaveCount(1));
        MSTestAssertions.That(() => rules[0].Kind.Should().Be("NotEmpty"));
        MSTestAssertions.That(() => rules[0].HasCondition.Should().BeTrue());
    }

    [TestMethod]
    public void FluentValidator_DescribesRuleOptionsAndRuleSets() {
        var descriptor = new RichDescriptorValidator().Describe();

        var rule = descriptor.RulesForMember(nameof(DescriptorModel.Name)).Single();

        MSTestAssertions.That(() => rule.Kind.Should().Be("NotEmpty"));
        MSTestAssertions.That(() => rule.ErrorCode.Should().Be("name_required"));
        MSTestAssertions.That(() => rule.Severity.Should().Be(ValidationErrorSeverity.Warning));
        MSTestAssertions.That(() => rule.CustomState.Should().Be("name-state"));
        MSTestAssertions.That(() => rule.Parameters["RuleSets"].Should().Be("Create"));
    }

    [TestMethod]
    public void FluentValidator_DescribesDependentRules() {
        var descriptor = new RichDescriptorValidator().Describe();

        var rules = descriptor.RulesForMember(nameof(DescriptorModel.Age));

        MSTestAssertions.That(() => rules.Should().HaveCount(2));
        MSTestAssertions.That(() => rules.Select(static rule => rule.Kind)
            .SequenceEqual(["GreaterThan", "LessThan"]).Should().BeTrue());
        MSTestAssertions.That(() => rules.All(static rule =>
            rule.Parameters["Dependent"] is true).Should().BeTrue());
    }

    [TestMethod]
    public void ValidationDescriptorBuilder_RecordsUndescribedRulesAsAdvisories() {
        var builder = new ValidationDescriptorBuilder(nameof(UndescribedRule<>), typeof(DescriptorModel));
        builder.AddRule(new UndescribedRule<DescriptorModel>());

        var descriptor = builder.Build();
        var advisory = descriptor.RulesForMember("*").Single();

        MSTestAssertions.That(() => advisory.Kind.Should().Be("Custom"));
        MSTestAssertions.That(() => advisory.Parameters["Reason"].Should().Be("Rule has no descriptor metadata."));
        MSTestAssertions.That(() => advisory.Parameters["Type"].Should().Be("UndescribedRule`1"));
    }

    [TestMethod]
    public void ValidationDescriptorBuilder_RetainsDeterministicInsertionOrder() {
        var builder = new ValidationDescriptorBuilder(nameof(DescriptorModel), typeof(DescriptorModel));

        builder.Member("Second").Rule("NotEmpty");
        builder.Member("First").Rule("Email");
        builder.Member("Second").Rule("Length");

        var descriptor = builder.Build();

        MSTestAssertions.That(() => descriptor.Members.Select(static member => member.Name)
            .SequenceEqual(["Second", "First"]).Should().BeTrue());
        MSTestAssertions.That(() => descriptor.RulesForMember("Second").Select(static rule => rule.Kind)
            .SequenceEqual(["NotEmpty", "Length"]).Should().BeTrue());
    }
}
