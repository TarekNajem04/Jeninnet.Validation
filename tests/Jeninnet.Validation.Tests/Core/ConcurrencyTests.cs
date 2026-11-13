//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Core;

internal sealed class ConcurrentModel {
    public string? Name { get; set; }
    public int Value { get; set; }
}

file sealed class ConcurrentValidator : Validator<ConcurrentModel> {
    protected override void Configure(ValidatorRuleBuilder<ConcurrentModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ConcurrentModel.Name))
               .NotEmpty()
               .MinimumLength(5);

        builder.RuleFor(static x => x.Value, nameof(ConcurrentModel.Value))
               .GreaterThan(0);
    }
}

[TestClass]
public sealed class ConcurrencyTests {
    private const int ITERATIONS = 1_000;

    [TestMethod]
    public void SharedValidator_SyncParallelCallsProduceIndependentResults() {
        using var provider = CreateProvider();
        var validator = new ConcurrentValidator();
        var options = new ValidationCallOptions { ServiceProvider = provider };

        Parallel.For(0, ITERATIONS, i => {
            var expectation = i % 3;

            var result = expectation switch {
                0 => validator.Validate(new ConcurrentModel { Name = "abcdef", Value = 1 }, options),
                1 => validator.Validate(new ConcurrentModel { Name = string.Empty, Value = 1 }, options),
                _ => validator.Validate(new ConcurrentModel { Name = "abcdef", Value = 0 }, options)
            };

            var expectedErrors = expectation switch {
                0 => 0,
                1 => 2,
                _ => 1
            };

            MSTestAssertions.That(() => result.Errors.Should().HaveCount(expectedErrors));
        });
    }

    [TestMethod]
    public async Task SharedValidator_AsyncParallelCallsProduceIndependentResultsAsync() {
        await using var provider = CreateProvider();
        var validator = new ConcurrentValidator();
        var options = new ValidationCallOptions { ServiceProvider = provider };

        var tasks = Enumerable.Range(0, ITERATIONS).Select(async i => {
            var expectation = i % 3;

            var result = expectation switch {
                0 => await validator.ValidateAsync(
                    new ConcurrentModel { Name = "abcdef", Value = 1 },
                    options,
                    TestCancellation),
                1 => await validator.ValidateAsync(
                    new ConcurrentModel { Name = string.Empty, Value = 1 },
                    options,
                    TestCancellation),
                _ => await validator.ValidateAsync(
                    new ConcurrentModel { Name = "abcdef", Value = 0 },
                    options,
                    TestCancellation)
            };

            var expectedErrors = expectation switch {
                0 => 0,
                1 => 2,
                _ => 1
            };

            AssertErrorCount(result, expectedErrors);
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    [TestMethod]
    public void SharedValidator_MixedSyncAndAsyncCallsDoNotInterfere() {
        using var provider = CreateProvider();
        var validator = new ConcurrentValidator();
        var options = new ValidationCallOptions { ServiceProvider = provider };

        var syncWork = Task.Run(() => Parallel.For(0, ITERATIONS / 2, i => {
            var result = validator.Validate(
                new ConcurrentModel {
                    Name = i % 2 == 0 ? "abcdef" : string.Empty,
                    Value = 1
                },
                options);

            MSTestAssertions.That(() => result.Errors.Should().HaveCount(i % 2 == 0 ? 0 : 2));
        }), TestCancellation);

        var asyncWork = Task.Run(async () => {
            var tasks = Enumerable.Range(0, ITERATIONS / 2).Select(async i => {
                var result = await validator.ValidateAsync(
                    new ConcurrentModel {
                        Name = "abcdef",
                        Value = i % 2 == 0 ? 1 : 0
                    },
                    options,
                    TestCancellation);

                AssertErrorCount(result, i % 2 == 0 ? 0 : 1);
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }, TestCancellation);

        Task.WaitAll(syncWork, asyncWork);
    }

    [TestMethod]
    public void ConcurrentContainers_KeepTheirOwnDisplayNames() {
        using var first = CreateProvider("First name");
        using var second = CreateProvider("Second name");
        var validator = new ConcurrentValidator();

        Parallel.For(0, ITERATIONS, i => {
            var useFirst = i % 2 == 0;
            var provider = useFirst ? first : second;
            var expected = useFirst ? "First name" : "Second name";
            var result = validator.Validate(
                new ConcurrentModel { Name = string.Empty, Value = 1 },
                new ValidationCallOptions { ServiceProvider = provider });

            MSTestAssertions.That(() => result.Errors.Should().HaveCount(2));

            foreach(var error in result.Errors) {
                MSTestAssertions.That(() => error.MessageParameters!["PropertyName"].Should().Be(expected));
            }
        });
    }

    private static void AssertErrorCount(ValidationResult result, int expected) =>
        MSTestAssertions.That(() => result.Errors.Should().HaveCount(expected));

    private CancellationToken TestCancellation =>
        TestContext?.CancellationToken ?? CancellationToken.None;

    public TestContext? TestContext { get; set; }

    private static ServiceProvider CreateProvider(string? displayName = null) {
        var services = new ServiceCollection();

        services.AddValidationCore(options => options with {
            DefaultClassLevelCascadeMode = CascadeMode.Continue,
            DefaultRuleLevelCascadeMode = CascadeMode.Continue,
            DisplayNameResolver = displayName is null ? null : (_, _) => displayName
        });

        return services.BuildServiceProvider();
    }
}

[TestClass]
[DoNotParallelize] // Mutates the documented process-wide fallback and restores it.
public sealed class StaticConfigurationConcurrencyTests {
    [TestMethod]
    public void ConfigureWhileValidating_DoesNotThrowOrTearResults() {
        var previous = ValidationConfiguration.Options;
        var validator = new ConcurrentValidator();

        try {
            ValidationConfiguration.Configure(options => options with {
                DisplayNameResolver = (_, _) => "Name A"
            });

            var reader = Task.Run(() => Parallel.For(0, 1_000, _ => {
                var result = validator.Validate(
                    new ConcurrentModel { Name = string.Empty, Value = 1 });

                MSTestAssertions.That(() => result.Errors.Should().HaveCount(2));

                foreach(var error in result.Errors) {
                    MSTestAssertions.That(() =>
                        error.MessageParameters!["PropertyName"].Should().BeOneOf(["Name A", "Name B"]));
                }
            }), TestCancellation);

            var writer = Task.Run(() => {
                for(var i = 0; i < 1_000; i++) {
                    var name = i % 2 == 0 ? "Name A" : "Name B";
                    ValidationConfiguration.Configure(options => options with {
                        DisplayNameResolver = (_, _) => name
                    });
                }
            }, TestCancellation);

            Task.WaitAll(reader, writer);
        }
        finally {
            ValidationConfiguration.Configure(_ => previous);
        }
    }

    private CancellationToken TestCancellation =>
        TestContext?.CancellationToken ?? CancellationToken.None;

    public TestContext? TestContext { get; set; }
}
