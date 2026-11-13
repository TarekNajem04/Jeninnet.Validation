//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Internal, allocation-free carrier for per-call selection filters.
/// </summary>
/// <param name="IncludeProperties">The member names to include, or <see langword="null"/> for all members.</param>
/// <param name="ExcludeProperties">The member names to exclude.</param>
/// <param name="ExcludeRuleSets">The rule-set names to exclude.</param>
/// <param name="ServiceProvider">The service provider used by selectors that need services.</param>
/// <param name="IncludeAllRuleSets">Whether rules from every rule set are included.</param>
/// <param name="Selector">The custom validator selector, when one is supplied.</param>
internal readonly record struct ValidationFilter(
    IReadOnlyCollection<string>? IncludeProperties = null,
    IReadOnlyCollection<string>? ExcludeProperties = null,
    IReadOnlyCollection<string>? ExcludeRuleSets = null,
    IServiceProvider? ServiceProvider = null,
    bool IncludeAllRuleSets = false,
    IValidatorSelector? Selector = null);
