//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Localization.AspNetCore;

/// <summary>
/// Configures the ASP.NET Core <see cref="IStringLocalizerFactory"/> bridge for validation
/// messages.
/// </summary>
public sealed class StringLocalizerValidationOptions {
    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="IStringLocalizer"/> source is
    /// skipped. Message packs and <see cref="ValidationError.Message"/> remain available.
    /// </summary>
    public bool DisableLocalization { get; set; }

    /// <summary>
    /// Gets or sets the culture forced for every lookup, regardless of the caller-supplied
    /// culture. Accepts a culture name such as <c>en</c> or <c>ar-SA</c>.
    /// </summary>
    public string? ForceCulture { get; set; }

    /// <summary>
    /// Gets or sets the culture used when the caller does not supply one and
    /// <see cref="ForceCulture"/> is not set. Accepts a culture name such as <c>en</c>.
    /// </summary>
    public string? DefaultCulture { get; set; }

    /// <summary>
    /// Gets or sets the type whose full name identifies the resource base name for
    /// <see cref="IStringLocalizerFactory.Create(Type)"/>. When <see langword="null"/>, the
    /// <see cref="IStringLocalizer"/> source is skipped and only message packs and
    /// <see cref="ValidationError.Message"/> are used.
    /// </summary>
    public Type? ResourceType { get; set; }
}
