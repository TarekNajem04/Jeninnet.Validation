//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.DataAnnotations.Tests;

[JeninnetValidatable(typeof(SkippedTypeValidator))]
[SkipValidation]
public sealed class SkippedTypeModel {
    public string? Name { get; set; }
}

public sealed class SkippedTypeValidator : Validator<SkippedTypeModel> {
    protected override void Configure(ValidatorRuleBuilder<SkippedTypeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static model => model.Name, nameof(SkippedTypeModel.Name)).NotEmpty();
    }
}

public sealed class SkippedMemberModel {
    [SkipValidation]
    [JeninnetValidateProperty(typeof(SkippedMemberModelValidator), nameof(SkippedMemberModel.Name))]
    public string? Name { get; set; }
}

public sealed class SkippedMemberModelValidator : Validator<SkippedMemberModel> {
    protected override void Configure(ValidatorRuleBuilder<SkippedMemberModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static model => model.Name, nameof(SkippedMemberModel.Name)).NotEmpty();
    }
}

[TestClass]
public sealed class SkipValidationBridgeTests {
    [TestMethod]
    public void ClassSkip_BypassesClassLevelBridge() {
        var model = new SkippedTypeModel { Name = string.Empty };
        var results = new List<DataAnnotationsResult>();

        var valid = DataAnnotationsValidator.TryValidateObject(
            model,
            new DataAnnotationsValidationContext(model),
            results,
            validateAllProperties: true);

        MSTestAssertions.That(() => valid.Should().BeTrue());
        MSTestAssertions.That(() => results.Should().BeEmpty());
    }

    [TestMethod]
    public void MemberSkip_BypassesPropertyLevelBridge() {
        var model = new SkippedMemberModel { Name = string.Empty };
        var results = new List<DataAnnotationsResult>();

        var valid = DataAnnotationsValidator.TryValidateObject(
            model,
            new DataAnnotationsValidationContext(model),
            results,
            validateAllProperties: true);

        MSTestAssertions.That(() => valid.Should().BeTrue());
        MSTestAssertions.That(() => results.Should().BeEmpty());
    }
}
