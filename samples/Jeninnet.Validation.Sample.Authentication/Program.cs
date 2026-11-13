//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Platform/
// Licensed under the MIT License.
//

var baseDir = AppContext.BaseDirectory;

Console.WriteLine("=== AUTHENTICATION SAMPLE ===");
Console.WriteLine("Demonstrates RuleSet, ValidateAndThrow, WithMessage,");
Console.WriteLine("OverridePropertyName, and Localization.");
Console.WriteLine();

// ---- DI Setup ----
var services = new ServiceCollection();
services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(services);

// Manual per-culture JSON provider registration (equivalent to AddJsonLocalizationProviders)
// Each provider wraps a culture-specific JSON file; CultureAwareJsonWrapper resolves by culture.
var enProvider = new JsonMessageProvider(Path.Combine(baseDir, "Localization", "en.json"));
var arProvider = new JsonMessageProvider(Path.Combine(baseDir, "Localization", "ar.json"));

services.AddSingleton<IInnerMessageProvider>(
    new CultureAwareJsonWrapper(new Dictionary<string, JsonMessageProvider>
    {
        ["en"] = enProvider,
        ["ar"] = arProvider,
    }));

var provider = services.BuildServiceProvider();
var localizer = provider.GetRequiredService<IMessageLocalizer>();

// =============================================================
// Part 1: RegisterRequest — RuleSet demo
// =============================================================
Console.WriteLine("--- Part 1: RegisterRequest — RuleSet ---");
Console.WriteLine("Validates identity fields (Default) + password strength (PasswordStrength RuleSet).");
Console.WriteLine();

var registerValidator = new RegisterRequestValidator();

var weakRegister = new RegisterRequest(
    Username: "",
    Email: "not-an-email",
    Password: "weak",
    ConfirmPassword: "",
    Age: 10);

var registerResult = await registerValidator.ValidateAsync(weakRegister);

Console.WriteLine("Default RuleSet (identity fields):");
foreach (var err in registerResult.Errors)
    Console.WriteLine($"  [{err.MemberName}] {err.Message}");

Console.WriteLine();

// Validate with both Default and PasswordStrength RuleSets
var fullRegisterResult = await registerValidator.ValidateAsync(
    weakRegister, ruleSets: ["Default", RegisterRequestValidator.PasswordStrengthRuleSet]);

Console.WriteLine("Default + PasswordStrength RuleSets:");
foreach (var err in fullRegisterResult.Errors)
    Console.WriteLine($"  [{err.MemberName}] {err.Message}");

Console.WriteLine();

// =============================================================
// Part 2: LoginRequest — OverridePropertyName demo
// =============================================================
Console.WriteLine("--- Part 2: LoginRequest — OverridePropertyName ---");
Console.WriteLine("Username property is displayed as 'LoginName' in error messages.");
Console.WriteLine();

var loginValidator = new LoginRequestValidator();
var invalidLogin = new LoginRequest(Username: "", Password: "");
var loginResult = await loginValidator.ValidateAsync(invalidLogin);

foreach (var err in loginResult.Errors)
    Console.WriteLine($"  [{err.MemberName}] {err.Message}");

Console.WriteLine();

// =============================================================
// Part 3: ChangePasswordRequest — ValidateAndThrow demo
// =============================================================
Console.WriteLine("--- Part 3: ChangePasswordRequest — ValidateAndThrow ---");
Console.WriteLine("Uses ValidateAndThrow — catches ValidationException with all errors.");
Console.WriteLine();

var changeValidator = new ChangePasswordRequestValidator();
var badChange = new ChangePasswordRequest(
    CurrentPassword: "",
    NewPassword: "short",
    ConfirmNewPassword: "different");

try
{
    await changeValidator.ValidateAndThrowAsync(badChange, ruleSets: ["Default", "PasswordMatch"]);
}
catch (ValidationException vex)
{
    foreach (var err in vex.Errors)
        Console.WriteLine($"  [{err.MemberName}] {err.Message}");
}

Console.WriteLine();

