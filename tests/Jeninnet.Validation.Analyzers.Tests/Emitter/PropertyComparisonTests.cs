//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.Emitter;

[TestClass]
public class PropertyComparisonTests {
    [TestMethod]
    public void PropertyEqualRuleEmitterTest() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class PasswordModel
            {
                public string Password { get; set; }

                [PropertyEqual("Password")]
                public string ConfirmPassword { get; set; }
            }
            """;

        var generatedCode = GetGeneratedCode(source, "TestNamespace.PasswordModel");

        MSTestAssertions.That(() => generatedCode.Should().Contain("new PropertyEqualRule<PasswordModel, string>("));
        MSTestAssertions.That(() => generatedCode.Should().Contain("ValidatorFactory.Accessor<PasswordModel, string>(x => x.ConfirmPassword, \"ConfirmPassword\")"));
        MSTestAssertions.That(() => generatedCode.Should().Contain("ValidatorFactory.Accessor<PasswordModel, string>(x => x.Password, \"Password\")"));
    }

    [TestMethod]
    public void PropertyEqualRule_MissingProperty_ShouldReportDiagnostic() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class InvalidModel
            {
                [PropertyEqual("NonExistent")]
                public string ConfirmPassword { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        MSTestAssertions.That(() => diagnostics.Should().Contain(d => d.Id == "VAL0015" && d.GetMessage(CultureInfo.InvariantCulture).Contains("NonExistent")));
    }

    private static ImmutableArray<Diagnostic> GetDiagnostics(string source) {
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

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var _, out var diagnostics);

        return diagnostics;
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
