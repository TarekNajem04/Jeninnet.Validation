//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.DataAnnotations.Tests;

// ---------------------------------------------------------------------------
// Models and validators
// ---------------------------------------------------------------------------

public sealed class PropertyLevelModel {
    [JeninnetValidateProperty(typeof(PropertyLevelModelValidator), nameof(Name))]
    public string? Name { get; set; }

    public int Age { get; set; }
}

public sealed class PropertyLevelModelValidator : Validator<PropertyLevelModel> {
    protected override void Configure(ValidatorRuleBuilder<PropertyLevelModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(PropertyLevelModel.Name))
               .NotEmpty();

        builder.RuleFor(static x => x.Age, nameof(PropertyLevelModel.Age))
               .InclusiveBetween(18, 120);
    }
}

public sealed class GenericPropertyModel {
    [JeninnetValidateProperty<GenericPropertyModelValidator>(nameof(Name))]
    public string? Name { get; set; }
}

public sealed class GenericPropertyModelValidator : Validator<GenericPropertyModel> {
    protected override void Configure(ValidatorRuleBuilder<GenericPropertyModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(GenericPropertyModel.Name))
               .NotEmpty();
    }
}

public sealed class NestedPathModel {
    [JeninnetValidateProperty(typeof(NestedPathModelValidator), "Address.Street")]
    public NestedAddress? Address { get; set; }
}

public sealed class NestedAddress {
    public string? Street { get; set; }
}

public sealed class NestedPathModelValidator : Validator<NestedPathModel> {
    protected override void Configure(ValidatorRuleBuilder<NestedPathModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Address, "Address.Street")
               .Must(static _ => false, "Nested failure.");
    }
}

public sealed class IndexedPathModel {
    [JeninnetValidateProperty(typeof(IndexedPathModelValidator), "Lines[0].ProductName")]
    public List<IndexedLine>? Lines { get; set; }
}

public sealed class IndexedLine {
    public string? ProductName { get; set; }
}

public sealed class IndexedPathModelValidator : Validator<IndexedPathModel> {
    protected override void Configure(ValidatorRuleBuilder<IndexedPathModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Lines, "Lines[0].ProductName")
               .Must(static _ => false, "Indexed failure.");
    }
}

public sealed class AsyncPropertyModel {
    [JeninnetValidateProperty(typeof(AsyncPropertyModelValidator), nameof(Code))]
    public string? Code { get; set; }
}

public sealed class AsyncPropertyModelValidator : Validator<AsyncPropertyModel> {
    protected override void Configure(ValidatorRuleBuilder<AsyncPropertyModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Code, nameof(AsyncPropertyModel.Code))
               .Must(
                   static (value, _) => ValueTask.FromResult(!string.IsNullOrWhiteSpace(value)),
                   "Code is required.");
    }
}

[JeninnetValidatable(typeof(ClassLevelBothValidator))]
public sealed class ClassLevelAndPropertyModel {
    [JeninnetValidateProperty(typeof(PropertyCounterValidator), nameof(Name))]
    public string? Name { get; set; }
}

public sealed class ClassLevelBothValidator : Validator<ClassLevelAndPropertyModel> {
    protected override void Configure(ValidatorRuleBuilder<ClassLevelAndPropertyModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ClassLevelAndPropertyModel.Name))
               .NotEmpty();
    }
}

public sealed class PropertyCounterValidator : Validator<ClassLevelAndPropertyModel> {
    public static int Calls { get; private set; }

    public static void Reset() => Calls = 0;

    protected override void Configure(ValidatorRuleBuilder<ClassLevelAndPropertyModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ClassLevelAndPropertyModel.Name))
               .Custom(static (_, _, _) => Calls++);
    }
}

public sealed class WrongValidatorModel {
    [JeninnetValidateProperty(typeof(WrongValidator), nameof(Name))]
    public string? Name { get; set; }
}

public sealed class WrongModel {
    public string? Name { get; set; }
}

public sealed class WrongValidator : Validator<WrongModel> {
    protected override void Configure(ValidatorRuleBuilder<WrongModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(WrongModel.Name))
               .NotEmpty();
    }
}

public sealed class UnknownMemberModel {
    [JeninnetValidateProperty(typeof(UnknownMemberModelValidator), "Missing")]
    public string? Name { get; set; }
}

public sealed class UnknownMemberModelValidator : Validator<UnknownMemberModel> {
    protected override void Configure(ValidatorRuleBuilder<UnknownMemberModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(UnknownMemberModel.Name))
               .NotEmpty();
    }
}

public sealed class DiPropertyModel {
    [JeninnetValidateProperty(typeof(DiPropertyModelValidator), nameof(Name))]
    public string? Name { get; set; }
}

