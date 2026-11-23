//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.DataAnnotations.Tests;

[JeninnetValidatable(typeof(AsyncClassValidator))]
public sealed class AsyncClassModel {
    public string? Name { get; set; }
}

public sealed class AsyncClassValidator : Validator<AsyncClassModel> {
    protected override void Configure(ValidatorRuleBuilder<AsyncClassModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(AsyncClassModel.Name))
               .Must(static (value, cancellationToken) => IsValidNameAsync(value, cancellationToken), "Name is required.");
    }

    private static async ValueTask<bool> IsValidNameAsync(
        string? value,
        CancellationToken cancellationToken) {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        return !string.IsNullOrWhiteSpace(value);
    }
}

[JeninnetValidatable(typeof(AsyncSeverityValidator), IncludeErrorCodes = true)]
public sealed class AsyncSeverityModel {
    public string? Name { get; set; }
}

public sealed class AsyncSeverityValidator : Validator<AsyncSeverityModel> {
    protected override void Configure(ValidatorRuleBuilder<AsyncSeverityModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(AsyncSeverityModel.Name))
               .Must(static (value, _) => ValueTask.FromResult(!string.IsNullOrWhiteSpace(value)), "warn")
               .WithSeverity(ValidationErrorSeverity.Warning)
               .WithCode("W001");
    }
}

[TestClass]
public sealed class AsyncDataAnnotationsBridgeTests {
    public TestContext? TestContext { get; set; }

    [TestMethod]
    public async Task ClassAttribute_UsesAsyncValidatorPathAsync() {
        var model = new AsyncClassModel { Name = string.Empty };
        var results = new List<DataAnnotationsResult>();

        var valid = await DataAnnotationsValidator.TryValidateObjectAsync(
            model,
            new DataAnnotationsValidationContext(model),
            results,
            validateAllProperties: true,
            TestContext!.CancellationToken);

        MSTestAssertions.That(() => valid.Should().BeFalse());
        MSTestAssertions.That(() => results.Should().ContainSingle());
        MSTestAssertions.That(() => results[0].ErrorMessage.Should().Contain("Name is required."));
    }

    [TestMethod]
    public async Task ClassAttribute_AcceptsValidModelAsync() {
        var model = new AsyncClassModel { Name = "Ada" };
        var results = new List<DataAnnotationsResult>();

        var valid = await DataAnnotationsValidator.TryValidateObjectAsync(
            model,
            new DataAnnotationsValidationContext(model),
            results,
            validateAllProperties: true,
            TestContext!.CancellationToken);

        MSTestAssertions.That(() => valid.Should().BeTrue());
        MSTestAssertions.That(() => results.Should().BeEmpty());
    }

    [TestMethod]
    public async Task PropertyAttribute_UsesAsyncValidatorPathAsync() {
        var model = new AsyncPropertyModel { Code = string.Empty };
        var results = new List<DataAnnotationsResult>();

        var valid = await DataAnnotationsValidator.TryValidateObjectAsync(
            model,
            new DataAnnotationsValidationContext(model),
            results,
            validateAllProperties: true,
            TestContext!.CancellationToken);

        MSTestAssertions.That(() => valid.Should().BeFalse());
        MSTestAssertions.That(() => results.Should().ContainSingle());
        MSTestAssertions.That(() => results[0].ErrorMessage.Should().Contain("Code is required."));
    }

    [TestMethod]
    public async Task AsyncFailure_PreservesSeverityAndErrorCodeAsync() {
        var model = new AsyncSeverityModel { Name = string.Empty };
        var results = new List<DataAnnotationsResult>();

        var valid = await DataAnnotationsValidator.TryValidateObjectAsync(
            model,
            new DataAnnotationsValidationContext(model),
            results,
            validateAllProperties: true,
            TestContext!.CancellationToken);

        MSTestAssertions.That(() => valid.Should().BeFalse());
        MSTestAssertions.That(() => results.Should().ContainSingle());
        MSTestAssertions.That(() => results[0].ErrorMessage.Should().Be("[Warning] warn (W001)"));
    }

    [TestMethod]
    public void SyncPath_OnAsyncOnlyValidator_StillThrows() {
        var model = new AsyncClassModel { Name = string.Empty };

        var act = () => DataAnnotationsValidator.ValidateObject(
            model,
            new DataAnnotationsValidationContext(model),
            validateAllProperties: true);

        MSTestAssertions.That(() => act.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>());
    }

    [TestMethod]
    public async Task Cancellation_IsObservedByAsyncRuleAsync() {
        var model = new AsyncClassModel { Name = string.Empty };
        var results = new List<DataAnnotationsResult>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var act = async () => await DataAnnotationsValidator.TryValidateObjectAsync(
            model,
            new DataAnnotationsValidationContext(model),
            results,
            validateAllProperties: true,
            cancellation.Token);

        await MSTestAssertions.ThatAsync(async () => await act.Should().ThrowAsync<OperationCanceledException>());
    }
}
