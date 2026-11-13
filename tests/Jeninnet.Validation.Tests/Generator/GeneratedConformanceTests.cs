//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Executable conformance matrix: a real source-generated validator and a hand-written fluent
/// validator for the same model must produce identical results across all cases.
/// </summary>
[TestClass]
public sealed class GeneratedConformanceTests {
    private const string SOURCE = """
        using System;
        using System.Collections.Generic;
        using Jeninnet.Validation;
        using Jeninnet.Validation.Attributes;
        using Jeninnet.Validation.Attributes.Common;
        using Jeninnet.Validation.Core;
        using Jeninnet.Validation.Fluent;

        namespace Conformance;

        [Validate("Conformance")]
        public sealed class ConformanceModel {
            [NotEmpty, Email] public string? Email { get; set; }
            [Length(3, 10)] public string? Name { get; set; }
            [InclusiveBetween(18, 99)] public int Age { get; set; }
            [GreaterThan(0), LessThan(100)] public int Score { get; set; }
            [ExactLength(5)] public string? Code { get; set; }
            [Regex("^[A-Z]+$")] public string? Upper { get; set; }
            [Contains("x"), StartsWith("a"), EndsWith("z")] public string? Pattern { get; set; }
            [Base64String] public string? Payload { get; set; }
            [FileExtensions(".png", ".jpg")] public string? FileName { get; set; }
            [After("2027-01-01")] public DateTime When { get; set; }
            [Enum] public DayOfWeek Day { get; set; }
            [PropertyEqual(nameof(Password))] public string? Confirm { get; set; }
            public string? Password { get; set; }
            [Count(1, 3)] public List<string>? Tags { get; set; }
        }

        public sealed class ConformanceFluentValidator : Validator<ConformanceModel> {
            protected override void Configure(ValidatorRuleBuilder<ConformanceModel> builder) {
                builder.RuleFor(static x => x.Email, nameof(ConformanceModel.Email))
                       .NotEmpty()
                       .EmailAddress();

                builder.RuleFor(static x => x.Name, nameof(ConformanceModel.Name))
                       .Length(3, 10);

                builder.RuleFor(static x => x.Age, nameof(ConformanceModel.Age))
                       .InclusiveBetween(18, 99);

                builder.RuleFor(static x => x.Score, nameof(ConformanceModel.Score))
                       .GreaterThan(0)
                       .LessThan(100);

                builder.RuleFor(static x => x.Code, nameof(ConformanceModel.Code))
                       .ExactLength(5);

                builder.RuleFor(static x => x.Upper, nameof(ConformanceModel.Upper))
                       .Matches("^[A-Z]+$");

                builder.RuleFor(static x => x.Pattern, nameof(ConformanceModel.Pattern))
                       .Contains("x")
                       .StartsWith("a")
                       .EndsWith("z");

                builder.RuleFor(static x => x.Payload, nameof(ConformanceModel.Payload))
                       .Base64String();

                builder.RuleFor(static x => x.FileName, nameof(ConformanceModel.FileName))
                       .FileExtensions(new[] { ".png", ".jpg" });

                builder.RuleFor(static x => x.When, nameof(ConformanceModel.When))
                       .After(new DateTime(2027, 1, 1));

                builder.RuleFor(static x => x.Day, nameof(ConformanceModel.Day))
                       .Enum();

                builder.RuleFor(static x => x.Confirm, nameof(ConformanceModel.Confirm))
                       .Equal(ValidatorFactory.Accessor<ConformanceModel, string?>(
                           static x => x.Password,
                           nameof(ConformanceModel.Password)));

                builder.RuleFor(static x => x.Tags, nameof(ConformanceModel.Tags))
                       .Count(1, 3);
            }
        }

        public static class ConformanceHarness {
            public static string Run() {
                var mismatches = new List<string>();
                var cases = new (string Name, Action<ConformanceModel> Mutate)[] {
                    ("Valid", static _ => { }),
                    ("EmailEmpty", static m => m.Email = string.Empty),
                    ("EmailShape", static m => m.Email = "not-an-email"),
                    ("NameShort", static m => m.Name = "ab"),
                    ("NameLong", static m => m.Name = "abcdefghijk"),
                    ("AgeLow", static m => m.Age = 17),
                    ("AgeHigh", static m => m.Age = 100),
                    ("ScoreLow", static m => m.Score = 0),
                    ("ScoreHigh", static m => m.Score = 100),
                    ("CodeShort", static m => m.Code = "ABCD"),
                    ("CodeLong", static m => m.Code = "ABCDEF"),
                    ("UpperLower", static m => m.Upper = "abc"),
                    ("PatternBad", static m => m.Pattern = "bzz"),
                    ("PayloadBad", static m => m.Payload = "not-base64!"),
                    ("FileBad", static m => m.FileName = "photo.gif"),
                    ("WhenOld", static m => m.When = new DateTime(2025, 1, 1)),
                    ("EnumUndefined", static m => m.Day = (DayOfWeek)99),
                    ("ConfirmMismatch", static m => m.Confirm = "other"),
                    ("TagsEmpty", static m => m.Tags = new List<string>()),
                    ("TagsTooMany", static m => m.Tags = new List<string> { "a", "b", "c", "d" }),
                    ("Nulls", static m => {
                        m.Email = null;
                        m.Name = null;
                        m.Code = null;
                        m.Upper = null;
                        m.Pattern = null;
                        m.Payload = null;
                        m.FileName = null;
                        m.Tags = null;
                    }),
                    ("Multiple", static m => {
                        m.Email = string.Empty;
                        m.Age = 10;
                        m.Name = "x";
                        m.Code = "x";
                    }),
                    ("AllInvalid", static m => {
                        m.Email = "bad";
                        m.Name = "x";
                        m.Age = 10;
                        m.Score = 0;
                        m.Code = "x";
                        m.Upper = "abc";
                        m.Pattern = "bzz";
                        m.Payload = "bad";
                        m.FileName = "x.gif";
                        m.When = new DateTime(2025, 1, 1);
                        m.Day = (DayOfWeek)99;
                        m.Confirm = "other";
                        m.Tags = new List<string>();
                    })
                };

                foreach(var testCase in cases) {
                    var model = CreateValidModel();
                    testCase.Mutate(model);

                    var generated = new ConformanceModelValidator().Validate(model);
                    var fluent = new ConformanceFluentValidator().Validate(model);

                    Compare(testCase.Name, generated, fluent, mismatches);
                }

                return string.Join(Environment.NewLine, mismatches);
            }

            private static ConformanceModel CreateValidModel() => new() {
                Email = "user@example.com",
                Name = "Valid",
                Age = 25,
                Score = 50,
                Code = "ABCDE",
                Upper = "ABC",
                Pattern = "axz",
                Payload = "SGVsbG8=",
                FileName = "photo.png",
                When = new DateTime(2027, 6, 1),
                Day = DayOfWeek.Monday,
                Confirm = "secret",
                Password = "secret",
                Tags = new List<string> { "a", "b" }
            };

            private static void Compare(
                string caseName,
                ValidationResult generated,
                ValidationResult fluent,
                List<string> mismatches) {
                if(generated.IsValid != fluent.IsValid) {
                    mismatches.Add($"{caseName}: IsValid generated={generated.IsValid}, fluent={fluent.IsValid}");
                    return;
                }

                if(generated.Errors.Count != fluent.Errors.Count) {
                    mismatches.Add(
                        $"{caseName}: error count generated={generated.Errors.Count}, fluent={fluent.Errors.Count}");
                    return;
                }

                for(var i = 0; i < generated.Errors.Count; i++) {
                    var left = generated.Errors[i];
                    var right = fluent.Errors[i];

                    if(left.MemberName != right.MemberName) {
                        mismatches.Add($"{caseName}[{i}]: member '{left.MemberName}' != '{right.MemberName}'");
                    }

                    if(left.MessageKey != right.MessageKey) {
                        mismatches.Add($"{caseName}[{i}]: key '{left.MessageKey}' != '{right.MessageKey}'");
                    }

                    if(left.ErrorCode != right.ErrorCode) {
                        mismatches.Add($"{caseName}[{i}]: code '{left.ErrorCode}' != '{right.ErrorCode}'");
                    }

                    if(left.Severity != right.Severity) {
                        mismatches.Add($"{caseName}[{i}]: severity '{left.Severity}' != '{right.Severity}'");
                    }

                    if(!Equals(left.CustomState, right.CustomState)) {
                        mismatches.Add($"{caseName}[{i}]: custom state differs");
                    }
                }
            }
        }
        """;

    [TestMethod]
    public void GeneratedAndFluent_ProduceIdenticalResultsAcrossMatrix() {
        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        var syntaxTree = CSharpSyntaxTree.ParseText(SOURCE, cancellationToken: cancellationToken);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            $"GeneratedConformanceAssembly_{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, cancellationToken);

        var runResult = driver.GetRunResult();
        MSTestAssertions.That(() => (runResult.GeneratedTrees.Length > 0).Should().BeTrue(
            string.Join(Environment.NewLine, runResult.Diagnostics)));

        var generatedCompilation = compilation.AddSyntaxTrees(runResult.GeneratedTrees);

        using var stream = new MemoryStream();
        var emit = generatedCompilation.Emit(stream, cancellationToken: cancellationToken);

        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        var assembly = Assembly.Load(stream.ToArray());
        var harness = assembly.GetType("Conformance.ConformanceHarness")!;
        var report = (string)harness.GetMethod("Run")!.Invoke(null, null)!;

        MSTestAssertions.That(() => report.Should().BeEmpty(report));
    }

    public TestContext? TestContext { get; set; }
}
