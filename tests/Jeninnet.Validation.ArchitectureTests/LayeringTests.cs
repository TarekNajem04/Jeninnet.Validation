//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.ArchitectureTests;

/// <summary>
/// Executable guards for the project's layering and dependency-direction rules
/// (§I, §IV, §V). Each failure names the offending reference.
/// </summary>
[TestClass]
public sealed class LayeringTests {
    private static Assembly AbstractionsAssembly => typeof(ValidationError).Assembly;

    private static Assembly ValidationAssembly => typeof(Validator<>).Assembly;

    private static Assembly AnalyzersAssembly => typeof(ValidatorGenerator).Assembly;

    private static Assembly AssertionsAssembly => typeof(ValidationResultAssertions).Assembly;

    [TestMethod]
    public void Abstractions_DoesNotReferenceRepositoryProjects() {
        var violations = ArchitectureRules.FindDisallowedReferences(
            "Jeninnet.Validation.Abstractions",
            AbstractionsAssembly.GetReferencedAssemblies(),
            allowedPrefixes: []);

        MSTestAssertions.That(() => violations.Should().BeEmpty(Format(violations)));
    }

    [TestMethod]
    public void Validation_ReferencesOnlyApprovedAssemblies() {
        var violations = ArchitectureRules.FindDisallowedReferences(
            "Jeninnet.Validation",
            ValidationAssembly.GetReferencedAssemblies(),
            allowedPrefixes: ["Microsoft.Extensions", "Jeninnet.Validation.Abstractions", "Jeninnet.Localization"]);

        MSTestAssertions.That(() => violations.Should().BeEmpty(Format(violations)));
    }

    [TestMethod]
    public void Analyzers_ReferencesOnlyRoslynAndFrameworkAssemblies() {
        var violations = ArchitectureRules.FindDisallowedReferences(
            "Jeninnet.Validation.Analyzers",
            AnalyzersAssembly.GetReferencedAssemblies(),
            allowedPrefixes: ["Microsoft.CodeAnalysis"]);

        MSTestAssertions.That(() => violations.Should().BeEmpty(Format(violations)));
    }

    [TestMethod]
    public void Analyzers_DoesNotReferenceRuntimeAssemblies() {
        var runtimeReferences = AnalyzersAssembly.GetReferencedAssemblies()
                                                 .Select(static reference => reference.Name)
                                                 .Where(static name => name?.StartsWith("Jeninnet.", StringComparison.Ordinal) is true)
                                                 .Select(static name => name!)
                                                 .ToList();

        MSTestAssertions.That(() => runtimeReferences.Should().BeEmpty(
            $"The analyzer/generator must stay independent of runtime assemblies. Found: {Format(runtimeReferences)}"));
    }

    [TestMethod]
    public void Assertions_ReferencesOnlyRuntimeContracts() {
        var violations = ArchitectureRules.FindDisallowedReferences(
            "Jeninnet.Validation.Assertions",
            AssertionsAssembly.GetReferencedAssemblies(),
            allowedPrefixes: ["Jeninnet.Validation", "Jeninnet.Validation.Abstractions"]);

        MSTestAssertions.That(() => violations.Should().BeEmpty(Format(violations)));
        MSTestAssertions.That(() => ArchitectureRules.ReferencesAssembly(AssertionsAssembly, "Jeninnet.Validation.Analyzers")
            .Should().BeFalse("the assertion helpers are test-time and must not depend on build-time analyzers"));
    }

    [TestMethod]
    public void Validation_DependsOnAbstractions_AndNotOnAnalyzers() {
        MSTestAssertions.That(() => ArchitectureRules.ReferencesAssembly(ValidationAssembly, "Jeninnet.Validation.Abstractions")
            .Should().BeTrue("the engine must consume its contracts"));

        MSTestAssertions.That(() => ArchitectureRules.ReferencesAssembly(ValidationAssembly, "Jeninnet.Validation.Analyzers")
            .Should().BeFalse("runtime assemblies must not depend on build-time analyzers"));

        MSTestAssertions.That(() => ArchitectureRules.ReferencesAssembly(AbstractionsAssembly, "Jeninnet.Validation")
            .Should().BeFalse("Abstractions must not depend on the engine"));
    }

    [TestMethod]
    public void ShippingAssemblies_DoNotReferenceDataAnnotations() {
        foreach(var assembly in new[] { AbstractionsAssembly, ValidationAssembly, AnalyzersAssembly, AssertionsAssembly }) {
            MSTestAssertions.That(() => ArchitectureRules.HasBannedReference(
                    assembly.GetReferencedAssemblies(),
                    "System.ComponentModel.DataAnnotations")
                .Should().BeFalse($"{assembly.GetName().Name} must not reference System.ComponentModel.DataAnnotations"));
        }
    }

    [TestMethod]
    public void PackableAssemblies_HaveExpectedIdentity() {
        AssertIdentity(AbstractionsAssembly, "Jeninnet.Validation.Abstractions");
        AssertIdentity(ValidationAssembly, "Jeninnet.Validation");
        AssertIdentity(AnalyzersAssembly, "Jeninnet.Validation.Analyzers");
        AssertIdentity(AssertionsAssembly, "Jeninnet.Validation.Assertions");
    }

    private static void AssertIdentity(Assembly assembly, string expectedName) {
        MSTestAssertions.That(() => assembly.GetName().Name.Should().Be(expectedName));

        var violations = ArchitectureRules.FindNamespaceViolations(assembly, "Jeninnet.Validation");
        MSTestAssertions.That(() => violations.Should().BeEmpty(Format(violations)));
    }

    private static string Format(IEnumerable<string?> messages) =>
        string.Join(Environment.NewLine, messages);
}
