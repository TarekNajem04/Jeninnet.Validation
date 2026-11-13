//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Generator;

/// <summary>
/// Verifies the reflection-free generated validator registry and the central catalog populated by
/// generated module initializers.
/// </summary>
[TestClass]
// The generated validator registry is process-wide static state.
[DoNotParallelize]
public sealed class GeneratedValidatorRegistryTests {
    private const string SOURCE = """
        using Jeninnet.Validation.Attributes;

        namespace RegistryModels;

        [Validate("RegistryModels")]
        public sealed class FirstModel {
            [NotEmpty]
            public string? Name { get; set; }
        }

        [Validate("RegistryModels")]
        public sealed class SecondModel {
            [NotEmpty]
            public string? Value { get; set; }
        }

        public sealed class IgnoredModel {
            public string? Name { get; set; }
        }
        """;

    [TestMethod]
    public void GeneratedRegistry_RegistersValidatorsWithoutReflectionAndIsIdempotent() {
        const string assemblyName = "GeneratedRegistryTestAssembly";
        var assembly = CompileGeneratedAssembly(SOURCE, assemblyName);
        var registryText = GetGeneratedRegistryText(SOURCE, assemblyName);

        MSTestAssertions.That(() => registryText.Should().Contain("AddGeneratedValidatorsFromThisAssembly"));
        MSTestAssertions.That(() => registryText.Should().Contain("RegisterValidator<global::RegistryModels.FirstModel"));
        MSTestAssertions.That(() => registryText.Should().Contain("ModuleInitializer"));
        MSTestAssertions.That(() => registryText.Should().Contain("GeneratedValidatorCatalog.Register"));
        MSTestAssertions.That(() => registryText.Should().NotContain("Assembly.GetTypes"));

        var extensionType = assembly.GetType(
            $"{assemblyName}.GeneratedValidation.GeneratedValidatorServiceCollectionExtensions",
            throwOnError: true)!;
        var register = extensionType.GetMethod(
            "AddGeneratedValidatorsFromThisAssembly",
            [typeof(IServiceCollection), typeof(ServiceLifetime)])!;

        var services = new ServiceCollection();
        register.Invoke(null, [services, ServiceLifetime.Scoped]);
        register.Invoke(null, [services, ServiceLifetime.Scoped]);

        var firstModelType = assembly.GetType("RegistryModels.FirstModel", throwOnError: true)!;
        var secondModelType = assembly.GetType("RegistryModels.SecondModel", throwOnError: true)!;
        var ignoredModelType = assembly.GetType("RegistryModels.IgnoredModel", throwOnError: true)!;

        var firstValidatorType = typeof(IValidator<>).MakeGenericType(firstModelType);
        var secondValidatorType = typeof(IValidator<>).MakeGenericType(secondModelType);
        var ignoredValidatorType = typeof(IValidator<>).MakeGenericType(ignoredModelType);

        MSTestAssertions.That(() => services.Count(descriptor => descriptor.ServiceType == firstValidatorType).Should().Be(1));
        MSTestAssertions.That(() => services.Single(descriptor => descriptor.ServiceType == firstValidatorType)
            .Lifetime.Should().Be(ServiceLifetime.Scoped));

        using var provider = services.BuildServiceProvider();
        var firstValidator = provider.GetRequiredService(firstValidatorType);
        var secondValidator = provider.GetRequiredService(secondValidatorType);

        MSTestAssertions.That(() => firstValidator.Should().NotBeNull());
        MSTestAssertions.That(() => secondValidator.Should().NotBeNull());
        MSTestAssertions.That(() => provider.GetService(ignoredValidatorType).Should().BeNull());

        var firstModel = Activator.CreateInstance(firstModelType)!;
        firstModelType.GetProperty("Name")!.SetValue(firstModel, string.Empty);

        var result = (ValidationResult)firstValidator.GetType()
            .GetMethod("Validate", [firstModelType])!
            .Invoke(firstValidator, [firstModel])!;

        ValidationAssert.HasError(result, "Name");

        var syncValidatorType = typeof(ISyncValidator<>).MakeGenericType(firstModelType);
        var asyncValidatorType = typeof(IAsyncValidator<>).MakeGenericType(firstModelType);
        MSTestAssertions.That(() => provider.GetRequiredService(syncValidatorType).Should().NotBeNull());
        MSTestAssertions.That(() => provider.GetRequiredService(asyncValidatorType).Should().NotBeNull());
    }

