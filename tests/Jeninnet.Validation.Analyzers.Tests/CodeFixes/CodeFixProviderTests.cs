//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.CodeFixes;

[TestClass]
public sealed class CodeFixProviderTests {
    private const string ATTRIBUTE_MODEL_SOURCE =
        """
        #nullable enable
        using Jeninnet.Validation.Attributes;

        namespace TestNamespace;

        public sealed class Model {
            [Length(1, 10)]
            public string? Name { get; set; }

            [Range(1, 10)]
            public int? Age { get; set; }
        }
        """;

    private const string CONVERSION_MODEL_SOURCE =
        """
        #nullable enable
        using System;
        using Jeninnet.Validation.Attributes;

        namespace TestNamespace;

        public enum Color { Red, Blue }

        public sealed class Model {
            [Equal("10.5")] public decimal Amount { get; set; }
            [Equal("true")] public bool Flag { get; set; }
            [After("2024-01-02")] public DateTime Timestamp { get; set; }
            [After("2024-02-03")] public DateOnly Day { get; set; }
            [Equal("01:02:03")] public TimeSpan Duration { get; set; }
            [Equal("00000000-0000-0000-0000-000000000001")] public Guid Id { get; set; }
            [Equal("Red")] public Color Shade { get; set; }
        }
        """;

