//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Represents the context in which a validation operation is executed.
/// </summary>
/// <typeparam name="T">The type of the object instance being validated.</typeparam>
/// <param name="Instance">The object instance being validated.</param>
/// <param name="MemberPath">The current member path within the object graph.</param>
/// <param name="RuleSets">The optional rule sets to apply during validation.</param>
/// <param name="FailFast">Whether validation stops at the first error.</param>
/// <param name="Root">The root instance of the validation walk (distinct from a child <paramref name="Instance"/>).</param>
/// <param name="RootData">The shared per-walk data bag (engine-provided; may be <see langword="null"/> for manually constructed contexts).</param>
/// <param name="ServiceProvider">The optional integration-provided service provider; the engine never resolves services itself.</param>
/// <param name="Options">
/// The effective validation options for this walk. When omitted, rules fall back to the
/// process-wide <see cref="ValidationConfiguration.Options"/>.
/// </param>
public readonly record struct ValidationContext<T>(
    T Instance,
    string MemberPath,
    IEnumerable<string>? RuleSets = null,
    bool FailFast = false,
    object? Root = null,
    RootContextData? RootData = null,
    IServiceProvider? ServiceProvider = null,
    ValidationOptions? Options = null) {
    /// <summary>
    /// Computes the full path for a member without unnecessary string builder overhead.
    /// </summary>
    /// <param name="memberName">The name of the member.</param>
    /// <returns>The full member path, combining <see cref="MemberPath"/> and <paramref name="memberName"/>.</returns>
    public string GetFullPath(string memberName)
        => string.IsNullOrEmpty(MemberPath) ? memberName : $"{MemberPath}.{memberName}";

    /// <summary>
    /// Creates a new <see cref="ValidationContext{TChild}"/> for a child member.
    /// </summary>
    /// <typeparam name="TChild">The type of the child member.</typeparam>
    /// <param name="child">The child instance.</param>
    /// <param name="memberName">The name of the child member.</param>
    /// <returns>
    /// A new <see cref="ValidationContext{TChild}"/> instance with an updated <see cref="MemberPath"/>.
    /// </returns>
    // For nested objects: Appends ".PropertyName"
    public ValidationContext<TChild> ForChild<TChild>(TChild child, string memberName) =>
        new(child, GetFullPath(memberName), RuleSets, FailFast, Root, RootData, ServiceProvider, Options);

    /// <summary>
    /// Creates a context for a collection element using collection expressions style indexers.
    /// </summary>
    /// <typeparam name="TElement">The type of the collection element.</typeparam>
    /// <param name="instance">The element instance.</param>
    /// <param name="memberName">The name of the collection member.</param>
    /// <param name="index">The index of the element.</param>
    /// <returns>A new <see cref="ValidationContext{TElement}"/> with an indexed path.</returns>
    public ValidationContext<TElement> ForElement<TElement>(TElement instance, string memberName, int index) =>
        ForElement(instance, memberName, index.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Creates a context for a collection element with a custom indexer.
    /// </summary>
    /// <typeparam name="TElement">The type of the collection element.</typeparam>
    /// <param name="instance">The element instance.</param>
    /// <param name="memberName">The name of the collection member.</param>
    /// <param name="indexer">The indexer text inserted inside the brackets.</param>
    /// <returns>A new <see cref="ValidationContext{TElement}"/> with an indexed path.</returns>
    public ValidationContext<TElement> ForElement<TElement>(TElement instance, string memberName, string indexer) =>
        new(instance, $"{GetFullPath(memberName)}[{indexer}]", RuleSets, FailFast, Root, RootData, ServiceProvider, Options);

    /// <summary>
    /// Computes the path for a member. An empty accessor member name represents the current
    /// instance (used by collection element rules).
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="accessor">The property accessor for the member.</param>
    internal string PathFor<TProperty>(IPropertyAccessor<T, TProperty> accessor) {
        if(string.IsNullOrEmpty(accessor.MemberName)) {
            return MemberPath;
        }

        return string.IsNullOrEmpty(MemberPath)
            ? accessor.MemberName
            : $"{MemberPath}.{accessor.MemberName}";
    }
}
