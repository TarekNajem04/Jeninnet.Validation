//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

#if NET11_0_OR_GREATER

namespace Jeninnet.Validation.Extensions.Validation.Tests;

internal sealed class MutableOptionsState {
    public string? Name { get; set; }
}

internal sealed class CountingAsyncOptionsValidator : IValidator<AsyncOptionsSettings> {
    public int SyncCalls { get; private set; }

    public int AsyncCalls { get; private set; }

    public ValidationResult Validate(AsyncOptionsSettings instance) {
        SyncCalls++;
        throw new AsyncValidatorInvokedSynchronouslyException(nameof(CountingAsyncOptionsValidator), 1);
    }

    public ValidationResult Validate(AsyncOptionsSettings instance, IEnumerable<string> ruleSets) =>
        Validate(instance);

    public ValueTask<ValidationResult> ValidateAsync(
        AsyncOptionsSettings instance,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(instance, [], cancellationToken);

    public ValueTask<ValidationResult> ValidateAsync(
        AsyncOptionsSettings instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) {
        AsyncCalls++;
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(
            string.IsNullOrWhiteSpace(instance.Name)
                ? Invalid()
                : ValidationResult.Success);
    }

    private static ValidationResult Invalid() {
        var errors = new ValidationErrorCollector {
            new ValidationError(
                nameof(AsyncOptionsSettings.Name),
                "Name is required.",
                "NAME_REQUIRED")
        };

        return ValidationResult.FromErrors(errors);
    }
}

internal sealed class SyncOptionsValidator : IValidator<AsyncOptionsSettings> {
    public int SyncCalls { get; private set; }

    public ValidationResult Validate(AsyncOptionsSettings instance) {
        SyncCalls++;
        return string.IsNullOrWhiteSpace(instance.Name) ? Invalid() : ValidationResult.Success;
    }

    public ValidationResult Validate(AsyncOptionsSettings instance, IEnumerable<string> ruleSets) =>
        Validate(instance);

    public ValueTask<ValidationResult> ValidateAsync(
        AsyncOptionsSettings instance,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Validate(instance));

    public ValueTask<ValidationResult> ValidateAsync(
        AsyncOptionsSettings instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Validate(instance));

    private static ValidationResult Invalid() {
        var errors = new ValidationErrorCollector {
            new ValidationError(
                nameof(AsyncOptionsSettings.Name),
                "Name is required.",
                "NAME_REQUIRED")
        };

        return ValidationResult.FromErrors(errors);
    }
}

[TestClass]
[DoNotParallelize]
public sealed class OptionsReloadBoundaryTests {
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("OptionsReloadBoundary")]
    public void MonitorReload_AsyncOnlyValidator_IsNotRunAndDoesNotThrow() {
        var state = new MutableOptionsState { Name = "worker" };
        var validator = new CountingAsyncOptionsValidator();
        using var provider = CreateProvider(state, validator);
        var monitor = provider.GetRequiredService<IOptionsMonitor<AsyncOptionsSettings>>();

        MSTestAssertions.That(() => monitor.CurrentValue.Name.Should().Be("worker"));

        state.Name = string.Empty;
        provider.GetRequiredService<IOptionsMonitorCache<AsyncOptionsSettings>>().Clear();
        Exception? caught = null;

        try {
            _ = monitor.CurrentValue;
        }
        catch(Exception exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().BeNull());
        MSTestAssertions.That(() => monitor.CurrentValue.Name.Should().BeEmpty());
        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(0));
        MSTestAssertions.That(() => validator.SyncCalls.Should().BeGreaterThan(0));
    }

    [TestMethod]
    [TestCategory("OptionsReloadBoundary")]
    public void MonitorReload_SyncValidator_ValidatesAndThrows() {
        var state = new MutableOptionsState { Name = "worker" };
        var validator = new SyncOptionsValidator();
        using var provider = CreateProvider(state, validator);
        var monitor = provider.GetRequiredService<IOptionsMonitor<AsyncOptionsSettings>>();

        MSTestAssertions.That(() => monitor.CurrentValue.Name.Should().Be("worker"));

        state.Name = string.Empty;
        provider.GetRequiredService<IOptionsMonitorCache<AsyncOptionsSettings>>().Clear();
        Exception? caught = null;

        try {
            _ = monitor.CurrentValue;
        }
        catch(Exception exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().BeOfType<OptionsValidationException>());
        MSTestAssertions.That(() => ((OptionsValidationException)caught!)
            .Failures.Any(static failure =>
                failure.Contains(nameof(AsyncOptionsSettings.Name), StringComparison.Ordinal) &&
                failure.Contains("NAME_REQUIRED", StringComparison.Ordinal))
            .Should().BeTrue());
    }

    [TestMethod]
    [TestCategory("OptionsReloadBoundary")]
    public void Snapshot_AsyncOnlyValidator_IsNotRunAndDoesNotThrow() {
        var state = new MutableOptionsState { Name = "worker" };
        var validator = new CountingAsyncOptionsValidator();
        using var provider = CreateProvider(state, validator);

        using(var scope = provider.CreateScope()) {
            var snapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<AsyncOptionsSettings>>();
            MSTestAssertions.That(() => snapshot.Value.Name.Should().Be("worker"));
        }

        state.Name = string.Empty;
        provider.GetRequiredService<IOptionsMonitorCache<AsyncOptionsSettings>>().Clear();
        Exception? caught = null;

        using(var scope = provider.CreateScope()) {
            var snapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<AsyncOptionsSettings>>();

            try {
                _ = snapshot.Value;
            }
            catch(Exception exception) {
                caught = exception;
            }

            MSTestAssertions.That(() => caught.Should().BeNull());
            MSTestAssertions.That(() => snapshot.Value.Name.Should().BeEmpty());
        }

        MSTestAssertions.That(() => validator.AsyncCalls.Should().Be(0));
    }

    private static ServiceProvider CreateProvider(
        MutableOptionsState state,
        IValidator<AsyncOptionsSettings> validator) {
        var services = new ServiceCollection();
        services.AddSingleton(validator);
        services.AddOptions<AsyncOptionsSettings>()
                .Configure(options => options.Name = state.Name)
                .AddValidatedOptions();

        return services.BuildServiceProvider();
    }
}

#endif
