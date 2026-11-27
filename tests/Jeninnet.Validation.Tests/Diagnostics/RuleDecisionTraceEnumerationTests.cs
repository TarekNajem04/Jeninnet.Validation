//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Diagnostics;

[TestClass]
[DoNotParallelize]
public sealed class RuleDecisionTraceEnumerationTests {
    [TestMethod]
    public void Skip_DependentAndRuleSetContainers_ReportsEveryLeafInBothPhases() {
        var sink = new TraceCaptureSink();
        var rootData = RootContextData.Rent();

        try {
            var scope = sink.Begin(typeof(TraceModel), "TraceEnumeration", ruleSets: null, isAsync: true)!;
            using var session = new RuleDecisionTraceSession(scope, rootData);
            var context = new ValidationContext<TraceModel>(
                new TraceModel(),
                MemberPath: string.Empty,
                RootData: rootData);

            var syncLeaf = new TraceLeafRule("SyncLeaf", "Name");
            var asyncLeaf = new TraceLeafRule("AsyncLeaf", "Age");
            var parentGroup = new MemberRuleGroup<TraceModel>("Name", null, [syncLeaf]);
            var dependent = new DependentRule<TraceModel>(
                parentSync: parentGroup,
                parentAsync: null,
                syncRules: [syncLeaf],
                asyncRules: [asyncLeaf],
                parentGroup: new MemberRuleGroup<TraceModel>("Name"));
            var ruleSet = new RuleSetRule<TraceModel>(["default"], [syncLeaf], [asyncLeaf]);

            session.Skip(dependent, in context, ValidationRuleDecision.SkippedCondition, RuleTracePhase.All);
            session.Skip(ruleSet, in context, ValidationRuleDecision.ShortCircuited, RuleTracePhase.All);

            MSTestAssertions.That(() => sink.Events.Should().HaveCount(5));
            MSTestAssertions.That(() => sink.Events.Should().Contain(
                traceEvent => traceEvent.Decision == ValidationRuleDecision.SkippedCondition));
            MSTestAssertions.That(() => sink.Events.Should().Contain(
                traceEvent => traceEvent.Decision == ValidationRuleDecision.ShortCircuited));
        }
        finally {
            RootContextData.Return(rootData);
        }
    }

    private sealed class TraceLeafRule(string kind, string memberName)
        : ISyncValidationRule<TraceModel>, IAsyncValidationRule<TraceModel>, IValidationMemberRule {
        public string MemberName => memberName;

        public void Validate(in ValidationContext<TraceModel> context, ValidationErrorCollector errors) =>
            ArgumentNullException.ThrowIfNull(errors);

        public ValueTask ValidateAsync(
            ValidationContext<TraceModel> context,
            ValidationErrorCollector errors,
            CancellationToken cancellationToken = default) {
            ArgumentNullException.ThrowIfNull(errors);
            _ = cancellationToken;
            return ValueTask.CompletedTask;
        }

        public override string ToString() => kind;
    }
}
