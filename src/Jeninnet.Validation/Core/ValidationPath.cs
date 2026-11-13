//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Helpers for member paths such as <c>Customer.Address.Street</c> or <c>Items[0].Name</c>.
/// </summary>
public static class ValidationPath {
    /// <summary>
    /// Gets the parent path. Indexers are stripped before member segments, so
    /// <c>Items[0].Name</c> returns <c>Items[0]</c> and <c>Items[0]</c> returns <c>Items</c>.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The parent path, or <see langword="null"/> when the path has no parent.</returns>
    public static string? GetParent(string? path) {
        if(string.IsNullOrWhiteSpace(path)) {
            return null;
        }

        var normalized = path.Trim();

        if(normalized.EndsWith(']')) {
            var index = normalized.LastIndexOf('[');
            if(index <= 0) {
                return null;
            }

            return normalized[..index];
        }

        var separator = normalized.LastIndexOf('.');
        return separator > 0 ? normalized[..separator] : null;
    }

    /// <summary>
    /// Gets all parent paths ordered from the immediate parent to the root, for example
    /// <c>A.B[0].C</c> returns <c>A.B[0]</c>, <c>A.B</c>, and <c>A</c>.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The parent chain, or an empty list when the path has no parent.</returns>
    public static IReadOnlyList<string> GetParentChain(string? path) {
        var parent = GetParent(path);
        if(parent is null) {
            return [];
        }

        var parents = new List<string>();
        while(parent is not null) {
            parents.Add(parent);
            parent = GetParent(parent);
        }

        return parents;
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="child"/> is a strict descendant of
    /// <paramref name="parent"/> through a member separator or an indexer.
    /// </summary>
    /// <param name="parent">The parent path.</param>
    /// <param name="child">The child path.</param>
    /// <returns><see langword="true"/> when the child is below the parent; otherwise <see langword="false"/>.</returns>
    public static bool IsChildOf(string? parent, string? child) {
        if(string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(child)) {
            return false;
        }

        var normalizedParent = parent.Trim();
        var normalizedChild = child.Trim();

        if(normalizedChild.Length <= normalizedParent.Length ||
           !normalizedChild.StartsWith(normalizedParent, StringComparison.Ordinal)) {
            return false;
        }

        var boundary = normalizedChild[normalizedParent.Length];
        return boundary is '.' or '[';
    }
}
