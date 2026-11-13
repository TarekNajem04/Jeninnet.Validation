//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Benchmarks;

/// <summary>
/// A model shared by the Jeninnet, FluentValidation, and DataAnnotations benchmarks.
/// The three validators express the same two rules.
/// </summary>
public sealed class PeerModel {
    [DataAnnotations.Required]
    public string? Name { get; set; }

    [DataAnnotations.Range(18, 120)]
    public int Age { get; set; }
}

internal sealed class PeerFluentValidationValidator : FluentValidationAbstractValidator {
    public PeerFluentValidationValidator() {
        FluentValidation.DefaultValidatorExtensions.MaximumLength(
            FluentValidation.DefaultValidatorExtensions.NotEmpty(RuleFor(static model => model.Name)), 10);
        FluentValidation.DefaultValidatorExtensions.InclusiveBetween(RuleFor(static model => model.Age), 18, 120);
    }
}

internal sealed class PeerJeninnetValidator : Jeninnet.Validation.Fluent.Validator<PeerModel> {
    protected override void Configure(ValidatorRuleBuilder<PeerModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static model => model.Name, nameof(PeerModel.Name))
               .NotEmpty()
               .MaximumLength(10);

        builder.RuleFor(static model => model.Age, nameof(PeerModel.Age))
               .InclusiveBetween(18, 120);
    }
}

/// <summary>
/// Apples-to-apples comparison for the common member-validation path: one string and one integer
/// rule, valid and invalid inputs, with allocations measured.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class PeerComparisonBenchmarks {
    private readonly PeerModel _valid = new() { Name = "Alice", Age = 30 };
    private readonly PeerModel _invalid = new() { Name = "", Age = 10 };
    private readonly PeerFluentValidationValidator _fluentValidation = new();
    private readonly PeerJeninnetValidator _jeninnet = new();

    [Benchmark(Baseline = true)]
    public FvValidationResult FluentValidation_Valid() => _fluentValidation.Validate(_valid);

    [Benchmark]
    public FvValidationResult FluentValidation_Invalid() => _fluentValidation.Validate(_invalid);

    [Benchmark]
    public JeninnetValidationResult Jeninnet_Valid() => _jeninnet.Validate(_valid);

    [Benchmark]
    public JeninnetValidationResult Jeninnet_Invalid() => _jeninnet.Validate(_invalid);

    [Benchmark]
    public bool DataAnnotations_Valid() => TryValidate(_valid);

    [Benchmark]
    public bool DataAnnotations_Invalid() => TryValidate(_invalid);

    private static bool TryValidate(PeerModel model) {
        var results = new List<DataAnnotations.ValidationResult>();

        return DataAnnotations.Validator.TryValidateObject(
            model,
            new DataAnnotations.ValidationContext(model),
            results,
            validateAllProperties: true);
    }
}
