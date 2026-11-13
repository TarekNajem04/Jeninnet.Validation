//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Benchmarks;

/// <summary>
/// Memory smoke coverage for the allocation-free Base64 rule. Run with BenchmarkDotNet; the rule
/// itself never allocates on the happy path.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class Base64ValidationBenchmarks {
    private sealed class Model {
        public string? Payload { get; set; }
    }

    private sealed class Base64Validator : Validator<Model> {
        protected override void Configure(ValidatorRuleBuilder<Model> builder) {
            ArgumentNullException.ThrowIfNull(builder);

            builder.RuleFor(static x => x.Payload, nameof(Model.Payload))
                   .Base64String();
        }
    }

    private readonly Model _validModel = new() { Payload = "SGVsbG8gV29ybGQ=" };
    private readonly Base64Validator _validator = new();

    [Benchmark]
    public ValidationResult ValidBase64() => _validator.Validate(_validModel);
}