    [TestMethod]
    public async Task AddNotEmptySuggestion_AddsNotEmptyAsync() {
        using var workspace = new AdhocWorkspace();
        var document = CodeFixTestSupport.CreateDocument(workspace, ATTRIBUTE_MODEL_SOURCE);
        var root = await document.GetSyntaxRootAsync(TestCancellation).ConfigureAwait(false);
        var property = root!.DescendantNodes().OfType<PropertyDeclarationSyntax>()
                            .Single(static node => node.Identifier.ValueText == "Name");
        var diagnostic = CodeFixTestSupport.CreateDiagnostic(
            DiagnosticDescriptors.SuggestNotEmptyForString.Id,
            property.GetLocation());

        var text = await CodeFixTestSupport.ApplyFixAndGetTextAsync(
            document,
            diagnostic,
            new AddNotEmptySuggestionCodeFix(),
            cancellationToken: TestCancellation).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().NotBeNull());
        MSTestAssertions.That(() => text.Should().Contain("NotEmpty"));
    }

    [TestMethod]
    public async Task AddNotNullSuggestion_AddsNotNullAsync() {
        using var workspace = new AdhocWorkspace();
        var document = CodeFixTestSupport.CreateDocument(workspace, ATTRIBUTE_MODEL_SOURCE);
        var root = await document.GetSyntaxRootAsync(TestCancellation).ConfigureAwait(false);
        var property = root!.DescendantNodes().OfType<PropertyDeclarationSyntax>()
                            .Single(static node => node.Identifier.ValueText == "Name");
        var diagnostic = CodeFixTestSupport.CreateDiagnostic(
            DiagnosticDescriptors.SuggestNotNullForNullable.Id,
            property.GetLocation());

        var text = await CodeFixTestSupport.ApplyFixAndGetTextAsync(
            document,
            diagnostic,
            new AddNotNullSuggestionCodeFix(),
            cancellationToken: TestCancellation).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().NotBeNull());
        MSTestAssertions.That(() => text.Should().Contain("NotNull"));
    }

    [TestMethod]
    public async Task AddNotNullBeforeNumericRule_AddsNotNullToTheAttributeListAsync() {
        using var workspace = new AdhocWorkspace();
        var document = CodeFixTestSupport.CreateDocument(workspace, ATTRIBUTE_MODEL_SOURCE);
        var root = await document.GetSyntaxRootAsync(TestCancellation).ConfigureAwait(false);
        var attribute = FindAttribute(root!, "Range");
        var diagnostic = CodeFixTestSupport.CreateDiagnostic(
            DiagnosticDescriptors.RuleNotAllowedOnNullableType.Id,
            attribute.GetLocation(),
            ("PropertyName", "Age"));

        var text = await CodeFixTestSupport.ApplyFixAndGetTextAsync(
            document,
            diagnostic,
            new AddNotNullBeforeNumericRuleCodeFix(),
            cancellationToken: TestCancellation).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().NotBeNull());
        MSTestAssertions.That(() => text.Should().Contain("NotNull"));
        MSTestAssertions.That(() => text.Should().Contain("Range"));
    }

    [TestMethod]
    public async Task ConvertToBooleanLiteral_ReplacesTheStringAsync() {
        var text = await ApplyConversionAsync(
            "true",
            new ConvertToBooleanLiteralCodeFix()).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().NotBeNull());
        MSTestAssertions.That(() => text.Should().Contain("true"));
        MSTestAssertions.That(() => text.Should().NotContain("\"true\""));
    }

    [TestMethod]
    public async Task ConvertToDecimalLiteral_AddsTheDecimalSuffixAsync() {
        var text = await ApplyConversionAsync(
            "10.5",
            new ConvertToDecimalLiteralCodeFix()).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().Contain("10.5m"));
    }

    [TestMethod]
    public async Task ConvertToDateTimeLiteral_WrapsTheLiteralInParseAsync() {
        var text = await ApplyConversionAsync(
            "2024-01-02",
            new ConvertToDateTimeLiteralCodeFix()).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().Contain("DateTime.Parse(\"2024-01-02\")"));
    }

    [TestMethod]
    public async Task ConvertToDateOnlyLiteral_WrapsTheLiteralInParseAsync() {
        var text = await ApplyConversionAsync(
            "2024-02-03",
            new ConvertToDateOnlyLiteralCodeFix()).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().Contain("DateOnly.Parse(\"2024-02-03\")"));
    }

    [TestMethod]
    public async Task ConvertToTimeSpanLiteral_WrapsTheLiteralInParseAsync() {
        var text = await ApplyConversionAsync(
            "01:02:03",
            new ConvertToTimeSpanLiteralCodeFix()).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().Contain("TimeSpan.Parse(\"01:02:03\")"));
    }

    [TestMethod]
    public async Task ConvertToGuidLiteral_WrapsTheLiteralInParseAsync() {
        var text = await ApplyConversionAsync(
            "00000000-0000-0000-0000-000000000001",
            new ConvertToGuidLiteralCodeFix()).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().Contain(
            "Guid.Parse(\"00000000-0000-0000-0000-000000000001\")"));
    }

    [TestMethod]
    public async Task ConvertToEnumLiteral_QualifiesTheMemberAsync() {
        using var workspace = new AdhocWorkspace();
        var document = CodeFixTestSupport.CreateDocument(workspace, CONVERSION_MODEL_SOURCE);
        var root = await document.GetSyntaxRootAsync(TestCancellation).ConfigureAwait(false);
        var literal = FindStringLiteral(root!, "Red");
        var diagnostic = CodeFixTestSupport.CreateDiagnostic(
            DiagnosticDescriptors.InvalidRuleParameter.Id,
            literal.GetLocation(),
            ("TargetType", "TestNamespace.Color"));

        var text = await CodeFixTestSupport.ApplyFixAndGetTextAsync(
            document,
            diagnostic,
            new ConvertToEnumLiteralCodeFix(),
            cancellationToken: TestCancellation).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().Contain("TestNamespace.Color.Red"));
    }

    [TestMethod]
    public async Task FixLengthAndNotEmptyConflict_OffersBothFixesAsync() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            public sealed class Model {
                [Length(0, 10)]
                [NotEmpty]
                public string? Name { get; set; }
            }
            """;

        using var workspace = new AdhocWorkspace();
        var document = CodeFixTestSupport.CreateDocument(workspace, source);
        var root = await document.GetSyntaxRootAsync(TestCancellation).ConfigureAwait(false);
        var notEmpty = FindAttribute(root!, "NotEmpty");
        var diagnostic = CodeFixTestSupport.CreateDiagnostic(
            DiagnosticDescriptors.InvalidRuleCombination.Id,
            notEmpty.GetLocation(),
            ("Rule1", "Length"),
            ("Rule2", "NotEmpty"));

        var titles = await CodeFixTestSupport.GetFixActionTitlesAsync(
            document,
            diagnostic,
            new FixLengthAndNotEmptyConflictCodeFix(),
            TestCancellation).ConfigureAwait(false);

        MSTestAssertions.That(() => titles.Should().Contain("Remove [NotEmpty]"));
        MSTestAssertions.That(() => titles.Should().Contain("Change Length(min=0) to Length(min=1)"));

        var text = await CodeFixTestSupport.ApplyFixAndGetTextAsync(
            document,
            diagnostic,
            new FixLengthAndNotEmptyConflictCodeFix(),
            actionIndex: 0,
            TestCancellation).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().NotContain("NotEmpty"));
        MSTestAssertions.That(() => text.Should().Contain("Length"));
    }

    [TestMethod]
    public async Task FixLengthAndNotEmptyConflict_RaisesTheMinimumTo1Async() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            public sealed class Model {
                [Length(0, 10)]
                [NotEmpty]
                public string? Name { get; set; }
            }
            """;

        using var workspace = new AdhocWorkspace();
        var document = CodeFixTestSupport.CreateDocument(workspace, source);
        var root = await document.GetSyntaxRootAsync(TestCancellation).ConfigureAwait(false);
        var length = FindAttribute(root!, "Length");
        var diagnostic = CodeFixTestSupport.CreateDiagnostic(
            DiagnosticDescriptors.InvalidRuleCombination.Id,
            length.GetLocation(),
            ("Rule1", "Length"),
            ("Rule2", "NotEmpty"));

        var text = await CodeFixTestSupport.ApplyFixAndGetTextAsync(
            document,
            diagnostic,
            new FixLengthAndNotEmptyConflictCodeFix(),
            actionIndex: 1,
            TestCancellation).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().Contain("Length(1, 10)"));
    }

    [TestMethod]
    public async Task RemoveConflictingRule_RemovesTheSelectedRuleAsync() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            public sealed class Model {
                [GreaterThan(10)]
                [LessThan(5)]
                public int Value { get; set; }
            }
            """;

        using var workspace = new AdhocWorkspace();
        var document = CodeFixTestSupport.CreateDocument(workspace, source);
        var root = await document.GetSyntaxRootAsync(TestCancellation).ConfigureAwait(false);
        var greaterThan = FindAttribute(root!, "GreaterThan");
        var diagnostic = CodeFixTestSupport.CreateDiagnostic(
            DiagnosticDescriptors.ConflictingRules.Id,
            greaterThan.GetLocation(),
            ("Rules", "GreaterThan,LessThan"));

        var text = await CodeFixTestSupport.ApplyFixAndGetTextAsync(
            document,
            diagnostic,
            new RemoveConflictingRuleCodeFix(),
            actionIndex: 0,
            TestCancellation).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().NotContain("GreaterThan"));
        MSTestAssertions.That(() => text.Should().Contain("LessThan"));
    }

    [TestMethod]
    public async Task SwapInclusiveBetweenParameters_SwapsTheArgumentsAsync() {
        const string source =
            """
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            public sealed class Model {
                [InclusiveBetween(10, 1)]
                public int Age { get; set; }
            }
            """;

        using var workspace = new AdhocWorkspace();
        var document = CodeFixTestSupport.CreateDocument(workspace, source);
        var root = await document.GetSyntaxRootAsync(TestCancellation).ConfigureAwait(false);
        var attribute = FindAttribute(root!, "InclusiveBetween");
        var diagnostic = CodeFixTestSupport.CreateDiagnostic(
            DiagnosticDescriptors.RuleParameterOutOfRange.Id,
            attribute.GetLocation());

        var text = await CodeFixTestSupport.ApplyFixAndGetTextAsync(
            document,
            diagnostic,
            new SwapInclusiveBetweenParametersCodeFix(),
            cancellationToken: TestCancellation).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().Contain("InclusiveBetween(1, 10)"));
    }

    [TestMethod]
    public async Task UseNameofForRuleForMember_ReplacesTheStringAsync() {
        const string source =
            """
            using Jeninnet.Validation.Fluent;

            namespace TestNamespace;

            public sealed class Model {
                public string? Name { get; set; }
            }

            public sealed class ModelValidator : Validator<Model> {
                protected override void Configure(ValidatorRuleBuilder<Model> builder) {
                    builder.RuleFor(static x => x.Name, "NameTypo").NotEmpty();
                }
            }
            """;

        using var workspace = new AdhocWorkspace();
        var document = CodeFixTestSupport.CreateDocument(workspace, source);
        var root = await document.GetSyntaxRootAsync(TestCancellation).ConfigureAwait(false);
        var literal = FindStringLiteral(root!, "NameTypo");
        var diagnostic = CodeFixTestSupport.CreateDiagnostic(
            DiagnosticDescriptors.RuleForMemberNameMismatch.Id,
            literal.GetLocation(),
            ("MemberName", "Name"),
            ("ModelType", "TestNamespace.Model"));

        var text = await CodeFixTestSupport.ApplyFixAndGetTextAsync(
            document,
            diagnostic,
            new UseNameofForRuleForMemberCodeFix(),
            cancellationToken: TestCancellation).ConfigureAwait(false);

        MSTestAssertions.That(() => text.Should().Contain("nameof(TestNamespace.Model.Name)"));
    }

    [TestMethod]
    public void AllProviders_ExposeABatchFixAllProvider() {
        var providers = AllProviders();

        MSTestAssertions.That(() => providers.Should().HaveCount(15));

        foreach(var provider in providers) {
            MSTestAssertions.That(() => provider.GetFixAllProvider().Should().NotBeNull(
                $"{provider.GetType().Name} must support FixAll."));
        }
    }

    [TestMethod]
    public async Task Providers_DoNotOfferFixesForUnrelatedDiagnosticsAsync() {
        using var workspace = new AdhocWorkspace();
        var document = CodeFixTestSupport.CreateDocument(
            workspace,
            "namespace TestNamespace; public sealed class Empty { }");
        var root = await document.GetSyntaxRootAsync(TestCancellation).ConfigureAwait(false);
        var classNode = root!.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        var diagnostic = CodeFixTestSupport.CreateDiagnostic(
            DiagnosticDescriptors.SuggestNotEmptyForString.Id,
            classNode.GetLocation());

        foreach(var provider in AllProviders().Where(static provider => provider is not UseValidateAsyncCodeFix)) {
            var titles = await CodeFixTestSupport.GetFixActionTitlesAsync(
                document,
                diagnostic,
                provider,
                TestCancellation).ConfigureAwait(false);

            MSTestAssertions.That(() => titles.Should().BeEmpty(
                $"{provider.GetType().Name} must not offer a fix for an unrelated class declaration; " +
                $"found: {string.Join(", ", titles)}."));
        }
    }

    private async Task<string?> ApplyConversionAsync(string literalValue, CodeFixProvider provider) {
        using var workspace = new AdhocWorkspace();
        var document = CodeFixTestSupport.CreateDocument(workspace, CONVERSION_MODEL_SOURCE);
        var root = await document.GetSyntaxRootAsync(TestCancellation).ConfigureAwait(false);
        var literal = FindStringLiteral(root!, literalValue);
        var diagnostic = CodeFixTestSupport.CreateDiagnostic(
            DiagnosticDescriptors.InvalidRuleParameter.Id,
            literal.GetLocation());

        return await CodeFixTestSupport.ApplyFixAndGetTextAsync(
            document,
            diagnostic,
            provider,
            cancellationToken: TestCancellation).ConfigureAwait(false);
    }

    private static AttributeSyntax FindAttribute(SyntaxNode root, string name) =>
        root.DescendantNodes()
            .OfType<AttributeSyntax>()
            .Single(attribute => attribute.Name.ToString() == name);

    private static LiteralExpressionSyntax FindStringLiteral(SyntaxNode root, string value) =>
        root.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Single(literal =>
                literal.IsKind(SyntaxKind.StringLiteralExpression) &&
                literal.Token.ValueText == value);

    private static CodeFixProvider[] AllProviders() =>
        [
            new AddNotEmptySuggestionCodeFix(),
            new AddNotNullBeforeNumericRuleCodeFix(),
            new AddNotNullSuggestionCodeFix(),
            new ConvertToBooleanLiteralCodeFix(),
            new ConvertToDateOnlyLiteralCodeFix(),
            new ConvertToDateTimeLiteralCodeFix(),
            new ConvertToDecimalLiteralCodeFix(),
            new ConvertToEnumLiteralCodeFix(),
            new ConvertToGuidLiteralCodeFix(),
            new ConvertToTimeSpanLiteralCodeFix(),
            new FixLengthAndNotEmptyConflictCodeFix(),
            new RemoveConflictingRuleCodeFix(),
            new SwapInclusiveBetweenParametersCodeFix(),
            new UseNameofForRuleForMemberCodeFix(),
            new UseValidateAsyncCodeFix()
        ];

    private CancellationToken TestCancellation =>
        TestContext?.CancellationToken ?? CancellationToken.None;

    public TestContext? TestContext { get; set; }
}
