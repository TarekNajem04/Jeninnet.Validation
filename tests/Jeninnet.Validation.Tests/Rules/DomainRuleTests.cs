//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Rules;

internal sealed class DomainRuleModel {
    public string? Iban { get; set; }
    public string? Vat { get; set; }
    public string? Postal { get; set; }
    public string? Bsn { get; set; }
    public string? Password { get; set; }
    public string? Uuid { get; set; }
    public string? Ulid { get; set; }
}

file sealed class DomainRuleValidator : Validator<DomainRuleModel> {
    protected override void Configure(ValidatorRuleBuilder<DomainRuleModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Iban, nameof(DomainRuleModel.Iban)).Iban("NL");
        builder.RuleFor(static x => x.Vat, nameof(DomainRuleModel.Vat)).VatNumber("NL");
        builder.RuleFor(static x => x.Postal, nameof(DomainRuleModel.Postal)).PostalCode("NL");
        builder.RuleFor(static x => x.Bsn, nameof(DomainRuleModel.Bsn)).NationalId("NL");
        builder.RuleFor(static x => x.Password, nameof(DomainRuleModel.Password)).PasswordStrength(PasswordPolicy.Strict);
        builder.RuleFor(static x => x.Uuid, nameof(DomainRuleModel.Uuid)).Uuid();
        builder.RuleFor(static x => x.Ulid, nameof(DomainRuleModel.Ulid)).Ulid();
    }
}

[TestClass]
public sealed class DomainRuleTests {
    public TestContext TestContext { get; set; } = null!;
    private static readonly IPropertyAccessor<DomainRuleModel, string?> _iban =
        ValidatorFactory.Accessor<DomainRuleModel, string?>(static x => x.Iban, nameof(DomainRuleModel.Iban));

    private static readonly IPropertyAccessor<DomainRuleModel, string?> _vat =
        ValidatorFactory.Accessor<DomainRuleModel, string?>(static x => x.Vat, nameof(DomainRuleModel.Vat));

    private static readonly IPropertyAccessor<DomainRuleModel, string?> _postal =
        ValidatorFactory.Accessor<DomainRuleModel, string?>(static x => x.Postal, nameof(DomainRuleModel.Postal));

    private static readonly IPropertyAccessor<DomainRuleModel, string?> _bsn =
        ValidatorFactory.Accessor<DomainRuleModel, string?>(static x => x.Bsn, nameof(DomainRuleModel.Bsn));

    private static readonly IPropertyAccessor<DomainRuleModel, string?> _password =
        ValidatorFactory.Accessor<DomainRuleModel, string?>(static x => x.Password, nameof(DomainRuleModel.Password));

    private static readonly IPropertyAccessor<DomainRuleModel, string?> _uuid =
        ValidatorFactory.Accessor<DomainRuleModel, string?>(static x => x.Uuid, nameof(DomainRuleModel.Uuid));

    private static readonly IPropertyAccessor<DomainRuleModel, string?> _ulid =
        ValidatorFactory.Accessor<DomainRuleModel, string?>(static x => x.Ulid, nameof(DomainRuleModel.Ulid));

    [TestMethod]
    public void Iban_ValidatesChecksumAndCountry() {
        AssertRule(new IbanRule<DomainRuleModel>(_iban), "NL91ABNA0417164300", "NL91ABNA0417164301");
        AssertRule(new IbanRule<DomainRuleModel>(_iban, "NL"), "NL91 ABNA 0417 1643 00", "DE89370400440532013000");
        AssertRule(new IbanRule<DomainRuleModel>(_iban), "DE89370400440532013000", "DE89370400440532013001");

        Action act = static () => _ = new IbanRule<DomainRuleModel>(_iban, "ZZ");
        MSTestAssertions.That(() => act.Should().ThrowExactly<NotSupportedException>());
    }

