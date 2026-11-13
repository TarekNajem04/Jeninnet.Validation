//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.Diagnostics;

/// <summary>
/// Tests for <see cref="SyncValidateAnalyzer"/> (VAL0016): sync Validate calls on
/// validators marked with <c>[AsyncOnlyValidator]</c> are reported at compile time,
/// mirroring the runtime <c>CompositeValidator</c> guard without diverging from it.
/// </summary>
[TestClass]
public sealed class SyncValidateAnalyzerTests {

    private const string SYNC_ONLY_SOURCE =
        """
        #nullable enable
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;

        using Jeninnet.Validation;
        using Jeninnet.Validation.Abstractions;

        namespace TestNamespace;

        public sealed class Payment {
            public string? Reference { get; init; }
        }

        [AsyncOnlyValidator]
        public sealed class PaymentValidator : ISyncValidator<Payment>, IAsyncValidator<Payment> {
            public ValidationResult Validate(Payment instance) => ValidationResult.Success;

            public ValidationResult Validate(Payment instance, IEnumerable<string> ruleSets) => ValidationResult.Success;

            public ValueTask<ValidationResult> ValidateAsync(Payment instance, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(ValidationResult.Success);

            public ValueTask<ValidationResult> ValidateAsync(Payment instance, IEnumerable<string> ruleSets, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(ValidationResult.Success);
        }

        public static class PaymentRunner {
            public static ValidationResult SyncRun(PaymentValidator validator, Payment payment)
                => validator.Validate(payment);

            public static ValidationResult SyncRunWithRuleSets(PaymentValidator validator, Payment payment)
                => validator.Validate(payment, ["default"]);

            public static ValueTask<ValidationResult> AsyncRun(PaymentValidator validator, Payment payment, CancellationToken token)
                => validator.ValidateAsync(payment, token);
        }
        """;

    private const string UNMARKED_SOURCE =
        """
        #nullable enable
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;

        using Jeninnet.Validation;
        using Jeninnet.Validation.Abstractions;

        namespace TestNamespace;

        public sealed class Payment {
            public string? Reference { get; init; }
        }

        public sealed class PaymentValidator : ISyncValidator<Payment>, IAsyncValidator<Payment> {
            public ValidationResult Validate(Payment instance) => ValidationResult.Success;

            public ValidationResult Validate(Payment instance, IEnumerable<string> ruleSets) => ValidationResult.Success;

            public ValueTask<ValidationResult> ValidateAsync(Payment instance, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(ValidationResult.Success);

            public ValueTask<ValidationResult> ValidateAsync(Payment instance, IEnumerable<string> ruleSets, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(ValidationResult.Success);
        }

        public static class PaymentRunner {
            public static ValidationResult SyncRun(PaymentValidator validator, Payment payment)
                => validator.Validate(payment);
        }
        """;

    private const string MARKED_BUT_NON_SYNC_VALIDATOR_SOURCE =
        """
        #nullable enable
        using System.Threading;

        using Jeninnet.Validation;

        namespace TestNamespace;

        [AsyncOnlyValidator]
        public sealed class PlainMarkedClass {
            public int Validate(int instance) => instance;
        }

        public static class PlainRunner {
            public static int Run(PlainMarkedClass target)
                => target.Validate(1);
        }
        """;

    [TestMethod]
    public async Task MarkedValidator_SyncValidate_BothOverloads_ReportWarningAsync() {
        var diagnostics = await GetSyncValidateDiagnosticsAsync(SYNC_ONLY_SOURCE, CancellationToken.None);

        // The runtime names both overloads of ISyncValidator<T>.Validate; warn on exactly both.
        // The async ValidateAsync call in the same source must never be flagged.
        MSTestAssertions.That(() => diagnostics.Should().Contain(d => d.Id == "VAL0016" && d.Severity == DiagnosticSeverity.Warning));
        MSTestAssertions.That(() => diagnostics.Where(d => d.Id == "VAL0016").Should().HaveCount(2));
    }

    [TestMethod]
    public async Task UnmarkedValidator_SyncValidate_NeverWarns_RuntimeGuardProtectsAsync() {
        var diagnostics = await GetSyncValidateDiagnosticsAsync(UNMARKED_SOURCE, CancellationToken.None);

        MSTestAssertions.That(() => diagnostics.Should().NotContain(d => d.Id == "VAL0016"));
    }

    [TestMethod]
    public async Task MarkedButNonValidatorClass_LocalMethod_NeverWarnsAsync() {
        // The warning is bound to the ISyncValidator<T>.Validate contract members only;
        // an own local method with the same name on a marked class is not the runtime trap.
        var diagnostics = await GetSyncValidateDiagnosticsAsync(MARKED_BUT_NON_SYNC_VALIDATOR_SOURCE, CancellationToken.None);

        MSTestAssertions.That(() => diagnostics.Should().NotContain(d => d.Id == "VAL0016"));
    }

    private static async Task<ImmutableArray<Diagnostic>> GetSyncValidateDiagnosticsAsync(string source, CancellationToken cancellationToken) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);

        List<MetadataReference> references = [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(int).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Jeninnet.Validation.AsyncOnlyValidatorAttribute).Assembly.Location)
        ];

        if(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedAssembliesStr) {
            var trustedAssemblies = trustedAssembliesStr.Split(Path.PathSeparator);
            var needed = new[] { "System.Runtime", "System.Collections", "mscorlib", "System.Private.CoreLib" };

            foreach(var path in trustedAssemblies) {
                var name = Path.GetFileNameWithoutExtension(path);
                if(needed.Contains(name)) {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var withAnalyzers = compilation.WithAnalyzers(
            [new SyncValidateAnalyzer()]);

        return await withAnalyzers.GetAllDiagnosticsAsync(cancellationToken)
                                  .ConfigureAwait(false);
    }
}
