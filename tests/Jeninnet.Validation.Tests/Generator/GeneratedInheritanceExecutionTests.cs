//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Executes generated validators that inherit rules from a base type and a generated container
/// whose collection elements are dispatched through a Fluent inheritance validator.
/// </summary>
[TestClass]
public sealed class GeneratedInheritanceExecutionTests {
    private const string SOURCE = """
        using System;
        using Jeninnet.Validation.Attributes;
        using Jeninnet.Validation.Fluent;

        namespace Inheritance;

        [Validate("Inheritance")]
        public abstract class Contact {
            [NotEmpty] public string? Name { get; set; }
        }

        [Validate("Inheritance")]
        public sealed class Person : Contact {
            [NotEmpty] public string? Email { get; set; }
        }

        [Validate("Inheritance")]
        public sealed class Organisation : Contact {
            [NotEmpty] public string? Registration { get; set; }
        }

        [Validate("Inheritance")]
        public sealed class ContactContainer {
            [ForEach] public Contact[]? Contacts { get; set; }
        }

        public sealed class PersonFluentValidator : Validator<Person> {
            protected override void Configure(ValidatorRuleBuilder<Person> builder) {
                ArgumentNullException.ThrowIfNull(builder);

                builder.RuleFor(static x => x.Name, nameof(Person.Name)).NotEmpty();
                builder.RuleFor(static x => x.Email, nameof(Person.Email)).NotEmpty();
            }
        }

        public sealed class ContactInheritanceValidator : Validator<Contact> {
            protected override void Configure(ValidatorRuleBuilder<Contact> builder) {
                ArgumentNullException.ThrowIfNull(builder);

                var map = new InheritanceValidatorMap<Contact>();
                map.Add<Person>(new PersonValidator());
                map.Add<Organisation>(new OrganisationValidator());

                builder.Include(new InheritanceValidator<Contact>(map));
            }
        }
        """;

    [TestMethod]
    public void GeneratedDerivedValidator_ContainsBaseAndOwnRules() {
        var generated = BuildGeneratedAssembly();
        var personType = generated.GetType("Inheritance.Person", throwOnError: true)!;
        var validatorType = generated.GetType("Inheritance.PersonValidator", throwOnError: true)!;
        var validator = Activator.CreateInstance(validatorType)!;
        var person = Activator.CreateInstance(personType)!;

        var result = (ValidationResult)validatorType
            .GetMethod("Validate", [personType])!
            .Invoke(validator, [person])!;

        ValidationAssert.ErrorCount(result, 2);
        ValidationAssert.HasError(result, "Name");
        ValidationAssert.HasError(result, "Email");
    }

    [TestMethod]
    public void GeneratedAndFluentValidators_ProduceIdenticalInheritanceResults() {
        var generated = BuildGeneratedAssembly();
        var personType = generated.GetType("Inheritance.Person", throwOnError: true)!;
        var generatedValidator = Activator.CreateInstance(
            generated.GetType("Inheritance.PersonValidator", throwOnError: true)!)!;
        var fluentValidator = Activator.CreateInstance(
            generated.GetType("Inheritance.PersonFluentValidator", throwOnError: true)!)!;

        var person = Activator.CreateInstance(personType)!;
        personType.GetProperty("Name")!.SetValue(person, string.Empty);
        personType.GetProperty("Email")!.SetValue(person, string.Empty);

        var generatedResult = (ValidationResult)generatedValidator.GetType()
            .GetMethod("Validate", [personType])!
            .Invoke(generatedValidator, [person])!;
        var fluentResult = (ValidationResult)fluentValidator.GetType()
            .GetMethod("Validate", [personType])!
            .Invoke(fluentValidator, [person])!;

        MSTestAssertions.That(() => (generatedResult.IsValid == fluentResult.IsValid).Should().BeTrue());
        MSTestAssertions.That(() => generatedResult.Errors.Count.Should().Be(fluentResult.Errors.Count));

        for(var i = 0; i < generatedResult.Errors.Count; i++) {
            MSTestAssertions.That(() => generatedResult.Errors[i].MemberName.Should().Be(fluentResult.Errors[i].MemberName));
            MSTestAssertions.That(() => generatedResult.Errors[i].MessageKey.Should().Be(fluentResult.Errors[i].MessageKey));
            MSTestAssertions.That(() => generatedResult.Errors[i].ErrorCode.Should().Be(fluentResult.Errors[i].ErrorCode));
        }
    }

    [TestMethod]
    public void GeneratedForEachContainer_DispatchesEachElementByRuntimeType() {
        var generated = BuildGeneratedAssembly();
        var containerType = generated.GetType("Inheritance.ContactContainer", throwOnError: true)!;
        var containerValidatorType = generated.GetType("Inheritance.ContactContainerValidator", throwOnError: true)!;
        var inheritanceValidatorType = generated.GetType("Inheritance.ContactInheritanceValidator", throwOnError: true)!;
        var personType = generated.GetType("Inheritance.Person", throwOnError: true)!;
        var organisationType = generated.GetType("Inheritance.Organisation", throwOnError: true)!;

        var inheritanceValidator = Activator.CreateInstance(inheritanceValidatorType)!;
        var containerValidator = Activator.CreateInstance(containerValidatorType, [inheritanceValidator])!;

        var person = Activator.CreateInstance(personType)!;
        personType.GetProperty("Name")!.SetValue(person, "Ada");
        personType.GetProperty("Email")!.SetValue(person, string.Empty);

        var organisation = Activator.CreateInstance(organisationType)!;
        organisationType.GetProperty("Name")!.SetValue(organisation, "Acme");
        organisationType.GetProperty("Registration")!.SetValue(organisation, string.Empty);

        var array = Array.CreateInstance(generated.GetType("Inheritance.Contact", throwOnError: true)!, 2);
        array.SetValue(person, 0);
        array.SetValue(organisation, 1);

        var container = Activator.CreateInstance(containerType)!;
        containerType.GetProperty("Contacts")!.SetValue(container, array);

        var result = (ValidationResult)containerValidatorType
            .GetMethod("Validate", [containerType])!
            .Invoke(containerValidator, [container])!;

        ValidationAssert.ErrorCount(result, 2);
        ValidationAssert.HasError(result, "Contacts[0].Email");
        ValidationAssert.HasError(result, "Contacts[1].Registration");
    }

    private Assembly BuildGeneratedAssembly() {
        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        var syntaxTree = CSharpSyntaxTree.ParseText(SOURCE, cancellationToken: cancellationToken);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            $"GeneratedInheritanceAssembly_{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, cancellationToken);
        var runResult = driver.GetRunResult();

        MSTestAssertions.That(() => (runResult.GeneratedTrees.Length > 0).Should().BeTrue(
            string.Join(Environment.NewLine, runResult.Diagnostics)));

        var generatedCompilation = compilation.AddSyntaxTrees(runResult.GeneratedTrees);

        using var stream = new MemoryStream();
        var emit = generatedCompilation.Emit(stream, cancellationToken: cancellationToken);

        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        return Assembly.Load(stream.ToArray());
    }

    public TestContext? TestContext { get; set; }
}