    [TestMethod]
    public void VatNumber_ValidatesSupportedCountries() {
        AssertRule(new VatNumberRule<DomainRuleModel>(_vat, "NL"), "NL111222333B01", "NL111222334B01");
        AssertRule(new VatNumberRule<DomainRuleModel>(_vat, "DE"), "DE123456788", "DE123456789");
        AssertRule(new VatNumberRule<DomainRuleModel>(_vat, "FR"), "FR40303265045", "FR40303265046");
        AssertRule(new VatNumberRule<DomainRuleModel>(_vat, "GB"), "GB123456782", "GB123456789");
        AssertRule(new VatNumberRule<DomainRuleModel>(_vat, "BE"), "BE0411905847", "BE0411905848");

        Action act = static () => _ = new VatNumberRule<DomainRuleModel>(_vat, "US");
        MSTestAssertions.That(() => act.Should().ThrowExactly<NotSupportedException>());
    }

    [TestMethod]
    public void PostalCode_ValidatesDutchAndOtherFormats() {
        AssertRule(new PostalCodeRule<DomainRuleModel>(_postal, "NL"), "1234 AB", "1234 SA");
        AssertRule(new PostalCodeRule<DomainRuleModel>(_postal, "NL"), "1234AB", "0123 AB");
        AssertRule(new PostalCodeRule<DomainRuleModel>(_postal, "US"), "12345-6789", "1234");
        AssertRule(new PostalCodeRule<DomainRuleModel>(_postal, "DE"), "10115", "01115");
        AssertRule(new PostalCodeRule<DomainRuleModel>(_postal, "FR"), "75001", "7500");
        AssertRule(new PostalCodeRule<DomainRuleModel>(_postal, "GB"), "SW1A 1AA", "SW1A 1A1");
        AssertRule(new PostalCodeRule<DomainRuleModel>(_postal, "BE"), "1000", "0000");
        AssertRule(new PostalCodeRule<DomainRuleModel>(_postal, "EG"), "11511", "1151");

        Action act = static () => _ = new PostalCodeRule<DomainRuleModel>(_postal, "ZZ");
        MSTestAssertions.That(() => act.Should().ThrowExactly<NotSupportedException>());
    }

    [TestMethod]
    public void NationalId_ValidatesBsnEgyptianIdAndSsn() {
        AssertRule(new NationalIdRule<DomainRuleModel>(_bsn, "NL"), "111222333", "111222334");
        AssertRule(new NationalIdRule<DomainRuleModel>(_bsn, "NL"), "111 222 333", "000000000");
        AssertRule(new NationalIdRule<DomainRuleModel>(_bsn, "EG"), "39001010112345", "39002300112345");
        AssertRule(new NationalIdRule<DomainRuleModel>(_bsn, "US"), "123-45-6789", "000-12-3456");
        AssertRule(new NationalIdRule<DomainRuleModel>(_bsn, "US"), "123456789", "666-12-3456");

        Action act = static () => _ = new NationalIdRule<DomainRuleModel>(_bsn, "DE");
        MSTestAssertions.That(() => act.Should().ThrowExactly<NotSupportedException>());
    }

    [TestMethod]
    public void PasswordStrength_UsesTheConfiguredPolicy() {
        AssertRule(
            new PasswordStrengthRule<DomainRuleModel>(_password, PasswordPolicy.Strict),
            "Str0ng!Passw0rd2026",
            "password");

        AssertRule(
            new PasswordStrengthRule<DomainRuleModel>(_password, PasswordPolicy.Basic),
            "Welkom123",
            "welkom");

        var custom = new PasswordPolicy {
            Name = "Custom",
            MinLength = 6,
            MaxLength = 20,
            RequireUppercase = false,
            RequireLowercase = false,
            RequireSymbol = false,
            DisallowCommonPasswords = false
        };

        AssertRule(new PasswordStrengthRule<DomainRuleModel>(_password, custom), "abc123", "abc");

        Action act = static () => _ = new PasswordStrengthRule<DomainRuleModel>(
            _password,
            new PasswordPolicy { MinLength = 0 });
        MSTestAssertions.That(() => act.Should().ThrowExactly<ArgumentException>());
    }

