//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Sample.Mvc.Models;

/// <summary>
/// Model used by the static SSR validation sample.
/// </summary>
[Validate("Jeninnet.Validation.Sample.Mvc.Validators")]
public sealed class StaticSsrRegistration {
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    [NotNull]
    [NotEmpty]
    [Length(2, 40)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    [NotNull]
    [NotEmpty]
    [Email]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the age.
    /// </summary>
    [InclusiveBetween(18, 120)]
    public int Age { get; set; }

    /// <summary>
    /// Gets or sets the three-letter code.
    /// </summary>
    [NotNull]
    [NotEmpty]
    [Regex("^[A-Z]{3}$")]
    public string? Code { get; set; }
}
