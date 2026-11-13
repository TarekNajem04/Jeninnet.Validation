//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Localization;

/// <summary>
/// Provides the built-in validation message packs embedded in this assembly, with optional custom
/// packs and per-key overrides merged over them.
/// </summary>
public sealed class DefaultValidationMessageProvider : IInnerMessageProvider {
    private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> _builtInPacks = LoadBuiltInPacks();

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultValidationMessageProvider"/> class with
    /// only the built-in packs.
    /// </summary>
    public DefaultValidationMessageProvider() : this(options: null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultValidationMessageProvider"/> class with
    /// optional custom packs and per-key overrides.
    /// </summary>
    /// <param name="options">The custom pack options, or <see langword="null"/> for built-ins only.</param>
    public DefaultValidationMessageProvider(ValidationMessagePackOptions? options) =>
        Catalog = new ValidationMessageCatalog(Merge(_builtInPacks, options?.Build()));

    /// <summary>
    /// Initializes a new provider over an existing catalog.
    /// </summary>
    /// <param name="catalog">The message catalog.</param>
    internal DefaultValidationMessageProvider(IValidationMessageCatalog catalog) {
        ArgumentNullException.ThrowIfNull(catalog);

        Catalog = catalog;
    }

    /// <summary>
    /// Gets the catalog used by this provider. Registrations expose the same catalog as
    /// <see cref="IValidationMessageCatalog"/>.
    /// </summary>
    internal IValidationMessageCatalog Catalog { get; }

    /// <summary>
    /// Gets the built-in English message templates.
    /// </summary>
    public static IReadOnlyDictionary<string, string> English => _builtInPacks["en"];

    /// <summary>
    /// Gets the built-in Arabic message templates.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Arabic => _builtInPacks["ar"];

    /// <summary>
    /// Gets the built-in culture codes in ordinal order.
    /// </summary>
    public static IReadOnlyCollection<string> SupportedCultures =>
        [.. _builtInPacks.Keys.OrderBy(static culture => culture, StringComparer.Ordinal)];

    /// <summary>
    /// Gets one built-in pack by culture code. Intended for catalog tests and diagnostics.
    /// </summary>
    /// <param name="culture">The culture code (for example <c>fr</c>, <c>pt-BR</c>, or <c>zh-Hans</c>).</param>
    /// <returns>The message templates for the culture.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="culture"/> is null.</exception>
    /// <exception cref="ArgumentException">No built-in pack uses the culture code.</exception>
    internal static IReadOnlyDictionary<string, string> GetPack(string culture) {
        ArgumentNullException.ThrowIfNull(culture);

        return _builtInPacks.TryGetValue(culture, out var pack)
            ? pack
            : throw new ArgumentException($"Unknown validation message pack '{culture}'.", nameof(culture));
    }

    /// <inheritdoc />
    public ValueTask<string?> GetMessageTemplateAsync(
        string key,
        CultureInfo culture,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(culture);
        cancellationToken.ThrowIfCancellationRequested();

        if(Catalog.TryGetTemplate(culture.Name, key, out var template)) {
            return ValueTask.FromResult(template);
        }

        if(Catalog.TryGetTemplate(culture.TwoLetterISOLanguageName, key, out template)) {
            return ValueTask.FromResult(template);
        }

        return ValueTask.FromResult<string?>(null);
    }

    private static Dictionary<string, IReadOnlyDictionary<string, string>> Merge(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> builtIn,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? custom) {
        var merged = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach(var pack in builtIn) {
            merged[pack.Key] = new Dictionary<string, string>(pack.Value, StringComparer.Ordinal);
        }

        if(custom is not null) {
            foreach(var pack in custom) {
                if(!merged.TryGetValue(pack.Key, out var target)) {
                    target = [with(StringComparer.Ordinal)];
                    merged[pack.Key] = target;
                }

                foreach(var message in pack.Value) {
                    target[message.Key] = message.Value;
                }
            }
        }

        var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach(var pack in merged) {
            result[pack.Key] = pack.Value;
        }

        return result;
    }

    private static Dictionary<string, IReadOnlyDictionary<string, string>> LoadBuiltInPacks() {
        const string resourceMarker = ".Resources.";
        const string resourceExtension = ".json";

        var assembly = typeof(DefaultValidationMessageProvider).Assembly;
        var packs = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach(var resourceName in assembly.GetManifestResourceNames()) {
            var markerIndex = resourceName.LastIndexOf(resourceMarker, StringComparison.Ordinal);

            if(markerIndex < 0 || !resourceName.EndsWith(resourceExtension, StringComparison.Ordinal)) {
                continue;
            }

            var culture = resourceName[(markerIndex + resourceMarker.Length)..^resourceExtension.Length];

            if(culture.Length == 0 || culture.Contains('.')) {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded validation message pack '{culture}' could not be opened.");
            using var document = JsonDocument.Parse(stream);

            var messages = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach(var property in document.RootElement.EnumerateObject()) {
                messages[property.Name] = property.Value.GetString()
                    ?? throw new InvalidOperationException(
                        $"Validation message '{property.Name}' in pack '{culture}' must be a string.");
            }

            packs[culture] = messages;
        }

        if(packs.Count == 0) {
            throw new InvalidOperationException("No embedded validation message packs were found.");
        }

        return packs;
    }
}
