//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Fluent;

internal sealed class UniqueItemsModel {
    public List<int>? Numbers { get; set; }

    public List<string>? Words { get; set; }

    public List<string>? Codes { get; set; }

    public List<UniqueTag>? Tags { get; set; }
}

internal sealed record UniqueTag(string Value);

internal sealed class UniqueShapesModel {
    public int[]? Array { get; set; }

    public ICollection<int>? Collection { get; set; }

    public IList<int>? List { get; set; }
}

file sealed class UniqueItemsValidator : Validator<UniqueItemsModel> {
    protected override void Configure(ValidatorRuleBuilder<UniqueItemsModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Numbers, nameof(UniqueItemsModel.Numbers))
               .Unique();

        builder.RuleFor(static model => model.Words, nameof(UniqueItemsModel.Words))
               .Unique(StringComparer.OrdinalIgnoreCase);

        builder.RuleFor(static model => model.Codes, nameof(UniqueItemsModel.Codes))
               .Unique();

        builder.RuleFor(static model => model.Tags, nameof(UniqueItemsModel.Tags))
               .Unique();
    }
}

file sealed class UniqueShapesValidator : Validator<UniqueShapesModel> {
    protected override void Configure(ValidatorRuleBuilder<UniqueShapesModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Array, nameof(UniqueShapesModel.Array)).Unique();
        builder.RuleFor(static model => model.Collection, nameof(UniqueShapesModel.Collection)).Unique();
        builder.RuleFor(static model => model.List, nameof(UniqueShapesModel.List)).Unique();
    }
}

[TestClass]
public sealed class UniqueItemsRuleTests {
    [TestMethod]
    public void Unique_DistinctItems_Passes() {
        var validator = new UniqueItemsValidator();
        var model = new UniqueItemsModel {
            Numbers = [1, 2, 3],
            Words = ["a", "B"],
            Codes = ["a", "A"],
            Tags = [new UniqueTag("first"), new UniqueTag("second")]
        };

        MSTestAssertions.That(() => validator.Validate(model).IsValid.Should().BeTrue());
    }

    [TestMethod]
    public void Unique_DuplicateItems_FailOnSyncPath() {
        var validator = new UniqueItemsValidator();
        var model = new UniqueItemsModel {
            Numbers = [1, 2, 2],
            Tags = [new UniqueTag("same"), new UniqueTag("same")]
        };

        var result = validator.Validate(model);

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
        MSTestAssertions.That(() => result.Errors.Should().HaveCount(2));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be(nameof(UniqueItemsModel.Numbers)));
        MSTestAssertions.That(() => result.Errors[0].MessageKey.Should().Be("UniqueItems"));
    }

    [TestMethod]
    public async Task Unique_DuplicateItems_FailOnAsyncPathAsync() {
        var validator = new UniqueItemsValidator();
        var model = new UniqueItemsModel {
            Numbers = [1, 2, 2],
            Tags = [new UniqueTag("same"), new UniqueTag("same")]
        };

        var result = await validator.ValidateAsync(model, CancellationToken.None);

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
        MSTestAssertions.That(() => result.Errors.Should().HaveCount(2));
    }

    [TestMethod]
    public void Unique_StringsAreCaseSensitiveByDefault() {
        var validator = new UniqueItemsValidator();
        var model = new UniqueItemsModel { Codes = ["a", "A"] };

        MSTestAssertions.That(() => validator.Validate(model).IsValid.Should().BeTrue());
    }

    [TestMethod]
    public void Unique_CustomComparer_CanCollapseCase() {
        var validator = new UniqueItemsValidator();
        var model = new UniqueItemsModel { Words = ["a", "A"] };

        MSTestAssertions.That(() => validator.Validate(model).IsValid.Should().BeFalse());
    }

    [TestMethod]
    public void Unique_NullAndEmptyCollections_Pass() {
        var validator = new UniqueItemsValidator();
        var model = new UniqueItemsModel { Numbers = null, Words = null, Tags = null };

        MSTestAssertions.That(() => validator.Validate(model).IsValid.Should().BeTrue());
        MSTestAssertions.That(() => validator.Validate(new UniqueItemsModel { Numbers = [] }).IsValid.Should().BeTrue());
    }

    [TestMethod]
    public void Unique_CollectionShapes_UseTheSameSemantics() {
        var validator = new UniqueShapesValidator();
        var valid = new UniqueShapesModel {
            Array = [1, 2],
            Collection = [1, 2],
            List = [1, 2]
        };
        var invalid = new UniqueShapesModel {
            Array = [1, 1],
            Collection = [2, 2],
            List = [3, 3]
        };

        MSTestAssertions.That(() => validator.Validate(valid).IsValid.Should().BeTrue());
        MSTestAssertions.That(() => validator.Validate(invalid).Errors.Should().HaveCount(3));
    }
}
