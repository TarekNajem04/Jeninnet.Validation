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

[JeninnetValidatable(typeof(DataAnnotationsModelValidator))]
public sealed class DataAnnotationsModel {
    public string? Name { get; set; }

    public int Age { get; set; }
}

public sealed class DataAnnotationsModelValidator : Validator<DataAnnotationsModel> {
    protected override void Configure(ValidatorRuleBuilder<DataAnnotationsModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(DataAnnotationsModel.Name))
               .NotEmpty();

        builder.RuleFor(static x => x.Age, nameof(DataAnnotationsModel.Age))
               .InclusiveBetween(18, 120);
    }
}

[JeninnetValidatable(typeof(ParameterlessModelValidator), ErrorMessage = "Custom failure")]
public sealed class ParameterlessModel {
    public string? Code { get; set; }
}

public sealed class ParameterlessModelValidator : Validator<ParameterlessModel> {
    protected override void Configure(ValidatorRuleBuilder<ParameterlessModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Code, nameof(ParameterlessModel.Code))
               .NotEmpty();
    }
}

[JeninnetValidatable(typeof(SeverityModelValidator), IncludeErrorCodes = true)]
public sealed class SeverityModel {
    public string? Name { get; set; }
}

public sealed class SeverityModelValidator : Validator<SeverityModel> {
    protected override void Configure(ValidatorRuleBuilder<SeverityModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(SeverityModel.Name))
               .NotEmpty()
               .WithMessage("warn")
               .WithSeverity(ValidationErrorSeverity.Warning)
               .WithCode("W001");
    }
}

[JeninnetValidatable(typeof(DependentModelValidator))]
public sealed class DependentModel {
    public string? Name { get; set; }
}

public sealed class DependentModelValidator(string dependency) : Validator<DependentModel> {
    public string Dependency { get; } = dependency;

    protected override void Configure(ValidatorRuleBuilder<DependentModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(DependentModel.Name))
               .NotEmpty();
    }
}

public sealed class ValidatableObjectModel : System.ComponentModel.DataAnnotations.IValidatableObject {
    public string? Name { get; set; }

    public IEnumerable<DataAnnotationsResult> Validate(DataAnnotationsValidationContext validationContext) {
        ArgumentNullException.ThrowIfNull(validationContext);

        var result = JeninnetDataAnnotationsAdapter.Validate(
            this,
            typeof(ValidatableObjectModel),
            typeof(ValidatableObjectModelValidator),
            validationContext.GetService);

        return JeninnetDataAnnotationsAdapter.ToValidationResults(result);
    }
}

public sealed class ValidatableObjectModelValidator : Validator<ValidatableObjectModel> {
    protected override void Configure(ValidatorRuleBuilder<ValidatableObjectModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ValidatableObjectModel.Name))
               .NotEmpty();
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class DataAnnotationsBridgeTests {
    [TestMethod]
    public void Attribute_ValidatesWithDiResolvedValidator() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<DataAnnotationsModel>, DataAnnotationsModelValidator>();
        using var provider = services.BuildServiceProvider();
        var model = new DataAnnotationsModel { Name = "", Age = 10 };
        var results = new List<DataAnnotationsResult>();

        var valid = DataAnnotationsValidator.TryValidateObject(
            model,
            new DataAnnotationsValidationContext(model, provider, items: null),
            results,
            validateAllProperties: true);

        MSTestAssertions.That(() => valid.Should().BeFalse());
        MSTestAssertions.That(() => results.Should().HaveCount(1));
        MSTestAssertions.That(() => results[0].MemberNames.Any(static name => name == nameof(DataAnnotationsModel.Name)).Should().BeTrue());
        MSTestAssertions.That(() => results[0].MemberNames.Any(static name => name == nameof(DataAnnotationsModel.Age)).Should().BeTrue());
        MSTestAssertions.That(() => results[0].ErrorMessage.Should().Contain("NotEmpty"));
        MSTestAssertions.That(() => results[0].ErrorMessage.Should().Contain("InclusiveBetween"));
    }

    [TestMethod]
    public void Attribute_FallsBackToParameterlessValidator() {
        var model = new ParameterlessModel { Code = "" };
        var results = new List<DataAnnotationsResult>();

        var valid = DataAnnotationsValidator.TryValidateObject(
            model,
            new DataAnnotationsValidationContext(model),
            results,
            validateAllProperties: true);

        MSTestAssertions.That(() => valid.Should().BeFalse());
        MSTestAssertions.That(() => results[0].ErrorMessage.Should().Be("Custom failure"));
    }

    [TestMethod]
    public void Attribute_ConvertsSeverityAndErrorCode() {
        var model = new SeverityModel { Name = "" };
        var results = new List<DataAnnotationsResult>();

        var valid = DataAnnotationsValidator.TryValidateObject(
            model,
            new DataAnnotationsValidationContext(model),
            results,
            validateAllProperties: true);

        MSTestAssertions.That(() => valid.Should().BeFalse());
        MSTestAssertions.That(() => results[0].ErrorMessage.Should().Be("[Warning] warn (W001)"));
    }

    [TestMethod]
    public void Attribute_ExplainsMissingValidatorResolution() {
        var model = new DependentModel { Name = "" };
        var results = new List<DataAnnotationsResult>();
        InvalidOperationException? caught = null;

        try {
            DataAnnotationsValidator.TryValidateObject(
                model,
                new DataAnnotationsValidationContext(model),
                results,
                validateAllProperties: true);
        }
        catch(InvalidOperationException exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().NotBeNull());
        MSTestAssertions.That(() => caught!.Message.Should().Contain("Register IValidator<DependentModel>"));
    }

    [TestMethod]
    public void IValidatableObject_HelperProducesDataAnnotationsResults() {
        var model = new ValidatableObjectModel { Name = "" };
        var results = new List<DataAnnotationsResult>();

        var valid = DataAnnotationsValidator.TryValidateObject(
            model,
            new DataAnnotationsValidationContext(model),
            results,
            validateAllProperties: true);

        MSTestAssertions.That(() => valid.Should().BeFalse());
        MSTestAssertions.That(() => results[0].MemberNames.Any(static name => name == nameof(ValidatableObjectModel.Name)).Should().BeTrue());
    }
}
