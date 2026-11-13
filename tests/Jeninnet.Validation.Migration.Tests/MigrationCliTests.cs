//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Migration.Tests;

/// <summary>
/// Covers <see cref="MigrationCli"/> exit codes: 0 success, 1 pending/error, 2 usage.
/// </summary>
[TestClass]
public sealed class MigrationCliTests {
    private const string VALIDATOR_SOURCE = """
        using FluentValidation;

        public class User {
            public string Name { get; set; } = string.Empty;
        }

        public class UserValidator : AbstractValidator<User> {
            public UserValidator() {
                RuleFor(x => x.Name).NotEmpty();
            }
        }
        """;

    private const string CLEAN_SOURCE = """
        public class UserValidator : Validator<User> {
            protected override void Configure(ValidatorRuleBuilder<User> builder) {
                builder.RuleFor(static x => x.Name, nameof(User.Name)).NotEmpty();
            }
        }
        """;

    [TestMethod]
    public void Run_Analyze_WritesMarkdownReport() {
        var directory = CreateWorkspace(("UserValidator.cs", VALIDATOR_SOURCE));
        var report = Path.Combine(directory, "report.md");

        try {
            var exit = RunCli(out var output, "migrate", "analyze", directory, "--output", report);

            MSTestAssertions.That(() => exit.Should().Be(0));
            MSTestAssertions.That(() => File.Exists(report).Should().BeTrue());
            MSTestAssertions.That(() => File.ReadAllText(report).Should().Contain("# Jeninnet migration report"));
            MSTestAssertions.That(() => File.ReadAllText(report).Should().Contain("rulefor-member-name"));
            MSTestAssertions.That(() => output.Should().Contain("report.md"));
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void Run_AnalyzeCheck_FailsWhenChangesPending() {
        var directory = CreateWorkspace(("UserValidator.cs", VALIDATOR_SOURCE));

        try {
            var exit = RunCli(out _, "migrate", "analyze", directory, "--check");

            MSTestAssertions.That(() => exit.Should().Be(1));
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void Run_AnalyzeCheck_PassesOnCLEAN_SOURCEs() {
        var directory = CreateWorkspace(("UserValidator.cs", CLEAN_SOURCE));

        try {
            var exit = RunCli(out _, "migrate", "analyze", directory, "--check");

            MSTestAssertions.That(() => exit.Should().Be(0));
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void Run_ApplyDryRun_WritesNothing() {
        var directory = CreateWorkspace(("UserValidator.cs", VALIDATOR_SOURCE));
        var file = Path.Combine(directory, "UserValidator.cs");

        try {
            var exit = RunCli(out var output, "migrate", "apply", directory, "--dry-run");

            MSTestAssertions.That(() => exit.Should().Be(0));
            MSTestAssertions.That(() => File.ReadAllText(file).Should().Be(VALIDATOR_SOURCE));
            MSTestAssertions.That(() => output.Should().Contain("Would change"));
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void Run_Apply_RewritesFilesInPlace() {
        var directory = CreateWorkspace(("UserValidator.cs", VALIDATOR_SOURCE));
        var file = Path.Combine(directory, "UserValidator.cs");

        try {
            var exit = RunCli(out _, "migrate", "apply", directory);

            MSTestAssertions.That(() => exit.Should().Be(0));
            MSTestAssertions.That(() => File.ReadAllText(file).Should().Contain("Validator<User>"));
            MSTestAssertions.That(() => File.ReadAllText(file).Should().Contain("nameof(User.Name)"));

            var second = RunCli(out _, "migrate", "apply", directory, "--check");

            MSTestAssertions.That(() => second.Should().Be(0));
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void Run_UnknownCommand_ReturnsUsageError() {
        var exit = RunCli(out _, "migrate", "bogus");

        MSTestAssertions.That(() => exit.Should().Be(2));
    }

    private static string CreateWorkspace(params (string Name, string Content)[] files) {
        var directory = Path.Combine(Path.GetTempPath(), "Jeninnet.Migration.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(directory);

        foreach(var (name, content) in files) {
            File.WriteAllText(Path.Combine(directory, name), content);
        }

        return directory;
    }

    private static int RunCli(out string output, params string[] args) {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        Console.SetOut(writer);
        Console.SetError(writer);

        try {
            return MigrationCli.Run(args);
        }
        finally {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            output = writer.ToString();
        }
    }
}
