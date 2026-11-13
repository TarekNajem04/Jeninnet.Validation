//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.Diagnostics;

[TestClass]
public sealed class TautologicalRuleAnalyzerTests {
    private const string TAUTOLOGICAL_SOURCE =
        """
        using Jeninnet.Validation.Attributes;

        namespace TestNamespace;

        public sealed class Model
        {
            [MinimumLength(0)] public string? MinLength { get; set; }
            [MaximumLength(int.MaxValue)] public string? MaxLength { get; set; }
            [Length(0, int.MaxValue)] public string? Length { get; set; }
            [InclusiveBetween(int.MinValue, int.MaxValue)] public int Between { get; set; }
            [GreaterThanOrEqual(int.MinValue)] public int GreaterOrEqual { get; set; }
            [LessThanOrEqual(int.MaxValue)] public int LessOrEqual { get; set; }
            [InclusiveBetween(int.MinValue, int.MaxValue)] public int? NullableBetween { get; set; }
        }
        """;

    private const string BOUNDED_SOURCE =
        """
        using Jeninnet.Validation.Attributes;

        namespace TestNamespace;

        public sealed class Model
        {
            [MinimumLength(1)] public string? MinLength { get; set; }
            [MaximumLength(10)] public string? MaxLength { get; set; }
            [Length(1, 10)] public string? Length { get; set; }
            [InclusiveBetween(0, 100)] public int Between { get; set; }
            [InclusiveBetween(0L, 100L)] public long LongBetween { get; set; }
            [GreaterThan(0)] public uint GreaterThanZero { get; set; }
            [LessThanOrEqual(int.MaxValue)] public long LongWithIntMax { get; set; }
        }
        """;

    private const string FOREIGN_ATTRIBUTE_SOURCE =
        """
        namespace Custom
        {
            public sealed class MinimumLengthAttribute : System.Attribute
            {
                public MinimumLengthAttribute(int minimum) { }
            }
        }

        namespace TestNamespace;

        public sealed class Model
        {
            [Custom.MinimumLength(0)] public string? Name { get; set; }
        }
        """;

    [TestMethod]
    public async Task TautologicalBounds_AreReportedAsInfoAsync() {
        var diagnostics = await AnalyzerTestSupport.GetDiagnosticsAsync(
            TAUTOLOGICAL_SOURCE,
            new TautologicalRuleAnalyzer(),
            TestContext?.CancellationToken ?? CancellationToken.None);

        var tautologies = diagnostics.Where(d => d.Id == "VAL0017").ToList();

        MSTestAssertions.That(() => tautologies.Should().HaveCount(7));
        MSTestAssertions.That(() => tautologies.All(d => d.Severity == DiagnosticSeverity.Info).Should().BeTrue());
        MSTestAssertions.That(() => tautologies.Any(d => d.GetMessage(CultureInfo.InvariantCulture).Contains("NullableBetween", StringComparison.Ordinal)).Should().BeTrue());
    }

    [TestMethod]
    public async Task BoundedRules_DoNotReportAsync() {
        var diagnostics = await AnalyzerTestSupport.GetDiagnosticsAsync(
            BOUNDED_SOURCE,
            new TautologicalRuleAnalyzer(),
            TestContext?.CancellationToken ?? CancellationToken.None);

        MSTestAssertions.That(() => diagnostics.Should().NotContain(d => d.Id == "VAL0017"));
    }

    [TestMethod]
    public async Task ForeignAttributes_AreIgnoredAsync() {
        var diagnostics = await AnalyzerTestSupport.GetDiagnosticsAsync(
            FOREIGN_ATTRIBUTE_SOURCE,
            new TautologicalRuleAnalyzer(),
            TestContext?.CancellationToken ?? CancellationToken.None);

        MSTestAssertions.That(() => diagnostics.Should().NotContain(d => d.Id == "VAL0017"));
    }

    [TestMethod]
    public async Task LargeSyntheticModel_StaysWithinAnalysisBudgetAsync() {
        var source = BuildLargeModel(200);
        var stopwatch = Stopwatch.StartNew();

        var diagnostics = await AnalyzerTestSupport.GetDiagnosticsAsync(
            source,
            new TautologicalRuleAnalyzer(),
            TestContext?.CancellationToken ?? CancellationToken.None);

        stopwatch.Stop();

        MSTestAssertions.That(() => diagnostics.Where(d => d.Id == "VAL0017").Should().HaveCount(200));
        MSTestAssertions.That(() => (stopwatch.Elapsed < TimeSpan.FromSeconds(5)).Should().BeTrue(
            $"analysis took {stopwatch.Elapsed.TotalMilliseconds:F0} ms"));

        TestContext?.WriteLine($"VAL0017 analysis over 200 properties: {stopwatch.Elapsed.TotalMilliseconds:F1} ms");
    }

    private static string BuildLargeModel(int propertyCount) {
        var builder = new StringBuilder();
        builder.AppendLine("using Jeninnet.Validation.Attributes;")
            .AppendLine()
            .AppendLine("namespace TestNamespace;")
            .AppendLine()
            .AppendLine("public sealed class LargeModel {");

        for(var index = 0; index < propertyCount; index++) {
            builder.AppendLine(CultureInfo.InvariantCulture, $"    [MinimumLength(0)] public string? P{index} {{ get; set; }}");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    public TestContext? TestContext { get; set; }
}
