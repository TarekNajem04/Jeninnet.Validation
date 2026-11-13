//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.Diagnostics;

[TestClass]
public sealed class CollectionRuleAnalyzerTests {
    private const string FLUENT_SOURCE =
        """
        #nullable enable
        using System.Collections.Generic;
        using Jeninnet.Validation.Fluent;

        namespace TestNamespace;

        public sealed class Model {
            public int CountValue { get; set; }
            public string? Name { get; set; }
            public List<string>? Items { get; set; }
            public string[]? Values { get; set; }
            public object? Anything { get; set; }
        }

        public sealed class ModelValidator : Validator<Model> {
            protected override void Configure(ValidatorRuleBuilder<Model> builder) {
                builder.RuleFor(static x => x.CountValue, nameof(Model.CountValue)).Count(1, 2);
                builder.RuleFor(static x => x.Name, nameof(Model.Name)).MinCount(1);
                builder.RuleFor(static x => x.Items, nameof(Model.Items)).Count(1, 2);
                builder.RuleFor(static x => x.Values, nameof(Model.Values)).MaxCount(2);
                builder.RuleFor(static x => x.Anything, nameof(Model.Anything)).MinCount(1);
            }
        }
        """;

    private const string ATTRIBUTE_SOURCE =
        """
        #nullable enable
        using System.Collections.Generic;
        using Jeninnet.Validation.Attributes;

        namespace TestNamespace;

        public sealed class Model {
            [Count(1, 2)] public int CountValue { get; set; }
            [MinCount(1)] public string? Name { get; set; }
            [MaxCount(2)] public List<string>? Items { get; set; }
            [Count(1, 2)] public string[]? Values { get; set; }
        }
        """;

    [TestMethod]
    public async Task FluentCountRules_OnNonCollections_ReportTwoWarningsAsync() {
        var diagnostics = await AnalyzerTestSupport.GetDiagnosticsAsync(
            FLUENT_SOURCE,
            new CollectionRuleAnalyzer(),
            TestContext?.CancellationToken ?? CancellationToken.None);

        var warnings = diagnostics.Where(diagnostic => diagnostic.Id == "VAL0024").ToList();

        MSTestAssertions.That(() => warnings.Should().HaveCount(2));
        MSTestAssertions.That(() => warnings.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).Should().BeTrue());
        MSTestAssertions.That(() => warnings.Any(diagnostic =>
            diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("int", StringComparison.Ordinal)).Should().BeTrue());
        MSTestAssertions.That(() => warnings.Any(diagnostic =>
            diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("string", StringComparison.Ordinal)).Should().BeTrue());
    }

    [TestMethod]
    public async Task AttributeCountRules_OnNonCollections_ReportTwoWarningsAsync() {
        var diagnostics = await AnalyzerTestSupport.GetDiagnosticsAsync(
            ATTRIBUTE_SOURCE,
            new CollectionRuleAnalyzer(),
            TestContext?.CancellationToken ?? CancellationToken.None);

        var warnings = diagnostics.Where(diagnostic => diagnostic.Id == "VAL0024").ToList();

        MSTestAssertions.That(() => warnings.Should().HaveCount(2));
        MSTestAssertions.That(() => warnings.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).Should().BeTrue());
    }

    [TestMethod]
    public async Task LargeSyntheticModel_StaysWithinAnalysisBudgetAsync() {
        var source = BuildLargeModel(200);
        var stopwatch = Stopwatch.StartNew();

        var diagnostics = await AnalyzerTestSupport.GetDiagnosticsAsync(
            source,
            new CollectionRuleAnalyzer(),
            TestContext?.CancellationToken ?? CancellationToken.None);

        stopwatch.Stop();

        MSTestAssertions.That(() => diagnostics.Where(diagnostic => diagnostic.Id == "VAL0024").Should().HaveCount(200));
        MSTestAssertions.That(() => (stopwatch.Elapsed < TimeSpan.FromSeconds(30)).Should().BeTrue(
            $"analysis took {stopwatch.Elapsed.TotalMilliseconds:F0} ms"));

        TestContext?.WriteLine($"VAL0024 analysis over 200 members: {stopwatch.Elapsed.TotalMilliseconds:F1} ms");
    }

    private static string BuildLargeModel(int propertyCount) {
        var builder = new StringBuilder();
        builder.AppendLine("using Jeninnet.Validation.Fluent;")
            .AppendLine()
            .AppendLine("namespace TestNamespace;")
            .AppendLine()
            .AppendLine("public sealed class LargeModel {");

        for(var index = 0; index < propertyCount; index++) {
            builder.AppendLine(CultureInfo.InvariantCulture, $"    public int P{index} {{ get; set; }}");
        }

        builder.AppendLine("}")
            .AppendLine()
            .AppendLine("public sealed class LargeValidator : Validator<LargeModel> {")
            .AppendLine("    protected override void Configure(ValidatorRuleBuilder<LargeModel> builder) {");

        for(var index = 0; index < propertyCount; index++) {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"        builder.RuleFor(static x => x.P{index}, nameof(LargeModel.P{index})).Count(1, 2);");
        }

        builder.AppendLine("    }")
            .AppendLine("}");
        return builder.ToString();
    }

    public TestContext? TestContext { get; set; }
}
