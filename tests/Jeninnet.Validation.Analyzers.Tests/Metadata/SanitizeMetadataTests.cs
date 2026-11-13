//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.Metadata;

[TestClass]
public class SanitizeMetadataTests {
    [TestMethod]
    public void PropertySanitize_ProducesOneStep() {
        var descriptor = GetDescriptor(
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [Sanitize(Trim = true)]
                [NotEmpty]
                public string Name { get; set; } = string.Empty;
            }
            """,
            "TestNamespace.User");

        MSTestAssertions.That(() => descriptor.Sanitizers.Should().HaveCount(1));

        var step = descriptor.Sanitizers![0];

        MSTestAssertions.That(() => step.PropertyName.Should().Be("Name"));
        MSTestAssertions.That(() => step.Trim.Should().BeTrue());
        MSTestAssertions.That(() => step.ToUpper.Should().BeFalse());
        MSTestAssertions.That(() => step.IsStringList.Should().BeFalse());
        MSTestAssertions.That(() => descriptor.Diagnostics.Should().BeEmpty());
    }

    [TestMethod]
    public void PropertySanitize_OverridesClassSanitize() {
        var descriptor = GetDescriptor(
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            [Sanitize(Trim = true)]
            public class User
            {
                [Sanitize(ToUpper = true, Culture = "tr-TR")]
                public string Code { get; set; } = string.Empty;

                public string Name { get; set; } = string.Empty;
            }
            """,
            "TestNamespace.User");

        MSTestAssertions.That(() => descriptor.Sanitizers.Should().HaveCount(2));

        var code = descriptor.Sanitizers!.Single(step => step.PropertyName == "Code");

        MSTestAssertions.That(() => code.ToUpper.Should().BeTrue());
        MSTestAssertions.That(() => code.Trim.Should().BeFalse());
        MSTestAssertions.That(() => code.Culture.Should().Be("tr-TR"));

        var name = descriptor.Sanitizers!.Single(step => step.PropertyName == "Name");

        MSTestAssertions.That(() => name.Trim.Should().BeTrue());
        MSTestAssertions.That(() => name.ToUpper.Should().BeFalse());
    }

    [TestMethod]
    public void BareSanitizeAttribute_ProducesNoStep() {
        var descriptor = GetDescriptor(
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [Sanitize]
                [NotEmpty]
                public string Name { get; set; } = string.Empty;
            }
            """,
            "TestNamespace.User");

        MSTestAssertions.That(() => descriptor.Sanitizers.Should().BeEmpty());
        MSTestAssertions.That(() => descriptor.Diagnostics.Should().BeEmpty());
    }

    [TestMethod]
    public void InitOnlyProperty_ReportsVAL0027() {
        var descriptor = GetDescriptor(
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [Sanitize(Trim = true)]
                [NotEmpty]
                public string Name { get; init; } = string.Empty;
            }
            """,
            "TestNamespace.User");

        var diagnostics = descriptor.Diagnostics!.Where(diagnostic => diagnostic.Descriptor.Id == "VAL0027").ToList();

        MSTestAssertions.That(() => diagnostics.Should().HaveCount(1));
        MSTestAssertions.That(() => diagnostics[0].Descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Error));
        MSTestAssertions.That(() => descriptor.Sanitizers.Should().BeEmpty());
    }

    [TestMethod]
    public void NonStringProperty_ReportsVAL0027() {
        var descriptor = GetDescriptor(
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class Order
            {
                [Sanitize(Trim = true)]
                public int Count { get; set; }
            }
            """,
            "TestNamespace.Order");

        var diagnostics = descriptor.Diagnostics!.Where(diagnostic => diagnostic.Descriptor.Id == "VAL0027").ToList();

        MSTestAssertions.That(() => diagnostics.Should().HaveCount(1));
        MSTestAssertions.That(() => descriptor.Sanitizers.Should().BeEmpty());
    }

    [TestMethod]
    public void UnknownCulture_ReportsVAL0027() {
        var descriptor = GetDescriptor(
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [Sanitize(ToUpper = true, Culture = "!")]
                public string Code { get; set; } = string.Empty;
            }
            """,
            "TestNamespace.User");

        var diagnostics = descriptor.Diagnostics!.Where(diagnostic => diagnostic.Descriptor.Id == "VAL0027").ToList();

        MSTestAssertions.That(() => diagnostics.Should().HaveCount(1));
        MSTestAssertions.That(() => descriptor.Sanitizers.Should().BeEmpty());
    }

    [TestMethod]
    public void Sanitize_EmitsMemberSanitizerAndWiresTheComposite() {
        var generatedCode = GetGeneratedCode(
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [Sanitize(Trim = true, ToUpper = true, Culture = "tr-TR")]
                [NotEmpty]
                public string Name { get; set; } = string.Empty;

                [NotEmpty]
                public string Plain { get; set; } = string.Empty;
            }
            """,
            "TestNamespace.User");

        MSTestAssertions.That(() => generatedCode.Should().Contain("_sanitizers = new()"));
        MSTestAssertions.That(() => generatedCode.Should().Contain(
            "Sanitizers.Member<User, string>(static x => x.Name, \"Name\", static (m, v) => m.Name = v!, Sanitize.Trim, Sanitize.ToUpper(System.Globalization.CultureInfo.GetCultureInfo(\"tr-TR\")))"));
        MSTestAssertions.That(() => generatedCode.Should().Contain(", _sanitizers);"));
        MSTestAssertions.That(() => generatedCode.Should().NotContain("x.Plain, \"Plain\", static (m, v)"));
    }

    [TestMethod]
    public void SanitizeList_EmitsEachElementWrapper() {
        var generatedCode = GetGeneratedCode(
            """
            using System.Collections.Generic;
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class Bag
            {
                [Sanitize(Trim = true)]
                public IList<string?> Tags { get; set; } = new List<string?>();
            }
            """,
            "TestNamespace.Bag");

        MSTestAssertions.That(() => generatedCode.Should().Contain("Sanitize.EachElement(Sanitize.Trim)"));
    }

    private static ValidatorDescriptor GetDescriptor(string source, string typeName) {
        var symbol = GetSymbol(source, typeName);

        return ValidatorMetadataBuilder.TryBuildValidatorDescriptor(symbol)
            ?? throw new InvalidOperationException("Failed to build ValidatorDescriptor");
    }

    private static string GetGeneratedCode(string source, string typeName) {
        var symbol = GetSymbol(source, typeName);
        var descriptor = ValidatorMetadataBuilder.TryBuildValidatorDescriptor(symbol)
            ?? throw new InvalidOperationException("Failed to build ValidatorDescriptor");

        return ValidatorEmitter.Emit(descriptor);
    }

    private static INamedTypeSymbol GetSymbol(string source, string typeName) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attributes.ValidateAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "System.Runtime").Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetTypeByMetadataName(typeName) ?? throw new ArgumentException($"Type {typeName} not found in compilation");
    }
}
