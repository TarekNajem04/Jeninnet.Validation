//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Localization;

/// <summary>
/// Registration helpers for validation message packs.
/// </summary>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// Registers the validation message pipeline (<see cref="IMessageLocalizer"/>,
    /// <see cref="IMessageProvider"/>, and <see cref="IMessageFormatter"/>) without registering
    /// message packs. Existing host registrations win. Call <c>AddDefaultMessagePacks</c> to add
    /// the built-in packs as well.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddValidationLocalization(this IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IMessageLocalizer>(static provider => new DefaultMessageLocalizer(
            provider.GetRequiredService<IMessageProvider>(),
            provider.GetRequiredService<IMessageFormatter>(),
            provider.GetService<LocalizationOptions>()));
        services.TryAddSingleton<IMessageFormatter, DefaultMessageFormatter>();
        services.TryAddSingleton<IMessageProvider, LocalizationProviderSelector>();
        return services;
    }

    /// <summary>
    /// Registers the built-in validation message packs and the mutable message catalog.
    /// </summary>
    /// <remarks>
    /// The sibling localization platform enforces exactly one <see cref="IInnerMessageProvider"/>.
    /// Use the overload that accepts a configuration callback to add custom packs or per-key
    /// overrides instead of registering a second provider. Resolve
    /// <see cref="IValidationMessageCatalog"/> to change messages or cultures at runtime.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <exception cref="InvalidOperationException">An <see cref="IInnerMessageProvider"/> is already registered.</exception>
    public static IServiceCollection AddDefaultMessagePacks(this IServiceCollection services) =>
        AddDefaultMessagePacks(services, static _ => { });

    /// <summary>
    /// Registers the built-in validation message packs and the mutable message catalog, with
    /// optional custom packs and per-key overrides.
    /// </summary>
    /// <remarks>
    /// The sibling localization platform enforces exactly one <see cref="IInnerMessageProvider"/>.
    /// Custom messages are therefore merged over the built-in templates inside the single provider
    /// rather than registered as a second provider. Resolve <see cref="IValidationMessageCatalog"/>
    /// to change messages or cultures at runtime.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures additional packs and per-key overrides.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">An <see cref="IInnerMessageProvider"/> is already registered.</exception>
    public static IServiceCollection AddDefaultMessagePacks(
        this IServiceCollection services,
        Action<ValidationMessagePackOptions> configure) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        if(services.Any(static descriptor => descriptor.ServiceType == typeof(IInnerMessageProvider))) {
            throw new InvalidOperationException(
                "The localization platform enforces exactly one IInnerMessageProvider. " +
                "Configure custom packs or overrides through AddDefaultMessagePacks(options => ...) " +
                "instead of registering a separate provider.");
        }

        var options = new ValidationMessagePackOptions();
        configure(options);

        services.AddValidationLocalization();

        var provider = new DefaultValidationMessageProvider(options);

        services.AddSingleton<IInnerMessageProvider>(provider);
        services.AddSingleton<IValidationMessageCatalog>(provider.Catalog);
        return services;
    }
}
