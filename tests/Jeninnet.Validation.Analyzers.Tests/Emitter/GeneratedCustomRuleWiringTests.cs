//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.Emitter;

/// <summary>
/// Regression tests for generated-validator custom-rule wiring: the
/// constructor must invoke <c>AddCustomValidators()</c> before the
/// <c>CompositeValidator</c> snapshots the rule lists, so rules added in the
/// partial hook actually execute.
/// </summary>
[TestClass]
public sealed class GeneratedCustomRuleWiringTests {

    private const string SYNC_CUSTOM_RULE_SOURCE =
        """
        #nullable enable
        using Jeninnet.Validation.Attributes;

        namespace Ee07Sync
        {
            [Validate]
            public sealed class Payment
            {
                [NotEmpty]
                public string? Reference { get; set; }
            }
        }

        namespace Ee07Sync.Validators
        {
            public sealed partial class PaymentValidator
            {
                partial void AddCustomValidators()
                {
                    _syncRules.Add(
                        new Jeninnet.Validation.Rules.MustRule<Ee07Sync.Payment, string?>(
                            Jeninnet.Validation.Core.ValidatorFactory.Accessor<Ee07Sync.Payment, string?>(
                                static x => x.Reference,
                                nameof(Ee07Sync.Payment.Reference)),
                            static reference => reference?.Contains("9", System.StringComparison.Ordinal) is true,
                            "Reference must contain a digit."));
                }
            }
        }
        """;

    private const string ASYNC_CUSTOM_RULE_SOURCE =
        """
        #nullable enable
        using Jeninnet.Validation.Attributes;

        namespace Ee07Async
        {
            [Validate]
            public sealed class Refund
            {
                public string? Reason { get; set; }
            }
        }

        namespace Ee07Async.Validators
        {
            public sealed partial class RefundValidator
            {
                partial void AddCustomValidators()
                {
                    _asyncRules.Add(
                        new Jeninnet.Validation.Rules.MustAsyncRule<Ee07Async.Refund, string?>(
                            Jeninnet.Validation.Core.ValidatorFactory.Accessor<Ee07Async.Refund, string?>(
                                static x => x.Reason,
                                nameof(Ee07Async.Refund.Reason)),
                            static (reason, _) => new System.Threading.Tasks.ValueTask<bool>(
                                reason?.Contains("ok", System.StringComparison.Ordinal) is true),
                            "Reason must contain 'ok'."));
                }
            }
        }
        """;

    [TestMethod]
    public void GeneratedValidator_WithSyncCustomRule_ExecutesTheRule() {
        var assembly = CompileGeneratedAssembly(
            SYNC_CUSTOM_RULE_SOURCE,
            "Ee07SyncAssembly",
            TestContext?.CancellationToken ?? CancellationToken.None);

        var validatorType = assembly.GetType("Ee07Sync.Validators.PaymentValidator", throwOnError: true)!;
        var modelType = assembly.GetType("Ee07Sync.Payment", throwOnError: true)!;

        var validator = Activator.CreateInstance(validatorType)!;
        var model = Activator.CreateInstance(modelType)!;
        modelType.GetProperty("Reference")!.SetValue(model, "abc");

        var validate = validatorType.GetMethod("Validate", [modelType])!;
        var result = (ValidationResult)validate.Invoke(validator, [model])!;

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse("the custom rule must execute and fail"));
        MSTestAssertions.That(() => result.Errors.Should().Contain(e => e.Message == "Reference must contain a digit."));
    }

    [TestMethod]
    public async Task GeneratedValidator_WithAsyncCustomRule_RunsAsyncPathAndThrowsOnSyncPathAsync() {
        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        var assembly = CompileGeneratedAssembly(ASYNC_CUSTOM_RULE_SOURCE, "Ee07AsyncAssembly", cancellationToken);

        var validatorType = assembly.GetType("Ee07Async.Validators.RefundValidator", throwOnError: true)!;
        var modelType = assembly.GetType("Ee07Async.Refund", throwOnError: true)!;

        var validator = Activator.CreateInstance(validatorType)!;
        var model = Activator.CreateInstance(modelType)!;
        modelType.GetProperty("Reason")!.SetValue(model, "declined");

        var validateSync = validatorType.GetMethod("Validate", [modelType])!;
        TargetInvocationException? thrown = null;
        try {
            _ = validateSync.Invoke(validator, [model]);
        }
        catch(TargetInvocationException exception) {
            thrown = exception;
        }

        MSTestAssertions.That(() => thrown.Should().NotBeNull("sync Validate on async-only custom content must throw"));
        MSTestAssertions.That(() => (thrown!.InnerException is AsyncValidatorInvokedSynchronouslyException).Should().BeTrue(
            "the runtime guard must reject the sync path for custom async content"));

        var validateAsync = validatorType.GetMethod("ValidateAsync", [modelType, typeof(CancellationToken)])!;
        var pending = (ValueTask<ValidationResult>?)validateAsync.Invoke(validator, [model, cancellationToken])
            ?? throw new InvalidOperationException("ValidateAsync must return a pending result.");
        var result = await pending;

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse("the async custom rule must execute and fail"));
        MSTestAssertions.That(() => result.Errors.Should().Contain(e => e.Message == "Reason must contain 'ok'."));
    }

    private static Assembly CompileGeneratedAssembly(string source, string assemblyName, CancellationToken cancellationToken) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);

        var references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>()
            .ToArray();

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        _ = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var generatorDiagnostics,
            cancellationToken);

        MSTestAssertions.That(() => generatorDiagnostics.Should().BeEmpty("the generator must not report diagnostics"));

        var compilationErrors = outputCompilation.GetDiagnostics(cancellationToken)
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        MSTestAssertions.That(() => compilationErrors.Should().BeEmpty(
            string.Join(Environment.NewLine, compilationErrors.Select(static diagnostic => diagnostic.ToString()))));

        using var stream = new MemoryStream();
        var emitResult = outputCompilation.Emit(stream, cancellationToken: cancellationToken);

        MSTestAssertions.That(() => emitResult.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emitResult.Diagnostics.Select(static diagnostic => diagnostic.ToString()))));

        return Assembly.Load(stream.ToArray());
    }

    public TestContext? TestContext { get; set; }
}
