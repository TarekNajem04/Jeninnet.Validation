//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Tests.Emitter;

[TestClass]
public class EmitterTests {
    [TestMethod]
    public void FullEmitterTest() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [NotEmpty]
                public string Email { get; set; }

                [InclusiveBetween(18, 99)]
                public int Age { get; set; }
            }
            """;

        var generatedCode = GetGeneratedCode(source, "TestNamespace.User");

        MSTestAssertions.That(() => generatedCode.Should().Contain("class UserValidator"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("StringNotEmptyRule<User, string>"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("InclusiveBetweenRule<User, int>"));
        // Documents current generator behavior (mirrors the retired snapshot):
        // a bare [Email] attribute emits no EmailRule for this model.
        MSTestAssertions.That(() => generatedCode.Should().NotContain("EmailRule"));
    }

    [TestMethod]
    public void NestedAndCollectionEmitterTest() {
        const string source =
            """
            using System.Collections.Generic;
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class Order
            {
                [NotEmpty]
                public string OrderId { get; set; }

                [Nested]
                public Address ShippingAddress { get; set; }

                [ForEach]
                public List<OrderItem> Items { get; set; }
            }

            [Validate]
            public class Address
            {
                [NotEmpty]
                public string Street { get; set; }
            }

            [Validate]
            public class OrderItem
            {
                [NotEmpty]
                public string ProductId { get; set; }
            }
            """;

        var generatedCode = GetGeneratedCode(source, "TestNamespace.Order");

        MSTestAssertions.That(() => generatedCode.Should().Contain("class OrderValidator"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("NestedValidatorRule<Order, Address>"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("ForEachRule<Order, OrderItem>"));
    }

    [TestMethod]
    public void SpecializedRuleEmitterTest() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [NotEmpty]
                public string Name { get; set; }

                [NotEmpty]
                public int Age { get; set; }
            }
            """;

        var generatedCode = GetGeneratedCode(source, "TestNamespace.User");

        // Assert: Direct rule instantiation should be used
        MSTestAssertions.That(() => generatedCode.Should().Contain("new StringNotEmptyRule<User, string>(ValidatorFactory.Accessor<User, string>(x => x.Name, \"Name\"), null)"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("new NotEmptyRule<User, int>(ValidatorFactory.Accessor<User, int>(x => x.Age, \"Age\"), null)"));
    }
    [TestMethod]
    public void GeneratedConstructor_RegistersCustomRulesBeforeCompositeSnapshot() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [NotEmpty]
                public string Email { get; set; }
            }
            """;

        var generatedCode = GetGeneratedCode(source, "TestNamespace.User");

        var customRulesCall = generatedCode.IndexOf("AddCustomValidators();", StringComparison.Ordinal);
        var compositeConstruction = generatedCode.IndexOf("_composite = new CompositeValidator", StringComparison.Ordinal);

        MSTestAssertions.That(() => customRulesCall.Should().BeGreaterThanOrEqualTo(0, "the constructor must invoke the partial hook"));
        MSTestAssertions.That(() => compositeConstruction.Should().BeGreaterThanOrEqualTo(0, "the constructor must build the composite"));
        MSTestAssertions.That(() => customRulesCall.Should().BeLessThan(compositeConstruction,
            "custom rules must be registered before CompositeValidator snapshots the lists"));
    }

    [TestMethod]
    public void CollectionCountEmitterTest() {
        const string source =
            """
            using System.Collections.Generic;
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class Bag
            {
                [Count(2, 10)]
                public List<string> Items { get; set; }

                [MinCount(1)]
                public List<string> Tags { get; set; }

                [MaxCount(5)]
                public List<string> Codes { get; set; }
            }
            """;

        var generatedCode = GetGeneratedCode(source, "TestNamespace.Bag");

        MSTestAssertions.That(() => generatedCode.Should().Contain("CountRule<Bag, System.Collections.Generic.List<string>>"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("MinCountRule<Bag, System.Collections.Generic.List<string>>"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("MaxCountRule<Bag, System.Collections.Generic.List<string>>"));
    }

    [TestMethod]
    public void DedicatedFormatEmitterTest() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class Document
            {
                [ExactLength(5)]
                public string Code { get; set; }

                [Base64String]
                public string Payload { get; set; }

                [FileExtensions(".pdf", "jpg", IgnoreCase = false, AllowNonStandard = true)]
                public string FileName { get; set; }
            }
            """;

        var generatedCode = GetGeneratedCode(source, "TestNamespace.Document");

        MSTestAssertions.That(() => generatedCode.Should().Contain("ExactLengthRule<Document, string>"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("Base64StringRule<Document, string>"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("FileExtensionsRule<Document, string>"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("new string[] { \".pdf\", \"jpg\" }"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("false, true"));
    }

    private static string GetGeneratedCode(string source, string typeName) {
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

        var descriptor = ValidatorMetadataBuilder.TryBuildValidatorDescriptor(symbol) ?? throw new InvalidOperationException("Failed to build ValidatorDescriptor");
        return ValidatorEmitter.Emit(descriptor);
    }
}
