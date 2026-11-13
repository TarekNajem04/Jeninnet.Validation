//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Localization.Sqlite;

public static class SeedDatabase {
    /// <summary>
    /// Creates the ValidationMessages table and seeds it with English and Arabic messages.
    /// </summary>
    /// <param name="connectionString">The connection string to the SQLite database.</param>
    public static void Initialize(string connectionString) {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        // Create table matching the schema expected by SqliteMessageProvider
        // (provider queries: SELECT Template FROM {TableName} WHERE MessageKey = @MessageKey AND Culture = @Culture)
        using var createTable = conn.CreateCommand();
        createTable.CommandText =
            """
            CREATE TABLE IF NOT EXISTS ValidationMessages (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Culture TEXT NOT NULL,
                MessageKey TEXT NOT NULL,
                Template TEXT NOT NULL
            );
            """;
        createTable.ExecuteNonQuery();

        // Seed English messages (custom keys for direct lookup + standard keys for validation auto-lookup)
        Seed(
            conn,
            "en-US",
            ("Required", "{PropertyName} is required."),
            ("NotEmpty", "{PropertyName} must not be empty."),
            ("Email", "{PropertyName} must be a valid email address."),
            ("Length", "{PropertyName} must be between {From} and {To} characters."),
            ("Range", "{PropertyName} must be between {From} and {To}.")
        );

        // Seed Arabic messages
        Seed(
            conn,
            "ar-SY",
            ("Required", "{PropertyName} مطلوب."),
            ("NotEmpty", "{PropertyName} يجب ألا يكون فارغاً."),
            ("Email", "{PropertyName} يجب أن يكون بريداً إلكترونياً صالحاً."),
            ("Length", "{PropertyName} يجب أن يكون بين {From} و {To} حرفاً."),
            ("Range", "{PropertyName} يجب أن يكون بين {From} و {To}.")
        );
    }

    private static void Seed(SqliteConnection conn, string culture, params (string Key, string Template)[] messages) {
        foreach(var (key, template) in messages) {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                """
                INSERT OR IGNORE INTO ValidationMessages (Culture, MessageKey, Template)
                VALUES (@Culture, @MessageKey, @Template)
                """;
            cmd.Parameters.AddWithValue("@Culture", culture);
            cmd.Parameters.AddWithValue("@MessageKey", key);
            cmd.Parameters.AddWithValue("@Template", template);
            cmd.ExecuteNonQuery();
        }
    }
}
