//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Localization.Base;

public static class Helper {
    public static Task ShowWrapperAsync(IMessageLocalizer localizer, string label, string key, CultureInfo culture) => ShowWrapperAsync(localizer, label, [key], culture);
    public static async Task ShowWrapperAsync(IMessageLocalizer localizer, string label, string[] keys, CultureInfo culture) {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentException.ThrowIfNullOrEmpty(label);
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(culture);

        if(keys.Length == 0) {
            throw new ArgumentException("At least one key must be specified.", nameof(keys));
        }

        foreach(var key in keys) {
            var msg = await localizer.LocalizeAsync(key, new Dictionary<string, object?> {
                [Constants.PROPERTY_NAME] = label,
                [Constants.COMPARISON_VALUE] = 0,
                [Constants.FROM] = 8,
                [Constants.TO] = 64,
            }, culture);
            Console.WriteLine($"  [{culture.Name}] {msg}");
        }
    }

    public static Task ShowMessageAsync(IMessageLocalizer localizer, string label, string key, CultureInfo culture, int from = 8, int to = 64)
        => ShowMessageAsync(localizer, label, [key], culture, from, to);
    public static async Task ShowMessageAsync(IMessageLocalizer localizer, string label, string[] keys, CultureInfo culture, int from = 8, int to = 64) {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentException.ThrowIfNullOrEmpty(label);
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(culture);

        foreach(var key in keys) {
            var msg = await localizer.LocalizeAsync(key, new Dictionary<string, object?> {
                [Constants.PROPERTY_NAME] = label,
                [Constants.COMPARISON_VALUE] = 0,
                [Constants.FROM] = from,
                [Constants.TO] = to,
            }, culture);
            Console.WriteLine($"  [{culture.Name}] {msg}");
        }
    }

    public static async Task ShowMessageForUserAsync(
        IMessageLocalizer localizer,
        [NotNull] IEnumerable<CultureInfo> cultures,
        int from = 8,
        int to = 64
    ) {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(cultures);

        if(!cultures.Any()) {
            throw new ArgumentException("At least one culture name must be provided.", nameof(cultures));
        }

        foreach(var culture in cultures) {
            await ShowMessageAsync(localizer, nameof(User.Name), Constants.NOT_EMPTY_MESSAGE_KEY, culture, from, to);
            await ShowMessageAsync(localizer, nameof(User.Email), Constants.EMAIL_MESSAGE_KEY, culture, from, to);
            await ShowMessageAsync(localizer, nameof(User.Password), [Constants.REQUIRED_MESSAGE_KEY, Constants.LENGTH_MESSAGE_KEY], culture, from, to);
            await ShowMessageAsync(localizer, nameof(User.Age), [Constants.REQUIRED_MESSAGE_KEY, Constants.LENGTH_MESSAGE_KEY], culture, from, to);
            Console.WriteLine();
        }

        Console.WriteLine();
    }

    public static async Task ShowLocalizedErrorsAsync<T>(IValidator<T> validator, IMessageLocalizer localizer, T model, string label, CultureInfo culture) {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentException.ThrowIfNullOrEmpty(label);
        ArgumentNullException.ThrowIfNull(culture);

        var result = await validator.ValidateAsync(model);
        Console.WriteLine($"{label} [{culture.Name}]: {(result.IsValid ? "PASS" : "FAIL")}");

        foreach(var error in result.Errors) {
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

            var msg = error.MessageKey is not null
                ? await localizer.LocalizeAsync(error.MessageKey, parameters, culture)
                : error.Message;
            Console.WriteLine($"  [{error.MemberName} → key '{error.MessageKey}'] {msg}");
        }

        Console.WriteLine();
    }

    public static async Task ShowLocalizedErrorsAsync<T>(
        IValidator<T> validator,
        IMessageLocalizer localizer,
        T model,
        [NotNull] IEnumerable<CultureInfo> cultures
    ) {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(cultures);

        if(!cultures.Any()) {
            throw new ArgumentException("At least one culture name must be provided.", nameof(cultures));
        }

        foreach(var culture in cultures) {
            await ShowLocalizedErrorsAsync(validator, localizer, model, label: $"Invalid ({culture.Name})", culture);
        }
    }
}
