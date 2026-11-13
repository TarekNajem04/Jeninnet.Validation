//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Metadata;

/// <summary>
/// Builds a frozen <see cref="ValidationDescriptor"/> from executable rules and optional
/// supplemental metadata. This is the supported hook for describing rules added at runtime.
/// </summary>
public sealed class ValidationDescriptorBuilder {
    private readonly string _validatorName;
    private readonly Type? _validatedType;
    private readonly Dictionary<string, ValidationMemberDescriptorBuilder> _members = [with(StringComparer.Ordinal)];
    private readonly List<string> _memberOrder = [];
    private readonly HashSet<object> _visitedRules = [with(ReferenceEqualityComparer.Instance)];

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationDescriptorBuilder"/> class.
    /// </summary>
    /// <param name="validatorName">The validator type name.</param>
    /// <param name="validatedType">The optional validated model type.</param>
    /// <exception cref="ArgumentException"><paramref name="validatorName"/> is empty or whitespace.</exception>
    public ValidationDescriptorBuilder(string validatorName, Type? validatedType = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(validatorName);

        _validatorName = validatorName;
        _validatedType = validatedType;
    }

    /// <summary>
    /// Gets or creates the descriptor builder for a member.
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <returns>The member builder.</returns>
    /// <exception cref="ArgumentException"><paramref name="memberName"/> is empty or whitespace.</exception>
    public ValidationMemberDescriptorBuilder Member(string memberName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        if(!_members.TryGetValue(memberName, out var member)) {
            member = new ValidationMemberDescriptorBuilder(memberName);
            _members[memberName] = member;
            _memberOrder.Add(memberName);
        }

        return member;
    }

    /// <summary>
    /// Adds an executable rule and reads its metadata without exposing the delegate. Rules that do
    /// not implement metadata are recorded as a <c>Custom</c> advisory entry with a reason.
    /// </summary>
    /// <param name="rule">The rule to describe.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is null.</exception>
    public ValidationDescriptorBuilder AddRule(object rule) {
        ArgumentNullException.ThrowIfNull(rule);

        AddRule(rule, memberNameOverride: null, inheritedParameters: null, hasCondition: false);
        return this;
    }

    /// <summary>
    /// Builds the frozen descriptor. Members and rules retain deterministic insertion order.
    /// </summary>
    /// <returns>The descriptor.</returns>
    public ValidationDescriptor Build() {
        var members = new List<IValidationMemberDescriptor>(_memberOrder.Count);

        foreach(var memberName in _memberOrder) {
            var member = _members[memberName];
            if(member.Rules.Count > 0) {
                members.Add(new ValidationMemberDescriptor(memberName, member.Rules, member.MemberType));
            }
        }

        return new ValidationDescriptor(_validatorName, members, _validatedType);
    }

    private void AddRule(
        object rule,
        string? memberNameOverride,
        IReadOnlyDictionary<string, object?>? inheritedParameters,
        bool hasCondition) {
        if(!_visitedRules.Add(rule)) {
            return;
        }

        if(rule is IValidationRuleContainer container) {
            var combinedMemberName = memberNameOverride ?? container.MemberName;
            var combinedParameters = MergeParameters(inheritedParameters, container.Parameters);
            var combinedCondition = hasCondition || container.HasCondition;

            foreach(var innerRule in container.InnerRules) {
                AddRule(innerRule, combinedMemberName, combinedParameters, combinedCondition);
            }

            return;
        }

        var memberName = memberNameOverride ??
                         (rule is IValidationMemberRule member && member.MemberName is { Length: > 0 } name
                             ? name
                             : "*");

        var memberBuilder = Member(memberName);

        if(rule is IValidationSchemaRuleMetadata schemaRule) {
            memberBuilder.Add(new ValidationRuleDescriptor(
                schemaRule.Kind,
                hasCondition: hasCondition,
                nestedDescriptor: schemaRule.NestedDescriptor));
            return;
        }

        if(rule is IRuleMetadataProvider metadata) {
            var parameters = MergeParameters(metadata.GetMetadataParameters(), inheritedParameters);
            var kind = rule is IRuleMetadataKindProvider kindProvider
                ? kindProvider.Kind
                : metadata.Kind;

            memberBuilder.MemberType ??= metadata.MemberType;
            memberBuilder.Add(new ValidationRuleDescriptor(
                kind,
                metadata.MessageKey,
                metadata.Severity,
                hasCondition || metadata.HasCondition,
                parameters,
                metadata.ErrorCode,
                metadata.DisplayName,
                metadata.CustomState) {
                MessageIsAuthored = metadata.MessageIsAuthored
            });
            return;
        }

        var advisoryParameters = MergeParameters(
            new Dictionary<string, object?> {
                { "Reason", "Rule has no descriptor metadata." },
                { "Type", rule.GetType().Name }
            },
            inheritedParameters);

        Member(memberName).Add(new ValidationRuleDescriptor(
            "Custom",
            hasCondition: hasCondition,
            parameters: advisoryParameters));
    }

    private static IReadOnlyDictionary<string, object?>? MergeParameters(
        IReadOnlyDictionary<string, object?>? source,
        IReadOnlyDictionary<string, object?>? inherited) {
        if(source is null or { Count: 0 }) {
            return inherited;
        }

        if(inherited is null or { Count: 0 }) {
            return source;
        }

        var merged = new Dictionary<string, object?>(source, StringComparer.Ordinal);

        foreach(var pair in inherited) {
            merged[pair.Key] = pair.Value;
        }

        return merged;
    }
}

/// <summary>
/// Collects explicit rule descriptors for one member. Used by generated descriptor hooks.
/// </summary>
public sealed class ValidationMemberDescriptorBuilder {
    private readonly List<IValidationRuleDescriptor> _rules = [];

    internal ValidationMemberDescriptorBuilder(string memberName) => MemberName = memberName;

    /// <summary>
    /// Gets the member name.
    /// </summary>
    public string MemberName { get; }

    /// <summary>
    /// Gets or sets the declared member type, when known.
    /// </summary>
    internal Type? MemberType { get; set; }

    /// <summary>
    /// Gets the rules added so far.
    /// </summary>
    public IReadOnlyList<IValidationRuleDescriptor> Rules => _rules;

    /// <summary>
    /// Adds an explicit descriptor entry for a runtime rule that cannot expose metadata.
    /// </summary>
    /// <param name="kind">The stable rule kind.</param>
    /// <param name="messageKey">The optional message key.</param>
    /// <param name="severity">The failure severity.</param>
    /// <param name="hasCondition">Whether the rule is conditional.</param>
    /// <param name="parameters">The optional rule parameters.</param>
    /// <param name="errorCode">The optional machine-readable code.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <param name="customState">The optional static custom state.</param>
    /// <returns>The same member builder for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="kind"/> is empty or whitespace.</exception>
    public ValidationMemberDescriptorBuilder Rule( // NOSONAR: fluent descriptor API mirrors ValidationRuleDescriptor's optional fields; a parameter object would break the public surface.
        string kind,
        string? messageKey = null,
        ValidationErrorSeverity severity = ValidationErrorSeverity.Error,
        bool hasCondition = false,
        IReadOnlyDictionary<string, object?>? parameters = null,
        string? errorCode = null,
        string? displayName = null,
        object? customState = null) {
        Add(new ValidationRuleDescriptor(
            kind,
            messageKey,
            severity,
            hasCondition,
            parameters,
            errorCode,
            displayName,
            customState));
        return this;
    }

    internal void Add(IValidationRuleDescriptor rule) => _rules.Add(rule);
}
