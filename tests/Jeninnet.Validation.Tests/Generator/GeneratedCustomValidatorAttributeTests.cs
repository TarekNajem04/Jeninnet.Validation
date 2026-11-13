//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Verifies custom and asynchronous validators declared through attributes.
/// </summary>
[TestClass]
public sealed class GeneratedCustomValidatorAttributeTests {
    private const string SOURCE = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using Jeninnet.Validation;
        using Jeninnet.Validation.Attributes;

        namespace AttributeCustom;

        public static class CustomRules {
            public static bool IsEven(int value) => value % 2 == 0;

            public static bool RuleA(string? value) => value?.Contains('a') is not true;

            public static bool RuleB(string? value) => value?.Contains('b') is not true;

            public static bool HasService(string? value, IServiceProvider services) => services is not null;

            public static bool ValidateModel(ProviderModel instance) => instance.Name != "blocked";

            public static ValidationResult CheckCode(string? value) {
                var errors = new ValidationErrorCollector();

                if(value == "bad") {
                    errors.Add(string.Empty, "bad code");
                    errors.Add("Other", "other error");
                }

                return ValidationResult.FromErrors(errors);
            }

            public static ValueTask<bool> IsAvailableAsync(string? value, CancellationToken cancellationToken) =>
                ValueTask.FromResult(value == "available" && cancellationToken.CanBeCanceled);

            public static ValueTask<ValidationResult> CheckCodeAsync(
                string? value,
                IServiceProvider services,
                CancellationToken cancellationToken) {
                var errors = new ValidationErrorCollector();

                if(value == "bad") {
                    errors.Add(string.Empty, "async bad code");
                }

                return ValueTask.FromResult(ValidationResult.FromErrors(errors));
            }
        }

        [Validate("AttributeCustom")]
        public sealed class CustomModel {
            [CustomValidator(typeof(CustomRules), nameof(CustomRules.IsEven))]
            public int Number { get; set; }

            [CustomValidator(typeof(CustomRules), nameof(CustomRules.CheckCode))]
            public string? Code { get; set; }

            [CustomValidator(typeof(CustomRules), nameof(CustomRules.HasService))]
            public string? ServiceValue { get; set; }

            [CustomValidator(typeof(CustomRules), nameof(CustomRules.RuleA))]
            [CustomValidator(typeof(CustomRules), nameof(CustomRules.RuleB))]
            public string? Multi { get; set; }
        }

        [Validate("AttributeCustom")]
        [CustomValidator(typeof(CustomRules), nameof(CustomRules.ValidateModel))]
        public sealed class ProviderModel {
            public string? Name { get; set; }
        }

        [Validate("AttributeCustom")]
        public sealed class AsyncOnlyModel {
            [CustomValidator(typeof(CustomRules), nameof(CustomRules.IsAvailableAsync), IsAsync = true)]
            public string? Value { get; set; }

            [CustomValidator(typeof(CustomRules), nameof(CustomRules.CheckCodeAsync), IsAsync = true)]
            public string? Code { get; set; }
        }
        """;

    [TestMethod]
    public void SyncPredicateCustomValidator_AddsErrorWhenFalse() {
        var generated = BuildValidator("CustomModel");
        var model = generated.CreateModel();
        generated.Set(model, "Number", 3);

        var result = generated.Validate(model);

        ValidationAssert.HasError(result, "Number");
    }

    [TestMethod]
    public void SyncResultCustomValidator_PreservesExplicitPathsAndFillsEmptyOnes() {
        var generated = BuildValidator("CustomModel");
        var model = generated.CreateModel();
        generated.Set(model, "Number", 2);
        generated.Set(model, "Code", "bad");

        var result = generated.Validate(model, new ValidationCallOptions {
            ServiceProvider = new TestServiceProvider()
        });

        MSTestAssertions.That(() => result.Errors.Should().HaveCount(2));
        MSTestAssertions.That(() => result.Errors.Should().Contain(error => error.MemberName == "Code"));
        MSTestAssertions.That(() => result.Errors.Should().Contain(error => error.MemberName == "Other"));
    }

    [TestMethod]
    public void CustomValidator_ReceivesServiceProvider() {
        var generated = BuildValidator("CustomModel");
        var model = generated.CreateModel();
        generated.Set(model, "Number", 2);
        generated.Set(model, "ServiceValue", "provided");

        var withoutProvider = generated.Validate(model);
        var withProvider = generated.Validate(model, new ValidationCallOptions {
            ServiceProvider = new TestServiceProvider()
        });

        ValidationAssert.HasError(withoutProvider, "ServiceValue");
        ValidationAssert.NoErrorFor(withProvider, "ServiceValue");
    }

    [TestMethod]
    public void ModelLevelCustomValidator_AddsRootError() {
        var generated = BuildValidator("ProviderModel");
        var model = generated.CreateModel();
        generated.Set(model, "Name", "blocked");

        var result = generated.Validate(model);

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
        MSTestAssertions.That(() => result.Errors.Should().Contain(error => error.MemberName.Length == 0));
    }

    [TestMethod]
    public void MultipleCustomValidatorsOnSameProperty_AllRun() {
        var generated = BuildValidator("CustomModel");
        var model = generated.CreateModel();
        generated.Set(model, "Number", 2);
        generated.Set(model, "Multi", "ab");

        var result = generated.Validate(model);

        MSTestAssertions.That(() => result.Errors.Count(error => error.MemberName == "Multi").Should().Be(2));
    }

    [TestMethod]
    public async Task AsyncOnlyCustomValidator_ThrowsOnSyncAndRunsAsync() {
        var generated = BuildValidator("AsyncOnlyModel");
        var model = generated.CreateModel();
        generated.Set(model, "Value", "available");
        generated.Set(model, "Code", "bad");

        MSTestAssertions.That(() => generated.IsAsyncOnly.Should().BeTrue());

        TargetInvocationException? thrown = null;
        try {
            _ = InvokeValidateSync(generated, model);
        }
        catch(TargetInvocationException exception) {
            thrown = exception;
        }

        MSTestAssertions.That(() => thrown.Should().NotBeNull());
        MSTestAssertions.That(() => (thrown!.InnerException is AsyncValidatorInvokedSynchronouslyException).Should().BeTrue());

        using var cancellationTokenSource = new CancellationTokenSource();
        var result = await generated.ValidateAsync(
            model,
            new ValidationCallOptions {
                ServiceProvider = new TestServiceProvider()
            },
            cancellationTokenSource.Token);

        ValidationAssert.NoErrorFor(result, "Value");
        ValidationAssert.HasError(result, "Code");
    }

    [TestMethod]
    public void InvalidCustomValidatorSignature_ProducesDiagnostic() {
        const string source = """
            using Jeninnet.Validation.Attributes;

