//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Covers the value-conditional attributes in the source generator: runtime behavior,
/// diagnostics, descriptors, and the server-only client rules contract.
/// </summary>
[TestClass]
public sealed class GeneratedConditionalValueTests {
    private const string SOURCE = """
        using System;
        using Jeninnet.Validation.Attributes;

        namespace ConditionalValueModels;

        public enum DeliveryChannel {
            Standard = 0,
            Express = 1
        }

        [Validate("ConditionalValueModels")]
        public sealed class Order {
            public string? Mode { get; set; }
            public bool IsActive { get; set; }
            public DeliveryChannel Channel { get; set; }
            public int Count { get; set; }

            [RequiredIf(nameof(Mode), "Express")]
            public string? Name { get; set; }

            [RequiredUnless(nameof(Mode), "Express")]
            public string? Note { get; set; }

            [ConditionalRange(nameof(Mode), "Business", 1, 10)]
            public int Quantity { get; set; }

            [RequiredIf(nameof(IsActive), true)]
            public DateTime? ActivatedAt { get; set; }

            [RequiredIf(nameof(Channel), DeliveryChannel.Express)]
            public string? ChannelNote { get; set; }

            [RequiredIf(nameof(Count), 1L)]
            public int? Extra { get; set; }
        }
        """;

    [TestMethod]
    public void RequiredIfAndUnless_FollowTheSiblingValue() {
        var generated = BuildGenerated(SOURCE);

        var express = generated.Create();
        generated.Set(express, "Mode", "Express");
        generated.Set(express, "Name", string.Empty);
        generated.Set(express, "Note", string.Empty);
        var expressResult = generated.Validate(express);

        ValidationAssert.HasError(expressResult, "Name");
        ValidationAssert.NoErrorFor(expressResult, "Note");

        var standard = generated.Create();
        generated.Set(standard, "Mode", "Standard");
        generated.Set(standard, "Name", string.Empty);
        generated.Set(standard, "Note", string.Empty);
        var standardResult = generated.Validate(standard);

        ValidationAssert.NoErrorFor(standardResult, "Name");
        ValidationAssert.HasError(standardResult, "Note");
    }

