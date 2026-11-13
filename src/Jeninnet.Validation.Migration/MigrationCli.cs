//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Migration;

/// <summary>
/// Entry point for the <c>jeninnet migrate</c> command. Returns a process exit code instead of
/// throwing for expected outcomes so hosts and tests can assert on it:
/// <c>0</c> is success, <c>1</c> means changes are pending (with <c>--check</c>) or an error
/// occurred, and <c>2</c> is a usage error.
/// </summary>
public static class MigrationCli {
    /// <summary>
    /// Runs the migration command.
    /// </summary>
    /// <param name="args">The command-line arguments (starting with <c>migrate</c>).</param>
    /// <returns>The process exit code.</returns>
    public static int Run(string[] args) {
        ArgumentNullException.ThrowIfNull(args);

        if(args.Length == 0 || !string.Equals(args[0], "migrate", StringComparison.OrdinalIgnoreCase)) {
            PrintUsage();
            return 2;
        }

        var rest = args[1..];

        if(rest.Length == 0) {
            PrintUsage();
            return 2;
        }

        return rest[0].ToLowerInvariant() switch {
            "analyze" => Analyze(rest[1..]),
            "apply" => Apply(rest[1..]),
            _ => UnknownCommand(rest[0])
        };
    }

    private static int UnknownCommand(string command) {
        Console.Error.WriteLine($"Unknown migrate subcommand '{command}'. Expected 'analyze' or 'apply'.");
        PrintUsage();
        return 2;
    }

    private static void PrintUsage() {
        Console.WriteLine("Usage:");
        Console.WriteLine("  jeninnet migrate analyze <path> [--format markdown|json] [--output <file>] [--check]");
        Console.WriteLine("  jeninnet migrate apply <path> [--dry-run] [--check]");
    }

    private static List<string> CollectSources(string path, out string? error) {
        error = null;

        if(File.Exists(path)) {
            return [Path.GetFullPath(path)];
        }

        if(!Directory.Exists(path)) {
            error = $"Path '{path}' does not exist.";
            return [];
        }

        return [.. Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
            .Where(static file => !IsGeneratedPath(file))
            .OrderBy(static file => file, StringComparer.Ordinal)];
    }

