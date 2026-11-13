//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Fluent;

internal abstract class Contact {
    public string? Name { get; set; }
}

internal sealed class Person : Contact {
    public string? Email { get; set; }
}

internal sealed class Organisation : Contact {
    public string? Registration { get; set; }
}

internal sealed class UnregisteredContact : Contact;

internal sealed class ContactContainer {
    public Contact? Primary { get; set; }
    public Contact[]? Contacts { get; set; }
    public List<Contact>? ContactList { get; set; }
}

internal interface IContact {
    string? Name { get; set; }
}

internal sealed class InterfacePerson : IContact {
    public string? Name { get; set; }
    public string? Email { get; set; }
}

internal sealed class InterfaceOrganisation : IContact {
    public string? Name { get; set; }
    public string? Registration { get; set; }
}

internal sealed class InterfaceContactContainer {
    public IContact? Primary { get; set; }
    public IContact[]? Contacts { get; set; }
}

file sealed class PersonValidator : Validator<Person> {
    protected override void Configure(ValidatorRuleBuilder<Person> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(Person.Name)).NotEmpty();
        builder.RuleFor(static x => x.Email, nameof(Person.Email)).NotEmpty();
    }
}

file sealed class OrganisationValidator : Validator<Organisation> {
    protected override void Configure(ValidatorRuleBuilder<Organisation> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(Organisation.Name)).NotEmpty();
        builder.RuleFor(static x => x.Registration, nameof(Organisation.Registration)).NotEmpty();
    }
}

file sealed class AsyncOrganisationValidator : Validator<Organisation> {
    protected override void Configure(ValidatorRuleBuilder<Organisation> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(Organisation.Name)).NotEmpty();
        builder.RuleFor(static x => x.Registration, nameof(Organisation.Registration))
               .NotEmpty()
               .Must(static (value, _) => new ValueTask<bool>(value != "blocked"));
    }
}

file sealed class ContactContainerValidator : Validator<ContactContainer> {
    protected override void Configure(ValidatorRuleBuilder<ContactContainer> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Primary, nameof(ContactContainer.Primary))
               .SetInheritanceValidator(ConfigureMap);

        builder.RuleFor(static x => x.Contacts, nameof(ContactContainer.Contacts))
               .ForEach(ConfigureMap);

        builder.RuleFor(static x => x.ContactList, nameof(ContactContainer.ContactList))
               .ForEach(ConfigureMap);
    }

    private static void ConfigureMap(InheritanceValidatorMap<Contact> map) =>
        map.Add<Person>(new PersonValidator())
           .Add<Organisation>(new OrganisationValidator());
}

file sealed class InterfacePersonValidator : Validator<InterfacePerson> {
    protected override void Configure(ValidatorRuleBuilder<InterfacePerson> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(InterfacePerson.Name)).NotEmpty();
        builder.RuleFor(static x => x.Email, nameof(InterfacePerson.Email)).NotEmpty();
    }
}

file sealed class InterfaceOrganisationValidator : Validator<InterfaceOrganisation> {
    protected override void Configure(ValidatorRuleBuilder<InterfaceOrganisation> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(InterfaceOrganisation.Name)).NotEmpty();
        builder.RuleFor(static x => x.Registration, nameof(InterfaceOrganisation.Registration)).NotEmpty();
    }
}

file sealed class InterfaceContactContainerValidator : Validator<InterfaceContactContainer> {
    protected override void Configure(ValidatorRuleBuilder<InterfaceContactContainer> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Primary, nameof(InterfaceContactContainer.Primary))
               .SetInheritanceValidator(ConfigureMap);

        builder.RuleFor(static x => x.Contacts, nameof(InterfaceContactContainer.Contacts))
               .ForEach(ConfigureMap);
    }

    private static void ConfigureMap(InheritanceValidatorMap<IContact> map) =>
        map.Add<InterfacePerson>(new InterfacePersonValidator())
           .Add<InterfaceOrganisation>(new InterfaceOrganisationValidator());
}

file sealed class AsyncContactContainerValidator : Validator<ContactContainer> {
    protected override void Configure(ValidatorRuleBuilder<ContactContainer> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Primary, nameof(ContactContainer.Primary))
               .SetInheritanceValidator(static map => map.Add<Person>(new PersonValidator())
                       .Add<Organisation>(new AsyncOrganisationValidator()));
    }
}

file sealed class ContactBaseValidator : Validator<Contact> {
    protected override void Configure(ValidatorRuleBuilder<Contact> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(Contact.Name)).NotEmpty();
    }
}

file sealed class PersonIncludingBaseValidator : Validator<Person> {
    protected override void Configure(ValidatorRuleBuilder<Person> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.IncludeBase<Person, Contact>(new ContactBaseValidator());
        builder.RuleFor(static x => x.Email, nameof(Person.Email)).NotEmpty();
    }
}

