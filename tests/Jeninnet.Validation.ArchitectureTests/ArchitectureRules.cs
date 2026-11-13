//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.ArchitectureTests;

/// <summary>
/// Pure predicates behind the executable architecture tests. Kept separate so the
/// meta-tests can feed synthetic violations and prove the guards can fail.
/// </summary>
internal static class ArchitectureRules {
    private static string[] FrameworkPrefixes => ["System", "netstandard", "mscorlib"];

    /// <summary>
    /// Returns one actionable message per disallowed assembly reference.
    /// </summary>
    /// <param name="projectName">The name reported in violation messages.</param>
    /// <param name="references">The assembly references to inspect.</param>
    /// <param name="allowedPrefixes">Additional assembly-name prefixes that are allowed.</param>
    internal static IReadOnlyList<string> FindDisallowedReferences(
        string projectName,
        IEnumerable<AssemblyName> references,
        IReadOnlyCollection<string> allowedPrefixes) {
        var prefixes = new List<string>(FrameworkPrefixes.Length + allowedPrefixes.Count);
        prefixes.AddRange(FrameworkPrefixes);
        prefixes.AddRange(allowedPrefixes);

        return [
            .. references
                .Select(static reference => reference.Name)
                .Where(static name => name is not null)
                .Select(static name => name!)
                .Where(name => !prefixes.Any(prefix => IsWithin(name, prefix)))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .Select(name => $"{projectName} references disallowed assembly '{name}'.")
        ];
    }

    /// <summary>
    /// Returns one message per exported type whose namespace is outside the required root.
    /// </summary>
    /// <param name="assembly">The assembly whose exported types are inspected.</param>
    /// <param name="requiredNamespaceRoot">The namespace root every exported type must use.</param>
    internal static IReadOnlyList<string> FindNamespaceViolations(Assembly assembly, string requiredNamespaceRoot) => [
        .. assembly.GetExportedTypes()
            .Where(type => !IsWithinNamespace(type.Namespace, requiredNamespaceRoot))
            .Select(type => $"Public type '{type.FullName}' is outside namespace root '{requiredNamespaceRoot}'.")
    ];

    /// <summary>
    /// Determines whether <paramref name="assembly"/> directly references the assembly
    /// with the given simple name.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <param name="simpleName">The simple name of the referenced assembly.</param>
    internal static bool ReferencesAssembly(Assembly assembly, string simpleName) =>
        assembly.GetReferencedAssemblies()
                .Any(reference => string.Equals(reference.Name, simpleName, StringComparison.Ordinal));

    /// <summary>
    /// Determines whether the reference set contains the explicitly banned assembly.
    /// </summary>
    /// <param name="references">The assembly references to inspect.</param>
    /// <param name="bannedName">The simple name of the banned assembly.</param>
    internal static bool HasBannedReference(IEnumerable<AssemblyName> references, string bannedName) =>
        references.Any(reference => string.Equals(reference.Name, bannedName, StringComparison.Ordinal));

    /// <summary>
    /// Determines whether an assembly name is the prefix itself or a dotted child.
    /// </summary>
    /// <param name="name">The assembly name to test.</param>
    /// <param name="prefix">The allowed prefix.</param>
    internal static bool IsWithin(string name, string prefix) =>
        string.Equals(name, prefix, StringComparison.Ordinal) ||
        name.StartsWith(prefix + ".", StringComparison.Ordinal);

    /// <summary>
    /// Determines whether a namespace is the root itself or a dotted child.
    /// </summary>
    /// <param name="namespace">The namespace to test.</param>
    /// <param name="root">The required namespace root.</param>
    internal static bool IsWithinNamespace(string? @namespace, string root) =>
        @namespace is not null &&
        (string.Equals(@namespace, root, StringComparison.Ordinal) ||
         @namespace.StartsWith(root + ".", StringComparison.Ordinal));
}
