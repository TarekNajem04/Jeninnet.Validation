//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Blazor.Tests;

internal sealed class HostConformanceBlazorModel {
    public string? Code { get; set; }

    public string? Name { get; set; }
}

internal sealed class HostConformanceBlazorValidator : Validator<HostConformanceBlazorModel> {
    protected override void Configure(ValidatorRuleBuilder<HostConformanceBlazorModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Code, nameof(HostConformanceBlazorModel.Code))
               .Must(
                   static (value, _) => ValueTask.FromResult(!string.IsNullOrWhiteSpace(value)),
                   "Code is required.");

        builder.RuleFor(static x => x.Name, nameof(HostConformanceBlazorModel.Name))
               .Must(
                   static (value, _) => ValueTask.FromResult(!string.IsNullOrWhiteSpace(value)),
                   "Name is required.");
    }
}

[TestClass]
[DoNotParallelize]
public sealed class HostConformanceTests {
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_Blazor_EditContextAsyncSubmit_ReportsAllAsyncErrorsAsync() {
        var model = new HostConformanceBlazorModel { Code = string.Empty, Name = string.Empty };
        var context = new EditContext(model);
        using var coordinator = new EditContextValidationCoordinator<HostConformanceBlazorModel>(
            context,
            new HostConformanceBlazorValidator(),
            model,
            new EditContextValidationOptions { UseAsyncValidation = true });

        var valid = await coordinator.ValidateAsync(TestContext.CancellationToken);

        MSTestAssertions.That(() => valid.Should().BeFalse());
        MSTestAssertions.That(() => context
            .GetValidationMessages(new FieldIdentifier(model, nameof(HostConformanceBlazorModel.Code)))
            .Should().Contain("Code is required."));
        MSTestAssertions.That(() => context
            .GetValidationMessages(new FieldIdentifier(model, nameof(HostConformanceBlazorModel.Name)))
            .Should().Contain("Name is required."));
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public void HostConformance_Blazor_SyncSubmitOnAsyncOnlyValidator_ThrowsInsteadOfPassing() {
        var model = new HostConformanceBlazorModel { Code = string.Empty, Name = string.Empty };
        var context = new EditContext(model);
        using var coordinator = new EditContextValidationCoordinator<HostConformanceBlazorModel>(
            context,
            new HostConformanceBlazorValidator(),
            model);

        var act = coordinator.Validate;

        MSTestAssertions.That(() => act.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>());
    }
}
