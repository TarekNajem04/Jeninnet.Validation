//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Localization.Infrastructure;

public static class SampleCultures {
    public static CultureInfo EnUs { get; } = new(Constants.EN_US_CULTURE);
    public static CultureInfo EnGb { get; } = new(Constants.EN_GB_CULTURE);
    public static CultureInfo ArSy { get; } = new(Constants.AR_SY_CULTURE);
    public static CultureInfo NlNl { get; } = new(Constants.NL_NL_CULTURE);
    public static CultureInfo NlBe { get; } = new(Constants.NL_BE_CULTURE);
    public static CultureInfo DeDe { get; } = new(Constants.DE_DE_CULTURE);
    public static CultureInfo DeCh { get; } = new(Constants.DE_CH_CULTURE);
    public static CultureInfo FrFr { get; } = new(Constants.FR_FR_CULTURE);
    public static CultureInfo FrCa { get; } = new(Constants.FR_CA_CULTURE);

    public static CultureInfo[] EnglishAndArabic { get; } = [EnUs, ArSy];

    public static CultureInfo[] EnglishArabicDutch { get; } = [EnUs, ArSy, NlNl];

    public static CultureInfo[] AllWithFallbackExamples { get; } = [
        EnUs, EnGb, NlNl, NlBe, FrFr, FrCa, DeDe, DeCh, ArSy
    ];
}
