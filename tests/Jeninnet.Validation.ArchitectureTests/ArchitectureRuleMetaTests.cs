//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.ArchitectureTests;

/// <summary>
/// Meta-tests: feed the architecture predicates synthetic violations and prove they
/// report them. These keep the guards themselves honest.
/// </summary>
[TestClass]
public sealed class ArchitectureRuleMetaTests {
    [TestMethod]
    public void DisallowedRepositoryReference_IsReported() {
        var violations = ArchitectureRules.FindDisallowedReferences(
            "Jeninnet.Validation.Abstractions",
            [new AssemblyName("Jeninnet.Validation")],
            allowedPrefixes: []);

        MSTestAssertions.That(() => violations.Should().HaveCount(1));
    }

    [TestMethod]
    public void ApprovedPrefix_IsAccepted() {
        var violations = ArchitectureRules.FindDisallowedReferences(
            "Probe",
            [new AssemblyName("Microsoft.Extensions.DependencyInjection")],
            allowedPrefixes: ["Microsoft.Extensions"]);

        MSTestAssertions.That(() => violations.Should().BeEmpty());
    }

    [TestMethod]
    public void BannedReference_IsDetected() =>
        MSTestAssertions.That(() => ArchitectureRules.HasBannedReference(
                [new AssemblyName("System.ComponentModel.DataAnnotations")],
                "System.ComponentModel.DataAnnotations")
            .Should().BeTrue());

    [TestMethod]
    public void NamespaceOutsideRoot_IsReported() {
        MSTestAssertions.That(() => ArchitectureRules.IsWithinNamespace(
                "Microsoft.Extensions.DependencyInjection",
                "Jeninnet.Validation")
            .Should().BeFalse());

        MSTestAssertions.That(() => ArchitectureRules.IsWithinNamespace(
                "Jeninnet.Validation.Rules",
                "Jeninnet.Validation")
            .Should().BeTrue());
    }
}
