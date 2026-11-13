//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Localization.AspNetCore;

/// <summary>
/// Resolves validation messages from an ASP.NET Core <see cref="IStringLocalizerFactory"/>, then
/// from the registered <see cref="IMessageLocalizer"/> message packs, then from
/// <see cref="ValidationError.Message"/>.
/// </summary>
/// <remarks>
/// <see cref="IStringLocalizer"/> has no culture-aware overload. The resolver therefore creates
/// the localizer and performs the lookup inside a scoped <see cref="CultureInfo.CurrentUICulture"/>
/// assignment that is restored in a <c>finally</c> block. <see cref="CultureInfo.CurrentCulture"/>
/// is never changed and no culture leaks beyond the call.
/// </remarks>
public sealed partial class StringLocalizerValidationMessageResolver : IValidationMessageResolver {
    private const string PROPERTY_NAME_KEY = "PropertyName";

    private readonly IStringLocalizerFactory _localizerFactory;
    private readonly StringLocalizerValidationOptions _options;
    private readonly IMessageLocalizer? _packLocalizer;
    private readonly IMessageFormatter? _formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringLocalizerValidationMessageResolver"/> class.
    /// </summary>
    /// <param name="localizerFactory">The ASP.NET Core string localizer factory.</param>
    /// <param name="options">The bridge options.</param>
    /// <param name="packLocalizers">The registered message-pack localizers; the last one wins.</param>
    /// <param name="formatters">The registered message formatters; the last one wins.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public StringLocalizerValidationMessageResolver(
        IStringLocalizerFactory localizerFactory,
        StringLocalizerValidationOptions options,
        IEnumerable<IMessageLocalizer> packLocalizers,
        IEnumerable<IMessageFormatter> formatters) {
        ArgumentNullException.ThrowIfNull(localizerFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(packLocalizers);
        ArgumentNullException.ThrowIfNull(formatters);

        _localizerFactory = localizerFactory;
        _options = options;
        _packLocalizer = packLocalizers.LastOrDefault();
        _formatter = formatters.LastOrDefault();
    }

    /// <inheritdoc />
    public async ValueTask<string?> ResolveAsync(
        ValidationError error,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        if(error.MessageIsAuthored) {
            return error.Message;
        }

        var effectiveCulture = ResolveCulture(culture);
        var parameters = CreateParameters(error);

        if(!_options.DisableLocalization) {
            var localized = ResolveFromStringLocalizer(error, effectiveCulture, parameters);

            if(localized is not null) {
                return localized;
            }
        }

        var packMessage = await ResolveFromPackAsync(error, effectiveCulture, parameters, cancellationToken)
            .ConfigureAwait(false);

        return packMessage ?? error.Message;
    }

    private CultureInfo ResolveCulture(CultureInfo? culture) {
        if(!string.IsNullOrWhiteSpace(_options.ForceCulture)) {
            return CultureInfo.GetCultureInfo(_options.ForceCulture);
        }

        if(culture is not null) {
            return culture;
        }

        if(!string.IsNullOrWhiteSpace(_options.DefaultCulture)) {
            return CultureInfo.GetCultureInfo(_options.DefaultCulture);
        }

        return CultureInfo.CurrentUICulture;
    }

    private string? ResolveFromStringLocalizer(
        ValidationError error,
        CultureInfo culture,
        IReadOnlyDictionary<string, object?> parameters) {
        if(_options.ResourceType is null || string.IsNullOrWhiteSpace(error.MessageKey)) {
            return null;
        }

        var originalCulture = CultureInfo.CurrentUICulture;

        try {
            // IStringLocalizer has no culture parameter. Scope the UI culture to the lookup and
            // restore it immediately; CurrentCulture and the caller's context stay untouched.
            CultureInfo.CurrentUICulture = culture;

            var localizer = _localizerFactory.Create(_options.ResourceType);
            var localized = localizer[error.MessageKey];

            if(localized.ResourceNotFound || string.IsNullOrWhiteSpace(localized.Value)) {
                return null;
            }

            return FormatTemplate(localized.Value, parameters, culture);
        }
        finally {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    private string FormatTemplate(
        string template,
        IReadOnlyDictionary<string, object?> parameters,
        CultureInfo culture) {
        if(parameters.Count == 0) {
            return template;
        }

        if(_formatter is ICultureAwareMessageFormatter cultureAwareFormatter) {
            return cultureAwareFormatter.Format(template, parameters, culture);
        }

        if(_formatter is not null) {
            return _formatter.Format(template, parameters);
        }

        return FormatNamedParameters(template, parameters, culture);
    }

    private static string FormatNamedParameters(
        string template,
        IReadOnlyDictionary<string, object?> parameters,
        CultureInfo culture) =>
        TemplatePattern().Replace(template, match => {
            var token = match.Groups["key"].Value;
            var separator = token.IndexOf(':');
            var key = separator < 0 ? token : token[..separator];

            if(!parameters.TryGetValue(key, out var value) || value is null) {
                return match.Value;
            }

            var format = separator < 0 ? null : token[(separator + 1)..];

            return value is IFormattable formattable
                ? formattable.ToString(format, culture) ?? string.Empty
                : Convert.ToString(value, culture) ?? string.Empty;
        });

    private async ValueTask<string?> ResolveFromPackAsync(
        ValidationError error,
        CultureInfo culture,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken) {
        if(_packLocalizer is null || string.IsNullOrWhiteSpace(error.MessageKey)) {
            return null;
        }

        var localized = await _packLocalizer
            .LocalizeAsync(error.MessageKey, parameters, culture, cancellationToken)
            .ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(localized) ? null : localized;
    }

    private static Dictionary<string, object?> CreateParameters(ValidationError error) {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        if(error.MessageParameters is not null) {
            foreach(var parameter in error.MessageParameters) {
                parameters[parameter.Key] = parameter.Value;
            }
        }

        if(!parameters.ContainsKey(PROPERTY_NAME_KEY)) {
            parameters[PROPERTY_NAME_KEY] = error.MemberName;
        }

        return parameters;
    }

    [GeneratedRegex(@"\{(?<key>[^{}]+)\}", RegexOptions.Compiled)]
    private static partial Regex TemplatePattern();
}
