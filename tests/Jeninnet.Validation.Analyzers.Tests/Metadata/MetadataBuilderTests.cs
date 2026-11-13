//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Tests.Metadata;

[TestClass]
public class MetadataBuilderTests {
    [TestMethod]
    public void FullMetadataExtractionTest() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [NotEmpty]
                [Email]
                public string Email { get; set; }

                [InclusiveBetween(18, 99)]
                public int Age { get; set; }
            }
            """;

        var descriptor = GetDescriptor(source, "TestNamespace.User");

        MSTestAssertions.That(() => descriptor.Should().NotBeNull());
        MSTestAssertions.That(() => descriptor.TargetTypeName.Should().Contain("TestNamespace.User"));
        MSTestAssertions.That(() => descriptor.Properties.Should().HaveCount(2));
        MSTestAssertions.That(() => descriptor.Rules.Select(r => r.Kind).Should().Contain(RuleKind.StringNotEmpty));
        MSTestAssertions.That(() => descriptor.Rules.Select(r => r.Kind).Should().Contain(RuleKind.Email));
        MSTestAssertions.That(() => descriptor.Rules.Select(r => r.Kind).Should().Contain(RuleKind.InclusiveBetween));
    }

    [TestMethod]
    public void CollectionCountMetadataExtractionTest() {
        const string source =
            """
            using System.Collections.Generic;
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class Bag
            {
                [Count(2, 10, "items")]
                public List<string> Items { get; set; }

                [MinCount(1)]
                public List<string> Tags { get; set; }

                [MaxCount(5)]
                public List<string> Codes { get; set; }
            }
            """;

        var descriptor = GetDescriptor(source, "TestNamespace.Bag");

        MSTestAssertions.That(() => descriptor.Rules.Select(r => r.Kind).Should().Contain(RuleKind.Count));
        MSTestAssertions.That(() => descriptor.Rules.Select(r => r.Kind).Should().Contain(RuleKind.MinCount));
        MSTestAssertions.That(() => descriptor.Rules.Select(r => r.Kind).Should().Contain(RuleKind.MaxCount));

        var count = descriptor.Rules.OfType<ParametricRuleDescriptor>().Single(r => r.Kind == RuleKind.Count);
        MSTestAssertions.That(() => count.Parameters[0].Value.Should().Be("2"));
        MSTestAssertions.That(() => count.Parameters[1].Value.Should().Be("10"));
        MSTestAssertions.That(() => count.Options.Message.Should().Be("items"));

        var min = descriptor.Rules.OfType<ParametricRuleDescriptor>().Single(r => r.Kind == RuleKind.MinCount);
        MSTestAssertions.That(() => min.Parameters[0].Value.Should().Be("1"));

        var max = descriptor.Rules.OfType<ParametricRuleDescriptor>().Single(r => r.Kind == RuleKind.MaxCount);
        MSTestAssertions.That(() => max.Parameters[0].Value.Should().Be("5"));
    }

    [TestMethod]
    public void DedicatedFormatMetadataExtractionTest() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class Document
            {
                [ExactLength(5, "code")]
                public string Code { get; set; }

                [Base64String]
                public string Payload { get; set; }

                [FileExtensions(".pdf", "jpg", IgnoreCase = false, AllowNonStandard = true, Message = "file")]
                public string FileName { get; set; }
            }
            """;

        var descriptor = GetDescriptor(source, "TestNamespace.Document");

        MSTestAssertions.That(() => descriptor.Rules.Select(r => r.Kind).Should().Contain(RuleKind.ExactLength));
        MSTestAssertions.That(() => descriptor.Rules.Select(r => r.Kind).Should().Contain(RuleKind.Base64String));
        MSTestAssertions.That(() => descriptor.Rules.Select(r => r.Kind).Should().Contain(RuleKind.FileExtensions));

        var exact = descriptor.Rules.OfType<ParametricRuleDescriptor>().Single(r => r.Kind == RuleKind.ExactLength);
        MSTestAssertions.That(() => exact.Parameters[0].Value.Should().Be("5"));
        MSTestAssertions.That(() => exact.Options.Message.Should().Be("code"));

        var extensions = descriptor.Rules.OfType<ParametricRuleDescriptor>().Single(r => r.Kind == RuleKind.FileExtensions);
        MSTestAssertions.That(() => extensions.Parameters[0].Value.Should().Be(".pdf,jpg"));
        MSTestAssertions.That(() => extensions.Parameters[1].Value.Should().Be("false"));
        MSTestAssertions.That(() => extensions.Parameters[2].Value.Should().Be("true"));
        MSTestAssertions.That(() => extensions.Options.Message.Should().Be("file"));
    }

    [TestMethod]
    public void ScalePrecisionIgnoreTrailingZerosMetadataExtractionTest() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;
            using Jeninnet.Validation.Attributes.Common;

            namespace TestNamespace;

            [Validate]
            public class Price
            {
                [ScalePrecision(5, 2, IgnoreTrailingZeros = true)]
                public decimal Amount { get; set; }
            }
            """;

        var descriptor = GetDescriptor(source, "TestNamespace.Price");
        var rule = descriptor.Rules.OfType<ParametricRuleDescriptor>()
                                .Single(r => r.Kind == RuleKind.ScalePrecision);

        MSTestAssertions.That(() => rule.Parameters[0].Value.Should().Be("5"));
        MSTestAssertions.That(() => rule.Parameters[1].Value.Should().Be("2"));
        MSTestAssertions.That(() => rule.Parameters[2].Value.Should().Be("true"));
    }

    private static ValidatorDescriptor GetDescriptor(string source, string typeName) {
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

        var symbol = compilation.GetTypeByMetadataName(typeName) ?? throw new ArgumentException($"Type {typeName} not found in compilation");

        return ValidatorMetadataBuilder.TryBuildValidatorDescriptor(symbol) ?? throw new InvalidOperationException("Failed to build ValidatorDescriptor");
    }
}
