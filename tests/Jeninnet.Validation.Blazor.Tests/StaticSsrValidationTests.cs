//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Blazor.Tests;

public sealed class StaticSsrRegistration {
    public string? Name { get; set; }

    public string? Code { get; set; }

    public int Age { get; set; }

    public string? Email { get; set; }

    public string? Reason { get; set; }
}

public sealed class StaticSsrRegistrationValidator : Validator<StaticSsrRegistration> {
    protected override void Configure(ValidatorRuleBuilder<StaticSsrRegistration> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(StaticSsrRegistration.Name))
               .NotEmpty()
               .Length(2, 10);

        builder.RuleFor(static x => x.Code, nameof(StaticSsrRegistration.Code))
               .Matches("^[A-Z]{3}$");

        builder.RuleFor(static x => x.Age, nameof(StaticSsrRegistration.Age))
               .InclusiveBetween(18, 120);

        builder.RuleFor(static x => x.Email, nameof(StaticSsrRegistration.Email))
               .EmailAddress();

        builder.When(
            static x => x.Age > 0,
            inner => inner.RuleFor(static x => x.Reason, nameof(StaticSsrRegistration.Reason)).NotEmpty());
    }
}

[TestClass]
public sealed class StaticSsrValidationTests {
    private static readonly IValidationDescriptor _descriptor = new StaticSsrRegistrationValidator().Describe();

    [TestMethod]
    public void For_MapsRequiredLengthRangeRegexAndEmail() {
        var name = JeninnetStaticSsrValidation.For(_descriptor, nameof(StaticSsrRegistration.Name));
        MSTestAssertions.That(() => name["data-val"].Should().Be("true"));
        MSTestAssertions.That(() => ((string)name["data-val-required"]).Should().Contain("required"));
        MSTestAssertions.That(() => name["data-val-length-min"].Should().Be("2"));
        MSTestAssertions.That(() => name["data-val-length-max"].Should().Be("10"));
        MSTestAssertions.That(() => name["data-jeninnet-member"].Should().Be("Name"));

        var code = JeninnetStaticSsrValidation.For(_descriptor, nameof(StaticSsrRegistration.Code));
        MSTestAssertions.That(() => code["data-val-regex-pattern"].Should().Be("^[A-Z]{3}$"));

        var age = JeninnetStaticSsrValidation.For(_descriptor, nameof(StaticSsrRegistration.Age));
        MSTestAssertions.That(() => age["data-val-range-min"].Should().Be("18"));
        MSTestAssertions.That(() => age["data-val-range-max"].Should().Be("120"));

        var email = JeninnetStaticSsrValidation.For(_descriptor, nameof(StaticSsrRegistration.Email));
        MSTestAssertions.That(() => email.ContainsKey("data-val-email").Should().BeTrue());
    }

    [TestMethod]
    public void For_MarksConditionalRulesServerOnly() {
        var reason = JeninnetStaticSsrValidation.For(_descriptor, nameof(StaticSsrRegistration.Reason));

        MSTestAssertions.That(() => reason.ContainsKey("data-val").Should().BeFalse());
        MSTestAssertions.That(() => reason["data-jeninnet-server-only"].Should().Be("true"));
        MSTestAssertions.That(() => ((string)reason["data-jeninnet-rules"]).Should().Contain("\"serverOnly\":true"));
    }

    [TestMethod]
    public void For_UnknownMemberReturnsEmptyAndMarkersAreStable() {
        JeninnetStaticSsrValidation.For(_descriptor, "Missing").Should().BeEmpty();
        JeninnetStaticSsrValidation.MessageFor("Name")["data-jeninnet-message-for"].Should().Be("Name");
        JeninnetStaticSsrValidation.FormAttributes()["data-jeninnet-validation"].Should().Be("true");
    }

    [TestMethod]
    public void For_RespectsTheMessageResolver() {
        var name = JeninnetStaticSsrValidation.For(
            _descriptor,
            nameof(StaticSsrRegistration.Name),
            static rule => rule.Kind == "Length" ? "localized length" : null);

        MSTestAssertions.That(() => name["data-val-length"].Should().Be("localized length"));
    }
}
