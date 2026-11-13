//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Covers <c>[FutureDate]</c> and <c>[PastDate]</c>: UTC semantics, <c>IncludeToday</c>,
/// nullable members, injected <see cref="TimeProvider"/>, diagnostics, and client rules.
/// </summary>
[TestClass]
public sealed class GeneratedRelativeDateTests {
    private static readonly DateTimeOffset _fixedNow = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private const string SOURCE = """
        using System;
        using Jeninnet.Validation.Attributes;

        namespace RelativeDateModels;

        [Validate("RelativeDateModels")]
        public sealed class Event {
            [FutureDate]
            public DateTime StartsAt { get; set; }

            [FutureDate(IncludeToday = true)]
            public DateTimeOffset EndsAt { get; set; }

            [PastDate]
            public DateOnly? ClosedOn { get; set; }

            [PastDate(IncludeToday = true)]
            public DateTime? AdjustedAt { get; set; }
        }
        """;

    [TestMethod]
    public void FutureDate_UsesUtcAndHonorsIncludeToday() {
        var generated = BuildGenerated(SOURCE);

        generated.Set("StartsAt", _fixedNow.UtcDateTime.AddMinutes(1));
        ValidationAssert.NoErrorFor(generated.Validate(_fixedNow), "StartsAt");

        generated.Set("StartsAt", _fixedNow.UtcDateTime.AddMinutes(-1));
        ValidationAssert.HasError(generated.Validate(_fixedNow), "StartsAt");

        generated.Set("StartsAt", _fixedNow.UtcDateTime);
        ValidationAssert.HasError(generated.Validate(_fixedNow), "StartsAt");

        generated.Set("EndsAt", _fixedNow);
        ValidationAssert.NoErrorFor(generated.Validate(_fixedNow), "EndsAt");

        generated.Set("EndsAt", _fixedNow.AddTicks(-1));
        ValidationAssert.HasError(generated.Validate(_fixedNow), "EndsAt");
    }

    [TestMethod]
    public void PastDate_SupportsNullableDateTimeAndDateOnly() {
        var generated = BuildGenerated(SOURCE);

        generated.Set("ClosedOn", DateOnly.FromDateTime(_fixedNow.UtcDateTime).AddDays(-1));
        generated.Set("AdjustedAt", _fixedNow.UtcDateTime);
        var valid = generated.Validate(_fixedNow);
        ValidationAssert.NoErrorFor(valid, "ClosedOn");
        ValidationAssert.NoErrorFor(valid, "AdjustedAt");

        generated.Set("ClosedOn", DateOnly.FromDateTime(_fixedNow.UtcDateTime));
        generated.Set("AdjustedAt", _fixedNow.UtcDateTime.AddTicks(1));
        var invalid = generated.Validate(_fixedNow);
        ValidationAssert.HasError(invalid, "ClosedOn");
        ValidationAssert.HasError(invalid, "AdjustedAt");

        generated.Set("ClosedOn", null);
        generated.Set("AdjustedAt", null);
        var missing = generated.Validate(_fixedNow);
        ValidationAssert.NoErrorFor(missing, "ClosedOn");
        ValidationAssert.NoErrorFor(missing, "AdjustedAt");
    }

    [TestMethod]
    public void DescriptorAndClientRules_ExposeRelativeDateRules() {
        var generated = BuildGenerated(SOURCE);
        var descriptor = generated.Describe();

        MSTestAssertions.That(() => descriptor.RulesForMember("StartsAt").Single().Kind.Should().Be("FutureDate"));
        MSTestAssertions.That(() => descriptor.RulesForMember("ClosedOn").Single().Kind.Should().Be("PastDate"));

        var json = ClientRulesWriter.WriteToString([descriptor]);
        MSTestAssertions.That(() => json.Should().Contain("\"rule\": \"FutureDate\""));
        MSTestAssertions.That(() => json.Should().Contain("\"serverOnly\": true"));
    }

    [TestMethod]
    public void GeneratedSource_ResolvesTimeProviderAndEmitsRelativeRules() {
        var run = RunGenerator(SOURCE);
        var text = string.Join(Environment.NewLine, run.GeneratedTrees.Select(static tree => tree.ToString()));

        MSTestAssertions.That(() => text.Should().Contain("FutureDateRule"));
        MSTestAssertions.That(() => text.Should().Contain("PastDateRule"));
        MSTestAssertions.That(() => text.Should().Contain("TimeProvider"));
        MSTestAssertions.That(() => text.Should().Contain("DateOnly.FromDateTime"));
    }

    [TestMethod]
    public void UnsupportedMemberType_ProducesDiagnostic() {
        const string source = """
            using Jeninnet.Validation.Attributes;

            namespace RelativeDateModels;

            [Validate]
            public sealed class BrokenEvent {
                [FutureDate]
                public string? When { get; set; }
            }
            """;

        var diagnostics = RunGenerator(source).Diagnostics;

        MSTestAssertions.That(() => diagnostics.Any(static diagnostic => diagnostic.Id == "VAL0032").Should().BeTrue());
    }

    private static GeneratorRun RunGenerator(string source) {
        var compilation = CreateCompilation(source, $"RelativeDateAssembly_{Guid.NewGuid():N}");
        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var result = driver.GetRunResult();

        return new GeneratorRun(result.GeneratedTrees, [.. result.Diagnostics]);
    }

    private static GeneratedEvent BuildGenerated(string source) {
        var compilation = CreateCompilation(source, $"RelativeDateAssembly_{Guid.NewGuid():N}");
        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var result = driver.GetRunResult();
        var errors = result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();

        MSTestAssertions.That(() => errors.Should().BeEmpty(
            string.Join(Environment.NewLine, errors)));

        using var stream = new MemoryStream();
        var emit = compilation.AddSyntaxTrees(result.GeneratedTrees).Emit(stream);
        var problems = emit.Diagnostics.Where(static diagnostic =>
            diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error).ToList();

        MSTestAssertions.That(() => problems.Should().BeEmpty(
            string.Join(Environment.NewLine, problems)));

        var assembly = Assembly.Load(stream.ToArray());
        var modelType = assembly.GetType("RelativeDateModels.Event", throwOnError: true)!;
        var validatorType = assembly.GetType("RelativeDateModels.EventValidator", throwOnError: true)!;
        var validator = Activator.CreateInstance(validatorType)!;

        return new GeneratedEvent(modelType, validatorType, validator);
    }

    private static CSharpCompilation CreateCompilation(string source, string assemblyName) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        return CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FixedServiceProvider(object service) : IServiceProvider {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(TimeProvider) ? service : null;
    }

    private sealed class GeneratedEvent(Type modelType, Type validatorType, object validator) {
        private readonly object _model = Activator.CreateInstance(modelType)!;

        public void Set(string property, object? value) =>
            modelType.GetProperty(property)!.SetValue(_model, value);

        public ValidationResult Validate(DateTimeOffset now) =>
            (ValidationResult)validatorType
                .GetMethod("Validate", [modelType, typeof(ValidationCallOptions)])!
                .Invoke(validator, [
                    _model,
                    new ValidationCallOptions { ServiceProvider = new FixedServiceProvider(new FixedTimeProvider(now)) }
                ])!;

        public IValidationDescriptor Describe() =>
            ((IValidationDescriptorProvider)validator).Describe();
    }

    private sealed record GeneratorRun(
        IEnumerable<SyntaxTree> GeneratedTrees,
        IReadOnlyList<Diagnostic> Diagnostics);
}
