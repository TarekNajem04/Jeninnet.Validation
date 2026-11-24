//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Metadata;

public sealed class CompositionChild {
    public string? Name { get; set; }
}

public sealed class CompositionParent {
    public CompositionChild? Child { get; set; }

    public List<string>? Children { get; set; }
}

public sealed class CompositionChildValidator : Validator<CompositionChild> {
    protected override void Configure(ValidatorRuleBuilder<CompositionChild> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(CompositionChild.Name))
               .NotEmpty();
    }
}

public sealed class BaseCompositionValidator : Validator<CompositionParent> {
    protected override void Configure(ValidatorRuleBuilder<CompositionParent> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Child, nameof(CompositionParent.Child))
               .NotNull();
    }
}

public sealed class DerivedCompositionValidator : Validator<CompositionParent> {
    protected override void Configure(ValidatorRuleBuilder<CompositionParent> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Include(new BaseCompositionValidator());
    }
}

public sealed class CompositionParentValidator : Validator<CompositionParent> {
    protected override void Configure(ValidatorRuleBuilder<CompositionParent> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Child, nameof(CompositionParent.Child))
               .SetValidator(new CompositionChildValidator());

        builder.RuleFor(static x => x.Child, nameof(CompositionParent.Child))
               .ChildRules(static child =>
                   child.RuleFor(static x => x.Name, nameof(CompositionChild.Name)).NotEmpty());

        builder.RuleFor(static x => x.Children, nameof(CompositionParent.Children))
               .ForEachElement(static element => element.NotEmpty());
    }
}

internal sealed class RawCompositionRule : ISyncValidationRule<string> {
    public void Validate(in ValidationContext<string> context, ValidationErrorCollector errors) {
    }
}

[TestClass]
public sealed class DescriptorCompositionCompletenessTests {
    [TestMethod]
    public void FluentComposition_SetValidatorAndChildRules_AreDescribedAsNested() {
        var descriptor = new CompositionParentValidator().Describe();
        var childRules = descriptor.RulesForMember(nameof(CompositionParent.Child))
            .OfType<ValidationRuleDescriptor>()
            .Where(static rule => rule.Kind == "Nested")
            .ToArray();

        MSTestAssertions.That(() => childRules.Should().HaveCount(2));

        foreach(var nested in childRules) {
            MSTestAssertions.That(() => nested.NestedDescriptor.Should().NotBeNull());
            MSTestAssertions.That(() => nested.NestedDescriptor!
                .RulesForMember(nameof(CompositionChild.Name))
                .Any(static rule => rule.Kind == "NotEmpty")
                .Should().BeTrue());
        }
    }

    [TestMethod]
    public void FluentComposition_ForEachElement_IsDescribedWithNestedRules() {
        var descriptor = new CompositionParentValidator().Describe();
        var forEach = descriptor.RulesForMember(nameof(CompositionParent.Children))
            .OfType<ValidationRuleDescriptor>()
            .Single(static rule => rule.Kind == "ForEachElement");

        MSTestAssertions.That(() => forEach.NestedDescriptor.Should().NotBeNull());
        MSTestAssertions.That(() => forEach.NestedDescriptor!
            .RulesForMember("*")
            .Any(static rule => rule.Kind == "NotEmpty")
            .Should().BeTrue());
    }

    [TestMethod]
    public void FluentInclude_IsDescribedWithNestedRules() {
        var descriptor = new DerivedCompositionValidator().Describe();
        var include = descriptor.Members
            .SelectMany(static member => member.Rules)
            .OfType<ValidationRuleDescriptor>()
            .Single(static rule => rule.Kind == "Include");

        MSTestAssertions.That(() => include.NestedDescriptor.Should().NotBeNull());
        MSTestAssertions.That(() => include.NestedDescriptor!
            .RulesForMember(nameof(CompositionParent.Child))
            .Any(static rule => rule.Kind == "NotNull")
            .Should().BeTrue());
    }

    [TestMethod]
    public void ForEachElement_HiddenInnerRule_IsAdvisoryNotDropped() {
        var accessor = ValidatorFactory.Accessor<CompositionParent, IEnumerable<string>?>(
            static parent => parent.Children,
            nameof(CompositionParent.Children));
        var rule = new ForEachElementRule<CompositionParent, string>(
            accessor,
            [new RawCompositionRule()],
            []);
        var builder = new ValidationDescriptorBuilder(nameof(DescriptorCompositionCompletenessTests), typeof(CompositionParent));
        builder.AddRule(rule);
        var descriptor = builder.Build();
        var outer = descriptor.RulesForMember(nameof(CompositionParent.Children))
            .OfType<ValidationRuleDescriptor>()
            .Single();

        MSTestAssertions.That(() => outer.Kind.Should().Be("ForEachElement"));
        MSTestAssertions.That(() => outer.NestedDescriptor.Should().NotBeNull());

        var advisory = outer.NestedDescriptor!.RulesForMember("*").Single();
        MSTestAssertions.That(() => advisory.Kind.Should().Be("Custom"));
        MSTestAssertions.That(() => advisory.Parameters["Reason"].Should().Be("Rule has no descriptor metadata."));
        MSTestAssertions.That(() => advisory.Parameters["Type"].Should().Be(nameof(RawCompositionRule)));
    }

    [TestMethod]
    public void ForEachElement_AdvisoryDoesNotBecomeSchemaConstraint() {
        var descriptor = new CompositionParentValidator().Describe();
        var schema = ValidationSchemaWriter.Write(
            descriptor,
            new ValidationSchemaOptions { IncludeAdvisoryRules = true });
        var children = schema["properties"]!["Children"]!;

        MSTestAssertions.That(() => children["items"].Should().BeNull());
        MSTestAssertions.That(() => children["type"]?.GetValue<string>().Should().NotBe("array"));
        MSTestAssertions.That(() => children["x-validation-rules"]!.AsArray()
            .Any(static rule => rule!["kind"]!.GetValue<string>() == "ForEachElement")
            .Should().BeTrue());
    }
}