// =============================================================
// Part 4: ForgotPasswordRequest — WithMessage custom text demo
// =============================================================
Console.WriteLine("--- Part 4: ForgotPasswordRequest — WithMessage ---");
Console.WriteLine("Custom business-friendly error messages via WithMessage().");
Console.WriteLine();

var forgotValidator = new ForgotPasswordRequestValidator();
var invalidForgot = new ForgotPasswordRequest(Email: "");
var forgotResult = await forgotValidator.ValidateAsync(invalidForgot);

foreach (var err in forgotResult.Errors)
    Console.WriteLine($"  [{err.MemberName}] {err.Message}");

Console.WriteLine();

// =============================================================
// Part 5: Localization — culture-aware error messages
// =============================================================
Console.WriteLine("--- Part 5: Localization ---");
Console.WriteLine("Standard validation keys auto-localized via JSON files.");
Console.WriteLine();

var resetValidator = new ResetPasswordRequestValidator();
var invalidReset = new ResetPasswordRequest(
    Token: "",
    NewPassword: "short",
    ConfirmNewPassword: "");

async Task ShowLocalizedAsync(string label, CultureInfo culture)
{
    var result = await resetValidator.ValidateAsync(invalidReset, ruleSets: ["Default", "Confirmation"]);
    Console.WriteLine($"Failures ({label}):");
    foreach (var err in result.Errors)
    {
        var msg = err.Message;
        if (err.MessageKey is not null)
        {
            var parameters = err.MessageParameters?.ToDictionary(
                static kv => kv.Key, static kv => (object?)kv.Value) ?? [];
            parameters["PropertyName"] = err.MemberName;
            if(parameters.TryGetValue("0", out var p0)) {
                parameters.TryAdd("ComparisonValue", p0);
                parameters.TryAdd("From", p0);
                parameters.TryAdd("MinLength", p0);
            }
            if(parameters.TryGetValue("1", out var p1)) {
                parameters.TryAdd("To", p1);
                parameters.TryAdd("MaxLength", p1);
            }

            var localized = await localizer.LocalizeAsync(err.MessageKey, parameters, culture);
            if (!string.IsNullOrEmpty(localized))
                msg = localized;
        }
        Console.WriteLine($"  [{err.MemberName}] {msg}");
    }
    Console.WriteLine();
}

await ShowLocalizedAsync("en-US", new CultureInfo("en-US"));
await ShowLocalizedAsync("ar-SA", new CultureInfo("ar-SA"));

// =============================================================
// Part 6: Successful validation
// =============================================================
Console.WriteLine("--- Part 6: Valid Registration ---");
Console.WriteLine("All rules pass when valid data is provided.");
Console.WriteLine();

var validRegister = new RegisterRequest(
    Username: "johndoe",
    Email: "john@example.com",
    Password: "Str0ng!Pass",
    ConfirmPassword: "Str0ng!Pass",
    Age: 25);

var validResult = await registerValidator.ValidateAsync(validRegister);
Console.WriteLine($"  Valid: {validResult.IsValid} ({(validResult.IsValid ? "PASS" : "FAIL")})");

Console.WriteLine();
Console.WriteLine("Done.");

// ---- Culture-aware wrapper for JSON providers ----
#pragma warning disable S3903 // Move into named namespace
file sealed class CultureAwareJsonWrapper(IReadOnlyDictionary<string, JsonMessageProvider> providers)
    : IInnerMessageProvider
#pragma warning restore S3903
{
    public async ValueTask<string?> GetMessageTemplateAsync(string key, CultureInfo culture, CancellationToken cancellationToken = default)
    {
        var current = culture;
        while (current is not null)
        {
            if (providers.TryGetValue(current.Name, out var provider) ||
                providers.TryGetValue(current.TwoLetterISOLanguageName, out provider))
            {
                var result = await provider.GetMessageTemplateAsync(key, culture, cancellationToken).ConfigureAwait(false);
                if (result is not null) return result;
            }
            if (current == CultureInfo.InvariantCulture) break;
            current = current.Parent;
        }
        return null;
    }
}
