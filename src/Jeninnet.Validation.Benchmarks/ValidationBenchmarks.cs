//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Benchmarks;

// ---------------------------------------------------------------------------
// Models: 5, 10, and 30 validated properties
// ---------------------------------------------------------------------------

[Validate("Jeninnet.Validation.Benchmarks")]
public sealed class SmallModel {
    [NotEmpty] public string Name { get; set; } = string.Empty;
    [InclusiveBetween(18, 99)] public int Age { get; set; }
    [Email] public string Email { get; set; } = string.Empty;
    [Length(3, 10)] public string Code { get; set; } = string.Empty;
    [GreaterThan(0)] public int Score { get; set; }
}

[Validate("Jeninnet.Validation.Benchmarks")]
public sealed class MediumModel {
    [NotEmpty] public string P01 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P02 { get; set; }
    [NotEmpty] public string P03 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P04 { get; set; }
    [NotEmpty] public string P05 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P06 { get; set; }
    [NotEmpty] public string P07 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P08 { get; set; }
    [NotEmpty] public string P09 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P10 { get; set; }
}

[Validate("Jeninnet.Validation.Benchmarks")]
public sealed class LargeModel {
    [NotEmpty] public string P01 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P02 { get; set; }
    [NotEmpty] public string P03 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P04 { get; set; }
    [NotEmpty] public string P05 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P06 { get; set; }
    [NotEmpty] public string P07 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P08 { get; set; }
    [NotEmpty] public string P09 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P10 { get; set; }
    [NotEmpty] public string P11 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P12 { get; set; }
    [NotEmpty] public string P13 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P14 { get; set; }
    [NotEmpty] public string P15 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P16 { get; set; }
    [NotEmpty] public string P17 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P18 { get; set; }
    [NotEmpty] public string P19 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P20 { get; set; }
    [NotEmpty] public string P21 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P22 { get; set; }
    [NotEmpty] public string P23 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P24 { get; set; }
    [NotEmpty] public string P25 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P26 { get; set; }
    [NotEmpty] public string P27 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P28 { get; set; }
    [NotEmpty] public string P29 { get; set; } = string.Empty;
    [InclusiveBetween(0, 100)] public int P30 { get; set; }
}

// ---------------------------------------------------------------------------
// Fluent equivalents
// ---------------------------------------------------------------------------

public sealed class FluentSmallValidator : Validator<SmallModel> {
    protected override void Configure(ValidatorRuleBuilder<SmallModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(SmallModel.Name)).NotEmpty();
        builder.RuleFor(static x => x.Age, nameof(SmallModel.Age)).InclusiveBetween(18, 99);
        builder.RuleFor(static x => x.Email, nameof(SmallModel.Email)).EmailAddress();
        builder.RuleFor(static x => x.Code, nameof(SmallModel.Code)).Length(3, 10);
        builder.RuleFor(static x => x.Score, nameof(SmallModel.Score)).GreaterThan(0);
    }
}

public sealed class FluentMediumValidator : Validator<MediumModel> {
    protected override void Configure(ValidatorRuleBuilder<MediumModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.P01, nameof(MediumModel.P01)).NotEmpty();
        builder.RuleFor(static x => x.P02, nameof(MediumModel.P02)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P03, nameof(MediumModel.P03)).NotEmpty();
        builder.RuleFor(static x => x.P04, nameof(MediumModel.P04)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P05, nameof(MediumModel.P05)).NotEmpty();
        builder.RuleFor(static x => x.P06, nameof(MediumModel.P06)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P07, nameof(MediumModel.P07)).NotEmpty();
        builder.RuleFor(static x => x.P08, nameof(MediumModel.P08)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P09, nameof(MediumModel.P09)).NotEmpty();
        builder.RuleFor(static x => x.P10, nameof(MediumModel.P10)).InclusiveBetween(0, 100);
    }
}

public sealed class FluentLargeValidator : Validator<LargeModel> {
    protected override void Configure(ValidatorRuleBuilder<LargeModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.P01, nameof(LargeModel.P01)).NotEmpty();
        builder.RuleFor(static x => x.P02, nameof(LargeModel.P02)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P03, nameof(LargeModel.P03)).NotEmpty();
        builder.RuleFor(static x => x.P04, nameof(LargeModel.P04)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P05, nameof(LargeModel.P05)).NotEmpty();
        builder.RuleFor(static x => x.P06, nameof(LargeModel.P06)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P07, nameof(LargeModel.P07)).NotEmpty();
        builder.RuleFor(static x => x.P08, nameof(LargeModel.P08)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P09, nameof(LargeModel.P09)).NotEmpty();
        builder.RuleFor(static x => x.P10, nameof(LargeModel.P10)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P11, nameof(LargeModel.P11)).NotEmpty();
        builder.RuleFor(static x => x.P12, nameof(LargeModel.P12)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P13, nameof(LargeModel.P13)).NotEmpty();
        builder.RuleFor(static x => x.P14, nameof(LargeModel.P14)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P15, nameof(LargeModel.P15)).NotEmpty();
        builder.RuleFor(static x => x.P16, nameof(LargeModel.P16)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P17, nameof(LargeModel.P17)).NotEmpty();
        builder.RuleFor(static x => x.P18, nameof(LargeModel.P18)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P19, nameof(LargeModel.P19)).NotEmpty();
        builder.RuleFor(static x => x.P20, nameof(LargeModel.P20)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P21, nameof(LargeModel.P21)).NotEmpty();
        builder.RuleFor(static x => x.P22, nameof(LargeModel.P22)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P23, nameof(LargeModel.P23)).NotEmpty();
        builder.RuleFor(static x => x.P24, nameof(LargeModel.P24)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P25, nameof(LargeModel.P25)).NotEmpty();
        builder.RuleFor(static x => x.P26, nameof(LargeModel.P26)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P27, nameof(LargeModel.P27)).NotEmpty();
        builder.RuleFor(static x => x.P28, nameof(LargeModel.P28)).InclusiveBetween(0, 100);
        builder.RuleFor(static x => x.P29, nameof(LargeModel.P29)).NotEmpty();
        builder.RuleFor(static x => x.P30, nameof(LargeModel.P30)).InclusiveBetween(0, 100);
    }
}