    [TestMethod]
    public void UuidAndUlid_ValidateCanonicalValues() {
        AssertRule(new UuidRule<DomainRuleModel>(_uuid), "6f9619ff-8b86-d011-b42d-00c04fc964ff", "not-a-uuid");
        AssertRule(new UlidRule<DomainRuleModel>(_ulid), "01ARZ3NDEKTSV4RRFFQ69G5FAV", "01ARZ3NDEKTSV4RRFFQ69G5FAI");
        AssertRule(new UlidRule<DomainRuleModel>(_ulid), "01arz3ndektsv4rrffq69g5fav", "81ARZ3NDEKTSV4RRFFQ69G5FAV");
    }

    [TestMethod]
    public void RuleMessagesCarryDomainParameters() {
        var validator = new DomainRuleValidator();
        var result = validator.Validate(new DomainRuleModel {
            Iban = "invalid",
            Vat = "invalid",
            Postal = "invalid",
            Bsn = "invalid",
            Password = "password",
            Uuid = "invalid",
            Ulid = "invalid"
        });

        var vatError = result.Errors.Single(static error => error.MemberName == nameof(DomainRuleModel.Vat));
        MSTestAssertions.That(() => vatError.MessageKey.Should().Be(ValidationMessageKeys.VAT_NUMBER));
        MSTestAssertions.That(() => vatError.MessageParameters![MessageParameterNames.Country].Should().Be("NL"));
        MSTestAssertions.That(() => vatError.MessageParameters![MessageParameterNames.PropertyValue].Should().Be("invalid"));

        var passwordError = result.Errors.Single(static error => error.MemberName == nameof(DomainRuleModel.Password));
        MSTestAssertions.That(() => passwordError.MessageKey.Should().Be(ValidationMessageKeys.PASSWORD_STRENGTH));
        MSTestAssertions.That(() => passwordError.MessageParameters![MessageParameterNames.Policy].Should().Be("Strict"));
        MSTestAssertions.That(() => passwordError.MessageParameters![MessageParameterNames.MinLength].Should().Be(16));
        MSTestAssertions.That(() => passwordError.MessageParameters![MessageParameterNames.MissingRequirements]
            .Should().BeOfType<string>());
        MSTestAssertions.That(() => ((string)passwordError.MessageParameters![MessageParameterNames.MissingRequirements])
            .Should().Contain("length"));
    }

    [TestMethod]
    public void DescriptorAndClientRules_ExposeDomainMetadata() {
        var descriptor = new DomainRuleValidator().Describe();

        var ibanRule = descriptor.RulesForMember(nameof(DomainRuleModel.Iban)).Single();
        MSTestAssertions.That(() => ibanRule.Kind.Should().Be("Iban"));
        MSTestAssertions.That(() => ibanRule.Parameters[MessageParameterNames.Country].Should().Be("NL"));

        var passwordRule = descriptor.RulesForMember(nameof(DomainRuleModel.Password)).Single();
        MSTestAssertions.That(() => passwordRule.Kind.Should().Be("PasswordStrength"));
        MSTestAssertions.That(() => passwordRule.Parameters[MessageParameterNames.Policy].Should().Be("Strict"));
        MSTestAssertions.That(() => passwordRule.Parameters[MessageParameterNames.MinLength].Should().Be(16));

        var document = JsonNode.Parse(ClientRulesWriter.WriteToString([descriptor]))!;
        var members = document["validators"]![0]!["members"]!.AsArray();
        var ibanClientRule = FindRule(members, nameof(DomainRuleModel.Iban), "Iban");
        MSTestAssertions.That(() => ibanClientRule["serverOnly"]!.GetValue<bool>().Should().BeTrue());
        MSTestAssertions.That(() => ibanClientRule["params"]![MessageParameterNames.Country]!.GetValue<string>().Should().Be("NL"));

        var passwordClientRule = FindRule(members, nameof(DomainRuleModel.Password), "PasswordStrength");
        MSTestAssertions.That(() => passwordClientRule["serverOnly"]!.GetValue<bool>().Should().BeTrue());
        MSTestAssertions.That(() => passwordClientRule["params"]![MessageParameterNames.Policy]!.GetValue<string>().Should().Be("Strict"));
    }

