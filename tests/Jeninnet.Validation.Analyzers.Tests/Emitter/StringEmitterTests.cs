//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Tests.Emitter;

[TestClass]
public class StringEmitterTests {
    [TestMethod]
    public void StringRuleEmitterTest() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [MaximumLength(50)]
                public string Name { get; set; }

                [MinimumLength(10)]
                public string Bio { get; set; }

                [CreditCard]
                public string CardNumber { get; set; }
            }
            """;

        var generatedCode = GetGeneratedCode(source, "TestNamespace.User");

        MSTestAssertions.That(() => generatedCode.Should().Contain("new LengthRule<User, string>(ValidatorFactory.Accessor<User, string>(x => x.Name, \"Name\"), 0, 50, null)"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("new LengthRule<User, string>(ValidatorFactory.Accessor<User, string>(x => x.Bio, \"Bio\"), 10, 2147483647, null)"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("new CreditCardRule<User, string>(ValidatorFactory.Accessor<User, string>(x => x.CardNumber, \"CardNumber\"), null)"));
    }

    [TestMethod]
    public void RegexRuleEmitter_EmitsTimeout() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class User
            {
                [Regex("^[A-Z]{3}$")]
                public string Code { get; set; }
            }
            """;

        var generatedCode = GetGeneratedCode(source, "TestNamespace.User");

        MSTestAssertions.That(() => generatedCode.Should().Contain("System.TimeSpan.FromSeconds(1)"));
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
