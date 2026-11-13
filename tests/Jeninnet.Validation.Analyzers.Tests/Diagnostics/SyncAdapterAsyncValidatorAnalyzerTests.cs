//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.Diagnostics;

[TestClass]
public sealed class SyncAdapterAsyncValidatorAnalyzerTests {
    private const string ASYNC_VALIDATOR_SOURCE =
        """
        #nullable enable
        using Jeninnet.Validation;
        using Jeninnet.Validation.DataAnnotations;
        using Jeninnet.Validation.Fluent;

        namespace TestNamespace;

        public sealed class Model {
            public string? Name { get; set; }
        }

        [AsyncOnlyValidator]
        public sealed class AsyncModelValidator : Validator<Model> {
            protected override void Configure(ValidatorRuleBuilder<Model> builder) {
            }
        }

        [JeninnetValidatable(typeof(AsyncModelValidator))]
        public sealed class AsyncClassModel {
            public string? Name { get; set; }
        }

        public sealed class AsyncPropertyModel {
            [JeninnetValidateProperty(typeof(AsyncModelValidator), nameof(Name))]
            public string? Name { get; set; }
        }

        public sealed class AsyncGenericPropertyModel {
            [JeninnetValidateProperty<AsyncModelValidator>(nameof(Name))]
            public string? Name { get; set; }
        }
        """;

    private const string SYNC_VALIDATOR_SOURCE =
        """
        #nullable enable
        using Jeninnet.Validation;
        using Jeninnet.Validation.DataAnnotations;
        using Jeninnet.Validation.Fluent;

        namespace TestNamespace;

        public sealed class Model {
            public string? Name { get; set; }
        }

        public sealed class SyncModelValidator : Validator<Model> {
            protected override void Configure(ValidatorRuleBuilder<Model> builder) {
            }
        }

        [JeninnetValidatable(typeof(SyncModelValidator))]
        public sealed class SyncClassModel {
            public string? Name { get; set; }
        }

        public sealed class SyncPropertyModel {
            [JeninnetValidateProperty(typeof(SyncModelValidator), nameof(Name))]
            public string? Name { get; set; }
        }
        """;

    [TestMethod]
    public async Task AsyncOnlyValidator_InDataAnnotationsAdapters_ReportsThreeWarningsAsync() {
        var diagnostics = await AnalyzerTestSupport.GetDiagnosticsAsync(
            ASYNC_VALIDATOR_SOURCE,
            new SyncAdapterAsyncValidatorAnalyzer(),
            TestContext?.CancellationToken ?? CancellationToken.None);

        var warnings = diagnostics.Where(diagnostic => diagnostic.Id == "VAL0025").ToList();

        MSTestAssertions.That(() => warnings.Should().HaveCount(3));
        MSTestAssertions.That(() => warnings.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).Should().BeTrue());
    }

    [TestMethod]
    public async Task SyncValidator_InDataAnnotationsAdapters_IsNotReportedAsync() {
        var diagnostics = await AnalyzerTestSupport.GetDiagnosticsAsync(
            SYNC_VALIDATOR_SOURCE,
            new SyncAdapterAsyncValidatorAnalyzer(),
            TestContext?.CancellationToken ?? CancellationToken.None);

        MSTestAssertions.That(() => diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "VAL0025"));
    }

    public TestContext? TestContext { get; set; }
}
