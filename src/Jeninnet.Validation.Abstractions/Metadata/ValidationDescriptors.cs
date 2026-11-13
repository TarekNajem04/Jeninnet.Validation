//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Metadata;

/// <summary>
/// Immutable description of one configured validation rule.
/// </summary>
public sealed class ValidationRuleDescriptor : IValidationRuleDescriptor {
    /// <summary>
    /// A shared empty parameter dictionary.
    /// </summary>
    public static IReadOnlyDictionary<string, object?> EmptyParameters { get; } =
        new Dictionary<string, object?>(0);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationRuleDescriptor"/> class.
    /// </summary>
    /// <param name="kind">The stable rule kind.</param>
    /// <param name="messageKey">The optional message key.</param>
    /// <param name="severity">The failure severity.</param>
    /// <param name="hasCondition">Whether a per-rule condition gates the rule.</param>
    /// <param name="parameters">The rule parameters; defaults to the shared empty dictionary.</param>
    /// <param name="errorCode">The optional machine-readable error code.</param>
    /// <param name="displayName">The optional display name for the <c>{PropertyName}</c> parameter.</param>
    /// <param name="customState">The optional static custom state.</param>
    /// <param name="nestedDescriptor">The optional nested validator descriptor for composition rules.</param>
    /// <exception cref="ArgumentException"><paramref name="kind"/> is empty or whitespace.</exception>
    public ValidationRuleDescriptor( // NOSONAR: immutable descriptor fields map one-to-one to constructor parameters; grouping them would break the public API.
        string kind,
        string? messageKey = null,
        ValidationErrorSeverity severity = ValidationErrorSeverity.Error,
        bool hasCondition = false,
        IReadOnlyDictionary<string, object?>? parameters = null,
        string? errorCode = null,
        string? displayName = null,
        object? customState = null,
        IValidationDescriptor? nestedDescriptor = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        Kind = kind;
        MessageKey = messageKey;
        Severity = severity;
        HasCondition = hasCondition;
        Parameters = parameters ?? EmptyParameters;
        ErrorCode = errorCode;
        DisplayName = displayName;
        CustomState = customState;
        NestedDescriptor = nestedDescriptor;
    }

    /// <inheritdoc />
    public string Kind { get; }

    /// <inheritdoc />
    public string? MessageKey { get; }

    /// <inheritdoc />
    public bool MessageIsAuthored { get; init; }

    /// <inheritdoc />
    public ValidationErrorSeverity Severity { get; }

    /// <inheritdoc />
    public string? ErrorCode { get; }

    /// <inheritdoc />
    public string? DisplayName { get; }

    /// <inheritdoc />
    public object? CustomState { get; }

    /// <inheritdoc />
    public bool HasCondition { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <summary>
    /// Gets the nested validator descriptor for <c>Nested</c> and <c>ForEach</c> rules, when known.
    /// </summary>
    public IValidationDescriptor? NestedDescriptor { get; }
}

/// <summary>
/// Immutable description of one member's rules.
/// </summary>
public sealed class ValidationMemberDescriptor : IValidationMemberDescriptor {
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationMemberDescriptor"/> class.
    /// </summary>
    /// <param name="name">The member name.</param>
    /// <param name="rules">The rules configured for the member.</param>
    /// <param name="memberType">The declared member type, when known.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="rules"/> is null.</exception>
    public ValidationMemberDescriptor(
        string name,
        IEnumerable<IValidationRuleDescriptor> rules,
        Type? memberType = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(rules);

        Name = name;
        Rules = [.. rules];
        MemberType = memberType;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<IValidationRuleDescriptor> Rules { get; }

    /// <summary>
    /// Gets the declared member type, when known.
    /// </summary>
    public Type? MemberType { get; }
}

/// <summary>
/// Immutable description of a validator with a precomputed member lookup.
/// </summary>
public sealed class ValidationDescriptor : IValidationDescriptor {
    private readonly Dictionary<string, IReadOnlyList<IValidationRuleDescriptor>> _rulesByMember;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationDescriptor"/> class.
    /// </summary>
    /// <param name="validatorName">The validator type name.</param>
    /// <param name="members">The member descriptors.</param>
    /// <param name="validatedType">The optional validated model type.</param>
    /// <exception cref="ArgumentException"><paramref name="validatorName"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="members"/> is null.</exception>
    public ValidationDescriptor(
        string validatorName,
        IEnumerable<IValidationMemberDescriptor> members,
        Type? validatedType = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(validatorName);
        ArgumentNullException.ThrowIfNull(members);

        ValidatorName = validatorName;
        ValidatedType = validatedType;

        var memberList = members.ToList();
        Members = memberList;

        _rulesByMember = new Dictionary<string, IReadOnlyList<IValidationRuleDescriptor>>(
            memberList.Count, StringComparer.Ordinal);

        foreach(var member in memberList) {
            _rulesByMember[member.Name] = member.Rules;
        }
    }

    /// <inheritdoc />
    public string ValidatorName { get; }

    /// <inheritdoc />
    public Type? ValidatedType { get; }

    /// <inheritdoc />
    public IReadOnlyList<IValidationMemberDescriptor> Members { get; }

    /// <inheritdoc />
    public IReadOnlyList<IValidationRuleDescriptor> RulesForMember(string memberName) {
        ArgumentNullException.ThrowIfNull(memberName);

        return _rulesByMember.TryGetValue(memberName, out var rules)
            ? rules
            : [];
    }
}
