//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A message-parameter dictionary created fresh for one emitted error. The validation engine
/// adds the property-name, property-path, and property-value entries in place instead of copying
/// the rule parameters into a second dictionary.
/// </summary>
/// <param name="ruleParameterCount">The number of entries the rule writes before enrichment.</param>
internal sealed class MessageParameterDictionary(int ruleParameterCount)
    : Dictionary<string, object>(ruleParameterCount + EnrichmentCapacity, StringComparer.Ordinal) {
    /// <summary>
    /// The number of entries the engine always contributes: property name, property path, and
    /// (unless the rule already supplied it) property value.
    /// </summary>
    internal const int EnrichmentCapacity = 3;
}
