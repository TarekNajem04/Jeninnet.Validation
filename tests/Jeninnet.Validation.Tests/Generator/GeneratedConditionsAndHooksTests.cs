//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Verifies synchronous attribute conditions and generated lifecycle hooks.
/// </summary>
[TestClass]
public sealed class GeneratedConditionsAndHooksTests {
    private const string SOURCE = """
        using System;
        using Jeninnet.Validation;
        using Jeninnet.Validation.Attributes;
        using Jeninnet.Validation.Core;

        namespace AttributeConditions;

        [Validate("AttributeConditions")]
        [WhenCondition(nameof(IsClassActive))]
        [PreValidate(nameof(BeforeValidate))]
        [OnFailure(nameof(AfterFailure))]
        public sealed class ConditionalModel {
            [NotEmpty, WhenCondition(nameof(IsNameRequired))]
            public string? Name { get; set; }

            [NotEmpty, UnlessCondition(nameof(IsCodeIgnored))]
            public string? Code { get; set; }

            public bool IsClassActive(ConditionalModel instance) => instance.Name != "inactive";

            public bool IsNameRequired(ConditionalModel instance) => instance.Name != "skip";

            public bool IsCodeIgnored(ConditionalModel instance) => instance.Code == "ignore";

            public static int FailureCount { get; set; }

            public static bool BeforeValidate(
                ConditionalModel instance,
                ValidationContext<ConditionalModel> context,
                ValidationErrorCollector errors) {
                if(instance.Name == "blocked") {
                    errors.Add(nameof(Name), "blocked");
                    return false;
                }

                return true;
            }

            public static void AfterFailure(ValidationResult result) => FailureCount++;
        }
        """;

    [TestMethod]
    public void PropertyCondition_OverridesClassConditionAndCanSkipRules() {
        var generated = BuildGenerated(SOURCE);
        var model = generated.CreateModel("skip", string.Empty);
        var result = generated.Validate(model);

        ValidationAssert.NoErrorFor(result, "Name");
        ValidationAssert.HasError(result, "Code");
    }

    [TestMethod]
    public void UnlessCondition_RunsWhenTheConditionIsFalse() {
        var generated = BuildGenerated(SOURCE);

        var result = generated.Validate(generated.CreateModel("valid", "ignored"));

        ValidationAssert.NoErrorFor(result, "Code");
    }

    [TestMethod]
    public async Task SyncConditions_WorkOnBothValidationPathsAsync() {
        var generated = BuildGenerated(SOURCE);
        var model = generated.CreateModel("skip", string.Empty);

        var sync = ValidateSync(generated, model);
        var asyncResult = await generated.ValidateAsync(model);

        ValidationAssert.NoErrorFor(sync, "Name");
        ValidationAssert.NoErrorFor(asyncResult, "Name");
        ValidationAssert.HasError(sync, "Code");
        ValidationAssert.HasError(asyncResult, "Code");
    }

    [TestMethod]
    public void PreValidate_CanAddAnErrorAndStopValidation() {
        var generated = BuildGenerated(SOURCE);

        var result = generated.Validate(generated.CreateModel("blocked", string.Empty));

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasErrorWithMessage(result, "Name", "blocked");
    }

    [TestMethod]
    public void OnFailure_IsCalledOnceWithAllErrors() {
        var generated = BuildGenerated(SOURCE);
        generated.ResetFailureCount();

        var result = generated.Validate(generated.CreateModel("valid", string.Empty));

        ValidationAssert.HasError(result, "Code");
        MSTestAssertions.That(() => generated.FailureCount.Should().Be(1));
    }

    [TestMethod]
    public void MissingConditionMethod_ProducesDiagnostic() {
        const string source = """
            using Jeninnet.Validation.Attributes;

            namespace AttributeConditions;

            [Validate]
            public sealed class MissingConditionModel {
                [WhenCondition("MissingCondition"), NotEmpty]
                public string? Name { get; set; }
            }
            """;

        var diagnostics = RunGenerator(source);
        MSTestAssertions.That(() => diagnostics.Any(
            static diagnostic => diagnostic.Id == "VAL0018").Should().BeTrue());
    }

