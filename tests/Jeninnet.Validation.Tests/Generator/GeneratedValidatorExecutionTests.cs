//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Exercises a real source-generated validator (not a hand-built composite) so the generated
/// execution surface stays interchangeable with the fluent validator.
/// </summary>
[TestClass]
public sealed class GeneratedValidatorExecutionTests {
    private const string SOURCE = """
        using System;
        using Jeninnet.Validation.Attributes;
        using Jeninnet.Validation.Core;
        using Jeninnet.Validation.Rules;

        namespace GeneratedExecution;

        [Validate("GeneratedExecution")]
        public sealed class Order {
            [NotEmpty] public string Name { get; set; } = string.Empty;
            [InclusiveBetween(18, 99)] public int Age { get; set; }
            [RuleSet("strict"), MinimumLength(5)] public string Code { get; set; } = string.Empty;
        }

        public sealed partial class OrderValidator {
            partial void AddCustomValidators() {
                _syncRules.Add(
                    new CustomRule<Order, string?>(
                        ValidatorFactory.Accessor<Order, string?>(
                            static x => x.Name,
                            nameof(Order.Name)),
                        static (_, context, errors) => {
                            if(context.ServiceProvider is not null) {
                                errors.Add(nameof(Order.Name), "service-provider-observed");
                            }
                        }));
            }
        }
        """;

    [TestMethod]
    public void GeneratedValidator_ExposesOptionsStrategyAndThrowApi() {
        var generated = BuildGeneratedValidator();

        MSTestAssertions.That(() => generated.ValidatorType.GetMethod(
            "Validate",
            [generated.ModelType, typeof(ValidationCallOptions)]).Should().NotBeNull());
        MSTestAssertions.That(() => generated.ValidatorType.GetMethod(
            "Validate",
            [generated.ModelType, typeof(Action<ValidationStrategy>)]).Should().NotBeNull());
        MSTestAssertions.That(() => generated.ValidatorType.GetMethod(
            "ValidateAndThrow",
            [generated.ModelType]).Should().NotBeNull());
        MSTestAssertions.That(() => generated.ValidatorType.GetProperty("ClassLevelCascadeMode").Should().NotBeNull());

        var optionsInterface = typeof(IValidationOptionsValidator<>).MakeGenericType(generated.ModelType);
        MSTestAssertions.That(() => optionsInterface.IsAssignableFrom(generated.ValidatorType).Should().BeTrue());
    }

    [TestMethod]
    public void GeneratedValidator_IncludePropertiesUsesGeneratedMemberRules() {
        var generated = BuildGeneratedValidator();
        var model = generated.CreateModel("", 10, "abc");

        var result = generated.ValidateWithOptions(
            model,
            new ValidationCallOptions { IncludeProperties = ["Name"] });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Name");
        ValidationAssert.NoErrorFor(result, "Age");
        ValidationAssert.NoErrorFor(result, "Code");
    }

    [TestMethod]
    public void GeneratedValidator_ExcludePropertiesSkipsGeneratedMemberRules() {
        var generated = BuildGeneratedValidator();
        var model = generated.CreateModel("valid", 10, "abc");

        var result = generated.ValidateWithOptions(
            model,
            new ValidationCallOptions { ExcludeProperties = ["Age"] });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.NoErrorFor(result, "Age");
        ValidationAssert.HasError(result, "Code");
    }

    [TestMethod]
    public void GeneratedValidator_RuleSetsSelectGeneratedRules() {
        var generated = BuildGeneratedValidator();
        var model = generated.CreateModel("valid", 20, "abc");

        var result = generated.ValidateWithOptions(
            model,
            new ValidationCallOptions { RuleSets = ["strict"] });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Code");
    }

    [TestMethod]
    public void GeneratedValidator_StrategyApiWorks() {
        var generated = BuildGeneratedValidator();
        var model = generated.CreateModel("", 10, "abc");

        var result = generated.ValidateWithStrategy(
            model,
            strategy => strategy.IncludeProperties("Age"));

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Age");
    }

    [TestMethod]
    public void GeneratedValidator_FailFastStopsAtFirstError() {
        var generated = BuildGeneratedValidator();
        var model = generated.CreateModel("", 10, "abc");

        var result = generated.ValidateWithOptions(
            model,
            new ValidationCallOptions { FailFast = true });

        ValidationAssert.ErrorCount(result, 1);
    }

    [TestMethod]
    public void GeneratedValidator_ClassLevelCascadeModeStopsAtFirstError() {
        var generated = BuildGeneratedValidator();
        var model = generated.CreateModel("", 10, "abc");

        generated.SetClassLevelCascadeMode(CascadeMode.Stop);
        var result = generated.ValidateBasic(model);

        ValidationAssert.ErrorCount(result, 1);
    }

