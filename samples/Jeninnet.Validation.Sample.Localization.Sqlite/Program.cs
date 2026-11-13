//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
// Create a temporary SQLite database
var dbPath = Path.Combine(Path.GetTempPath(), $"Jeninnet_EmployeeValidation_{Guid.NewGuid():N}.db");
var connectionString = $"Data Source={dbPath}";

// Seed the database with English and Arabic messages
SeedDatabase.Initialize(connectionString);

Console.WriteLine("=== SQLITE LOCALIZATION SHOWCASE ===");
Console.WriteLine($"Database: {dbPath}");
Console.WriteLine();

// Configure DI with Sqlite provider
var services = new ServiceCollection();

services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(services);
services.AddSqliteLocalization(
    options => {
        options.ConnectionString = connectionString;
        options.TableName = "ValidationMessages";
    }
);

var provider = services.BuildServiceProvider();
var localizer = provider.GetRequiredService<IMessageLocalizer>();

// ---- Part 1: Direct message lookup ----
Console.WriteLine("--- Direct Message Lookup ---");
Console.WriteLine("English (en-US), Arabic (ar-SY) — custom keys (Required, Email, Range):");

await Helper.ShowMessageForUserAsync(
    localizer,
    [
        new(Constants.EN_US_CULTURE),
        new(Constants.AR_SY_CULTURE),
    ]
);

Console.WriteLine();

// ---- Part 2: Fallback to default culture ----
Console.WriteLine("--- Fallback to Default Culture ---");
Console.WriteLine("Dutch (nl-NL) has no messages — falls back to English:");
await Helper.ShowMessageAsync(localizer, nameof(Employee.Name), Constants.REQUIRED_MESSAGE_KEY, new CultureInfo(Constants.NL_NL_CULTURE));
Console.WriteLine();

// ---- Part 3: Validation with localized messages ----
Console.WriteLine("--- Validation with Localized Messages ---");

var validator = new EmployeeValidator();
var invalidEmployee = new Employee("", "not-a-valid-email", "");

async Task ShowValidationAsync(string label, CultureInfo culture) {
    var result = await validator.ValidateAsync(invalidEmployee);
    Console.WriteLine($"{label} [{culture.Name}]: {(result.IsValid ? "PASS" : "FAIL")}");
    foreach(var error in result.Errors) {
        var parameters = error.MessageParameters?.ToDictionary(static kv => kv.Key, static kv => (object?)kv.Value) ?? [];
        parameters["PropertyName"] = error.MemberName;
        if(parameters.TryGetValue("0", out var p0)) {
            parameters.TryAdd("ComparisonValue", p0);
            parameters.TryAdd("From", p0);
            parameters.TryAdd("MinLength", p0);
        }

        if(parameters.TryGetValue("1", out var p1)) {
            parameters.TryAdd("To", p1);
            parameters.TryAdd("MaxLength", p1);
        }

        var msg = error.MessageKey is not null
            ? await localizer.LocalizeAsync(error.MessageKey, parameters, culture)
            : error.Message;
        Console.WriteLine($"  [{error.MemberName}] {msg}");
    }

    Console.WriteLine();
}

await ShowValidationAsync("Failure (EN)", new CultureInfo(Constants.EN_US_CULTURE));
await ShowValidationAsync("Failure (AR)", new CultureInfo(Constants.AR_SY_CULTURE));

// Cleanup
try {
    if(File.Exists(dbPath)) {
        File.Delete(dbPath);
    }
}
catch { /* best-effort cleanup */ }

Console.WriteLine("Done.");
