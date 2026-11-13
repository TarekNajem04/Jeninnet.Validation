//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Core;

/// <summary>
/// Represents a composite validator that aggregates multiple validation rules for a given type.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="syncRules">The set of synchronous validation rules.</param>
/// <param name="asyncRules">The set of asynchronous validation rules.</param>
/// <param name="validatorName">
/// The display name of the validator owning this composite; used in the
/// <see cref="AsyncValidatorInvokedSynchronouslyException"/> message. Falls back to
/// <c>typeof(T).FullName</c> when omitted.
/// </param>
/// <param name="sanitizers">
/// Optional preprocessing steps that run in registration order before validation rules on every
/// <c>Validate</c>/<c>ValidateAsync</c> call. Sanitization is explicit opt-in preprocessing, not
/// validation: steps run even when rule-set filtering selects no rules, and never appear in
/// descriptors or client payloads.
/// </param>
/// <param name="options">
/// Optional validation options captured for this validator. A container-registered
/// <see cref="ValidationOptions"/> resolved from the per-call service provider takes precedence.
/// </param>
/// <remarks>
/// Synchronous execution is honest by construction: a validator whose content carries
/// asynchronous rules but no synchronous rules throws
/// <see cref="AsyncValidatorInvokedSynchronouslyException"/> instead of returning a false
/// <see cref="ValidationResult.Success"/>. Use the async path for such validators.
/// </remarks>
public sealed class CompositeValidator<T>(
    IEnumerable<ISyncValidationRule<T>>? syncRules = null,
    IEnumerable<IAsyncValidationRule<T>>? asyncRules = null,
    string? validatorName = null,
    IEnumerable<ISanitizationStep<T>>? sanitizers = null,
    ValidationOptions? options = null
) : IValidator<T>, IValidationOptionsValidator<T>, IValidationContextValidator<T> {
    private ISyncValidationRule<T>[] SyncRules { get; } = [.. syncRules ?? []];
    private IAsyncValidationRule<T>[] AsyncRules { get; } = [.. asyncRules ?? []];
    private readonly string? _validatorName = validatorName;
    private ISanitizationStep<T>[] Sanitizers { get; } = [.. sanitizers ?? []];
    private ValidationOptions? Options { get; } = options;
    [SuppressMessage(
        "Style",
        "IDE0032:Use auto property",
        Justification = "The backing field distinguishes an explicit caller value from the inherited default resolved at validation time.")]
    private CascadeMode _classLevelCascadeMode = ValidationConfiguration.Options.DefaultClassLevelCascadeMode;
    private bool _classLevelCascadeModeSet;

    /// <summary>
    /// Gets or sets the class-level cascade mode used when a call does not override it.
    /// </summary>
    public CascadeMode ClassLevelCascadeMode {
        get => _classLevelCascadeMode;
        set {
            _classLevelCascadeMode = value;
            _classLevelCascadeModeSet = true;
        }
    }

    private CascadeMode? ExplicitClassLevelCascadeMode =>
        _classLevelCascadeModeSet ? _classLevelCascadeMode : null;

    private ValidationOptions ResolveOptions(IServiceProvider? serviceProvider, ValidationOptions? contextOptions) =>
        contextOptions ??
        serviceProvider?.GetService<ValidationOptions>() ??
        Options ??
        ValidationConfiguration.Options;

    /// <summary>
    /// Fluent registration parks every member rule group in BOTH lists, so the length of
    /// SyncRules alone cannot prove synchronous executability: a group whose own sync list
    /// is empty contributes nothing. The scan runs once, lazily, and is cached — the happy
    /// path pays a single boolean check it already paid as a length check before.
    /// </summary>
    private bool? _hasSyncContent;

    /// <summary>
    /// Gets a value indicating whether any registered synchronous rule contributes synchronous content.
    /// </summary>
    private bool HasSyncContent {
        get {
            if(_hasSyncContent is bool cached) {
                return cached;
            }

            var hasSyncContent = RuleContentInspector.HasSyncContent(SyncRules);

            _hasSyncContent = hasSyncContent;
            return hasSyncContent;
        }
    }

    private static ValidationResult ThrowIfRequested(ValidationResult result, ValidationCallOptions options) {
        if(options.ThrowOnFailures && !result.IsValid) {
            throw new ValidationException(result.Errors);
        }

        return result;
    }

    private static async ValueTask<ValidationResult> ThrowIfRequestedAsync(
        ValueTask<ValidationResult> pending,
        ValidationCallOptions options) {
        var result = await pending.ConfigureAwait(false);

        if(options.ThrowOnFailures && !result.IsValid) {
            throw new ValidationException(result.Errors);
        }

        return result;
    }

    [ThreadStatic]
    private static ValidationErrorCollector? _cachedCollector;

    private static ValidationErrorCollector RentCollector() {
        var collector = _cachedCollector;
        if(collector is not null) {
            _cachedCollector = null;
            return collector;
        }

        return [];
    }

    /// <summary>
    /// Runs the registered sanitization steps in order. A <see langword="null"/> instance is
    /// skipped: there is nothing to sanitize, and rules report presence failures themselves.
    /// </summary>
    /// <param name="instance">The model instance.</param>
    private void RunSanitizers(T instance) {
        if(instance is null || Sanitizers.Length is 0) {
            return;
        }

        foreach(var sanitizer in Sanitizers) {
            sanitizer.Sanitize(instance);
        }
    }

    private static void ReturnCollector(ValidationErrorCollector collector) {
        collector.Clear();
        _cachedCollector = collector;
    }

    /// <inheritdoc cref="ISyncValidator{T}.Validate(T)" />
    public ValidationResult Validate(T instance) => Validate(instance, ruleSets: null);

    /// <inheritdoc cref="ISyncValidator{T}.Validate(T, IEnumerable{string})"/>
    /// <exception cref="AsyncValidatorInvokedSynchronouslyException">
    /// Thrown when the composite carries asynchronous rules but no synchronous content.
    /// </exception>
    public ValidationResult Validate(T instance, IEnumerable<string>? ruleSets) =>
        Validate(instance, ruleSets, ExplicitClassLevelCascadeMode, failFast: false, filter: default);

    /// <inheritdoc />
    public ValidationResult Validate(T instance, ValidationCallOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        var result = Validate(
            instance,
            options.RuleSets,
            options.CascadeMode ?? ExplicitClassLevelCascadeMode,
            options.FailFast,
            new ValidationFilter(
                options.IncludeProperties,
                options.ExcludeProperties,
                options.ExcludeRuleSets,
                options.ServiceProvider,
                options.IncludeAllRuleSets,
                options.Selector));

        return ThrowIfRequested(result, options);
    }

    /// <inheritdoc />
    public ValidationResult Validate(T instance, ValidationContext<T> context) =>
        Validate(instance, context, ExplicitClassLevelCascadeMode);

    internal ValidationResult Validate(T instance, ValidationContext<T> context, CascadeMode? classLevelCascadeMode) {
        var diagnostics = ValidationDiagnostics.Sink?.Begin(
            typeof(T), _validatorName, context.RuleSets, isAsync: false);

        try {
            if(!HasSyncContent) {
                if(AsyncRules.Length is 0) {
                    var success = ValidationResult.Success;
                    diagnostics?.Complete(success);
                    return success;
                }

                throw new AsyncValidatorInvokedSynchronouslyException(
                    _validatorName ?? typeof(T).FullName!, AsyncRules.Length);
            }

            RunSanitizers(instance);

            var errors = RentCollector();

            try {
                var effectiveOptions = ResolveOptions(context.ServiceProvider, context.Options);
                var effectiveContext = new ValidationContext<T>(
                    instance,
                    context.MemberPath,
                    context.RuleSets,
                    context.FailFast,
                    context.Root ?? instance,
                    context.RootData,
                    context.ServiceProvider,
                    effectiveOptions);
                var effectiveClassLevelCascadeMode = classLevelCascadeMode ?? effectiveOptions.DefaultClassLevelCascadeMode;

                var filter = context.RootData?.Filter ?? default;
                var selected = SelectRules(SyncRules, context.RuleSets, filter, context.MemberPath, in effectiveContext);

                ValidateRules(in effectiveContext, selected, errors, effectiveClassLevelCascadeMode, context.FailFast);

                var result = ValidationResult.FromErrors(errors);
                diagnostics?.Complete(result);
                return result;
            }
            finally {
                ReturnCollector(errors);
            }
        }
        catch(Exception exception) {
            diagnostics?.Fail(exception);
            throw;
        }
        finally {
            diagnostics?.Dispose();
        }
    }

    internal ValidationResult Validate(T instance, IEnumerable<string>? ruleSets, CascadeMode? classLevelCascadeMode, bool failFast, ValidationFilter filter) {
        var diagnostics = ValidationDiagnostics.Sink?.Begin(
            typeof(T), _validatorName, ruleSets, isAsync: false);

        try {
            if(!HasSyncContent) {
                if(AsyncRules.Length is 0) {
                    var success = ValidationResult.Success;
                    diagnostics?.Complete(success);
                    return success;
                }

                throw new AsyncValidatorInvokedSynchronouslyException(
                    _validatorName ?? typeof(T).FullName!, AsyncRules.Length);
            }

            RunSanitizers(instance);

            var errors = RentCollector();
            var rootData = RootContextData.Rent();

            try {
                var effectiveOptions = ResolveOptions(filter.ServiceProvider, contextOptions: null);
                var context = new ValidationContext<T>(
                    instance,
                    MemberPath: string.Empty,
                    ruleSets,
                    failFast,
                    Root: instance,
                    RootData: rootData,
                    ServiceProvider: filter.ServiceProvider,
                    Options: effectiveOptions);
                var effectiveClassLevelCascadeMode = classLevelCascadeMode ?? effectiveOptions.DefaultClassLevelCascadeMode;

                rootData.Filter = filter;
                var selected = SelectRules(SyncRules, ruleSets, filter, string.Empty, in context);

                ValidateRules(in context, selected, errors, effectiveClassLevelCascadeMode, failFast);

                var result = ValidationResult.FromErrors(errors);
                diagnostics?.Complete(result);
                return result;
            }
            finally {
                ReturnCollector(errors);
                RootContextData.Return(rootData);
            }
        }
        catch(Exception exception) {
            diagnostics?.Fail(exception);
            throw;
        }
        finally {
            diagnostics?.Dispose();
        }
    }

    /// <inheritdoc cref="IAsyncValidator{T}.ValidateAsync(T, CancellationToken)"/>
    public ValueTask<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default) =>
        ValidateAsync(instance, ruleSets: null, cancellationToken);

    /// <inheritdoc cref="IAsyncValidator{T}.ValidateAsync(T, IEnumerable{string}, CancellationToken)"/>
    public ValueTask<ValidationResult> ValidateAsync(T instance, IEnumerable<string>? ruleSets, CancellationToken cancellationToken = default) =>
        ValidateAsync(instance, ruleSets, ExplicitClassLevelCascadeMode, failFast: false, filter: default, cancellationToken);

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        T instance,
        ValidationCallOptions options,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(options);

        var pending = ValidateAsync(
            instance,
            options.RuleSets,
            options.CascadeMode ?? ExplicitClassLevelCascadeMode,
            options.FailFast,
            new ValidationFilter(
                options.IncludeProperties,
                options.ExcludeProperties,
                options.ExcludeRuleSets,
                options.ServiceProvider,
                options.IncludeAllRuleSets,
                options.Selector),
            cancellationToken);

        return options.ThrowOnFailures ? ThrowIfRequestedAsync(pending, options) : pending;
    }

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        T instance,
        ValidationContext<T> context,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(instance, context, ExplicitClassLevelCascadeMode, cancellationToken);

    internal async ValueTask<ValidationResult> ValidateAsync(
        T instance,
        ValidationContext<T> context,
        CascadeMode? classLevelCascadeMode,
        CancellationToken cancellationToken = default) {
        var diagnostics = ValidationDiagnostics.Sink?.Begin(
            typeof(T), _validatorName, context.RuleSets, isAsync: true);

        try {
            var hasSync = SyncRules.Length > 0;
            var hasAsync = AsyncRules.Length > 0;

            if(!hasSync && !hasAsync) {
                var success = ValidationResult.Success;
                diagnostics?.Complete(success);
                return success;
            }

            RunSanitizers(instance);

            var errors = RentCollector();

            try {
                var effectiveOptions = ResolveOptions(context.ServiceProvider, context.Options);
                var effectiveContext = new ValidationContext<T>(
                    instance,
                    context.MemberPath,
                    context.RuleSets,
                    context.FailFast,
                    context.Root ?? instance,
                    context.RootData,
                    context.ServiceProvider,
                    effectiveOptions);
                var effectiveClassLevelCascadeMode = classLevelCascadeMode ?? effectiveOptions.DefaultClassLevelCascadeMode;

                var filter = context.RootData?.Filter ?? default;
                var stopOnError = context.FailFast || effectiveClassLevelCascadeMode is CascadeMode.Stop;

                var selectedSync = SelectRules(SyncRules, context.RuleSets, filter, context.MemberPath, in effectiveContext);
                ValidateRules(in effectiveContext, selectedSync, errors, effectiveClassLevelCascadeMode, context.FailFast);

                if(!stopOnError || errors.Count == 0) {
                    var selectedAsync = SelectRules(AsyncRules, context.RuleSets, filter, context.MemberPath, in effectiveContext);
                    await ValidateRulesAsync(
                        effectiveContext,
                        selectedAsync,
                        errors,
                        effectiveClassLevelCascadeMode,
                        context.FailFast,
                        cancellationToken).ConfigureAwait(false);
                }

                var result = ValidationResult.FromErrors(errors);
                diagnostics?.Complete(result);
                return result;
            }
            finally {
                ReturnCollector(errors);
            }
        }
        catch(Exception exception) {
            diagnostics?.Fail(exception);
            throw;
        }
        finally {
            diagnostics?.Dispose();
        }
    }

    internal async ValueTask<ValidationResult> ValidateAsync(
        T instance,
        IEnumerable<string>? ruleSets,
        CascadeMode? classLevelCascadeMode,
        bool failFast,
        ValidationFilter filter,
        CancellationToken cancellationToken = default) {
        var diagnostics = ValidationDiagnostics.Sink?.Begin(
            typeof(T), _validatorName, ruleSets, isAsync: true);

        try {
            var hasSync = SyncRules.Length > 0;
            var hasAsync = AsyncRules.Length > 0;

            if(!hasSync && !hasAsync) {
                var success = ValidationResult.Success;
                diagnostics?.Complete(success);
                return success;
            }

            RunSanitizers(instance);

            var errors = RentCollector();
            var rootData = RootContextData.Rent();

            try {
                var effectiveOptions = ResolveOptions(filter.ServiceProvider, contextOptions: null);
                var context = new ValidationContext<T>(
                    instance,
                    MemberPath: string.Empty,
                    ruleSets,
                    failFast,
                    Root: instance,
                    RootData: rootData,
                    ServiceProvider: filter.ServiceProvider,
                    Options: effectiveOptions);
                var effectiveClassLevelCascadeMode = classLevelCascadeMode ?? effectiveOptions.DefaultClassLevelCascadeMode;

                rootData.Filter = filter;
                var stopOnError = failFast || effectiveClassLevelCascadeMode is CascadeMode.Stop;

                // 1. Sync Rules
                var selectedSync = SelectRules(SyncRules, ruleSets, filter, string.Empty, in context);
                ValidateRules(in context, selectedSync, errors, effectiveClassLevelCascadeMode, failFast);

                // 2. Async Rules — skipped when the sync phase already failed under stop/fail-fast.
                if(!stopOnError || errors.Count == 0) {
                    var selectedAsync = SelectRules(AsyncRules, ruleSets, filter, string.Empty, in context);
                    await ValidateRulesAsync(context, selectedAsync, errors, effectiveClassLevelCascadeMode, failFast, cancellationToken).ConfigureAwait(false);
                }

                var result = ValidationResult.FromErrors(errors);
                diagnostics?.Complete(result);
                return result;
            }
            finally {
                ReturnCollector(errors);
                RootContextData.Return(rootData);
            }
        }
        catch(Exception exception) {
            diagnostics?.Fail(exception);
            throw;
        }
        finally {
            diagnostics?.Dispose();
        }
    }

    private static void ValidateRules(
        in ValidationContext<T> context,
        ISyncValidationRule<T>[] rules,
        ValidationErrorCollector errors,
        CascadeMode classLevelCascadeMode,
        bool failFast) {
        var stopOnError = failFast || classLevelCascadeMode is CascadeMode.Stop;

        for(var i = 0; i < rules.Length; i++) {
            var countBefore = errors.Count;
            rules[i].Validate(in context, errors);

            if(stopOnError && errors.Count > countBefore) {
                break;
            }
        }
    }

    private static async ValueTask ValidateRulesAsync(
        ValidationContext<T> context,
        IAsyncValidationRule<T>[] rules,
        ValidationErrorCollector errors,
        CascadeMode classLevelCascadeMode,
        bool failFast,
        CancellationToken cancellationToken) {
        var stopOnError = failFast || classLevelCascadeMode is CascadeMode.Stop;

        for(var i = 0; i < rules.Length; i++) {
            var countBefore = errors.Count;
            await rules[i].ValidateAsync(context, errors, cancellationToken)
                          .ConfigureAwait(false);

            if(stopOnError && errors.Count > countBefore) {
                break;
            }
        }
    }

    private static TRule[] SelectRules<TRule>(
        TRule[] rules,
        IEnumerable<string>? ruleSets,
        ValidationFilter filter,
        string memberPath,
        in ValidationContext<T> context) where TRule : class {
        const string defaultRuleSetName = "default";

        var hasRuleSets = ruleSets?.Any() == true;
        var hasExcludedRuleSets = filter.ExcludeRuleSets is { Count: > 0 };
        var hasIncludedProperties = filter.IncludeProperties is not null;
        var hasExcludedProperties = filter.ExcludeProperties is { Count: > 0 };

        if(!hasRuleSets && !hasExcludedRuleSets && !hasIncludedProperties && !hasExcludedProperties && filter.Selector is null) {
            return rules;
        }

        var includedRuleSets = hasRuleSets
            ? new HashSet<string>(ruleSets!, StringComparer.OrdinalIgnoreCase)
            : null;
        var excludedRuleSets = hasExcludedRuleSets
            ? new HashSet<string>(filter.ExcludeRuleSets!, StringComparer.OrdinalIgnoreCase)
            : null;
        var selected = new List<TRule>(rules.Length);

        foreach(var rule in rules) {
            if(!IsIncludedByRuleSet(rule, includedRuleSets, filter.IncludeAllRuleSets, defaultRuleSetName)) {
                continue;
            }

            if(IsExcludedByRuleSet(rule, excludedRuleSets, defaultRuleSetName)) {
                continue;
            }

            if(!IsIncludedByMember(rule, memberPath, filter.IncludeProperties)) {
                continue;
            }

            if(IsExcludedByMember(rule, memberPath, filter.ExcludeProperties)) {
                continue;
            }

            if(!IsSelectedBySelector(rule, memberPath, filter.Selector, in context)) {
                continue;
            }

            selected.Add(rule);
        }

        return [.. selected];
    }

    private static bool IsIncludedByRuleSet<TRule>(
        TRule rule,
        HashSet<string>? includedRuleSets,
        bool includeAllRuleSets,
        string defaultRuleSetName) where TRule : class {
        if(includeAllRuleSets || includedRuleSets is null) {
            return true;
        }

        return (rule is RuleSetRule<T> includedSet && includedSet.Names.Any(includedRuleSets.Contains)) ||
               (includedRuleSets.Contains(defaultRuleSetName) && rule is not RuleSetRule<T>);
    }

    private static bool IsExcludedByRuleSet<TRule>(
        TRule rule,
        HashSet<string>? excludedRuleSets,
        string defaultRuleSetName) where TRule : class {
        if(excludedRuleSets is null) {
            return false;
        }

        return (rule is RuleSetRule<T> excludedSet && excludedSet.Names.Any(excludedRuleSets.Contains)) ||
               (excludedRuleSets.Contains(defaultRuleSetName) && rule is not RuleSetRule<T>);
    }

    private static bool IsIncludedByMember<TRule>(
        TRule rule,
        string memberPath,
        IReadOnlyCollection<string>? includeProperties) where TRule : class {
        if(includeProperties is null) {
            return true;
        }

        return rule is IValidationMemberRule includedMember &&
               includedMember.MemberName is { } includedName &&
               MatchesInclude(memberPath, includedName, includeProperties);
    }

    private static bool IsExcludedByMember<TRule>(
        TRule rule,
        string memberPath,
        IReadOnlyCollection<string>? excludeProperties) where TRule : class {
        if(excludeProperties is null) {
            return false;
        }

        return rule is IValidationMemberRule excludedMember &&
               excludedMember.MemberName is { } excludedName &&
               MatchesExclude(memberPath, excludedName, excludeProperties);
    }

    private static bool IsSelectedBySelector<TRule>(
        TRule rule,
        string memberPath,
        IValidatorSelector? selector,
        in ValidationContext<T> context) where TRule : class {
        if(selector is null) {
            return true;
        }

        var ruleMemberName = rule is IValidationMemberRule selectedMember &&
                             selectedMember.MemberName is { Length: > 0 } selectedName
            ? CombineMemberPath(memberPath, selectedName)
            : memberPath;

        return selector.CanExecute(ValidatorRuleSelection.GetDescriptor(rule), ruleMemberName, context);
    }

    private static bool MatchesInclude(
        string memberPath,
        string memberName,
        IReadOnlyCollection<string> patterns) {
        var path = NormalizePath(CombineMemberPath(memberPath, memberName));

        foreach(var pattern in patterns) {
            var normalized = NormalizePath(pattern);

            if(normalized.Length is 0) {
                continue;
            }

            if(path.Equals(normalized, StringComparison.Ordinal) ||
               path.StartsWith(normalized + ".", StringComparison.Ordinal) ||
               normalized.StartsWith(path + ".", StringComparison.Ordinal)) {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesExclude(
        string memberPath,
        string memberName,
        IReadOnlyCollection<string> patterns) {
        var path = NormalizePath(CombineMemberPath(memberPath, memberName));

        foreach(var pattern in patterns) {
            var normalized = NormalizePath(pattern);

            if(normalized.Length is 0) {
                continue;
            }

            if(path.Equals(normalized, StringComparison.Ordinal) ||
               path.StartsWith(normalized + ".", StringComparison.Ordinal)) {
                return true;
            }
        }

        return false;
    }

    private static string CombineMemberPath(string memberPath, string memberName) =>
        string.IsNullOrEmpty(memberPath) ? memberName : $"{memberPath}.{memberName}";

    private static string NormalizePath(string path) {
        if(path.IndexOf('[') < 0) {
            return path;
        }

        var builder = new StringBuilder(path.Length);
        var inIndexer = false;

        foreach(var character in path) {
            if(character is '[') {
                inIndexer = true;
                continue;
            }

            if(character is ']') {
                inIndexer = false;
                continue;
            }

            if(!inIndexer) {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