    [TestMethod]
    public void GeneratedValidator_ServiceProviderFlowsToCustomRule() {
        var generated = BuildGeneratedValidator();
        var model = generated.CreateModel("valid", 20, "abcde");

        var withoutProvider = generated.ValidateWithOptions(model, new ValidationCallOptions());
        ValidationAssert.IsValid(withoutProvider);

        var withProvider = generated.ValidateWithOptions(
            model,
            new ValidationCallOptions { ServiceProvider = new StubServiceProvider() });
        ValidationAssert.HasError(withProvider, "Name");
        ValidationAssert.HasErrorWithMessage(withProvider, "Name", "service-provider-observed");
    }

    [TestMethod]
    public async Task GeneratedValidator_AsyncOptionsUseSameSelectionAsync() {
        var generated = BuildGeneratedValidator();
        var model = generated.CreateModel("valid", 10, "abcde");

        var result = await generated.ValidateWithOptionsAsync(
            model,
            new ValidationCallOptions { IncludeProperties = ["Age"] });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Age");
    }

    [TestMethod]
    public void GeneratedValidator_ContextOverloadUsesMemberPath() {
        var generated = BuildGeneratedValidator();
        var model = generated.CreateModel("", 20, "abcde");
        var contextType = typeof(ValidationContext<>).MakeGenericType(generated.ModelType);
        object?[] arguments = [model, "Parent", null, false, model, new RootContextData(), null, null];
        var context = Activator.CreateInstance(contextType, arguments)!;

        var result = (ValidationResult)generated.ValidatorType
            .GetMethod("Validate", [generated.ModelType, contextType])!
            .Invoke(generated.Validator, [model, context])!;

        ValidationAssert.HasError(result, "Parent.Name");
    }

    [TestMethod]
    public void GeneratedValidator_ValidateAndThrowCarriesErrors() {
        var generated = BuildGeneratedValidator();
        var model = generated.CreateModel("", 10, "abc");

        try {
            generated.ValidatorType
                     .GetMethod("ValidateAndThrow", [generated.ModelType])!
                     .Invoke(generated.Validator, [model]);

            MSTestAssertions.That(() => false.Should().BeTrue("ValidateAndThrow should have thrown."));
        }
        catch(TargetInvocationException exception) {
            MSTestAssertions.That(() => exception.InnerException.Should().BeOfType<ValidationException>());
            var validationException = (ValidationException)exception.InnerException!;
            MSTestAssertions.That(() => (validationException.Errors.Count > 0).Should().BeTrue());
        }
    }

    private static GeneratedValidator BuildGeneratedValidator() {
        var syntaxTree = CSharpSyntaxTree.ParseText(SOURCE);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            $"GeneratedExecutionAssembly_{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCompilation = compilation.AddSyntaxTrees(runResult.GeneratedTrees);

        using var stream = new MemoryStream();
        var emit = generatedCompilation.Emit(stream);

        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        var assembly = Assembly.Load(stream.ToArray());
        var modelType = assembly.GetTypes().Single(static type => type.Name == "Order");
        var validatorType = assembly.GetTypes().Single(static type => type.Name == "OrderValidator");
        var validator = Activator.CreateInstance(validatorType)!;

        return new GeneratedValidator(modelType, validatorType, validator);
    }

    private sealed class StubServiceProvider : IServiceProvider {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class GeneratedValidator(Type modelType, Type validatorType, object validator) {
        public Type ModelType { get; } = modelType;

        public Type ValidatorType { get; } = validatorType;

        public object Validator { get; } = validator;

        public object CreateModel(string name, int age, string code) {
            var model = Activator.CreateInstance(ModelType)!;
            ModelType.GetProperty("Name")!.SetValue(model, name);
            ModelType.GetProperty("Age")!.SetValue(model, age);
            ModelType.GetProperty("Code")!.SetValue(model, code);
            return model;
        }

        public ValidationResult ValidateBasic(object model) =>
            InvokeResult("Validate", [ModelType], [model]);

        public ValidationResult ValidateWithOptions(object model, ValidationCallOptions options) =>
            InvokeResult("Validate", [ModelType, typeof(ValidationCallOptions)], [model, options]);

        public ValidationResult ValidateWithStrategy(object model, Action<ValidationStrategy> configureStrategy) =>
            InvokeResult("Validate", [ModelType, typeof(Action<ValidationStrategy>)], [model, configureStrategy]);

        public async Task<ValidationResult> ValidateWithOptionsAsync(object model, ValidationCallOptions options) {
            var method = ValidatorType.GetMethod(
                "ValidateAsync",
                [ModelType, typeof(ValidationCallOptions), typeof(CancellationToken)])!;

            var valueTask = (ValueTask<ValidationResult>)method.Invoke(
                Validator,
                [model, options, CancellationToken.None])!;

            return await valueTask;
        }

        public void SetClassLevelCascadeMode(CascadeMode mode) =>
            ValidatorType.GetProperty("ClassLevelCascadeMode")!.SetValue(Validator, mode);

        private ValidationResult InvokeResult(string methodName, Type[] parameterTypes, object?[] arguments) {
            var method = ValidatorType.GetMethod(methodName, parameterTypes)
                         ?? throw new InvalidOperationException(
                             $"Method '{methodName}' was not found on '{ValidatorType.FullName}'.");

            return (ValidationResult)method.Invoke(Validator, arguments)!;
        }
    }
}