public sealed class DiPropertyModelValidator(string dependency) : Validator<DiPropertyModel> {
    public string Dependency { get; } = dependency;

    protected override void Configure(ValidatorRuleBuilder<DiPropertyModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(DiPropertyModel.Name))
               .NotEmpty();
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class PropertyLevelDataAnnotationsTests {
    [TestMethod]
    public void PropertyAttribute_ValidatesOnlySpecifiedMember() {
        var model = new PropertyLevelModel { Name = string.Empty, Age = 10 };
        var results = Validate(model);

        MSTestAssertions.That(() => results.Should().HaveCount(1));
        MSTestAssertions.That(() => results[0].MemberNames.Should().Contain(nameof(PropertyLevelModel.Name)));
        MSTestAssertions.That(() => results[0].MemberNames.Should().NotContain(nameof(PropertyLevelModel.Age)));
        MSTestAssertions.That(() => results[0].ErrorMessage.Should().Contain("NotEmpty"));
        MSTestAssertions.That(() => results[0].ErrorMessage.Should().NotContain("InclusiveBetween"));
    }

    [TestMethod]
    public void PropertyAttribute_GenericSyntaxWorks() {
        var model = new GenericPropertyModel { Name = string.Empty };
        var results = Validate(model);

        MSTestAssertions.That(() => results.Should().HaveCount(1));
        MSTestAssertions.That(() => results[0].MemberNames.Should().Contain(nameof(GenericPropertyModel.Name)));
        MSTestAssertions.That(() => results[0].ErrorMessage.Should().Contain("NotEmpty"));
    }

    [TestMethod]
    public void PropertyAttribute_SupportsNestedAndIndexedPaths() {
        var nestedResults = Validate(new NestedPathModel { Address = new NestedAddress() });
        var indexedResults = Validate(new IndexedPathModel { Lines = [] });

        MSTestAssertions.That(() => nestedResults.Should().HaveCount(1));
        MSTestAssertions.That(() => nestedResults[0].MemberNames.Should().Contain("Address.Street"));
        MSTestAssertions.That(() => indexedResults.Should().HaveCount(1));
        MSTestAssertions.That(() => indexedResults[0].MemberNames.Should().Contain("Lines[0].ProductName"));
    }

    [TestMethod]
    public void PropertyAttribute_AsyncValidatorThrows() {
        var model = new AsyncPropertyModel { Code = string.Empty };
        var act = () => Validate(model);

        MSTestAssertions.That(() => act.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>());
    }

    [TestMethod]
    public void PropertyAttribute_SkipsWhenClassLevelAttributeExists() {
        PropertyCounterValidator.Reset();
        var model = new ClassLevelAndPropertyModel { Name = string.Empty };
        var results = Validate(model);

        MSTestAssertions.That(() => results.Should().HaveCount(1));
        MSTestAssertions.That(() => PropertyCounterValidator.Calls.Should().Be(0));
        MSTestAssertions.That(() => results[0].ErrorMessage.Should().Contain("NotEmpty"));
    }

    [TestMethod]
    public void PropertyAttribute_UnknownMemberProducesNoErrors() {
        var model = new UnknownMemberModel { Name = string.Empty };
        var results = Validate(model);

        MSTestAssertions.That(() => results.Should().BeEmpty());
    }

    [TestMethod]
    public void PropertyAttribute_WrongValidatorExplains() {
        var model = new WrongValidatorModel { Name = string.Empty };
        InvalidOperationException? caught = null;

        try {
            _ = Validate(model);
        }
        catch(InvalidOperationException exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().NotBeNull());
        MSTestAssertions.That(() => caught!.Message.Should().Contain("does not implement IValidator<WrongValidatorModel>"));
    }

    [TestMethod]
    public void PropertyAttribute_ValidatesWithDiResolvedValidator() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<DiPropertyModel>>(_ => new DiPropertyModelValidator("dependency"));
        using var provider = services.BuildServiceProvider();
        var model = new DiPropertyModel { Name = string.Empty };
        var results = new List<DataAnnotationsResult>();

        var valid = DataAnnotationsValidator.TryValidateObject(
            model,
            new DataAnnotationsValidationContext(model, provider, items: null),
            results,
            validateAllProperties: true);

        MSTestAssertions.That(() => valid.Should().BeFalse());
        MSTestAssertions.That(() => results.Should().HaveCount(1));
        MSTestAssertions.That(() => results[0].MemberNames.Should().Contain(nameof(DiPropertyModel.Name)));
    }

    private static List<DataAnnotationsResult> Validate(object model) {
        var results = new List<DataAnnotationsResult>();

        _ = DataAnnotationsValidator.TryValidateObject(
            model,
            new DataAnnotationsValidationContext(model),
            results,
            validateAllProperties: true);

        return results;
    }
}