    [TestMethod]
    public void GeneratedRegistry_PopulatesCentralCatalogAndAddValidationResolves() {
        const string assemblyName = "CentralCatalogTestAssembly";
        var assembly = CompileGeneratedAssembly(SOURCE, assemblyName);
        var firstModelType = assembly.GetType("RegistryModels.FirstModel", throwOnError: true)!;
        var secondModelType = assembly.GetType("RegistryModels.SecondModel", throwOnError: true)!;

        var found = GeneratedValidatorCatalog.TryGet(firstModelType, out var registration);

        MSTestAssertions.That(() => found.Should().BeTrue());
        MSTestAssertions.That(() => registration!.ValidatorType.Name.Should().Contain("FirstModel"));

        var services = new ServiceCollection();
        services.AddValidation();
        using var provider = services.BuildServiceProvider();

        var firstValidator = provider.GetRequiredService(typeof(IValidator<>).MakeGenericType(firstModelType));
        var secondValidator = provider.GetRequiredService(typeof(IValidator<>).MakeGenericType(secondModelType));

        MSTestAssertions.That(() => firstValidator.Should().NotBeNull());
        MSTestAssertions.That(() => secondValidator.Should().NotBeNull());
    }

    [TestMethod]
    public void AddGeneratedValidators_UsesCatalogBeforeReflection() {
        _ = CompileGeneratedAssembly(SOURCE, "CatalogPreferredTestAssembly");
        ValidationServiceCollectionExtensions.ResetReflectionFallbackCount();

        var services = new ServiceCollection();
        services.AddGeneratedValidators();

        MSTestAssertions.That(() => ValidationServiceCollectionExtensions.ReflectionFallbackCount.Should().Be(0));
        MSTestAssertions.That(() => services
            .Any(static descriptor => descriptor.ServiceType.IsGenericType &&
                                      descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>))
            .Should().BeTrue());
    }

    [TestMethod]
    public void AddGeneratedValidators_FallsBackToScanningWhenCatalogIsEmpty() {
        var assembly = CompileGeneratedAssembly(SOURCE, "CatalogFallbackTestAssembly");
        var firstModelType = assembly.GetType("RegistryModels.FirstModel", throwOnError: true)!;
        var snapshot = GeneratedValidatorCatalog.Entries;

        GeneratedValidatorCatalog.ClearForTests();
        ValidationServiceCollectionExtensions.ResetReflectionFallbackCount();

        try {
            var services = new ServiceCollection();
            services.AddGeneratedValidators();

            MSTestAssertions.That(() => ValidationServiceCollectionExtensions.ReflectionFallbackCount.Should().Be(1));

            using var provider = services.BuildServiceProvider();
            var validator = provider.GetRequiredService(typeof(IValidator<>).MakeGenericType(firstModelType));

            MSTestAssertions.That(() => validator.Should().NotBeNull());
        }
        finally {
            foreach(var entry in snapshot) {
                GeneratedValidatorCatalog.Register(entry.ModelType, entry.ValidatorType, entry.RegisterAction);
            }

            ValidationServiceCollectionExtensions.ResetReflectionFallbackCount();
        }
    }

    private static string GetGeneratedRegistryText(string source, string assemblyName) {
        var compilation = CreateCompilation(source, assemblyName);
        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var result = driver.GetRunResult();

        return string.Join(
            Environment.NewLine,
            result.GeneratedTrees
                  .Where(static tree => tree.FilePath.EndsWith("GeneratedValidatorRegistry.g.cs", StringComparison.Ordinal))
                  .Select(static tree => tree.ToString()));
    }

    private static Assembly CompileGeneratedAssembly(string source, string assemblyName) {
        var compilation = CreateCompilation(source, assemblyName);
        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var result = driver.GetRunResult();
        var errors = result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();

        MSTestAssertions.That(() => errors.Should().BeEmpty(
            string.Join(Environment.NewLine, errors)));

        using var stream = new MemoryStream();
        var emit = compilation.AddSyntaxTrees(result.GeneratedTrees).Emit(stream);
        var problems = emit.Diagnostics.Where(static diagnostic =>
            diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error).ToList();

        MSTestAssertions.That(() => problems.Should().BeEmpty(
            string.Join(Environment.NewLine, problems)));

        var assembly = Assembly.Load(stream.ToArray());
        RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        return assembly;
    }

    private static CSharpCompilation CreateCompilation(string source, string assemblyName) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();

        return CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
    }
}
