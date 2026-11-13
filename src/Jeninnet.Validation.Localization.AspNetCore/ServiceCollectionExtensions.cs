//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Localization.AspNetCore;

/// <summary>
/// Registration helpers for the ASP.NET Core string-localizer bridge.
/// </summary>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// Registers the ASP.NET Core string-localizer bridge and its options.
    /// </summary>
    /// <remarks>
    /// The method calls <c>AddLogging()</c> and <c>AddLocalization()</c> to guarantee the
    /// dependencies of the default <see cref="IStringLocalizerFactory"/>. Factories registered
    /// before this call win because the framework uses <c>TryAdd</c>, so a custom factory or the
    /// store-backed factory from <c>Jeninnet.Localization.Core</c> is preserved. The store-backed
    /// factory requires an <c>ILocalizationStore</c>; register a store provider when that factory
    /// is active. The bridge registers no <see cref="IInnerMessageProvider"/>; message packs remain
    /// the single provider.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional options configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddJeninnetStringLocalizer(
        this IServiceCollection services,
        Action<StringLocalizerValidationOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLogging();
        services.AddLocalization();

        var options = new StringLocalizerValidationOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<StringLocalizerValidationMessageResolver>();
        services.TryAddSingleton<IValidationMessageResolver>(
            static provider => provider.GetRequiredService<StringLocalizerValidationMessageResolver>());

        return services;
    }
}