    [TestMethod]
    public void RequiredIf_WorksForBoolEnumDateTimeAndNumericCoercion() {
        var generated = BuildGenerated(SOURCE);

        var active = generated.Create();
        generated.Set(active, "IsActive", true);
        generated.Set(active, "Channel", 1);
        generated.Set(active, "Count", 1);
        var activeResult = generated.Validate(active);

        ValidationAssert.HasError(activeResult, "ActivatedAt");
        ValidationAssert.HasError(activeResult, "ChannelNote");
        ValidationAssert.HasError(activeResult, "Extra");

        var populated = generated.Create();
        generated.Set(populated, "IsActive", true);
        generated.Set(populated, "Channel", 1);
        generated.Set(populated, "Count", 1);
        generated.Set(populated, "ActivatedAt", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        generated.Set(populated, "ChannelNote", "sent");
        generated.Set(populated, "Extra", 1);
        var populatedResult = generated.Validate(populated);

        ValidationAssert.NoErrorFor(populatedResult, "ActivatedAt");
        ValidationAssert.NoErrorFor(populatedResult, "ChannelNote");
        ValidationAssert.NoErrorFor(populatedResult, "Extra");
    }

    [TestMethod]
    public void ConditionalRange_UsesInclusiveBoundsOnlyWhenTheConditionMatches() {
        var generated = BuildGenerated(SOURCE);

        var business = generated.Create();
        generated.Set(business, "Mode", "Business");
        generated.Set(business, "Quantity", 1);
        ValidationAssert.NoErrorFor(generated.Validate(business), "Quantity");

        generated.Set(business, "Quantity", 10);
        ValidationAssert.NoErrorFor(generated.Validate(business), "Quantity");

        generated.Set(business, "Quantity", 0);
        ValidationAssert.HasError(generated.Validate(business), "Quantity");

        generated.Set(business, "Quantity", 11);
        ValidationAssert.HasError(generated.Validate(business), "Quantity");

        var standard = generated.Create();
        generated.Set(standard, "Mode", "Standard");
        generated.Set(standard, "Quantity", 0);
        ValidationAssert.NoErrorFor(generated.Validate(standard), "Quantity");
    }

    [TestMethod]
    public void Descriptor_ExposesTheConditionalRangeAndConditionFlag() {
        var generated = BuildGenerated(SOURCE);
        var validator = Activator.CreateInstance(generated.ValidatorType)!;
        var descriptor = ((IValidationDescriptorProvider)validator).Describe();

        var quantity = descriptor.RulesForMember("Quantity").Single();
        MSTestAssertions.That(() => quantity.Kind.Should().Be("InclusiveBetween"));
        MSTestAssertions.That(() => quantity.HasCondition.Should().BeTrue());
        MSTestAssertions.That(() => quantity.Parameters["Min"].Should().Be(1));
        MSTestAssertions.That(() => quantity.Parameters["Max"].Should().Be(10));

        var name = descriptor.RulesForMember("Name").Single();
        MSTestAssertions.That(() => name.Kind.Should().Be("NotEmpty"));
        MSTestAssertions.That(() => name.HasCondition.Should().BeTrue());
    }

    [TestMethod]
    public void ClientRules_MarkConditionalRulesAsServerOnly() {
        var generated = BuildGenerated(SOURCE);
        var validator = Activator.CreateInstance(generated.ValidatorType)!;
        var descriptor = ((IValidationDescriptorProvider)validator).Describe();
        var json = ClientRulesWriter.WriteToString([descriptor]);

        MSTestAssertions.That(() => json.Should().Contain("\"name\": \"Name\""));
        MSTestAssertions.That(() => json.Should().Contain("\"hasCondition\": true"));
        MSTestAssertions.That(() => json.Should().Contain("\"serverOnly\": true"));
    }

    [TestMethod]
    public void GeneratedSource_UsesTheConditionalWrapper() {
        var run = RunGenerator(SOURCE);
        var text = string.Join(Environment.NewLine, run.GeneratedTrees.Select(static tree => tree.ToString()));

        MSTestAssertions.That(() => text.Should().Contain("ConditionalRule"));
        MSTestAssertions.That(() => text.Should().Contain("InclusiveBetweenRule"));
        MSTestAssertions.That(() => text.Should().Contain("x.Mode == @\"Express\""));
    }

    [TestMethod]
    public void InvalidConditionalAttributes_ProduceActionableDiagnostics() {
        const string source = """
            using Jeninnet.Validation.Attributes;

            namespace ConditionalValueModels;

            [Validate("ConditionalValueModels")]
            public sealed class BrokenOrder {
                public bool Flag { get; set; }
                public string? Secret { private get; set; }

                [RequiredIf("Missing", 1)]
                public string? MissingTarget { get; set; }

                [RequiredIf(nameof(Self), 1)]
                public string? Self { get; set; }

                [RequiredIf(nameof(Flag), true), RequiredIf(nameof(Flag), false)]
                public string? Duplicate { get; set; }

                [RequiredIf(nameof(Flag), "yes")]
                public string? BadValue { get; set; }

                [RequiredIf(nameof(Secret), "x")]
                public string? Unreadable { get; set; }
            }
            """;

        var diagnostics = RunGenerator(source).Diagnostics;

        MSTestAssertions.That(() => diagnostics.Count(static diagnostic => diagnostic.Id == "VAL0029").Should().Be(3));
        MSTestAssertions.That(() => diagnostics.Any(static diagnostic => diagnostic.Id == "VAL0030").Should().BeTrue());
        MSTestAssertions.That(() => diagnostics.Any(static diagnostic => diagnostic.Id == "VAL0031").Should().BeTrue());
    }

    [TestMethod]
    public void ValidConditionalAttributes_ProduceNoNewDiagnostics() {
        var diagnostics = RunGenerator(SOURCE).Diagnostics;

        MSTestAssertions.That(() => diagnostics.Any(static diagnostic =>
            diagnostic.Id is "VAL0029" or "VAL0030" or "VAL0031").Should().BeFalse());
    }

    private static GeneratorRun RunGenerator(string source) {
        var compilation = CreateCompilation(source, $"ConditionalValueAssembly_{Guid.NewGuid():N}");
        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var result = driver.GetRunResult();

        return new GeneratorRun(result.GeneratedTrees, [.. result.Diagnostics]);
    }

    private static GeneratedOrder BuildGenerated(string source) {
        var compilation = CreateCompilation(source, $"ConditionalValueAssembly_{Guid.NewGuid():N}");
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
        var modelType = assembly.GetType("ConditionalValueModels.Order", throwOnError: true)!;
        var validatorType = assembly.GetType("ConditionalValueModels.OrderValidator", throwOnError: true)!;
        var validator = Activator.CreateInstance(validatorType)!;

        return new GeneratedOrder(assembly, modelType, validatorType, validator);
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

    private sealed class GeneratedOrder(
        Assembly assembly,
        Type modelType,
        Type validatorType,
        object validator) {
        public Assembly Assembly { get; } = assembly;

        public Type ValidatorType { get; } = validatorType;

        public object Create() => Activator.CreateInstance(modelType)!;

        public void Set(object model, string property, object? value) =>
            modelType.GetProperty(property)!.SetValue(model, value);

        public ValidationResult Validate(object model) =>
            (ValidationResult)ValidatorType.GetMethod("Validate", [modelType])!.Invoke(validator, [model])!;
    }

    private sealed record GeneratorRun(
        IEnumerable<SyntaxTree> GeneratedTrees,
        IReadOnlyList<Diagnostic> Diagnostics);
}
