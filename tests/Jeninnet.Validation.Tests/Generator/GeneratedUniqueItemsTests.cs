//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Verifies the generated <c>[UniqueItems]</c> rule and its parity with the fluent rule.
/// </summary>
[TestClass]
public sealed class GeneratedUniqueItemsTests {
    private const string SOURCE = """
        using System.Collections.Generic;
        using Jeninnet.Validation.Attributes;

        namespace UniqueItemsParity;

        [Validate("UniqueItemsParity")]
        public sealed class Basket {
            [UniqueItems]
            public List<int>? Numbers { get; set; }
        }
        """;

    [TestMethod]
    public void GeneratedUniqueItems_Duplicates_FailWithElementMember() {
        var (validator, model) = Build([1, 2, 2]);

        var result = Validate(validator, model);

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("Numbers"));
        MSTestAssertions.That(() => result.Errors[0].MessageKey.Should().Be("UniqueItems"));
    }

    [TestMethod]
    public void GeneratedUniqueItems_DistinctItems_Pass() {
        var (validator, model) = Build([1, 2, 3]);

        MSTestAssertions.That(() => Validate(validator, model).IsValid.Should().BeTrue());
    }

    [TestMethod]
    public void GeneratedUniqueItems_EmitsRuleAndDescriptor() {
        _ = BuildAssembly(out var sources);

        MSTestAssertions.That(() => sources.Should().Contain(static source => source.Contains(
            "UniqueItemsRule",
            StringComparison.Ordinal)));
    }

    private static ValidationResult Validate(object validator, object model) {
        var method = validator.GetType().GetMethod("Validate", [model.GetType()])!;
        return (ValidationResult)method.Invoke(validator, [model])!;
    }

    private (object Validator, object Model) Build(IList<int> numbers) {
        var assembly = BuildAssembly(out _);
        var modelType = assembly.GetType("UniqueItemsParity.Basket", throwOnError: true)!;
        var validatorType = assembly.GetType("UniqueItemsParity.BasketValidator", throwOnError: true)!;

        var listType = typeof(List<int>);
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach(var number in numbers) {
            list.Add(number);
        }

        var model = Activator.CreateInstance(modelType)!;
        modelType.GetProperty("Numbers")!.SetValue(model, list);

        return (Activator.CreateInstance(validatorType)!, model);
    }

    private Assembly BuildAssembly(out string[] sources) {
        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        var syntaxTree = CSharpSyntaxTree.ParseText(SOURCE, cancellationToken: cancellationToken);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            $"GeneratedUniqueItemsAssembly_{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, cancellationToken);
        var runResult = driver.GetRunResult();

        MSTestAssertions.That(() => (runResult.GeneratedTrees.Length > 0).Should().BeTrue(
            string.Join(Environment.NewLine, runResult.Diagnostics)));

        sources = [.. runResult.GeneratedTrees.Select(tree => tree.GetText(cancellationToken).ToString())];

        var generatedCompilation = compilation.AddSyntaxTrees(runResult.GeneratedTrees);

        using var stream = new MemoryStream();
        var emit = generatedCompilation.Emit(stream, cancellationToken: cancellationToken);

        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        return Assembly.Load(stream.ToArray());
    }

    public TestContext? TestContext { get; set; }
}
