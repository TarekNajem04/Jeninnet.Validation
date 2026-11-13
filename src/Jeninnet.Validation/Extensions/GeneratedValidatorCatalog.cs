//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Extensions;

/// <summary>
/// Central catalog of validators emitted by the Jeninnet source generator. Each generated assembly
/// registers its validators from a module initializer, so a single <c>AddValidation()</c> call can
/// register them without reflection or assembly scanning. This type is infrastructure for generated
/// code; application code should not call it directly.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GeneratedValidatorCatalog {
    private static readonly Lock _sync = new();
    private static readonly List<GeneratedValidatorRegistration> _registrations = [];
    private static readonly Dictionary<Type, GeneratedValidatorRegistration> _byModel = [];

    /// <summary>
    /// Gets a snapshot of the registered validators in registration order.
    /// </summary>
    public static IReadOnlyList<GeneratedValidatorRegistration> Entries {
        get {
            lock(_sync) {
                return [.. _registrations];
            }
        }
    }

    /// <summary>
    /// Registers one generated validator. Duplicate model registrations are ignored, so repeated
    /// module initialization or multiple generators cannot register the same model twice.
    /// </summary>
    /// <param name="modelType">The validated model type.</param>
    /// <param name="validatorType">The generated validator type.</param>
    /// <param name="register">The registration callback emitted by the generator.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static void Register(
        Type modelType,
        Type validatorType,
        Action<IServiceCollection, ServiceLifetime> register) {
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(validatorType);
        ArgumentNullException.ThrowIfNull(register);

        lock(_sync) {
            if(_byModel.ContainsKey(modelType)) {
                return;
            }

            var registration = new GeneratedValidatorRegistration(modelType, validatorType, register);
            _byModel.Add(modelType, registration);
            _registrations.Add(registration);
        }
    }

    /// <summary>
    /// Tries to get the registration for a validated model type.
    /// </summary>
    /// <param name="modelType">The model type.</param>
    /// <param name="registration">The registration when found.</param>
    /// <returns><see langword="true"/> when a registration exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="modelType"/> is null.</exception>
    public static bool TryGet(
        Type modelType,
        [NotNullWhen(true)] out GeneratedValidatorRegistration? registration) {
        ArgumentNullException.ThrowIfNull(modelType);

        lock(_sync) {
            return _byModel.TryGetValue(modelType, out registration);
        }
    }

    internal static void ClearForTests() {
        lock(_sync) {
            _registrations.Clear();
            _byModel.Clear();
        }
    }
}

/// <summary>
/// One generated validator registration. This type is infrastructure for generated code.
/// </summary>
/// <remarks>
/// Initializes a registration.
/// </remarks>
/// <param name="modelType">The validated model type.</param>
/// <param name="validatorType">The generated validator type.</param>
/// <param name="register">The registration callback emitted by the generator.</param>
/// <exception cref="ArgumentNullException">Any argument is null.</exception>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class GeneratedValidatorRegistration(
    Type modelType,
    Type validatorType,
    Action<IServiceCollection, ServiceLifetime> register) {

    /// <summary>
    /// Gets the validated model type.
    /// </summary>
    public Type ModelType { get; } = modelType ?? throw new ArgumentNullException(nameof(modelType));

    /// <summary>
    /// Gets the generated validator type.
    /// </summary>
    public Type ValidatorType { get; } = validatorType ?? throw new ArgumentNullException(nameof(validatorType));

    /// <summary>
    /// Gets the registration callback for catalogue snapshots used by tests.
    /// </summary>
    internal Action<IServiceCollection, ServiceLifetime> RegisterAction { get; } = register ?? throw new ArgumentNullException(nameof(register));

    /// <summary>
    /// Registers the validator with the supplied service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The lifetime for new registrations.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public void Register(IServiceCollection services, ServiceLifetime lifetime) {
        ArgumentNullException.ThrowIfNull(services);
        RegisterAction(services, lifetime);
    }
}
