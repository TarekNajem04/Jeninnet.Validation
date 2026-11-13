//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Verifies that a condition on a generated <c>[ForEach]</c> container preserves element paths and
/// gates both validation paths.
/// </summary>
[TestClass]
public sealed class GeneratedCollectionConditionTests {
    private const string SOURCE = """
        using System;
        using System.Collections.Generic;
        using Jeninnet.Validation.Attributes;

        namespace CollectionConditionParity;

        [Validate("CollectionConditionParity")]
        public sealed class Line {
            [NotEmpty] public string? Name { get; set; }
        }

        [Validate("CollectionConditionParity")]
        public sealed class Order {
            public bool Enabled { get; set; }

            public bool ShouldValidateLines(Order instance) => instance.Enabled;

            [ForEach, WhenCondition(nameof(ShouldValidateLines))]
            public List<Line>? Lines { get; set; }
        }
        """;

    [TestMethod]
    public void GeneratedForEachCondition_False_SkipsValidation() {
        var (validator, order) = Build(enabled: false);

        var result = Validate(validator, order);

        MSTestAssertions.That(() => result.IsValid.Should().BeTrue());
    }

    [TestMethod]
    public void GeneratedForEachCondition_True_ReportsElementPath() {
        var (validator, order) = Build(enabled: true);

        var result = Validate(validator, order);

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("Lines[0].Name"));
    }

    [TestMethod]
    public async Task GeneratedForEachCondition_AsyncPathMatchesSyncAsync() {
        var (validator, order) = Build(enabled: true);

        var asyncMethod = validator.GetType().GetMethod(
            "ValidateAsync",
            [order.GetType(), typeof(CancellationToken)])!;
        var task = (ValueTask<ValidationResult>)asyncMethod.Invoke(
            validator,
            [order, CancellationToken.None])!;
        var result = await task;

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("Lines[0].Name"));
    }

    private static ValidationResult Validate(object validator, object order) {
        var method = validator.GetType().GetMethod("Validate", [order.GetType()])!;
        return (ValidationResult)method.Invoke(validator, [order])!;
    }

    private (object Validator, object Order) Build(bool enabled) {
        var assembly = BuildAssembly();
        var lineType = assembly.GetType("CollectionConditionParity.Line", throwOnError: true)!;
        var orderType = assembly.GetType("CollectionConditionParity.Order", throwOnError: true)!;
        var lineValidatorType = assembly.GetType("CollectionConditionParity.LineValidator", throwOnError: true)!;
        var orderValidatorType = assembly.GetType("CollectionConditionParity.OrderValidator", throwOnError: true)!;

        var line = Activator.CreateInstance(lineType)!;
        lineType.GetProperty("Name")!.SetValue(line, string.Empty);

        var listType = typeof(List<>).MakeGenericType(lineType);
        var lines = (IList)Activator.CreateInstance(listType)!;
        lines.Add(line);

        var order = Activator.CreateInstance(orderType)!;
        orderType.GetProperty("Enabled")!.SetValue(order, enabled);
        orderType.GetProperty("Lines")!.SetValue(order, lines);

        var lineValidator = Activator.CreateInstance(lineValidatorType)!;
        var orderValidator = Activator.CreateInstance(orderValidatorType, [lineValidator])!;

        return (orderValidator, order);
    }

    private Assembly BuildAssembly() {
        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        var syntaxTree = CSharpSyntaxTree.ParseText(SOURCE, cancellationToken: cancellationToken);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            $"GeneratedCollectionConditionAssembly_{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, cancellationToken);
        var runResult = driver.GetRunResult();

        MSTestAssertions.That(() => (runResult.GeneratedTrees.Length > 0).Should().BeTrue(
            string.Join(Environment.NewLine, runResult.Diagnostics)));

        var generatedCompilation = compilation.AddSyntaxTrees(runResult.GeneratedTrees);

        using var stream = new MemoryStream();
        var emit = generatedCompilation.Emit(stream, cancellationToken: cancellationToken);

        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        return Assembly.Load(stream.ToArray());
    }

    public TestContext? TestContext { get; set; }
}
