//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
Console.WriteLine("=== COMPOSITE LOCALIZATION SHOWCASE ===");
Console.WriteLine();

// ============================================================
// Scenario 1: Memory → Json → ResX (fallback by KEY coverage)
//   Memory:  NotEmpty only
//   Json:    NotEmpty, Email
//   ResX:    NotEmpty, Email, GreaterThan, InclusiveBetween
// ============================================================
Console.WriteLine("=== SCENARIO 1: Key-coverage fallback ===");
Console.WriteLine(" Memory → Json → ResX");
Console.WriteLine(" Memory: NotEmpty only");
Console.WriteLine(" Json:   NotEmpty, Email");
Console.WriteLine(" ResX:   All 4 keys");
Console.WriteLine();

// Manual provider composition: Memory provides NOTEMPTY, Json provides NOTEMPTY+EMAIL,
// ResX provides all four keys (culture-aware). Composite chains them in registration order.
var baseDir = AppContext.BaseDirectory;
var memory = new MemoryMessageProvider(new MemoryLocalizationOptions {
    Messages = new Dictionary<string, string> {
        [Constants.NOT_EMPTY_MESSAGE_KEY] = "{PropertyName} must not be empty.",
    }
});
var json = new JsonMessageProvider(Path.Combine(baseDir, "Localization", "en.json"));
var resx = new ResXMessageProvider(
    "Jeninnet.Validation.Sample.Localization.Composite.CompositeMessages",
    Assembly.GetExecutingAssembly());

var s1 = CreateServices(memory, json, resx);
Console.WriteLine("English (en-US):"); await ShowCompositeAsync(s1, new CultureInfo(Constants.EN_US_CULTURE));
Console.WriteLine("Arabic  (ar-SY):"); await ShowCompositeAsync(s1, new CultureInfo(Constants.AR_SY_CULTURE));
Console.WriteLine("Dutch   (nl-NL):"); await ShowCompositeAsync(s1, new CultureInfo(Constants.NL_NL_CULTURE));
Console.WriteLine("Note: Memory & Json are culture-agnostic, so Arabic/Dutch fall back to English via ResX for GreaterThan/InclusiveBetween.");
Console.WriteLine();

// ============================================================
// Scenario 2: Sqlite → Json → ResX (culture-coverage fallback)
//   Sqlite: NotEmpty, Email (en, ar, nl)
//   Json:   NotEmpty, Email, GreaterThan (en, ar only)
//   ResX:   All 4 keys (en, ar, nl)
// For NotEmpty/Email:  Sqlite provides (first-provider hit)
// For GreaterThan(ar): Sqlite misses → Json provides (English text)
// For GreaterThan(nl): Sqlite misses → Json misses → ResX provides (Dutch)
// For InclusiveBetween: Sqlite misses → Json misses → ResX provides
// ============================================================
Console.WriteLine("=== SCENARIO 2: Culture-coverage fallback ===");
Console.WriteLine(" Sqlite → Json → ResX");
Console.WriteLine(" Sqlite: NotEmpty, Email (en, ar, nl)");
Console.WriteLine(" Json:   NotEmpty, Email, GreaterThan (en, ar only)");
Console.WriteLine(" ResX:   All 4 keys (en, ar, nl)");
Console.WriteLine();

var dbPath = Path.Combine(Path.GetTempPath(), $"Jeninnet_Composite_{Guid.NewGuid():N}.db");
var connStr = $"Data Source={dbPath}";
InitializeSqlite(connStr);

var sqlite = new SqliteMessageProvider(new SqliteLocalizationOptions {
    ConnectionString = connStr,
    TableName = "ValidationMessages"
});
var jsonFiltered = new CultureFilter(json, [Constants.EN_US_CULTURE, "en", Constants.AR_SY_CULTURE, "ar"]);

var s2 = CreateServices(sqlite, jsonFiltered, resx);
Console.WriteLine("English (en-US):"); await ShowCompositeAsync(s2, new CultureInfo(Constants.EN_US_CULTURE));
Console.WriteLine("Arabic  (ar-SY):"); await ShowCompositeAsync(s2, new CultureInfo(Constants.AR_SY_CULTURE));
Console.WriteLine("Dutch   (nl-NL):"); await ShowCompositeAsync(s2, new CultureInfo(Constants.NL_NL_CULTURE));
Console.WriteLine("Expect: en → Sqlite (all); ar → Sqlite (Not/Email), Json (Greater); nl → Sqlite (Not/Email), ResX (Greater/Inclusive).");
Console.WriteLine();

if(File.Exists(dbPath)) {
    try { File.Delete(dbPath); }
    catch { /* cleanup best-effort */ }
}

Console.WriteLine("Done.");

static ServiceProvider CreateServices(params IInnerMessageProvider[] innerProviders) {
    var svc = new ServiceCollection();
    svc.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
    JeninnetLocalizationExtensions.AddLocalization(svc);
    foreach(var p in innerProviders) {
        svc.AddSingleton(p);
    }

    return svc.BuildServiceProvider();
}

static async Task ShowCompositeAsync(ServiceProvider sp, CultureInfo culture) {
    var localizer = sp.GetRequiredService<IMessageLocalizer>();
    var validator = new CompositeUserValidator();
    var user = new Customer(Name: "", Email: "bad", Age: -1);
    var result = await validator.ValidateAsync(user);
    Console.WriteLine($"  {(result.IsValid ? "PASS" : "FAIL")}");
    foreach(var error in result.Errors) {
        var parameters = ValidationErrorDisplay.BuildParameters(error);
        var msg = error.MessageKey is not null
            ? await localizer.LocalizeAsync(error.MessageKey, parameters, culture)
            : error.Message;
        Console.WriteLine($"  [{error.MemberName}] {msg}");
    }
}

static void InitializeSqlite(string connectionString) {
    using var conn = new SqliteConnection(connectionString);
    conn.Open();
    using var create = conn.CreateCommand();
    create.CommandText = "CREATE TABLE IF NOT EXISTS ValidationMessages (MessageKey TEXT NOT NULL, Culture TEXT NOT NULL, Template TEXT NOT NULL, PRIMARY KEY (MessageKey, Culture))";
    create.ExecuteNonQuery();

    foreach(var (k, c, m) in new[] {
        (Constants.NOT_EMPTY_MESSAGE_KEY, Constants.EN_US_CULTURE, "{PropertyName} must not be empty."),
        (Constants.NOT_EMPTY_MESSAGE_KEY, Constants.AR_SY_CULTURE, "{PropertyName} يجب ألا يكون فارغاً."),
        (Constants.NOT_EMPTY_MESSAGE_KEY, Constants.NL_NL_CULTURE, "{PropertyName} mag niet leeg zijn."),
        (Constants.EMAIL_MESSAGE_KEY, Constants.EN_US_CULTURE, "{PropertyName} is not a valid email address."),
        (Constants.EMAIL_MESSAGE_KEY, Constants.AR_SY_CULTURE, "{PropertyName} ليس عنوان بريد إلكتروني صالح."),
        (Constants.EMAIL_MESSAGE_KEY, Constants.NL_NL_CULTURE, "{PropertyName} is geen geldig e-mailadres."),
    }) {
        using var insert = conn.CreateCommand();
        insert.CommandText = "INSERT OR IGNORE INTO ValidationMessages (MessageKey, Culture, Template) VALUES (@MessageKey, @Culture, @Template)";
        insert.Parameters.AddWithValue("@MessageKey", k);
        insert.Parameters.AddWithValue("@Culture", c);
        insert.Parameters.AddWithValue("@Template", m);
        insert.ExecuteNonQuery();
    }
}