            namespace AttributeCustomBad;

            public static class BadRules {
                public bool NotStatic(string? value) => true;
            }

            [Validate]
            public sealed class BadModel {
                [CustomValidator(typeof(BadRules), nameof(BadRules.NotStatic))]
                public string? Name { get; set; }
            }
            """;

        var diagnostics = RunGenerator(source);
        MSTestAssertions.That(() => diagnostics.Any(
            static diagnostic => diagnostic.Id == "VAL0020").Should().BeTrue());
    }

    private static ValidationResult InvokeValidateSync(GeneratedValidator generated, object model) =>
        generated.Validate(model);

    private static GeneratedValidator BuildValidator(string modelName) {
        var assembly = CompileGeneratedAssembly(SOURCE, $"CustomAttributeAssembly_{Guid.NewGuid():N}");
        var modelType = assembly.GetType($"AttributeCustom.{modelName}", throwOnError: true)!;
        var validatorType = assembly.GetType($"AttributeCustom.{modelName}Validator", throwOnError: true)!;

        return new GeneratedValidator(modelType, validatorType, Activator.CreateInstance(validatorType)!);
    }

    private static List<Diagnostic> RunGenerator(string source) {
        var compilation = CreateCompilation(source, $"CustomDiagnosticAssembly_{Guid.NewGuid():N}");
        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        return [.. driver.GetRunResult().Diagnostics];
    }

    private static Assembly CompileGeneratedAssembly(string source, string assemblyName) {
        var compilation = CreateCompilation(source, assemblyName);
        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();
        var errors = runResult.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();

        MSTestAssertions.That(() => errors.Should().BeEmpty(
            string.Join(Environment.NewLine, errors)));

        using var stream = new MemoryStream();
        var emit = compilation.AddSyntaxTrees(runResult.GeneratedTrees).Emit(stream);
        var problems = emit.Diagnostics.Where(static diagnostic =>
            diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error).ToList();

        MSTestAssertions.That(() => problems.Should().BeEmpty(
            string.Join(Environment.NewLine, problems)));

        return Assembly.Load(stream.ToArray());
    }

    private static CSharpCompilation CreateCompilation(string source, string assemblyName) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        return CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
    }

    private sealed class GeneratedValidator(Type modelType, Type validatorType, object validator) {
        public bool IsAsyncOnly => validatorType.GetCustomAttribute<AsyncOnlyValidatorAttribute>() is not null;

        public object CreateModel() => Activator.CreateInstance(modelType)!;

        public void Set(object model, string propertyName, object? value) =>
            modelType.GetProperty(propertyName)!.SetValue(model, value);

        public ValidationResult Validate(object model) =>
            (ValidationResult)validatorType.GetMethod("Validate", [modelType])!.Invoke(validator, [model])!;

        public ValidationResult Validate(object model, ValidationCallOptions options) =>
            (ValidationResult)validatorType
                .GetMethod("Validate", [modelType, typeof(ValidationCallOptions)])!
                .Invoke(validator, [model, options])!;

        public async Task<ValidationResult> ValidateAsync(
            object model,
            ValidationCallOptions options,
            CancellationToken cancellationToken) {
            var pending = (ValueTask<ValidationResult>)validatorType
                .GetMethod("ValidateAsync", [modelType, typeof(ValidationCallOptions), typeof(CancellationToken)])!
                .Invoke(validator, [model, options, cancellationToken])!;

            return await pending;
        }
    }

    private sealed class TestServiceProvider : IServiceProvider {
        public object? GetService(Type serviceType) => null;
    }
}
