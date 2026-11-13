//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Verifies that domain attributes are emitted by the source generator and behave like the
/// equivalent fluent rules.
/// </summary>
[TestClass]
// Generator tests share process-wide metadata fixtures.
[DoNotParallelize]
public sealed class GeneratedDomainRuleTests {
    private const string SOURCE = """
        using System;
        using Jeninnet.Validation;
        using Jeninnet.Validation.Attributes;
        using Jeninnet.Validation.Fluent;
        using Jeninnet.Validation.Rules;

        namespace GeneratedDomain;

        [Validate("GeneratedDomain")]
        public sealed class DomainGenerationModel {
            [Iban("NL")] public string? Iban { get; set; }
            [VatNumber("NL")] public string? Vat { get; set; }
            [PostalCode("NL")] public string? Postal { get; set; }
            [NationalId("NL")] public string? Bsn { get; set; }
            [PasswordStrength(PasswordStrengthPreset.Strict)] public string? Password { get; set; }
            [Uuid] public string? Uuid { get; set; }
            [Ulid] public string? Ulid { get; set; }
        }

        public sealed class DomainGenerationFluentValidator : Validator<DomainGenerationModel> {
            protected override void Configure(ValidatorRuleBuilder<DomainGenerationModel> builder) {
                builder.RuleFor(static x => x.Iban, nameof(DomainGenerationModel.Iban)).Iban("NL");
                builder.RuleFor(static x => x.Vat, nameof(DomainGenerationModel.Vat)).VatNumber("NL");
                builder.RuleFor(static x => x.Postal, nameof(DomainGenerationModel.Postal)).PostalCode("NL");
                builder.RuleFor(static x => x.Bsn, nameof(DomainGenerationModel.Bsn)).NationalId("NL");
                builder.RuleFor(static x => x.Password, nameof(DomainGenerationModel.Password)).PasswordStrength(PasswordPolicy.Strict);
                builder.RuleFor(static x => x.Uuid, nameof(DomainGenerationModel.Uuid)).Uuid();
                builder.RuleFor(static x => x.Ulid, nameof(DomainGenerationModel.Ulid)).Ulid();
            }
        }
        """;

    [TestMethod]
    public void GeneratedDomainRules_ExecuteAndMatchTheFluentPath() {
        _ = typeof(PasswordPolicy);
        var generated = BuildGenerated();
        var invalidModel = CreateModel(
            iban: "invalid",
            vat: "invalid",
            postal: "invalid",
            bsn: "invalid",
            password: "password",
            uuid: "invalid",
            ulid: "invalid");

        var generatedResult = generated.Validate(invalidModel);
        var fluentResult = generated.ValidateFluent(invalidModel);

        MSTestAssertions.That(() => generatedResult.Errors.Should().HaveCount(7));

        var generatedKeys = generatedResult.Errors
            .Select(static error => $"{error.MemberName}|{error.MessageKey}")
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();
        var fluentKeys = fluentResult.Errors
            .Select(static error => $"{error.MemberName}|{error.MessageKey}")
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();

        MSTestAssertions.That(() => generatedKeys.SequenceEqual(fluentKeys).Should().BeTrue(
            $"Generated errors: {string.Join(", ", generatedKeys)}; fluent errors: {string.Join(", ", fluentKeys)}."));

        var descriptor = generated.Describe();
        var ibanRule = descriptor.RulesForMember("Iban").Single();
        MSTestAssertions.That(() => ibanRule.Kind.Should().Be("Iban"));
        MSTestAssertions.That(() => ibanRule.Parameters["Country"].Should().Be("NL"));

        var passwordRule = descriptor.RulesForMember("Password").Single();
        MSTestAssertions.That(() => passwordRule.Kind.Should().Be("PasswordStrength"));
        MSTestAssertions.That(() => passwordRule.Parameters["Policy"].Should().Be("Strict"));
        MSTestAssertions.That(() => passwordRule.Parameters["MinLength"].Should().Be(16));
    }

    [TestMethod]
    public void GeneratedDomainRules_AcceptValidValues() {
        var generated = BuildGenerated();
        var validModel = CreateModel(
            iban: "NL91ABNA0417164300",
            vat: "NL111222333B01",
            postal: "1234 AB",
            bsn: "111222333",
            password: "Str0ng!Passw0rd2026",
            uuid: "6f9619ff-8b86-d011-b42d-00c04fc964ff",
            ulid: "01ARZ3NDEKTSV4RRFFQ69G5FAV");

        MSTestAssertions.That(() => generated.Validate(validModel).IsValid.Should().BeTrue());
    }

    private static object CreateModel(
        string? iban,
        string? vat,
        string? postal,
        string? bsn,
        string? password,
        string? uuid,
        string? ulid) {
        var assembly = _generatedAssembly!;
        var modelType = assembly.GetType("GeneratedDomain.DomainGenerationModel", throwOnError: true)!;
        var model = Activator.CreateInstance(modelType)!;

        modelType.GetProperty("Iban")!.SetValue(model, iban);
        modelType.GetProperty("Vat")!.SetValue(model, vat);
        modelType.GetProperty("Postal")!.SetValue(model, postal);
        modelType.GetProperty("Bsn")!.SetValue(model, bsn);
        modelType.GetProperty("Password")!.SetValue(model, password);
        modelType.GetProperty("Uuid")!.SetValue(model, uuid);
        modelType.GetProperty("Ulid")!.SetValue(model, ulid);
        return model;
    }

    private GeneratedDomainValidator BuildGenerated() {
        var cancellationToken = TestContext?.CancellationToken ?? CancellationToken.None;
        var syntaxTree = CSharpSyntaxTree.ParseText(SOURCE, cancellationToken: cancellationToken);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => assembly.Location)
                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                  .Select(static location => MetadataReference.CreateFromFile(location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        var compilation = CSharpCompilation.Create(
            $"GeneratedDomainAssembly_{Guid.NewGuid():N}",
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

        _generatedAssembly = Assembly.Load(stream.ToArray());
        var modelType = _generatedAssembly.GetType("GeneratedDomain.DomainGenerationModel", throwOnError: true)!;
        var validatorType = _generatedAssembly.GetType("GeneratedDomain.DomainGenerationModelValidator", throwOnError: true)!;
        var fluentType = _generatedAssembly.GetType("GeneratedDomain.DomainGenerationFluentValidator", throwOnError: true)!;

        return new GeneratedDomainValidator(
            modelType,
            validatorType,
            Activator.CreateInstance(validatorType)!,
            Activator.CreateInstance(fluentType)!);
    }

    private static Assembly? _generatedAssembly;

    private sealed class GeneratedDomainValidator(
        Type modelType,
        Type validatorType,
        object validator,
        object fluentValidator) {
        public ValidationResult Validate(object model) =>
            (ValidationResult)validatorType.GetMethod("Validate", [modelType])!.Invoke(validator, [model])!;

        public ValidationResult ValidateFluent(object model) =>
            (ValidationResult)fluentValidator.GetType().GetMethod("Validate", [modelType])!.Invoke(fluentValidator, [model])!;

        public IValidationDescriptor Describe() =>
            (IValidationDescriptor)validatorType.GetMethod("Describe")!.Invoke(validator, null)!;
    }

    public TestContext? TestContext { get; set; }
}
