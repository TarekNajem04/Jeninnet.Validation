//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Covers struct, record-struct, generic-model, and date/time type support in the generator.
/// </summary>
[TestClass]
public sealed class GeneratedTypeCoverageTests {
    [TestMethod]
    public void StructModel_GeneratesAndExecutes() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace StructCoverage;

            [Validate]
            public struct Contact {
                [NotEmpty]
                public string? Name { get; set; }
            }
            """;

        var result = BuildGenerated(source);

        MSTestAssertions.That(() => result.Validate("StructCoverage.Contact", "Name", string.Empty)
            .Should().BeFalse());

        MSTestAssertions.That(() => result.Validate("StructCoverage.Contact", "Name", "Tarek")
            .Should().BeTrue());
    }

    [TestMethod]
    public void RecordStructModel_GeneratesAndExecutes() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace StructCoverage;

            [Validate]
            public record struct ContactRecord {
                [NotEmpty]
                public string? Name { get; set; }
            }
            """;

        var result = BuildGenerated(source);

        MSTestAssertions.That(() => result.Validate("StructCoverage.ContactRecord", "Name", string.Empty)
            .Should().BeFalse());

        MSTestAssertions.That(() => result.Validate("StructCoverage.ContactRecord", "Name", "Tarek")
            .Should().BeTrue());
    }

    [TestMethod]
    public void DateOnlyTimeOnlyAndDateTimeOffset_GenerateAndExecute() {
        const string source = """
            using System;
            using Jeninnet.Validation.Attributes;
            namespace DateCoverage;

            [Validate]
            public sealed class Schedule {
                [After("2026-01-01")]
                public DateOnly StartDate { get; set; }

                [Before("12:00")]
                public TimeOnly EndTime { get; set; }

                [After("2026-01-01T00:00:00+00:00")]
                public DateTimeOffset Timestamp { get; set; }
            }
            """;

        var result = BuildGenerated(source);

        MSTestAssertions.That(() => result.ValidateSchedule(
            new DateOnly(2025, 12, 31),
            new TimeOnly(13, 0),
            new DateTimeOffset(2025, 12, 31, 23, 0, 0, TimeSpan.Zero)).Should().BeFalse());

        MSTestAssertions.That(() => result.ValidateSchedule(
            new DateOnly(2026, 1, 2),
            new TimeOnly(11, 0),
            new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)).Should().BeTrue());
    }

    [TestMethod]
    public void InvalidDateValue_ProducesDiagnosticWithoutFallback() {
        const string source = """
            using System;
            using Jeninnet.Validation.Attributes;
            namespace DateCoverage;

            [Validate]
            public sealed class InvalidDate {
                [After("not-a-date")]
                public DateOnly StartDate { get; set; }
            }
            """;

        var (diagnostics, sources, _) = RunGenerator(source);

        MSTestAssertions.That(() => diagnostics.Any(static diagnostic => diagnostic.Id == "VAL0021")
            .Should().BeTrue("an invalid date must report VAL0021"));
        MSTestAssertions.That(() => string.Join(Environment.NewLine, sources)
            .Should().NotContain("DateTime.Now"));
        MSTestAssertions.That(() => string.Join(Environment.NewLine, sources)
            .Should().NotContain("AfterRule"));
    }

    [TestMethod]
    public void GenericModel_ProducesDiagnosticAndIsNotEmitted() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace GenericCoverage;

            [Validate]
            public sealed class GenericModel<T> {
                [NotEmpty]
                public string? Name { get; set; }
            }
            """;

        var (diagnostics, sources, _) = RunGenerator(source);

        MSTestAssertions.That(() => diagnostics.Any(static diagnostic => diagnostic.Id == "VAL0022")
            .Should().BeTrue("open generic models must report VAL0022"));
        MSTestAssertions.That(() => string.Join(Environment.NewLine, sources)
            .Should().NotContain("GenericModelValidator"));
    }

    [TestMethod]
    public void GeneratorOutput_IsCultureInvariant() {
        const string source = """
            using System;
            using Jeninnet.Validation.Attributes;
            namespace DeterminismCoverage;

            [Validate]
            public sealed class DeterministicModel {
                [After("2026-01-01")]
                public DateOnly StartDate { get; set; }

                [GreaterThan(1.5)]
                public double Amount { get; set; }
            }
            """;

        var invariant = GenerateUnderCulture(source, CultureInfo.InvariantCulture);
        var french = GenerateUnderCulture(source, new CultureInfo("fr-FR"));

        MSTestAssertions.That(() => french.Should().Be(invariant));
    }

    private static string GenerateUnderCulture(string source, CultureInfo culture) {
        string? result = null;
        Exception? failure = null;

        var thread = new Thread(() => {
            try {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var (_, sources, _) = RunGenerator(source, "DeterminismCoverageAssembly");
                result = string.Join(Environment.NewLine, sources);
            }
            catch(Exception exception) {
                failure = exception;
            }
        });

        thread.Start();
        thread.Join();

        if(failure is not null) {
            throw failure;
        }

        return result!;
    }

    private static GeneratedTypeResult BuildGenerated(string source) {
        var (diagnostics, _, outputCompilation) = RunGenerator(source);

        var errors = diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
        MSTestAssertions.That(() => errors.Should().BeEmpty(
            string.Join(Environment.NewLine, errors)));

        using var stream = new MemoryStream();
        var emit = outputCompilation.Emit(stream);
        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        var assembly = Assembly.Load(stream.ToArray());

        return new GeneratedTypeResult(assembly);
    }

    private static (ImmutableArray<Diagnostic> Diagnostics, string[] Sources, Compilation OutputCompilation)
        RunGenerator(string source, string? assemblyName = null) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            assemblyName ?? $"GeneratedTypeCoverageAssembly_{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        var sources = driver.GetRunResult().GeneratedTrees
                            .Select(static tree => tree.ToString())
                            .ToArray();

        return (diagnostics, sources, outputCompilation);
    }

    private sealed class GeneratedTypeResult(Assembly assembly) {
        public bool Validate(string typeName, string propertyName, string value) {
            var modelType = assembly.GetType(typeName, throwOnError: true)!;
            var validatorType = assembly.GetType(GetValidatorTypeName(typeName), throwOnError: true)!;
            var validator = Activator.CreateInstance(validatorType)!;
            var model = Activator.CreateInstance(modelType)!;
            modelType.GetProperty(propertyName)!.SetValue(model, value);

            var result = (ValidationResult)validatorType
                .GetMethod("Validate", [modelType])!
                .Invoke(validator, [model])!;

            return result.IsValid;
        }

        public bool ValidateSchedule(DateOnly startDate, TimeOnly endTime, DateTimeOffset timestamp) {
            var modelType = assembly.GetType("DateCoverage.Schedule", throwOnError: true)!;
            var validatorType = assembly.GetType(GetValidatorTypeName("DateCoverage.Schedule"), throwOnError: true)!;
            var validator = Activator.CreateInstance(validatorType)!;
            var model = Activator.CreateInstance(modelType)!;
            modelType.GetProperty("StartDate")!.SetValue(model, startDate);
            modelType.GetProperty("EndTime")!.SetValue(model, endTime);
            modelType.GetProperty("Timestamp")!.SetValue(model, timestamp);

            var result = (ValidationResult)validatorType
                .GetMethod("Validate", [modelType])!
                .Invoke(validator, [model])!;

            return result.IsValid;
        }

        private static string GetValidatorTypeName(string modelTypeName) {
            var lastDot = modelTypeName.LastIndexOf('.');
            var ns = lastDot >= 0 ? modelTypeName[..lastDot] : string.Empty;
            var simpleName = lastDot >= 0 ? modelTypeName[(lastDot + 1)..] : modelTypeName;

            return string.IsNullOrEmpty(ns)
                ? $"{simpleName}Validator"
                : $"{ns}.Validators.{simpleName}Validator";
        }
    }
}
