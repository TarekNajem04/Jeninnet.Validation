//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Core;

[TestClass]
public sealed class PropertyChainTests {
    [TestMethod]
    public void ValidationPath_GetParentChain_ReturnsImmediateParentToRoot() {
        MSTestAssertions.That(() => ValidationPath.GetParentChain("A.B[0].C").Should().Equal("A.B[0]", "A.B", "A"));
        MSTestAssertions.That(() => ValidationPath.GetParentChain("Items[0]").Should().Equal("Items"));
        MSTestAssertions.That(() => ValidationPath.GetParentChain("A.B.C").Should().Equal("A.B", "A"));
    }

    [TestMethod]
    public void ValidationPath_GetParentChain_ReturnsEmptyForRootsAndEmptyPaths() {
        MSTestAssertions.That(() => ValidationPath.GetParentChain("A").Should().BeEmpty());
        MSTestAssertions.That(() => ValidationPath.GetParentChain("[0]").Should().BeEmpty());
        MSTestAssertions.That(() => ValidationPath.GetParentChain(null).Should().BeEmpty());
        MSTestAssertions.That(() => ValidationPath.GetParentChain(string.Empty).Should().BeEmpty());
        MSTestAssertions.That(() => ValidationPath.GetParentChain("   ").Should().BeEmpty());
    }

    [TestMethod]
    public void PropertyChain_FromPath_NormalizesAndExposesThePath() {
        var chain = PropertyChain.FromPath("  A.B[0].C  ");

        MSTestAssertions.That(() => chain.Path.Should().Be("A.B[0].C"));
        MSTestAssertions.That(() => chain.ToString().Should().Be("A.B[0].C"));
        MSTestAssertions.That(() => chain.IsEmpty.Should().BeFalse());
        MSTestAssertions.That(() => chain.GetParentChain().Should().Equal("A.B[0]", "A.B", "A"));
    }

    [TestMethod]
    public void PropertyChain_Empty_RepresentsMissingOrWhitespacePaths() {
        var fromNull = PropertyChain.FromPath(null);
        var fromWhitespace = PropertyChain.FromPath(" ");

        MSTestAssertions.That(() => PropertyChain.Empty.Path.Should().BeEmpty());
        MSTestAssertions.That(() => fromNull.IsEmpty.Should().BeTrue());
        MSTestAssertions.That(() => fromWhitespace.IsEmpty.Should().BeTrue());
        MSTestAssertions.That(() => fromNull.ToString().Should().BeEmpty());
        MSTestAssertions.That(() => fromNull.GetParentChain().Should().BeEmpty());
        MSTestAssertions.That(() => fromNull.Should().Be(PropertyChain.Empty));
    }

    [TestMethod]
    public void PropertyChain_Add_AppendsChildMembers() {
        var chain = PropertyChain.Empty.Add("A").Add("B").Add("Items[0]");

        MSTestAssertions.That(() => chain.Path.Should().Be("A.B.Items[0]"));
        MSTestAssertions.That(() => PropertyChain.FromPath("A").Add("B").Path.Should().Be("A.B"));
    }

    [TestMethod]
    public void PropertyChain_Add_RejectsMissingMemberNames() {
        Action addEmpty = static () => _ = PropertyChain.Empty.Add(string.Empty);
        Action addWhitespace = static () => _ = PropertyChain.Empty.Add(" ");
        Action addNull = static () => _ = PropertyChain.Empty.Add(null!);

        MSTestAssertions.That(() => addEmpty.Should().Throw<ArgumentException>());
        MSTestAssertions.That(() => addWhitespace.Should().Throw<ArgumentException>());
        MSTestAssertions.That(() => addNull.Should().Throw<ArgumentException>());
    }

    [TestMethod]
    public void PropertyChain_IsChildChainOf_MatchesValidationPath() {
        var child = PropertyChain.FromPath("A.B.C");
        var sibling = PropertyChain.FromPath("AB.C");

        MSTestAssertions.That(() => child.IsChildChainOf(PropertyChain.FromPath("A")).Should().BeTrue());
        MSTestAssertions.That(() => child.IsChildChainOf(PropertyChain.FromPath("A.B")).Should().BeTrue());
        MSTestAssertions.That(() => child.IsChildChainOf("A").Should().BeTrue());
        MSTestAssertions.That(() => child.IsChildChainOf(PropertyChain.FromPath("A.B.C")).Should().BeFalse());
        MSTestAssertions.That(() => sibling.IsChildChainOf(PropertyChain.FromPath("A")).Should().BeFalse());
        MSTestAssertions.That(() => PropertyChain.Empty.IsChildChainOf(PropertyChain.Empty).Should().BeFalse());
    }

    [TestMethod]
    public void PropertyChain_Equality_IsOrdinalAndNormalized() {
        var first = PropertyChain.FromPath(" A.B ");
        var second = PropertyChain.FromPath("A.B");
        var other = PropertyChain.FromPath("a.b");

        MSTestAssertions.That(() => first.Should().Be(second));
        MSTestAssertions.That(() => first.GetHashCode().Should().Be(second.GetHashCode()));
        MSTestAssertions.That(() => first.Should().NotBe(other));
    }
}
