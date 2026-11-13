//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Provides compiled regular expressions used by string validation rules.
/// </summary>
internal static partial class StringRegexes {
    /// <summary>Simplified email pattern; not fully RFC-compliant but practical.</summary>
    internal static readonly Regex Email = EmailRegex();

    /// <summary>Very basic credit card pattern: digits only, length between 12 and 19.</summary>
    internal static readonly Regex CreditCard = CreditCardRegex();

    /// <summary>Simple phone number pattern.</summary>
    internal static readonly Regex Phone = PhoneRegex();

    /// <summary>Basic URL pattern.</summary>
    internal static readonly Regex Url = UrlRegex();

    /// <remarks>
    /// Pattern:<br/>
    /// <code>^[^@\s]+@[^@\s]+\.[^@\s]+$</code><br/>
    /// Options:<br/>
    /// <code>RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant</code><br/>
    /// Explanation:<br/>
    /// <code>
    /// ○ Match if at the beginning of the string.<br/>
    /// ○ Match a character in the set [^@\s] atomically at least once.<br/>
    /// ○ Match '@'.<br/>
    /// ○ Match a character in the set [^@\s] greedily at least once.<br/>
    /// ○ Match '.'.<br/>
    /// ○ Match a character in the set [^@\s] atomically at least once.<br/>
    /// ○ Match if at the end of the string or if before an ending newline.<br/>
    /// </code>
    /// </remarks>
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    /// <remarks>
    /// Pattern:<br/>
    /// <code>^[0-9]{12,19}$</code><br/>
    /// Options:<br/>
    /// <code>RegexOptions.Compiled | RegexOptions.CultureInvariant</code><br/>
    /// Explanation:<br/>
    /// <code>
    /// ○ Match if at the beginning of the string.<br/>
    /// ○ Match a character in the set [0-9] atomically at least 12 and at most 19 times.<br/>
    /// ○ Match if at the end of the string or if before an ending newline.<br/>
    /// </code>
    /// </remarks>
    [GeneratedRegex(@"^[0-9]{12,19}$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex CreditCardRegex();

    /// <remarks>
    /// Pattern:<br/>
    /// <code>^\+?[0-9\s-]{7,20}$</code><br/>
    /// Options:<br/>
    /// <code>RegexOptions.Compiled | RegexOptions.CultureInvariant</code><br/>
    /// Explanation:<br/>
    /// <code>
    /// ○ Match if at the beginning of the string.<br/>
    /// ○ Match '+' atomically, optionally.<br/>
    /// ○ Match a character in the set [\-0-9\s] greedily at least 7 and at most 20 times.<br/>
    /// ○ Match if at the end of the string or if before an ending newline.<br/>
    /// </code>
    /// </remarks>
    [GeneratedRegex(@"^\+?[0-9\s-]{7,20}$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();

    /// <remarks>
    /// Pattern:<br/>
    /// <code>^(https?|ftp)://[^\s/$.?#].[^\s]*$</code><br/>
    /// Options:<br/>
    /// <code>RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant</code><br/>
    /// Explanation:<br/>
    /// <code>
    /// ○ Match if at the beginning of the string.<br/>
    /// ○ 1st capture group.<br/>
    ///     ○ Match with 2 alternative expressions.<br/>
    ///         ○ Match a sequence of expressions.<br/>
    ///             ○ Match a character in the set [Hh].<br/>
    ///             ○ Match a character in the set [Tt] exactly 2 times.<br/>
    ///             ○ Match a character in the set [Pp].<br/>
    ///             ○ Match a character in the set [Ss] atomically, optionally.<br/>
    ///         ○ Match a sequence of expressions.<br/>
    ///             ○ Match a character in the set [Ff].<br/>
    ///             ○ Match a character in the set [Tt].<br/>
    ///             ○ Match a character in the set [Pp].<br/>
    /// ○ Match the string "://".<br/>
    /// ○ Match a character in the set [^#$./?\s].<br/>
    /// ○ Match any character other than '\n'.<br/>
    /// ○ Match any character other than a whitespace character atomically any number of times.<br/>
    /// ○ Match if at the end of the string or if before an ending newline.<br/>
    /// </code>
    /// </remarks>
    [GeneratedRegex(@"^(https?|ftp)://[^\s/$.?#].[^\s]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();
}
