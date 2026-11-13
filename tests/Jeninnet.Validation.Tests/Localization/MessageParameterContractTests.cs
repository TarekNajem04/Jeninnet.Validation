//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Localization;

// ---------------------------------------------------------------------------
// Model and validators
// ---------------------------------------------------------------------------

internal enum ParameterKind {
    None = 0,
    Basic = 1
}

internal sealed class ParameterLine {
    public string? Name { get; set; }
}

internal sealed class MessageParameterModel {
    public int Score { get; set; }
    public int Left { get; set; }
    public int Right { get; set; }
    public int Bounded { get; set; }
    public int Ranged { get; set; }
    public string? Sized { get; set; }
    public string? Exact { get; set; }
    public string? Pattern { get; set; }
    public string? FileName { get; set; }
    public string? ContainsText { get; set; }
    public string? StartsText { get; set; }
    public string? EndsText { get; set; }
    public int Allowed { get; set; }
    public int Disallowed { get; set; }
    public ParameterKind Kind { get; set; }
    public string? KindName { get; set; }
    public decimal Amount { get; set; }
    public List<string>? Items { get; set; }
    public List<string>? MinItems { get; set; }
    public List<string>? MaxItems { get; set; }
    public DateTime? Created { get; set; }
    public string? Password { get; set; }
    public string? Confirm { get; set; }
    public List<string?>? Tags { get; set; }
    public List<ParameterLine>? Lines { get; set; }
}

file sealed class ParameterLineValidator : Validator<ParameterLine> {
    protected override void Configure(ValidatorRuleBuilder<ParameterLine> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(ParameterLine.Name)).NotEmpty();
    }
}