    private static bool IsGeneratedPath(string path) {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;

        return directory.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
            .Any(static segment => string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase));
    }

    private static MigrationReport AnalyzeFiles(IReadOnlyList<string> files) {
        var report = new MigrationReport();

        foreach(var file in files) {
            var text = File.ReadAllText(file);
            var migration = MigrationEngine.MigrateSource(file, text);
            var entry = new FileMigration {
                Path = file,
                NewText = migration.NewText,
                Changed = migration.Changed
            };
            entry.Converted.AddRange(migration.Converted);
            entry.NeedsReview.AddRange(migration.NeedsReview);

            report.Files.Add(entry);
        }

        return report;
    }

    private static int Analyze(string[] args) {
        var options = new AnalyzeOptions();
        var index = 0;

        while(index < args.Length) {
            if(!TryParseAnalyzeArgument(args, ref index, options, out var error)) {
                Console.Error.WriteLine(error);
                return 2;
            }
        }

        if(options.Path is null) {
            Console.Error.WriteLine("Missing <path> to analyze.");
            return 2;
        }

        if(options.Format is not ("markdown" or "json")) {
            Console.Error.WriteLine($"Unknown format '{options.Format}'. Expected 'markdown' or 'json'.");
            return 2;
        }

        var files = CollectSources(options.Path, out var collectError);

        if(collectError is not null) {
            Console.Error.WriteLine(collectError);
            return 1;
        }

        var report = AnalyzeFiles(files);
        var rendered = options.Format == "json" ? report.ToJson() : report.ToMarkdown();

        if(options.Output is null) {
            Console.Write(rendered);
        } else {
            File.WriteAllText(options.Output, rendered);
            Console.WriteLine($"Wrote {options.Format} report for {report.FilesScanned} file(s) to '{options.Output}'.");
        }

        var needsReview = report.GetNeedsReview();
        if(options.Check && (report.FilesChanged > 0 || needsReview.Count > 0)) {
            Console.Error.WriteLine(
                $"Migration check failed: {report.FilesChanged} file(s) would change, {needsReview.Count} item(s) need review.");
            return 1;
        }

        return 0;
    }

    private static bool TryParseAnalyzeArgument(
        string[] args,
        ref int index,
        AnalyzeOptions options,
        out string? error) {
        var arg = args[index];

        if(string.Equals(arg, "--format", StringComparison.Ordinal)) {
            if(!TryReadOptionValue(args, ref index, arg, out var value, out error)) {
                return false;
            }

            options.Format = value.ToLowerInvariant();
            return true;
        }

        if(string.Equals(arg, "--output", StringComparison.Ordinal)) {
            if(!TryReadOptionValue(args, ref index, arg, out var value, out error)) {
                return false;
            }

            options.Output = value;
            return true;
        }

        if(string.Equals(arg, "--check", StringComparison.Ordinal)) {
            options.Check = true;
            index++;
            error = null;
            return true;
        }

        if(!arg.StartsWith("--", StringComparison.Ordinal) && options.Path is null) {
            options.Path = arg;
            index++;
            error = null;
            return true;
        }

        error = $"Unknown analyze argument '{arg}'.";
        return false;
    }

    private static bool TryReadOptionValue(
        string[] args,
        ref int index,
        string option,
        out string value,
        out string? error) {
        if(index + 1 >= args.Length) {
            value = string.Empty;
            error = $"Missing value for '{option}'.";
            return false;
        }

        value = args[index + 1];
        error = null;
        index += 2;
        return true;
    }

    private static int Apply(string[] args) {
        var options = new ApplyOptions();

        foreach(var arg in args) {
            if(!TryParseApplyArgument(arg, options, out var error)) {
                Console.Error.WriteLine(error);
                return 2;
            }
        }

        if(options.Path is null) {
            Console.Error.WriteLine("Missing <path> to apply to.");
            return 2;
        }

        var files = CollectSources(options.Path, out var collectError);

        if(collectError is not null) {
            Console.Error.WriteLine(collectError);
            return 1;
        }

        var changed = 0;
        var todos = 0;

        foreach(var file in files) {
            var migration = MigrationEngine.MigrateSource(file, File.ReadAllText(file));

            if(!migration.Changed) {
                continue;
            }

            changed++;
            todos += migration.NeedsReview.Count;

            if(options.Check || options.DryRun) {
                Console.WriteLine($"Would change '{file}' ({migration.Converted.Count} conversion(s), {migration.NeedsReview.Count} review item(s)).");
                continue;
            }

            File.WriteAllText(file, migration.NewText);
            Console.WriteLine($"Changed '{file}' ({migration.Converted.Count} conversion(s), {migration.NeedsReview.Count} review item(s)).");
        }

        if(options.Check && changed > 0) {
            Console.Error.WriteLine($"Migration check failed: {changed} file(s) would change.");
            return 1;
        }

        if(!options.DryRun && !options.Check) {
            Console.WriteLine($"Done: {changed} file(s) changed, {todos} review item(s) left as TODO markers or report entries.");
        }

        return 0;
    }

    private static bool TryParseApplyArgument(string arg, ApplyOptions options, out string? error) {
        if(arg == "--dry-run") {
            options.DryRun = true;
            error = null;
            return true;
        }

        if(arg == "--check") {
            options.Check = true;
            error = null;
            return true;
        }

        if(!arg.StartsWith("--", StringComparison.Ordinal) && options.Path is null) {
            options.Path = arg;
            error = null;
            return true;
        }

        error = $"Unknown apply argument '{arg}'.";
        return false;
    }

    private sealed class AnalyzeOptions {
        public string? Path { get; set; }

        public string Format { get; set; } = "markdown";

        public string? Output { get; set; }

        public bool Check { get; set; }
    }

    private sealed class ApplyOptions {
        public string? Path { get; set; }

        public bool DryRun { get; set; }

        public bool Check { get; set; }
    }
}
