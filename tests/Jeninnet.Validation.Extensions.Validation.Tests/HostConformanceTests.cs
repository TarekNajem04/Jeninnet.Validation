//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Extensions.Validation.Tests;

internal sealed class HostConformanceBridgeModel {
    public string? Name { get; set; }

    public string? Email { get; set; }
}

internal sealed class HostConformanceBridgeValidator : IValidator<HostConformanceBridgeModel> {
    public int SyncCalls { get; private set; }

    public int AsyncCalls { get; private set; }

    public ValidationResult Validate(HostConformanceBridgeModel instance) {
        SyncCalls++;
        throw new AsyncValidatorInvokedSynchronouslyException(nameof(HostConformanceBridgeValidator), 2);
    }

    public ValidationResult Validate(HostConformanceBridgeModel instance, IEnumerable<string> ruleSets) =>
        Validate(instance);

    public ValueTask<ValidationResult> ValidateAsync(
        HostConformanceBridgeModel instance,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(instance, [], cancellationToken);

    public ValueTask<ValidationResult> ValidateAsync(
        HostConformanceBridgeModel instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) {
        AsyncCalls++;
        var errors = new ValidationErrorCollector {
            { nameof(HostConformanceBridgeModel.Name), "Name is required." },
            { nameof(HostConformanceBridgeModel.Email), "Email is invalid." }
        };

        return ValueTask.FromResult(ValidationResult.FromErrors(errors));
    }
}

[TestClass]
[DoNotParallelize]
public sealed class HostConformanceTests {
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_ExtensionsValidation_AsyncOnlyInvalidRequest_DoesNotCallHandlerAsync() {
        var validator = new HostConformanceBridgeValidator();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddValidation();
        builder.Services.AddSingleton<IValidator<HostConformanceBridgeModel>>(validator);
        builder.Services.AddJeninnetValidation();

        await using var app = builder.Build();
        var handlerCalled = false;
        app.MapPost("/host-conformance", (HostConformanceBridgeModel model) => {
            handlerCalled = true;
            return Results.Ok(model);
        });

        await app.StartAsync(TestContext.CancellationToken);
        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync(
            "/host-conformance",
            new HostConformanceBridgeModel { Name = string.Empty, Email = "bad" },
            TestContext.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.CancellationToken);

        MSTestAssertions.That(() => response.StatusCode.Should().Be(HttpStatusCode.BadRequest));
        MSTestAssertions.That(() => problem!["errors"]!.AsObject()
            .ContainsKey(nameof(HostConformanceBridgeModel.Name)).Should().BeTrue());
        MSTestAssertions.That(() => problem!["errors"]!.AsObject()
            .ContainsKey(nameof(HostConformanceBridgeModel.Email)).Should().BeTrue());
        MSTestAssertions.That(() => validator.SyncCalls.Should().Be(0));
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(1));
        MSTestAssertions.That(() => handlerCalled.Should().BeFalse());
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_ExtensionsValidation_MissingValidator_PassesThroughAsync() {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddValidation();
        builder.Services.AddJeninnetValidation();

        await using var app = builder.Build();
        var handlerCalled = false;
        app.MapPost("/host-conformance-missing", (HostConformanceBridgeModel model) => {
            handlerCalled = true;
            return Results.Ok(model);
        });

        await app.StartAsync(TestContext.CancellationToken);
        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync(
            "/host-conformance-missing",
            new HostConformanceBridgeModel { Name = string.Empty },
            TestContext.CancellationToken);

        MSTestAssertions.That(() => response.StatusCode.Should().Be(HttpStatusCode.OK));
        MSTestAssertions.That(() => handlerCalled.Should().BeTrue());
    }
}
