//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Configuration;

/// <summary>
/// Provides access to global validation configuration.
/// </summary>
public static class ValidationConfiguration {
    private static ValidationOptions _options = new();

    /// <summary>
    /// Gets the current global validation options.
    /// </summary>
    public static ValidationOptions Options => Volatile.Read(ref _options);

    /// <summary>
    /// Configures the global validation options.
    /// </summary>
    /// <param name="configure">
    /// A callback that receives the current <see cref="ValidationOptions"/> and returns a new instance.
    /// </param>
    public static void Configure(Func<ValidationOptions, ValidationOptions> configure) {
        ArgumentNullException.ThrowIfNull(configure);

        while(true) {
            var snapshot = Options;
            var updated = configure(snapshot);

            var original = Interlocked.CompareExchange(ref _options, updated, snapshot);
            if(ReferenceEquals(original, snapshot)) {
                break;
            }
        }
    }

    /// <summary>
    /// Resolves the validation options for a service provider. A container-registered
    /// <see cref="ValidationOptions"/> instance wins over the process-wide fallback.
    /// </summary>
    /// <param name="serviceProvider">The optional service provider.</param>
    /// <returns>The container options when registered; otherwise the process-wide options.</returns>
    public static ValidationOptions ResolveOptions(IServiceProvider? serviceProvider) =>
        serviceProvider?.GetService<ValidationOptions>() ?? Options;

    /// <summary>
    /// Creates a validator instance for <paramref name="modelType"/> through
    /// <see cref="ValidationOptions.ValidatorFactory"/> when a factory is configured.
    /// </summary>
    /// <param name="modelType">The model type to validate.</param>
    /// <param name="serviceProvider">The optional service provider passed to the factory.</param>
    /// <returns>The created validator, or <see langword="null"/> when no factory is configured or it returns null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="modelType"/> is null.</exception>
    public static object? CreateValidator(Type modelType, IServiceProvider? serviceProvider) {
        ArgumentNullException.ThrowIfNull(modelType);

        var factory = Options.ValidatorFactory;
        if(factory is null) {
            return null;
        }

        return factory(modelType, serviceProvider);
    }

    /// <summary>
    /// Creates a validator instance for <typeparamref name="T"/> through
    /// <see cref="ValidationOptions.ValidatorFactory"/> when a factory is configured.
    /// </summary>
    /// <typeparam name="T">The model type to validate.</typeparam>
    /// <param name="serviceProvider">The optional service provider passed to the factory.</param>
    /// <returns>The created validator, or <see langword="null"/> when no factory is configured or it returns null.</returns>
    /// <exception cref="InvalidCastException">
    /// The factory result does not implement <see cref="IValidator{T}"/>.
    /// </exception>
    public static IValidator<T>? CreateValidator<T>(IServiceProvider? serviceProvider) =>
        (IValidator<T>?)CreateValidator(typeof(T), serviceProvider);
}
