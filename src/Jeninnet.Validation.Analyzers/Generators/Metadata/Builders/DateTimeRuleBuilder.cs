//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Metadata.Builders;

internal static class DateTimeRuleBuilder {
    public static RuleModel? CreateDateTimeRule(
        string propertyName,
        RuleKind kind,
        AttributeData attribute,
        string propertyTypeName,
        List<GeneratorDiagnostic> diagnostics) {
        if(attribute is null) {
            throw new ArgumentNullException(nameof(attribute));
        }

        if(propertyTypeName is null) {
            throw new ArgumentNullException(nameof(propertyTypeName));
        }

        if(diagnostics is null) {
            throw new ArgumentNullException(nameof(diagnostics));
        }

        var param = MetadataHelper.GetParam(attribute, 0);

        if(!TryFormatValue(param.Value, propertyTypeName, out var formattedValue)) {
            diagnostics.Add(new GeneratorDiagnostic(
                DiagnosticDescriptors.InvalidDateTimeValue,
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                [param.Value ?? "null", propertyTypeName, propertyName]));

            return null;
        }

        return new RuleModel(
            kind,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(formattedValue, NormalizeTypeName(propertyTypeName)),
            Message: MetadataHelper.GetMessageArgument(attribute)
        );
    }

    internal static bool TryFormatValue(string? value, string propertyTypeName, out string formattedValue) {
        formattedValue = string.Empty;

        if(string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        var text = value!;

        return GetSimpleTypeName(propertyTypeName) switch {
            "DateTime" => TryFormatDateTime(text, out formattedValue),
            "DateTimeOffset" => TryFormatDateTimeOffset(text, out formattedValue),
            "DateOnly" => TryFormatDateOnly(text, out formattedValue),
            "TimeOnly" => TryFormatTimeOnly(text, out formattedValue),
            _ => false,
        };
    }

    private static bool TryFormatDateTime(string value, out string formattedValue) {
        if(!DateTime.TryParse(
               value,
               CultureInfo.InvariantCulture,
               DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
               out var dateTime)) {
            formattedValue = string.Empty;
            return false;
        }

        // DateTime.Parse(RoundtripKind) converts offset-bearing values to the local time zone.
        // Normalize them to UTC so the generated literal does not depend on the build machine.
        if(dateTime.Kind is DateTimeKind.Local) {
            dateTime = dateTime.ToUniversalTime();
        }

        formattedValue = dateTime.ToString("o", CultureInfo.InvariantCulture);
        return true;
    }

    private static bool TryFormatDateTimeOffset(string value, out string formattedValue) {
        if(!DateTimeOffset.TryParse(
               value,
               CultureInfo.InvariantCulture,
               DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
               out var dateTimeOffset)) {
            formattedValue = string.Empty;
            return false;
        }

        formattedValue = dateTimeOffset.ToString("o", CultureInfo.InvariantCulture);
        return true;
    }

    private static bool TryFormatDateOnly(string value, out string formattedValue) {
        // The analyzer targets netstandard2.0, where DateOnly does not exist. Parse with DateTime
        // under the invariant culture and emit the round-trip date-only literal.
        if(!DateTime.TryParse(
               value,
               CultureInfo.InvariantCulture,
               DateTimeStyles.AllowWhiteSpaces,
               out var dateTime)) {
            formattedValue = string.Empty;
            return false;
        }

        formattedValue = dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

    private static bool TryFormatTimeOnly(string value, out string formattedValue) {
        // The analyzer targets netstandard2.0, where TimeOnly does not exist. Parse the clock
        // portion with TimeSpan under the invariant culture and reject day-carrying values.
        if(!TimeSpan.TryParse(
               value,
               CultureInfo.InvariantCulture,
               out var timeSpan) ||
           timeSpan < TimeSpan.Zero ||
           timeSpan >= TimeSpan.FromDays(1)) {
            formattedValue = string.Empty;
            return false;
        }

        formattedValue = timeSpan.ToString(@"hh\:mm\:ss\.fffffff", CultureInfo.InvariantCulture);
        return true;
    }

    private static string NormalizeTypeName(string typeName) =>
        typeName.Replace("global::", string.Empty).TrimEnd('?');

    private static string GetSimpleTypeName(string typeName) {
        var normalized = NormalizeTypeName(typeName);
        var lastDot = normalized.LastIndexOf('.');

        return lastDot >= 0 ? normalized.Substring(lastDot + 1) : normalized;
    }
}