// ---------------------------------------------------------------------------
// Benchmarks
// ---------------------------------------------------------------------------

[MemoryDiagnoser]
[ShortRunJob]
public class ValidationBenchmarks {
    private readonly SmallModel _smallValid = new() {
        Name = "Valid",
        Age = 25,
        Email = "buyer@example.com",
        Code = "ABC",
        Score = 1
    };

    private readonly SmallModel _smallInvalid = new() {
        Name = "",
        Age = 100,
        Email = "not-an-email",
        Code = "X",
        Score = 0
    };

    private readonly MediumModel _mediumValid = new() {
        P01 = "a",
        P02 = 1,
        P03 = "b",
        P04 = 2,
        P05 = "c",
        P06 = 3,
        P07 = "d",
        P08 = 4,
        P09 = "e",
        P10 = 5
    };

    private readonly MediumModel _mediumInvalid = new() {
        P01 = "",
        P02 = 101,
        P03 = "",
        P04 = 101,
        P05 = "",
        P06 = 101,
        P07 = "",
        P08 = 101,
        P09 = "",
        P10 = 101
    };

    private readonly LargeModel _largeValid = CreateLargeModel(valid: true);
    private readonly LargeModel _largeInvalid = CreateLargeModel(valid: false);

    private readonly FluentSmallValidator _fluentSmall = new();
    private readonly FluentMediumValidator _fluentMedium = new();
    private readonly FluentLargeValidator _fluentLarge = new();
    private ServiceProvider _provider = null!;
    private IValidator<SmallModel> _generatedSmall = null!;
    private IValidator<MediumModel> _generatedMedium = null!;
    private IValidator<LargeModel> _generatedLarge = null!;

    [GlobalSetup]
    public void Setup() {
        var services = new ServiceCollection();
        services.AddValidation();
        _provider = services.BuildServiceProvider();
        _generatedSmall = _provider.GetRequiredService<IValidator<SmallModel>>();
        _generatedMedium = _provider.GetRequiredService<IValidator<MediumModel>>();
        _generatedLarge = _provider.GetRequiredService<IValidator<LargeModel>>();
    }

    [GlobalCleanup]
    public void Cleanup() => _provider.Dispose();

    private static LargeModel CreateLargeModel(bool valid) {
        var model = new LargeModel();
        var type = typeof(LargeModel);

        for(var i = 1; i <= 30; i++) {
            var property = type.GetProperty($"P{i:00}")!;

            if(i % 2 == 1) {
                property.SetValue(model, valid ? "value" : string.Empty);
            } else {
                property.SetValue(model, valid ? 1 : 101);
            }
        }

        return model;
    }

    [Benchmark(Baseline = true)]
    public ValidationResult Fluent_Small_Valid() => _fluentSmall.Validate(_smallValid);

    [Benchmark]
    public ValidationResult Generated_Small_Valid() => _generatedSmall.Validate(_smallValid);

    [Benchmark]
    public ValidationResult Fluent_Small_Invalid() => _fluentSmall.Validate(_smallInvalid);

    [Benchmark]
    public ValidationResult Generated_Small_Invalid() => _generatedSmall.Validate(_smallInvalid);

    [Benchmark]
    public ValidationResult Fluent_Medium_Valid() => _fluentMedium.Validate(_mediumValid);

    [Benchmark]
    public ValidationResult Generated_Medium_Valid() => _generatedMedium.Validate(_mediumValid);

    [Benchmark]
    public ValidationResult Fluent_Medium_Invalid() => _fluentMedium.Validate(_mediumInvalid);

    [Benchmark]
    public ValidationResult Generated_Medium_Invalid() => _generatedMedium.Validate(_mediumInvalid);

    [Benchmark]
    public ValidationResult Fluent_Large_Valid() => _fluentLarge.Validate(_largeValid);

    [Benchmark]
    public ValidationResult Generated_Large_Valid() => _generatedLarge.Validate(_largeValid);

    [Benchmark]
    public ValidationResult Fluent_Large_Invalid() => _fluentLarge.Validate(_largeInvalid);

    [Benchmark]
    public ValidationResult Generated_Large_Invalid() => _generatedLarge.Validate(_largeInvalid);

    [Benchmark]
    public ValidationResult Fluent_Small_Invalid_FailFast() =>
        _fluentSmall.Validate(_smallInvalid, new ValidationCallOptions { FailFast = true });
}
