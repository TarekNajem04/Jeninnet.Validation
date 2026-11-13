//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Compares a generated <c>[ForEach]</c> container against a hand-written fluent collection
/// validator, including the collection index parameter on child errors.
/// </summary>
[TestClass]
public sealed class GeneratedCollectionElementTests {
    private const string SOURCE = """
        using System;
        using System.Collections.Generic;
        using Jeninnet.Validation.Attributes;
        using Jeninnet.Validation.Fluent;

        namespace CollectionParity;

        [Validate("CollectionParity")]
        public sealed class OrderLine {
            [NotEmpty] public string? Name { get; set; }
            [GreaterThan(0)] public int Quantity { get; set; }
        }

        [Validate("CollectionParity")]
        public sealed class Order {
            [ForEach] public List<OrderLine>? Lines { get; set; }
        }

        public sealed class OrderLineFluentValidator : Validator<OrderLine> {
            protected override void Configure(ValidatorRuleBuilder<OrderLine> builder) {
                ArgumentNullException.ThrowIfNull(builder);

                builder.RuleFor(static x => x.Name, nameof(OrderLine.Name)).NotEmpty();
                builder.RuleFor(static x => x.Quantity, nameof(OrderLine.Quantity)).GreaterThan(0);
            }
        }

        public sealed class OrderFluentValidator : Validator<Order> {
            protected override void Configure(ValidatorRuleBuilder<Order> builder) {
                ArgumentNullException.ThrowIfNull(builder);

                builder.RuleFor(static x => x.Lines, nameof(Order.Lines))
                       .ForEach(new OrderLineFluentValidator());
            }
        }
        """;

    [TestMethod]
    public void GeneratedAndFluentCollectionValidators_ProduceIdenticalResults() {
        var assembly = BuildAssembly();
        var orderType = assembly.GetType("CollectionParity.Order", throwOnError: true)!;
        var lineType = assembly.GetType("CollectionParity.OrderLine", throwOnError: true)!;
        var generatedValidatorType = assembly.GetType("CollectionParity.OrderValidator", throwOnError: true)!;
        var generatedLineValidatorType = assembly.GetType("CollectionParity.OrderLineValidator", throwOnError: true)!;
        var fluentValidatorType = assembly.GetType("CollectionParity.OrderFluentValidator", throwOnError: true)!;

        var generatedLineValidator = Activator.CreateInstance(generatedLineValidatorType)!;
        var generatedValidator = Activator.CreateInstance(generatedValidatorType, [generatedLineValidator])!;
        var fluentValidator = Activator.CreateInstance(fluentValidatorType)!;
        var order = CreateOrder(orderType, lineType);

        var generatedResult = (ValidationResult)generatedValidatorType
            .GetMethod("Validate", [orderType])!
            .Invoke(generatedValidator, [order])!;
        var fluentResult = (ValidationResult)fluentValidatorType
            .GetMethod("Validate", [orderType])!
            .Invoke(fluentValidator, [order])!;

        MSTestAssertions.That(() => (generatedResult.IsValid == fluentResult.IsValid).Should().BeTrue());
        MSTestAssertions.That(() => generatedResult.Errors.Count.Should().Be(fluentResult.Errors.Count));

        for(var i = 0; i < generatedResult.Errors.Count; i++) {
            MSTestAssertions.That(() => generatedResult.Errors[i].MemberName.Should().Be(fluentResult.Errors[i].MemberName));
            MSTestAssertions.That(() => generatedResult.Errors[i].MessageKey.Should().Be(fluentResult.Errors[i].MessageKey));
            MSTestAssertions.That(() => generatedResult.Errors[i].ErrorCode.Should().Be(fluentResult.Errors[i].ErrorCode));
            MSTestAssertions.That(() => generatedResult.Errors[i].MessageParameters!["CollectionIndex"]
                .Should().Be(fluentResult.Errors[i].MessageParameters!["CollectionIndex"]));
        }
    }

    private static object CreateOrder(Type orderType, Type lineType) {
        var listType = typeof(List<>).MakeGenericType(lineType);
        var lines = (IList)Activator.CreateInstance(listType)!;

        var first = Activator.CreateInstance(lineType)!;
        lineType.GetProperty("Name")!.SetValue(first, string.Empty);
        lineType.GetProperty("Quantity")!.SetValue(first, 0);
        lines.Add(first);

        var second = Activator.CreateInstance(lineType)!;
        lineType.GetProperty("Name")!.SetValue(second, "valid");
        lineType.GetProperty("Quantity")!.SetValue(second, 0);
        lines.Add(second);

        var order = Activator.CreateInstance(orderType)!;
        orderType.GetProperty("Lines")!.SetValue(order, lines);
        return order;
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
            $"GeneratedCollectionParityAssembly_{Guid.NewGuid():N}",
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