    [TestMethod]
    public void InvalidHookSignature_ProducesDiagnostic() {
        const string source = """
            using System;
            using Jeninnet.Validation.Attributes;

            namespace AttributeConditions;

            [Validate, PreValidate("BrokenHook")]
            public sealed class BrokenHookModel {
                [NotEmpty] public string? Name { get; set; }

                public static void BrokenHook(BrokenHookModel instance) { }
            }
            """;

        var diagnostics = RunGenerator(source);
        MSTestAssertions.That(() => diagnostics.Any(
            static diagnostic => diagnostic.Id == "VAL0019").Should().BeTrue());
    }

    [TestMethod]
    public void SingleLifecycleHook_GeneratesCompilableValidator() {
        const string onFailureOnly = """
            using Jeninnet.Validation;
            using Jeninnet.Validation.Attributes;

            namespace AttributeConditions;

            [Validate, OnFailure(nameof(AfterFailure))]
            public sealed class OnFailureOnlyModel {
                [NotEmpty] public string? Name { get; set; }

                public static void AfterFailure(ValidationResult result) { }
            }
            """;
        const string preValidateOnly = """
            using Jeninnet.Validation.Attributes;
            using Jeninnet.Validation.Core;

            namespace AttributeConditions;

            [Validate, PreValidate(nameof(BeforeValidate))]
            public sealed class PreValidateOnlyModel {
                [NotEmpty] public string? Name { get; set; }

                public static bool BeforeValidate(
                    PreValidateOnlyModel instance,
                    ValidationContext<PreValidateOnlyModel> context) => true;
            }
            """;

        AssertGeneratedCompiles(onFailureOnly);
        AssertGeneratedCompiles(preValidateOnly);
    }

    [TestMethod]
    public void GeneratedValidators_AreNotAffectedByFluentOnRuleAdded() {
        var generated = BuildGenerated(SOURCE);
        var result = generated.Validate(generated.CreateModel("valid", string.Empty));

        ValidationAssert.HasError(result, "Code");
        MSTestAssertions.That(() => result.Errors[0].Severity.Should().Be(ValidationErrorSeverity.Error));
    }

    private static ValidationResult ValidateSync(GeneratedConditional generated, object model) =>
        generated.Validate(model);

    private static void AssertGeneratedCompiles(string source) {
        var compilation = CreateCompilation(source, $"SingleHookAssembly_{Guid.NewGuid():N}");
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
    }

    private static GeneratedConditional BuildGenerated(string source) {
        var compilation = CreateCompilation(source, $"GeneratedConditionAssembly_{Guid.NewGuid():N}");

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();
        var errors = runResult.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();

        MSTestAssertions.That(() => errors.Should().BeEmpty(
            string.Join(Environment.NewLine, errors)));

        var generatedCompilation = compilation.AddSyntaxTrees(runResult.GeneratedTrees);

        using var stream = new MemoryStream();
        var emit = generatedCompilation.Emit(stream);

        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        var assembly = Assembly.Load(stream.ToArray());
        var modelType = assembly.GetType("AttributeConditions.ConditionalModel", throwOnError: true)!;
        var validatorType = assembly.GetType("AttributeConditions.ConditionalModelValidator", throwOnError: true)!;
        var validator = Activator.CreateInstance(validatorType)!;

        return new GeneratedConditional(modelType, validatorType, validator);
    }

    private static List<Diagnostic> RunGenerator(string source) {
        var compilation = CreateCompilation(source, $"ConditionDiagnosticAssembly_{Guid.NewGuid():N}");
        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        return [.. driver.GetRunResult().Diagnostics];
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

    private sealed class GeneratedConditional(Type modelType, Type validatorType, object validator) {
        public Type ValidatorType { get; } = validatorType;

        public object Validator { get; } = validator;

        public object CreateModel(string name, string code) {
            var model = Activator.CreateInstance(modelType)!;
            modelType.GetProperty("Name")!.SetValue(model, name);
            modelType.GetProperty("Code")!.SetValue(model, code);
            return model;
        }

        public ValidationResult Validate(object model) =>
            (ValidationResult)ValidatorType.GetMethod("Validate", [modelType])!.Invoke(Validator, [model])!;

        public async Task<ValidationResult> ValidateAsync(object model) {
            var valueTask = (ValueTask<ValidationResult>)ValidatorType
                .GetMethod("ValidateAsync", [modelType, typeof(CancellationToken)])!
                .Invoke(Validator, [model, CancellationToken.None])!;
            return await valueTask;
        }

        public int FailureCount => (int)modelType.GetProperty("FailureCount")!.GetValue(null)!;

        public void ResetFailureCount() => modelType.GetProperty("FailureCount")!.SetValue(null, 0);
    }
}
