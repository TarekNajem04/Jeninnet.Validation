# SQLite Localization Sample

## Purpose

Demonstrates storing and retrieving validation messages from a SQLite database. Messages are culture-aware and queried by key and culture at runtime.

## Package References

- Jeninnet.Validation
- Jeninnet.Validation.Localization
- Jeninnet.Validation.Localization.Database.Sqlite
- Microsoft.Data.Sqlite

## Folder Structure

```
Employee.cs                    Employee model
EmployeeValidator.cs           Validation rules
SeedDatabase.cs                Database creation and seeding
Program.cs                     DI setup, message lookup, validation demo
```

## Database Schema

```sql
CREATE TABLE ValidationMessages (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Culture TEXT NOT NULL,
    MessageKey TEXT NOT NULL,
    MessageTemplate TEXT NOT NULL
);
```

The database is created temporarily at runtime and cleaned up on exit.

## Running

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.Localization.Sqlite
```

## Expected Output

The console displays:
- Direct message lookup for each culture
- Fallback behavior when a culture has no matching messages
- Full validation output with culture-specific errors

## When to Use

Use the SQLite provider when:
- Messages need to be editable at runtime (by administrators)
- You have a shared database across multiple services
- You need per-tenant or per-user message customization
- You want to integrate with existing database infrastructure

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.Localization.Sqlite/Jeninnet.Validation.Sample.Localization.Sqlite.csproj
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
