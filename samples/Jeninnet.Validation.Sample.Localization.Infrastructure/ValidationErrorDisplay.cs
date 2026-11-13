//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Localization.Infrastructure;

public static class ValidationErrorDisplay {
    public static Dictionary<string, object?> BuildParameters(ValidationError error) {
        var parameters = error.MessageParameters?.ToDictionary(static kv => kv.Key, static kv => (object?)kv.Value) ?? [];
        parameters[Constants.PROPERTY_NAME] = error.MemberName;

        if(parameters.TryGetValue("0", out var p0)) {
            parameters.TryAdd(Constants.COMPARISON_VALUE, p0);
            parameters.TryAdd(Constants.FROM, p0);
            parameters.TryAdd(Constants.MIN_LENGTH, p0);
        }

        if(parameters.TryGetValue("1", out var p1)) {
            parameters.TryAdd(Constants.TO, p1);
            parameters.TryAdd(Constants.MAX_LENGTH, p1);
        }

        return parameters;
    }

    public static async Task ShowValidationErrorsAsync<T>(
        IValidator<T> validator,
        IMessageLocalizer localizer,
        T model,
        string label,
        CultureInfo culture
    ) where T : notnull {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(culture);

        var result = await validator.ValidateAsync(model);
        Console.WriteLine($"{label} [{culture.Name}]: {(result.IsValid ? "PASS" : "FAIL")}");

        foreach(var error in result.Errors) {
            var parameters = BuildParameters(error);
            var msg = error.MessageKey is not null
                ? await localizer.LocalizeAsync(error.MessageKey, parameters, culture)
                : error.Message;
            Console.WriteLine($"  [{error.MemberName} → key '{error.MessageKey}'] {msg}");
        }

        Console.WriteLine();
    }
}