[TestClass]
public sealed class InheritanceAndPolymorphismTests {
    [TestMethod]
    public void SetInheritanceValidator_RoutesPropertyToRuntimeType() {
        var validator = new ContactContainerValidator();

        var personResult = validator.Validate(
            new ContactContainer { Primary = new Person { Name = "Ada", Email = string.Empty } });

        ValidationAssert.ErrorCount(personResult, 1);
        ValidationAssert.HasError(personResult, "Primary.Email");

        var organisationResult = validator.Validate(
            new ContactContainer { Primary = new Organisation { Name = "Acme", Registration = string.Empty } });

        ValidationAssert.ErrorCount(organisationResult, 1);
        ValidationAssert.HasError(organisationResult, "Primary.Registration");
    }

    [TestMethod]
    public void SetInheritanceValidator_UnknownRuntimeTypeIsSkipped() {
        var validator = new ContactContainerValidator();

        var result = validator.Validate(
            new ContactContainer { Primary = new UnregisteredContact { Name = string.Empty } });

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public void ForEach_ValidatesEachElementByRuntimeType() {
        var validator = new ContactContainerValidator();

        var result = validator.Validate(
            new ContactContainer {
                Contacts = [
                    new Person { Name = "Ada", Email = string.Empty },
                    new Organisation { Name = "Acme", Registration = string.Empty }
                ]
            });

        ValidationAssert.ErrorCount(result, 2);
        ValidationAssert.HasError(result, "Contacts[0].Email");
        ValidationAssert.HasError(result, "Contacts[1].Registration");
    }

    [TestMethod]
    public void SetInheritanceValidator_RoutesInterfaceImplementations() {
        var validator = new InterfaceContactContainerValidator();

        var personResult = validator.Validate(
            new InterfaceContactContainer {
                Primary = new InterfacePerson { Name = "Ada", Email = string.Empty }
            });

        ValidationAssert.ErrorCount(personResult, 1);
        ValidationAssert.HasError(personResult, "Primary.Email");

        var organisationResult = validator.Validate(
            new InterfaceContactContainer {
                Primary = new InterfaceOrganisation { Name = "Acme", Registration = string.Empty }
            });

        ValidationAssert.ErrorCount(organisationResult, 1);
        ValidationAssert.HasError(organisationResult, "Primary.Registration");
    }

    [TestMethod]
    public void ForEach_ValidatesInterfaceElementsByRuntimeType() {
        var validator = new InterfaceContactContainerValidator();

        var result = validator.Validate(
            new InterfaceContactContainer {
                Contacts = [
                    new InterfacePerson { Name = "Ada", Email = string.Empty },
                    new InterfaceOrganisation { Name = "Acme", Registration = string.Empty }
                ]
            });

        ValidationAssert.ErrorCount(result, 2);
        ValidationAssert.HasError(result, "Contacts[0].Email");
        ValidationAssert.HasError(result, "Contacts[1].Registration");
    }

    [TestMethod]
    public void ForEach_ListElementsUseTheSameRuntimeDispatch() {
        var validator = new ContactContainerValidator();

        var result = validator.Validate(
            new ContactContainer {
                ContactList = [new Person { Name = "Ada", Email = string.Empty }]
            });

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "ContactList[0].Email");
    }

    [TestMethod]
    public void ForEach_UnknownElementIsSkipped() {
        var validator = new ContactContainerValidator();

        var result = validator.Validate(
            new ContactContainer {
                Contacts = [new UnregisteredContact { Name = string.Empty }]
            });

        ValidationAssert.IsValid(result);
    }

    [TestMethod]
    public async Task SetInheritanceValidator_AsyncPathDispatchesByRuntimeTypeAsync() {
        var validator = new AsyncContactContainerValidator();

        var result = await validator.ValidateAsync(
            new ContactContainer {
                Primary = new Organisation { Name = "Acme", Registration = "blocked" }
            },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, "Primary.Registration");
    }

    [TestMethod]
    public void IncludeBase_ComposesBaseAndDerivedRules() {
        var validator = new PersonIncludingBaseValidator();

        var result = validator.Validate(new Person { Name = string.Empty, Email = string.Empty });

        ValidationAssert.ErrorCount(result, 2);
        ValidationAssert.HasError(result, "Name");
        ValidationAssert.HasError(result, "Email");
    }

    public TestContext? TestContext { get; set; }

    [TestMethod]
    public void InheritanceValidatorMap_RejectsDuplicateRuntimeTypes() {
        var map = new InheritanceValidatorMap<Contact>();
        map.Add<Person>(new PersonValidator());

        MSTestAssertions.That(
            () => Invoking(() => map.Add<Person>(new PersonValidator()))
                .Should()
                .ThrowExactly<ArgumentException>());
    }
}