file sealed class MessageParameterValidator : Validator<MessageParameterModel> {
    private static readonly int[] _allowedValues = [1, 2];
    private static readonly int[] _disallowedValues = [3];
    private static readonly DateTime _createdThreshold = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly IPropertyAccessor<MessageParameterModel, string?> _password =
        ValidatorFactory.Accessor<MessageParameterModel, string?>(
            static x => x.Password,
            nameof(MessageParameterModel.Password));

    private static readonly IPropertyAccessor<MessageParameterModel, int> _right =
        ValidatorFactory.Accessor<MessageParameterModel, int>(
            static x => x.Right,
            nameof(MessageParameterModel.Right));

    protected override void Configure(ValidatorRuleBuilder<MessageParameterModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Score, nameof(MessageParameterModel.Score)).GreaterThan(10);
        builder.RuleFor(static x => x.Left, nameof(MessageParameterModel.Left)).GreaterThan(_right);
        builder.RuleFor(static x => x.Bounded, nameof(MessageParameterModel.Bounded)).InclusiveBetween(1, 5);
        builder.RuleFor(static x => x.Ranged, nameof(MessageParameterModel.Ranged)).Range(1, 5);
        builder.RuleFor(static x => x.Sized, nameof(MessageParameterModel.Sized)).Length(2, 5);
        builder.RuleFor(static x => x.Exact, nameof(MessageParameterModel.Exact)).ExactLength(3);
        builder.RuleFor(static x => x.Pattern, nameof(MessageParameterModel.Pattern)).Matches("^[A-Z]+$");
        builder.RuleFor(static x => x.FileName, nameof(MessageParameterModel.FileName)).FileExtensions("pdf");
        builder.RuleFor(static x => x.ContainsText, nameof(MessageParameterModel.ContainsText)).Contains("needle");
        builder.RuleFor(static x => x.StartsText, nameof(MessageParameterModel.StartsText)).StartsWith("pre");
        builder.RuleFor(static x => x.EndsText, nameof(MessageParameterModel.EndsText)).EndsWith("post");
        builder.RuleFor(static x => x.Allowed, nameof(MessageParameterModel.Allowed)).In(_allowedValues);
        builder.RuleFor(static x => x.Disallowed, nameof(MessageParameterModel.Disallowed)).NotIn(_disallowedValues);
        builder.RuleFor(static x => x.Kind, nameof(MessageParameterModel.Kind)).Enum();
        builder.RuleFor(static x => x.KindName, nameof(MessageParameterModel.KindName)).IsEnumName(typeof(ParameterKind));
        builder.RuleFor(static x => x.Amount, nameof(MessageParameterModel.Amount)).ScalePrecision(2, 1);
        builder.RuleFor(static x => x.Items, nameof(MessageParameterModel.Items)).Count(1, 3);
        builder.RuleFor(static x => x.MinItems, nameof(MessageParameterModel.MinItems)).MinCount(2);
        builder.RuleFor(static x => x.MaxItems, nameof(MessageParameterModel.MaxItems)).MaxCount(1);
        builder.RuleFor(static x => x.Created, nameof(MessageParameterModel.Created)).After(_createdThreshold);
        builder.RuleFor(static x => x.Confirm, nameof(MessageParameterModel.Confirm)).Equal(_password);
        builder.RuleFor(static x => x.Tags, nameof(MessageParameterModel.Tags))
               .ForEachElement(static element => element.NotNull());
        builder.RuleFor(static x => x.Lines, nameof(MessageParameterModel.Lines))
               .ForEach(new ParameterLineValidator());
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class MessageParameterContractTests {
    public TestContext TestContext { get; set; } = null!;
    private static readonly DateTime _afterThreshold = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Rules_ExposeTheUnifiedParameters() {
        var result = ValidateInvalidModel();

        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Score), MessageParameterNames.ComparisonValue).Should().Be(10));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Score), "0").Should().Be(10));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Score), MessageParameterNames.PropertyValue).Should().Be(5));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Score), MessageParameterNames.PropertyPath).Should().Be(nameof(MessageParameterModel.Score)));

        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Left), MessageParameterNames.ComparisonProperty).Should().Be(nameof(MessageParameterModel.Right)));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Left), MessageParameterNames.ComparisonValue).Should().Be(2));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Left), MessageParameterNames.PropertyValue).Should().Be(1));

        AssertBounds(result, nameof(MessageParameterModel.Bounded), 1, 5);
        AssertBounds(result, nameof(MessageParameterModel.Ranged), 1, 5);

        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Sized), MessageParameterNames.Min).Should().Be(2));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Sized), MessageParameterNames.Max).Should().Be(5));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Sized), MessageParameterNames.TotalLength).Should().Be(1));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Sized), MessageParameterNames.PropertyValue).Should().Be("a"));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Sized), "0").Should().Be(2));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Sized), "1").Should().Be(5));

        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Exact), MessageParameterNames.Length).Should().Be(3));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Exact), MessageParameterNames.ExactLength).Should().Be(3));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Exact), "0").Should().Be(3));

        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Pattern), MessageParameterNames.Pattern).Should().Be("^[A-Z]+$"));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Pattern), "0").Should().Be("^[A-Z]+$"));

        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.FileName), MessageParameterNames.Extensions).Should().Be(".pdf"));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.FileName), "0").Should().Be(".pdf"));

        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.ContainsText), MessageParameterNames.Value).Should().Be("needle"));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.ContainsText), "0").Should().Be("needle"));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.StartsText), MessageParameterNames.Value).Should().Be("pre"));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.EndsText), MessageParameterNames.Value).Should().Be("post"));

        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Allowed), MessageParameterNames.Values).Should().Be("1, 2"));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Allowed), "0").Should().Be("1, 2"));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Disallowed), MessageParameterNames.Values).Should().Be("3"));

        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Kind), MessageParameterNames.EnumType).Should().Be(nameof(ParameterKind)));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Kind), "0").Should().Be(nameof(ParameterKind)));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.KindName), MessageParameterNames.EnumType).Should().Be(nameof(ParameterKind)));

        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Amount), MessageParameterNames.Precision).Should().Be(2));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Amount), MessageParameterNames.Scale).Should().Be(1));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Amount), "0").Should().Be(2));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Amount), "1").Should().Be(1));

        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Items), MessageParameterNames.Min).Should().Be(1));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Items), MessageParameterNames.Max).Should().Be(3));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Items), MessageParameterNames.Count).Should().Be(0));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.MinItems), MessageParameterNames.Min).Should().Be(2));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.MinItems), MessageParameterNames.Count).Should().Be(1));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.MaxItems), MessageParameterNames.Max).Should().Be(1));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.MaxItems), MessageParameterNames.Count).Should().Be(2));

        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Created), MessageParameterNames.ComparisonValue).Should().Be(_afterThreshold));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Created), "0").Should().Be(_afterThreshold));

        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Confirm), MessageParameterNames.ComparisonProperty).Should().Be(nameof(MessageParameterModel.Password)));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Confirm), MessageParameterNames.ComparisonValue).Should().Be("a"));
        MSTestAssertions.That(() => Parameter(result, nameof(MessageParameterModel.Confirm), MessageParameterNames.PropertyValue).Should().Be("b"));

        var tagError = ErrorFor(result, "Tags[1]");
        MSTestAssertions.That(() => tagError.MessageParameters![MessageParameterNames.CollectionIndex].Should().Be(1));
        MSTestAssertions.That(() => tagError.MessageParameters![MessageParameterNames.PropertyPath].Should().Be("Tags[1]"));

        var lineError = ErrorFor(result, "Lines[0].Name");
        MSTestAssertions.That(() => lineError.MessageParameters![MessageParameterNames.PropertyPath].Should().Be("Lines[0].Name"));
        MSTestAssertions.That(() => lineError.MessageParameters![MessageParameterNames.PropertyName].Should().Be("Lines[0].Name"));

        foreach(var error in result.Errors) {
            MSTestAssertions.That(() => error.MessageParameters.Should().NotBeNull());
            MSTestAssertions.That(() => error.MessageParameters!.ContainsKey(MessageParameterNames.PropertyName).Should().BeTrue());
            MSTestAssertions.That(() => error.MessageParameters!.ContainsKey(MessageParameterNames.PropertyValue).Should().BeTrue());
            MSTestAssertions.That(() => error.MessageParameters![MessageParameterNames.PropertyPath].Should().Be(error.MemberName));
        }
    }

    [TestMethod]
    public async Task Localization_RendersSemanticParametersInEnglishAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var result = ValidateInvalidModel();

        var score = await localizer.LocalizeValidationErrorAsync(
            ErrorFor(result, nameof(MessageParameterModel.Score)),
            CultureInfo.GetCultureInfo("en"), TestContext.CancellationToken);
        var sized = await localizer.LocalizeValidationErrorAsync(
            ErrorFor(result, nameof(MessageParameterModel.Sized)),
            CultureInfo.GetCultureInfo("en"), TestContext.CancellationToken);
        var exact = await localizer.LocalizeValidationErrorAsync(
            ErrorFor(result, nameof(MessageParameterModel.Exact)),
            CultureInfo.GetCultureInfo("en"), TestContext.CancellationToken);
        var pattern = await localizer.LocalizeValidationErrorAsync(
            ErrorFor(result, nameof(MessageParameterModel.Pattern)),
            CultureInfo.GetCultureInfo("en"), TestContext.CancellationToken);
        var extensions = await localizer.LocalizeValidationErrorAsync(
            ErrorFor(result, nameof(MessageParameterModel.FileName)),
            CultureInfo.GetCultureInfo("en"), TestContext.CancellationToken);
        var values = await localizer.LocalizeValidationErrorAsync(
            ErrorFor(result, nameof(MessageParameterModel.Allowed)),
            CultureInfo.GetCultureInfo("en"), TestContext.CancellationToken);
        var enumType = await localizer.LocalizeValidationErrorAsync(
            ErrorFor(result, nameof(MessageParameterModel.Kind)),
            CultureInfo.GetCultureInfo("en"), TestContext.CancellationToken);
        var count = await localizer.LocalizeValidationErrorAsync(
            ErrorFor(result, nameof(MessageParameterModel.Items)),
            CultureInfo.GetCultureInfo("en"), TestContext.CancellationToken);
        var confirm = await localizer.LocalizeValidationErrorAsync(
            ErrorFor(result, nameof(MessageParameterModel.Confirm)),
            CultureInfo.GetCultureInfo("en"), TestContext.CancellationToken);
        var tag = await localizer.LocalizeValidationErrorAsync(
            ErrorFor(result, "Tags[1]"),
            CultureInfo.GetCultureInfo("en"), TestContext.CancellationToken);
        var line = await localizer.LocalizeValidationErrorAsync(
            ErrorFor(result, "Lines[0].Name"),
            CultureInfo.GetCultureInfo("en"), TestContext.CancellationToken);

        MSTestAssertions.That(() => score.Should().Be("Score|5|Score|10"));
        MSTestAssertions.That(() => sized.Should().Be("2|5|1"));
        MSTestAssertions.That(() => exact.Should().Be("3|3"));
        MSTestAssertions.That(() => pattern.Should().Be("^[A-Z]+$"));
        MSTestAssertions.That(() => extensions.Should().Be(".pdf"));
        MSTestAssertions.That(() => values.Should().Be("1, 2"));
        MSTestAssertions.That(() => enumType.Should().Be(nameof(ParameterKind)));
        MSTestAssertions.That(() => count.Should().Be("1|3|0"));
        MSTestAssertions.That(() => confirm.Should().Be("b|a|Password"));
        MSTestAssertions.That(() => tag.Should().Be("Tags[1]|1"));
        MSTestAssertions.That(() => line.Should().Be("Lines[0].Name|Lines[0].Name|"));
    }

    [TestMethod]
    public void ClientRules_ExportTheSemanticParameterNames() {
        var descriptor = new MessageParameterValidator().Describe();
        var document = JsonNode.Parse(ClientRulesWriter.WriteToString([descriptor]))!;
        var members = document["validators"]![0]!["members"]!.AsArray();

        var scoreRule = RuleFor(members, nameof(MessageParameterModel.Score), "GreaterThan");
        MSTestAssertions.That(() => scoreRule["params"]![MessageParameterNames.ComparisonValue]!.GetValue<int>().Should().Be(10));

        var boundedRule = RuleFor(members, nameof(MessageParameterModel.Bounded), "InclusiveBetween");
        MSTestAssertions.That(() => boundedRule["params"]![MessageParameterNames.From]!.GetValue<int>().Should().Be(1));
        MSTestAssertions.That(() => boundedRule["params"]![MessageParameterNames.To]!.GetValue<int>().Should().Be(5));

        var exactRule = RuleFor(members, nameof(MessageParameterModel.Exact), "ExactLength");
        MSTestAssertions.That(() => exactRule["params"]![MessageParameterNames.ExactLength]!.GetValue<int>().Should().Be(3));

        var leftRule = RuleFor(members, nameof(MessageParameterModel.Left), "PropertyGreaterThan");
        MSTestAssertions.That(() => leftRule["params"]![MessageParameterNames.ComparisonProperty]!.GetValue<string>().Should().Be(nameof(MessageParameterModel.Right)));
    }

    private static ValidationResult ValidateInvalidModel() =>
        new MessageParameterValidator().Validate(new MessageParameterModel {
            Score = 5,
            Left = 1,
            Right = 2,
            Bounded = 10,
            Ranged = 10,
            Sized = "a",
            Exact = "a",
            Pattern = "abc",
            FileName = "x.txt",
            ContainsText = "haystack",
            StartsText = "x",
            EndsText = "x",
            Allowed = 3,
            Disallowed = 3,
            Kind = (ParameterKind)99,
            KindName = "Nope",
            Amount = 12.34m,
            Items = [],
            MinItems = ["x"],
            MaxItems = ["x", "y"],
            Created = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Password = "a",
            Confirm = "b",
            Tags = ["ok", null],
            Lines = [new ParameterLine { Name = "" }]
        });

    private static void AssertBounds(ValidationResult result, string memberName, int min, int max) {
        MSTestAssertions.That(() => Parameter(result, memberName, MessageParameterNames.Min).Should().Be(min));
        MSTestAssertions.That(() => Parameter(result, memberName, MessageParameterNames.Max).Should().Be(max));
        MSTestAssertions.That(() => Parameter(result, memberName, MessageParameterNames.From).Should().Be(min));
        MSTestAssertions.That(() => Parameter(result, memberName, MessageParameterNames.To).Should().Be(max));
        MSTestAssertions.That(() => Parameter(result, memberName, "0").Should().Be(min));
        MSTestAssertions.That(() => Parameter(result, memberName, "1").Should().Be(max));
    }

    private static ValidationError ErrorFor(ValidationResult result, string memberName) =>
        result.Errors.Single(error => error.MemberName == memberName);

    private static object? Parameter(ValidationResult result, string memberName, string key) =>
        ErrorFor(result, memberName).MessageParameters![key];

    private static JsonNode RuleFor(JsonArray members, string memberName, string ruleName) {
        var member = members.Single(node => node!["name"]!.GetValue<string>() == memberName)!;
        return member["rules"]!.AsArray().Single(node => node!["rule"]!.GetValue<string>() == ruleName)!;
    }

    private static ServiceProvider BuildProvider() {
        var services = new ServiceCollection();
        services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
        JeninnetLocalizationExtensions.AddLocalization(services);
        services.AddDefaultMessagePacks(options => options.Override("en", ValidationMessageKeys.GREATER_THAN, "{PropertyName}|{PropertyValue}|{PropertyPath}|{ComparisonValue}")
                .Override("en", ValidationMessageKeys.LENGTH, "{Min}|{Max}|{TotalLength}")
                .Override("en", ValidationMessageKeys.EXACT_LENGTH, "{Length}|{ExactLength}")
                .Override("en", ValidationMessageKeys.REGEX, "{Pattern}")
                .Override("en", ValidationMessageKeys.FILE_EXTENSIONS, "{Extensions}")
                .Override("en", ValidationMessageKeys.IN, "{Values}")
                .Override("en", ValidationMessageKeys.ENUM, "{EnumType}")
                .Override("en", ValidationMessageKeys.COUNT, "{Min}|{Max}|{Count}")
                .Override("en", ValidationMessageKeys.EQUAL, "{PropertyValue}|{ComparisonValue}|{ComparisonProperty}")
                .Override("en", ValidationMessageKeys.NOT_NULL, "{PropertyPath}|{CollectionIndex}")
                .Override("en", ValidationMessageKeys.NOT_EMPTY, "{PropertyPath}|{PropertyName}|{PropertyValue}"));

        return services.BuildServiceProvider();
    }
}
