//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.ClientRules;

/// <summary>
/// Options for client-side rule export.
/// </summary>
public sealed class ClientRulesOptions {
    /// <summary>
    /// Gets or sets the payload schema version stamped into the document.
    /// Defaults to <c>1.0</c>.
    /// </summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets an optional resolver that turns a rule descriptor into a
    /// pre-resolved message. When <see langword="null"/> (the default), the
    /// payload carries only <c>messageKey</c> so the client can resolve the
    /// message for its own culture at runtime.
    /// </summary>
    public Func<IValidationRuleDescriptor, string?>? MessageResolver { get; set; }

    /// <summary>
    /// Gets the allow-list of validator names to export. When empty, every
    /// supplied validator is exported.
    /// </summary>
    public ISet<string> IncludedValidators { get; } = new HashSet<string>(StringComparer.Ordinal);
}
