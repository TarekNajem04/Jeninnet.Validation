//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.JsonSchema;

/// <summary>
/// Provides structural schema patterns for domain rules. Checksums remain server-side.
/// </summary>
internal static class DomainSchemaPatterns {
    public static string Iban(string? country) =>
        country switch {
            "NL" => "^NL[0-9]{2}[A-Z]{4}[0-9]{10}$",
            "DE" => "^DE[0-9]{20}$",
            "FR" => "^FR[0-9]{12}[0-9A-Z]{11}$",
            "GB" => "^GB[0-9]{2}[A-Z]{4}[0-9]{14}$",
            "BE" => "^BE[0-9]{14}$",
            _ => "^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$"
        };

    public static string VatNumber(string? country) =>
        country switch {
            "NL" => "^NL[0-9]{9}B[0-9]{2}$",
            "DE" => "^DE[0-9]{9}$",
            "FR" => "^FR[0-9A-Z]{2}[0-9]{9}$",
            "GB" => "^GB[0-9]{9}([0-9]{3})?$",
            "BE" => "^BE[01][0-9]{9}$",
            _ => "^[A-Z]{2}[A-Z0-9]{2,13}$"
        };

    public static string PostalCode(string? country) =>
        country switch {
            "NL" => "^[1-9][0-9]{3}(?!SA|SD|SS)[A-Z]{2}$",
            "US" => "^[0-9]{5}(-[0-9]{4})?$",
            "DE" or "FR" => "^[1-9][0-9]{4}$",
            "BE" => "^[1-9][0-9]{3}$",
            "EG" => "^[0-9]{5}$",
            "GB" => "^(GIR0AA|[A-PR-UWYZ][A-HK-Y]?[0-9][0-9A-Z]?[0-9][ABD-HJLNP-UW-Z]{2})$",
            _ => "[0-9A-Z-]{3,10}"
        };

    public static string NationalId(string? country) =>
        country switch {
            "NL" => "^[0-9]{9}$",
            "EG" => "^[23][0-9]{13}$",
            "US" => "^(?!000|666|9[0-9]{2})[0-9]{3}-?(?!00)[0-9]{2}-?(?!0000)[0-9]{4}$",
            _ => "[0-9A-Z-]{4,20}"
        };
}
