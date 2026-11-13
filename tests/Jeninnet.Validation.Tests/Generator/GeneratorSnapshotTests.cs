//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Snapshot tests for <c>ValidatorGenerator</c>.
/// Each test compiles a minimal piece of annotated C# source in memory, runs the
/// generator, and then makes assertions about the generated output.
/// </summary>
[TestClass]
public sealed class GeneratorSnapshotTests {
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Runs the <c>ValidatorGenerator</c> against the supplied source text and returns
    /// the generated sources together with any diagnostics.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <returns>A tuple containing diagnostics and generated source strings.</returns>
    private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources)
        RunGenerator(string source) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain
                                  .GetAssemblies()
                                  .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                                  .Select(a => MetadataReference.CreateFromFile(a.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                         .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();

        var generatedSources = runResult.GeneratedTrees
                                        .Select(t => t.ToString())
                                        .ToArray();

        var diagnostics = runResult.Results
                                   .SelectMany(r => r.Diagnostics)
                                   .ToImmutableArray();

        return (diagnostics, generatedSources);
    }

    private static bool GeneratedSourceContains(string[] sources, string fragment) =>
        sources.Any(s => s.Contains(fragment, StringComparison.Ordinal));

    private CancellationToken TestCancellation => TestContext?.CancellationToken ?? CancellationToken.None;

    public TestContext? TestContext { get; set; }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public void SimpleClass_WithValidateAttribute_ProducesValidatorClass() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class Customer {
                [NotEmpty]
                public string Name { get; init; } = string.Empty;
            }
            """;

        var (_, sources) = RunGenerator(source);

        MSTestAssertions.That(() => (sources.Length > 0).Should().BeTrue("the generator must emit at least one validator"));

        MSTestAssertions.That(() => GeneratedSourceContains(sources, "CustomerValidator").Should().BeTrue(
            "Generator must emit a class named 'CustomerValidator'."));
    }

    [TestMethod]
    public void ClassWithNotEmptyString_EmitsNotEmptyRule() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class Invoice {
                [NotEmpty]
                public string Number { get; init; } = string.Empty;
            }
            """;

        var (_, sources) = RunGenerator(source);

        MSTestAssertions.That(() => (sources.Length > 0).Should().BeTrue("the generator must emit at least one validator"));

        MSTestAssertions.That(() => GeneratedSourceContains(sources, "NotEmptyRule").Should().BeTrue(
            "Generator must emit a NotEmptyRule for a [NotEmpty] string property."));
    }

    [TestMethod]
    public void ClassWithGreaterThan_EmitsGreaterThanRule() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class OrderLine {
                [GreaterThan(0)]
                public int Quantity { get; init; }
            }
            """;

        var (_, sources) = RunGenerator(source);

        MSTestAssertions.That(() => (sources.Length > 0).Should().BeTrue("the generator must emit at least one validator"));

        MSTestAssertions.That(() => GeneratedSourceContains(sources, "GreaterThanRule").Should().BeTrue(
            "Generator must emit a GreaterThanRule for a [GreaterThan] numeric property."));
    }

    [TestMethod]
    public void ClassWithNestedAttribute_EmitsNestedValidatorRule() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class Address {
                [NotEmpty] public string Street { get; init; } = string.Empty;
            }

            [Validate]
            public sealed class Order {
                [Nested]
                public Address? ShippingAddress { get; init; }
            }
            """;

        var (_, sources) = RunGenerator(source);

        MSTestAssertions.That(() => (sources.Length > 0).Should().BeTrue("the generator must emit at least one validator"));

        MSTestAssertions.That(() => GeneratedSourceContains(sources, "NestedValidatorRule").Should().BeTrue(
            "Generator must emit a NestedValidatorRule for a [Nested] property."));
    }

    [TestMethod]
    public void ClassWithForEach_EmitsForEachRule() {
        const string source = """
            using System.Collections.Generic;
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class Item {
                [NotEmpty] public string Code { get; init; } = string.Empty;
            }

            [Validate]
            public sealed class Basket {
                [ForEach]
                public List<Item> Items { get; init; } = new();
            }
            """;

        var (_, sources) = RunGenerator(source);

        MSTestAssertions.That(() => (sources.Length > 0).Should().BeTrue("the generator must emit at least one validator"));

        MSTestAssertions.That(() => GeneratedSourceContains(sources, "ForEachRule").Should().BeTrue(
            "Generator must emit a ForEachRule for a [ForEach] collection property."));
    }

    [TestMethod]
    public void ClassWithCountAttributes_EmitsCountRules() {
        const string source = """
            using System.Collections.Generic;
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class Bag {
                [Count(2, 10)] public List<string> Items { get; init; } = new();
                [MinCount(1)] public List<string> Tags { get; init; } = new();
                [MaxCount(5)] public List<string> Codes { get; init; } = new();
            }
            """;

        var (_, sources) = RunGenerator(source);

        MSTestAssertions.That(() => (sources.Length > 0).Should().BeTrue("the generator must emit at least one validator"));
        MSTestAssertions.That(() => GeneratedSourceContains(sources, "CountRule").Should().BeTrue(
            "Generator must emit a CountRule for a [Count] collection property."));
        MSTestAssertions.That(() => GeneratedSourceContains(sources, "MinCountRule").Should().BeTrue(
            "Generator must emit a MinCountRule for a [MinCount] collection property."));
        MSTestAssertions.That(() => GeneratedSourceContains(sources, "MaxCountRule").Should().BeTrue(
            "Generator must emit a MaxCountRule for a [MaxCount] collection property."));
    }

    [TestMethod]
    public void ClassWithDedicatedFormatAttributes_EmitsFormatRules() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class Document {
                [ExactLength(5)] public string Code { get; init; } = string.Empty;
                [Base64String] public string Payload { get; init; } = string.Empty;
                [FileExtensions(".pdf", "jpg")] public string FileName { get; init; } = string.Empty;
            }
            """;

        var (_, sources) = RunGenerator(source);

        MSTestAssertions.That(() => (sources.Length > 0).Should().BeTrue("the generator must emit at least one validator"));
        MSTestAssertions.That(() => GeneratedSourceContains(sources, "ExactLengthRule").Should().BeTrue(
            "Generator must emit an ExactLengthRule for [ExactLength]."));
        MSTestAssertions.That(() => GeneratedSourceContains(sources, "Base64StringRule").Should().BeTrue(
            "Generator must emit a Base64StringRule for [Base64String]."));
        MSTestAssertions.That(() => GeneratedSourceContains(sources, "FileExtensionsRule").Should().BeTrue(
            "Generator must emit a FileExtensionsRule for [FileExtensions]."));
    }

    [TestMethod]
    public void GeneratedValidator_DescribesRulesAtRuntime() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class User {
                [NotEmpty] public string Email { get; init; } = string.Empty;
                [Length(1, 100)] public string Name { get; init; } = string.Empty;
            }
            """;

        var (_, generatedSources) = RunGenerator(source);

        var syntaxTrees = new[] { CSharpSyntaxTree.ParseText(source, cancellationToken: TestCancellation) }
            .Concat(generatedSources.Select(text => CSharpSyntaxTree.ParseText(text, cancellationToken: TestCancellation)))
            .ToArray();

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>()
            .ToArray();

        var compilation = CSharpCompilation.Create(
            "GeneratedMetadataAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream, cancellationToken: TestCancellation);

        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        var assembly = Assembly.Load(stream.ToArray());
        var validatorType = assembly.GetTypes().Single(static type => type.Name == "UserValidator");
        var validator = (IValidationDescriptorProvider)Activator.CreateInstance(validatorType)!;
        var descriptor = validator.Describe();

        var emailRules = descriptor.RulesForMember("Email");
        MSTestAssertions.That(() => emailRules.Should().HaveCount(1));
        MSTestAssertions.That(() => emailRules[0].Kind.Should().Be("NotEmpty"));

        var nameRules = descriptor.RulesForMember("Name");
        MSTestAssertions.That(() => nameRules.Should().HaveCount(1));
        MSTestAssertions.That(() => nameRules[0].Kind.Should().Be("Length"));
        MSTestAssertions.That(() => nameRules[0].Parameters["Min"].Should().Be(1));
        MSTestAssertions.That(() => nameRules[0].Parameters["Max"].Should().Be(100));
    }

    [TestMethod]
    public void GeneratedCode_HasNullableEnable() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class Widget {
                [NotEmpty] public string Label { get; init; } = string.Empty;
            }
            """;

        var (_, sources) = RunGenerator(source);

        MSTestAssertions.That(() => (sources.Length > 0).Should().BeTrue("the generator must emit at least one validator"));

        MSTestAssertions.That(() => GeneratedSourceContains(sources, "#nullable enable").Should().BeTrue(
            "Generated code must include '#nullable enable'."));
    }

    [TestMethod]
    public void GeneratedCode_HasAutoGeneratedComment() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class Tag {
                [NotEmpty] public string Value { get; init; } = string.Empty;
            }
            """;

        var (_, sources) = RunGenerator(source);

        MSTestAssertions.That(() => (sources.Length > 0).Should().BeTrue("the generator must emit at least one validator"));

        MSTestAssertions.That(() => GeneratedSourceContains(sources, "auto-generated").Should().BeTrue(
            "Generated code must begin with an auto-generated comment."));
    }

    [TestMethod]
    public void ClassWithNoValidateAttribute_ProducesNoOutput() {
        const string source = """
            namespace MyApp;
            public sealed class Plain {
                public string Name { get; init; } = string.Empty;
            }
            """;

        var (_, sources) = RunGenerator(source);

        MSTestAssertions.That(() => sources.Should().BeEmpty("Generator must not emit any code for types without [Validate]."));
    }

    [TestMethod]
    public void DerivedClass_IncludesBaseClassAndOwnRules() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public class BaseModel {
                [NotEmpty] public string Name { get; set; } = string.Empty;
            }

            [Validate]
            public sealed class DerivedModel : BaseModel {
                [InclusiveBetween(1, 3)] public int Count { get; set; }
            }
            """;

        var (_, sources) = RunGenerator(source);
        var derived = sources.Single(static sourceText =>
            sourceText.Contains("class DerivedModelValidator", StringComparison.Ordinal));

        MSTestAssertions.That(() => derived.Contains("x => x.Name", StringComparison.Ordinal).Should().BeTrue(
            "The derived validator must include the base property rules."));
        MSTestAssertions.That(() => derived.Contains("x => x.Count", StringComparison.Ordinal).Should().BeTrue(
            "The derived validator must include its own property rules."));
    }

    [TestMethod]
    public void DerivedClass_DoesNotDuplicateOverriddenBaseRule() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public class BaseModel {
                [NotEmpty] public virtual string Name { get; set; } = string.Empty;
            }

            [Validate]
            public sealed class DerivedModel : BaseModel {
                public override string Name { get; set; } = string.Empty;
            }
            """;

        var (_, sources) = RunGenerator(source);
        var derived = sources.Single(static sourceText =>
            sourceText.Contains("class DerivedModelValidator", StringComparison.Ordinal));

        var accessorCount = derived.Split("x => x.Name", StringSplitOptions.None).Length - 1;
        MSTestAssertions.That(() => accessorCount.Should().Be(1));
    }

    [TestMethod]
    public void InterfaceWithValidateAttribute_ProducesValidator() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public interface IContact {
                [NotEmpty] string Name { get; }
            }
            """;

        var (_, sources) = RunGenerator(source);
        var validator = sources.Single(static sourceText =>
            sourceText.Contains("class IContactValidator", StringComparison.Ordinal));

        MSTestAssertions.That(() => validator.Contains("x => x.Name", StringComparison.Ordinal).Should().BeTrue(
            "The interface validator must include the interface property rules."));
    }

    [TestMethod]
    public void ClassImplementingAnnotatedInterface_InheritsInterfaceRules() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public interface IContact {
                [NotEmpty] string Name { get; }
            }

            [Validate]
            public sealed class Customer : IContact {
                public string Name { get; set; } = string.Empty;
            }
            """;

        var (_, sources) = RunGenerator(source);
        var customer = sources.Single(static sourceText =>
            sourceText.Contains("class CustomerValidator", StringComparison.Ordinal));

        MSTestAssertions.That(() => customer.Contains("NotEmptyRule", StringComparison.Ordinal).Should().BeTrue(
            "A class implementing an annotated interface must inherit the interface rules."));
    }

    [TestMethod]
    public void BaseValidateAttribute_DoesNotGenerateDerivedValidator() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public class BaseModel {
                [NotEmpty] public string Name { get; set; } = string.Empty;
            }

            public sealed class DerivedModel : BaseModel {
                public string Code { get; set; } = string.Empty;
            }
            """;

        var (_, sources) = RunGenerator(source);

        MSTestAssertions.That(() => sources.Any(static sourceText =>
            sourceText.Contains("class DerivedModelValidator", StringComparison.Ordinal)).Should().BeFalse(
            "The generator must require an explicit [Validate] on the derived type."));
    }

    [TestMethod]
    public void NoGeneratorDiagnosticErrors_ForValidInput() {
        const string source = """
            using Jeninnet.Validation.Attributes;
            namespace MyApp;

            [Validate]
            public sealed class Invoice {
                [NotEmpty] public string Number { get; init; } = string.Empty;
                [GreaterThan(0)] public decimal Amount { get; init; }
            }
            """;

        var (diagnostics, sources) = RunGenerator(source);

        MSTestAssertions.That(() => (sources.Length > 0).Should().BeTrue("the generator must emit at least one validator"));

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        MSTestAssertions.That(() => errors.Should().BeEmpty(
            $"Generator must not produce error diagnostics for valid input. Errors: {string.Join(", ", errors.Select(e => e.GetMessage(CultureInfo.InvariantCulture)))}"));
    }
}
