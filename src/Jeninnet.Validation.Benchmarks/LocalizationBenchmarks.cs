//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Benchmarks;

/// <summary>
/// Measures the built-in message-pack lookup cost. The provider is the single
/// registered provider in the exactly-one-provider model.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class LocalizationBenchmarks {
    private static readonly CultureInfo _english = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo _arabic = CultureInfo.GetCultureInfo("ar");

    private readonly DefaultValidationMessageProvider _provider = new();

    [Benchmark]
    public ValueTask<string?> EnglishHit() =>
        _provider.GetMessageTemplateAsync(ValidationMessageKeys.NOT_EMPTY, _english);

    [Benchmark]
    public ValueTask<string?> ArabicHit() =>
        _provider.GetMessageTemplateAsync(ValidationMessageKeys.NOT_EMPTY, _arabic);

    [Benchmark]
    public ValueTask<string?> MissingKey() =>
        _provider.GetMessageTemplateAsync("Validation_Missing", _english);

    [Benchmark]
    public ValueTask<string?> MissingCulture() =>
        _provider.GetMessageTemplateAsync(ValidationMessageKeys.NOT_EMPTY, CultureInfo.GetCultureInfo("fr"));
}
