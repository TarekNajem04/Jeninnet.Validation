//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Core;

[TestClass]
public sealed class MemberRuleGroupTests {
    [TestMethod]
    public void Constructor_CopiesSourceRuleLists() {
        var nested = new MemberRuleGroup<object>("Nested", CascadeMode.Continue);
        var syncRules = new List<ISyncValidationRule<object>> { nested };
        var asyncRules = new List<IAsyncValidationRule<object>> { nested };

        var group = new MemberRuleGroup<object>("Name", CascadeMode.Stop, syncRules, asyncRules);

        syncRules.Clear();
        asyncRules.Clear();

        MSTestAssertions.That(() => group.SyncRules.Should().HaveCount(1));
        MSTestAssertions.That(() => group.AsyncRules.Should().HaveCount(1));
        MSTestAssertions.That(() => group.CascadeMode.Should().Be(CascadeMode.Stop));
    }

    [TestMethod]
    public void Constructor_WithoutRules_CreatesEmptyReadOnlyCollections() {
        var group = new MemberRuleGroup<object>("Name", CascadeMode.Continue);

        MSTestAssertions.That(() => group.SyncRules.Should().BeEmpty());
        MSTestAssertions.That(() => group.AsyncRules.Should().BeEmpty());
        MSTestAssertions.That(() => (group.SyncRules is List<ISyncValidationRule<object>>).Should().BeFalse(
            "the public surface must not expose the mutable backing list."));
        MSTestAssertions.That(() => (group.AsyncRules is List<IAsyncValidationRule<object>>).Should().BeFalse(
            "the public surface must not expose the mutable backing list."));
    }

    [TestMethod]
    public void PublicRuleCollections_RejectMutation() {
        var group = new MemberRuleGroup<object>("Name", CascadeMode.Continue);
        var syncRules = (IList<ISyncValidationRule<object>>)group.SyncRules;
        var asyncRules = (IList<IAsyncValidationRule<object>>)group.AsyncRules;

        MSTestAssertions.That(() => Invoking(() => syncRules.Add(null!))
            .Should().Throw<NotSupportedException>());
        MSTestAssertions.That(() => Invoking(() => asyncRules.Add(null!))
            .Should().Throw<NotSupportedException>());
    }
}