    [TestMethod]
    public void JsonSchema_ExposesDomainPatternsAndFormats() {
        var descriptor = new DomainRuleValidator().Describe();
        var schema = ValidationSchemaWriter.Write(descriptor);
        var properties = schema["properties"]!;

        MSTestAssertions.That(() => properties[nameof(DomainRuleModel.Iban)]!["pattern"]!.GetValue<string>()
            .Should().Contain("NL"));
        MSTestAssertions.That(() => properties[nameof(DomainRuleModel.Postal)]!["pattern"]!.GetValue<string>()
            .Should().Contain("SA"));
        MSTestAssertions.That(() => properties[nameof(DomainRuleModel.Password)]!["minLength"]!.GetValue<int>()
            .Should().Be(16));
        MSTestAssertions.That(() => properties[nameof(DomainRuleModel.Uuid)]!["format"]!.GetValue<string>()
            .Should().Be("uuid"));
        MSTestAssertions.That(() => properties[nameof(DomainRuleModel.Ulid)]!["pattern"]!.GetValue<string>()
            .Should().Contain("[0-7]"));
    }

    [TestMethod]
    public async Task Localization_RendersDutchDomainMessagesAsync() {
        await using var provider = BuildProvider();
        var localizer = provider.GetRequiredService<IMessageLocalizer>();
        var result = await new DomainRuleValidator().ValidateAsync(
            new DomainRuleModel {
                Vat = "invalid",
                Password = "password"
            },
            TestContext.CancellationToken);

        var vat = await localizer.LocalizeValidationErrorAsync(
            result.Errors.Single(static error => error.MemberName == nameof(DomainRuleModel.Vat)),
            new CultureInfo("nl"),
            TestContext.CancellationToken);
        var password = await localizer.LocalizeValidationErrorAsync(
            result.Errors.Single(static error => error.MemberName == nameof(DomainRuleModel.Password)),
            new CultureInfo("nl"),
            TestContext.CancellationToken);

        MSTestAssertions.That(() => vat.Should().Be("Vat moet een geldig btw-nummer zijn voor NL."));
        MSTestAssertions.That(() => password.Should().Contain("wachtwoordbeleid Strict"));
        MSTestAssertions.That(() => password.Should().Contain("length"));
    }

    private static void AssertRule(
        ISyncValidationRule<DomainRuleModel> rule,
        string validValue,
        string invalidValue) {
        var valid = RuleRunner.Run(rule, new DomainRuleModel {
            Iban = validValue,
            Vat = validValue,
            Postal = validValue,
            Bsn = validValue,
            Password = validValue,
            Uuid = validValue,
            Ulid = validValue
        });

        var invalid = RuleRunner.Run(rule, new DomainRuleModel {
            Iban = invalidValue,
            Vat = invalidValue,
            Postal = invalidValue,
            Bsn = invalidValue,
            Password = invalidValue,
            Uuid = invalidValue,
            Ulid = invalidValue
        });

        MSTestAssertions.That(() => valid.IsValid.Should().BeTrue());
        MSTestAssertions.That(() => invalid.Errors.Should().HaveCount(1));
    }

    private static JsonNode FindRule(JsonArray members, string memberName, string ruleName) {
        var member = members.Single(node => node!["name"]!.GetValue<string>() == memberName)!;
        return member["rules"]!.AsArray().Single(node => node!["rule"]!.GetValue<string>() == ruleName)!;
    }

    private static ServiceProvider BuildProvider() {
        var services = new ServiceCollection();
        services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
        JeninnetLocalizationExtensions.AddLocalization(services);
        services.AddDefaultMessagePacks();
        return services.BuildServiceProvider();
    }
}
