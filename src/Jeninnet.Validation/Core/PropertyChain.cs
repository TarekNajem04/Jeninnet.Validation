//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Represents an immutable member path such as <c>Customer.Address.Street</c> or
/// <c>Items[0].Name</c>.
/// </summary>
/// <remarks>
/// The type is a lightweight wrapper over <see cref="ValidationPath"/> for advanced rule authors.
/// It performs no path validation; comparisons are ordinal and separators are <c>.</c> and
/// <c>[</c> only.
/// </remarks>
public readonly record struct PropertyChain {
    private readonly string? _path;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyChain"/> struct.
    /// </summary>
    /// <param name="path">The member path; <see langword="null"/> or whitespace creates an empty chain.</param>
    public PropertyChain(string? path) => _path = string.IsNullOrWhiteSpace(path) ? null : path.Trim();

    /// <summary>
    /// Gets the empty chain.
    /// </summary>
    public static PropertyChain Empty => default;

    /// <summary>
    /// Gets the normalized path, or an empty string for an empty chain.
    /// </summary>
    public string Path => _path ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether this chain has no path.
    /// </summary>
    public bool IsEmpty => _path is null;

    /// <summary>
    /// Creates a chain from a member path.
    /// </summary>
    /// <param name="path">The member path.</param>
    /// <returns>The chain for <paramref name="path"/>.</returns>
    public static PropertyChain FromPath(string? path) => new(path);

    /// <summary>
    /// Gets all parent paths ordered from the immediate parent to the root.
    /// </summary>
    /// <returns>The parent chain, or an empty list when the chain has no parent.</returns>
    public IReadOnlyList<string> GetParentChain() => ValidationPath.GetParentChain(_path);

    /// <summary>
    /// Gets a value indicating whether this chain is a strict descendant of
    /// <paramref name="parent"/>.
    /// </summary>
    /// <param name="parent">The candidate parent chain.</param>
    /// <returns><see langword="true"/> when this chain is below <paramref name="parent"/>.</returns>
    public bool IsChildChainOf(PropertyChain parent) => ValidationPath.IsChildOf(parent._path, _path);

    /// <summary>
    /// Gets a value indicating whether this chain is a strict descendant of
    /// <paramref name="parentPath"/>.
    /// </summary>
    /// <param name="parentPath">The candidate parent path.</param>
    /// <returns><see langword="true"/> when this chain is below <paramref name="parentPath"/>.</returns>
    public bool IsChildChainOf(string? parentPath) => ValidationPath.IsChildOf(parentPath, _path);

    /// <summary>
    /// Returns a new chain with <paramref name="memberName"/> appended as a child member.
    /// </summary>
    /// <param name="memberName">The child member name.</param>
    /// <returns>The extended chain.</returns>
    /// <exception cref="ArgumentException"><paramref name="memberName"/> is empty or whitespace.</exception>
    public PropertyChain Add(string memberName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        var member = memberName.Trim();
        return _path is null ? new PropertyChain(member) : new PropertyChain($"{_path}.{member}");
    }

    /// <summary>
    /// Returns the path.
    /// </summary>
    /// <returns>The normalized path, or an empty string for an empty chain.</returns>
    public override string ToString() => Path;
}
