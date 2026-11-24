//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.CodeFixes;

/// <summary>
/// Shared syntax operation for code fixes that delete one validation attribute while keeping the
/// property declaration and any sibling attributes intact.
/// </summary>
internal static class AttributeRemoval {
    /// <summary>
    /// Removes the attribute. An attribute list that only contains the removed attribute is
    /// removed as well, so the declaration does not keep an empty <c>[]</c> list.
    /// </summary>
    /// <param name="root">The syntax root that contains the attribute.</param>
    /// <param name="attribute">The attribute to remove.</param>
    /// <returns>The rewritten root, or <see langword="null"/> when the node cannot be removed.</returns>
    internal static SyntaxNode? Remove(SyntaxNode root, AttributeSyntax attribute) {
        if(attribute.Parent is not AttributeListSyntax list) {
            return null;
        }

        if(list.Attributes.Count == 1) {
            return root.RemoveNode(list, SyntaxRemoveOptions.KeepNoTrivia);
        }

        return root.ReplaceNode(list, list.WithAttributes(list.Attributes.Remove(attribute)));
    }
}
