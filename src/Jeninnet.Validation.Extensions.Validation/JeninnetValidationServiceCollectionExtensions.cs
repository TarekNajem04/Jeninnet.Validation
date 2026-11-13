//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

#if NET11_0_OR_GREATER
using ValidatableTypeInfo = Microsoft.Extensions.Validation.IValidatableTypeInfo;
#else
using ValidatableTypeInfo = Microsoft.Extensions.Validation.IValidatableInfo;
#endif

namespace Jeninnet.Validation.Extensions.Validation;

/// <summary>
/// Registers Jeninnet validators with the <c>Microsoft.Extensions.Validation</c> pipeline.
/// </summary>
public static class JeninnetValidationServiceCollectionExtensions {
    /// <summary>
    /// Adds a resolver for every <c>IValidator&lt;T&gt;</c> already registered in the service collection.
    /// Register the generated or fluent validators before calling this overload.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    [RequiresUnreferencedCode("The non-generic overload discovers validator model types from service descriptors. Use the generic overload for Native AOT.")]
    [RequiresDynamicCode("The non-generic overload creates generic validation adapters at runtime. Use the generic overload for Native AOT.")]
    public static IServiceCollection AddJeninnetValidation(this IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);

        var modelTypes = services
            .Where(static descriptor =>
                descriptor.ServiceType.IsGenericType &&
                descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>))
            .Select(static descriptor => descriptor.ServiceType.GetGenericArguments()[0])
            .Distinct()
            .ToArray();

        return services.AddJeninnetValidation(modelTypes);
    }

    /// <summary>
    /// Adds a resolver for the specified model types. Their validators must be registered as
    /// <c>IValidator&lt;T&gt;</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="modelTypes">The model types to validate.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="modelTypes"/> is null.</exception>
    /// <exception cref="ArgumentException">A model type is null or has no validator.</exception>
    [RequiresUnreferencedCode("This overload creates generic validation adapters at runtime. Use AddJeninnetValidation<TModel>() for Native AOT.")]
    [RequiresDynamicCode("This overload creates generic validation adapters at runtime. Use AddJeninnetValidation<TModel>() for Native AOT.")]
    public static IServiceCollection AddJeninnetValidation(
        this IServiceCollection services,
        params Type[] modelTypes) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(modelTypes);

        var factories = new Dictionary<Type, Func<ValidatableTypeInfo>>(modelTypes.Length);

        foreach(var modelType in modelTypes) {
            ArgumentNullException.ThrowIfNull(modelType);

            if(!factories.ContainsKey(modelType)) {
                factories[modelType] = CreateFactory(modelType);
            }
        }

        return AddResolver(services, factories);
    }

    /// <summary>
    /// Adds a resolver for one model type without runtime reflection or dynamic code.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddJeninnetValidation<TModel>(this IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);

        var factories = new Dictionary<Type, Func<ValidatableTypeInfo>> {
            [typeof(TModel)] = static () => new JeninnetValidatableInfo<TModel>()
        };

        return AddResolver(services, factories);
    }

    private static IServiceCollection AddResolver(
        IServiceCollection services,
        Dictionary<Type, Func<ValidatableTypeInfo>> factories) {
        if(factories.Count == 0) {
            return services;
        }

        var resolver = new JeninnetValidatableInfoResolver(factories);
        services.Configure<ValidationOptions>(options => options.Resolvers.Add(resolver));
        return services;
    }

    [RequiresUnreferencedCode("Generic adapter construction is not trim-safe.")]
    [RequiresDynamicCode("Generic adapter construction is not Native AOT-safe.")]
    private static Func<ValidatableTypeInfo> CreateFactory(Type modelType) {
        var infoType = typeof(JeninnetValidatableInfo<>).MakeGenericType(modelType);

        return () => (ValidatableTypeInfo)Activator.CreateInstance(infoType)!;
    }
}
