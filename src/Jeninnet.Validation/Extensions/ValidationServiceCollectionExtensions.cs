//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Extensions;

/// <summary>
/// Provides extension methods for registering generated validators with the DI container.
/// </summary>
public static class ValidationServiceCollectionExtensions {
    private static int _reflectionFallbackCount;

    /// <summary>
    /// Registers the validator infrastructure required by generated validators: the validator
    /// factory and the service-provider forwarder. This overload performs no assembly scanning,
    /// has no localization dependency, and is safe for trimmed and Native AOT applications.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    /// Optional transform applied to the process-wide defaults before the options are registered.
    /// The first registration wins, so call this overload once per container.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="configure"/> returns null.</exception>
    public static IServiceCollection AddValidationCore(
        this IServiceCollection services,
        Func<ValidationOptions, ValidationOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);

        EnsureInfrastructure(services, configure);
        return services;
    }

    /// <summary>
    /// Scans all loaded assemblies for generated validators and registers them with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This overload uses reflection-based assembly scanning. Trimmed and NativeAOT applications
    /// should register validators explicitly instead (for example,
    /// <c>services.AddScoped&lt;OrderValidator&gt;();</c> plus
    /// <c>services.AddScoped&lt;IValidator&lt;Order&gt;, OrderValidator&gt;();</c>).
    /// </remarks>
    /// <remarks>
    /// This overload uses the generated catalog first and falls back to reflection-based assembly
    /// scanning only when no generated validators have registered themselves. Trimmed and NativeAOT
    /// applications should call <c>AddValidation()</c> from <c>Jeninnet.Validation.Catalog</c> instead.
    /// </remarks>
    [RequiresUnreferencedCode("Assembly scanning uses reflection; register validators explicitly in trimmed or NativeAOT applications.")]
    public static IServiceCollection AddGeneratedValidators(this IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidationCore();

        var registrations = GeneratedValidatorCatalog.Entries;
        if(registrations.Count > 0) {
            foreach(var registration in registrations) {
                registration.Register(services, ServiceLifetime.Scoped);
            }

            return services;
        }

        Interlocked.Increment(ref _reflectionFallbackCount);
        return services.AddGeneratedValidators(AppDomain.CurrentDomain.GetAssemblies());
    }

    /// <summary>
    /// Scans the specified assemblies for generated validators and registers them with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">
    /// The assemblies to scan. If <see langword="null"/> or empty, all loaded assemblies are scanned.
    /// Dynamic assemblies are skipped, and assemblies whose types cannot all be loaded are partially scanned.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This overload uses reflection-based assembly scanning. Trimmed and NativeAOT applications
    /// should register validators explicitly instead.
    /// </remarks>
    [RequiresUnreferencedCode("Assembly scanning uses reflection; register validators explicitly in trimmed or NativeAOT applications.")]
    public static IServiceCollection AddGeneratedValidators(this IServiceCollection services, params Assembly[] assemblies) {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidationCore();

        if(assemblies is null || assemblies.Length == 0) {
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }

        foreach(var assembly in assemblies) {
            foreach(var type in GetLoadableTypes(assembly)) {
                RegisterValidatorType(services, type);
            }
        }

        return services;
    }

    /// <summary>
    /// Registers the specified validator types with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="types">The validator types to register.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This entry point registers the validator infrastructure and the given types; it does not scan
    /// assemblies. Use the assembly overload to discover generated validators automatically.
    /// Trimmed and NativeAOT applications should register validators explicitly instead.
    /// </remarks>
    [RequiresUnreferencedCode("Type-based registration inspects interfaces at runtime; register validators explicitly in trimmed or NativeAOT applications.")]
    public static IServiceCollection AddGeneratedValidators(this IServiceCollection services, params Type[] types) {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidationCore();

        if(types is null) {
            return services;
        }

        foreach(var type in types) {
            RegisterValidatorType(services, type);
        }

        return services;
    }

    private static void EnsureInfrastructure(
        IServiceCollection services,
        Func<ValidationOptions, ValidationOptions>? configure) {
        var options = configure is null
            ? ValidationConfiguration.Options
            : configure(ValidationConfiguration.Options) ??
                throw new InvalidOperationException("The validation options configuration returned null.");
        services.TryAddSingleton(options);
        services.TryAddSingleton<IValidatorFactory, ValidatorFactoryImplementation>();
        services.TryAddSingleton<IServiceProvider>(static provider => {
            ValidatorFactory.SetProvider(provider);
            return provider;
        });
    }

    internal static int ReflectionFallbackCount => Volatile.Read(ref _reflectionFallbackCount);

    internal static void ResetReflectionFallbackCount() =>
        Volatile.Write(ref _reflectionFallbackCount, 0);

    [RequiresUnreferencedCode("Assembly type enumeration may be trimmed.")]
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly) {
        if(assembly.IsDynamic) {
            return [];
        }

        try {
            return assembly.GetTypes();
        }
        catch(ReflectionTypeLoadException exception) {
            return exception.Types.OfType<Type>();
        }
    }

    private static void RegisterValidatorType(
        IServiceCollection services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] Type? type) {
        if(
            type?.IsAbstract is not false ||
            type.IsInterface ||
            !typeof(IGeneratedValidator).IsAssignableFrom(type)
        ) {
            return;
        }

        var validatorInterfaces = type.GetInterfaces()
                                      .Where(
                                          static i =>
                                              i.IsGenericType && (
                                                  i.GetGenericTypeDefinition() == typeof(IValidator<>) ||
                                                  i.GetGenericTypeDefinition() == typeof(ISyncValidator<>) ||
                                                  i.GetGenericTypeDefinition() == typeof(IAsyncValidator<>)))
                                      .ToList();

        services.AddScoped(type);

        for(var i = 0; i < validatorInterfaces.Count; i++) {
            var iface = validatorInterfaces[i];
            services.AddScoped(iface, type);
        }
    }
}
