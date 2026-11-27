//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Extensions.Validation;

/// <summary>
/// Registers Jeninnet validators as Microsoft.Extensions.Options validators.
/// </summary>
public static class OptionsValidationExtensions {
    /// <summary>
    /// Adds a Jeninnet <see cref="IValidator{T}"/> as an options validator and enables
    /// <c>ValidateOnStart</c>. Register the validator before resolving the options.
    /// </summary>
    /// <typeparam name="T">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddValidatedOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(this IServiceCollection services)
        where T : class {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<T>().AddValidatedOptions();
        return services;
    }

    /// <summary>
    /// Adds a Jeninnet <see cref="IValidator{T}"/> as an options validator and enables
    /// <c>ValidateOnStart</c>.
    /// </summary>
    /// <typeparam name="T">The options type.</typeparam>
    /// <param name="builder">The options builder.</param>
    /// <returns>The same options builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static OptionsBuilder<T> AddValidatedOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(this OptionsBuilder<T> builder)
        where T : class {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<T>, JeninnetOptionsValidator<T>>());

#if NET11_0_OR_GREATER
        // The options infrastructure detects IAsyncValidateOptions<T> on a registered
        // IValidateOptions<T> implementation; registering the async interface directly
        // bypasses the async startup path.
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<T>, JeninnetAsyncOptionsValidator<T>>());
#endif

        builder.ValidateOnStart();

        return builder;
    }
}

/// <summary>
/// Runs the registered Jeninnet validator for an options instance.
/// </summary>
/// <typeparam name="T">The options type.</typeparam>
/// <param name="services">The service provider used to resolve validators.</param>
internal sealed class JeninnetOptionsValidator<T>(IServiceProvider services) : IValidateOptions<T>
    where T : class {
    public ValidateOptionsResult Validate(string? name, T options) =>
        JeninnetOptionsValidation.Validate(services, name, options);
}

internal static class JeninnetOptionsValidation {
    internal static ValidateOptionsResult Validate<T>(
        IServiceProvider services,
        string? name,
        T options)
        where T : class {
        ArgumentNullException.ThrowIfNull(options);

        using var scope = services.CreateScope();
        var validator = scope.ServiceProvider.GetService<IValidator<T>>();

        if(validator is null) {
            return ValidateOptionsResult.Fail(
                $"No IValidator<{typeof(T).Name}> is registered. " +
                "Register the validator before AddValidatedOptions<T>().");
        }

        ValidationResult result;

        try {
            result = validator.Validate(options);
        }
#if NET11_0_OR_GREATER
        catch(AsyncValidatorInvokedSynchronouslyException) {
            // The .NET 11 startup validator runs the synchronous validators first,
            // then the asynchronous ones. Skip the synchronous pass so an async-only
            // validator is not turned into a false startup failure.
            return ValidateOptionsResult.Skip;
        }
#else
        catch(AsyncValidatorInvokedSynchronouslyException exception) {
            return ValidateOptionsResult.Fail(
                $"The validator for options '{typeof(T).Name}' requires the asynchronous path; " +
                "synchronous options validation cannot run it. " +
                $"Use a synchronous validator or .NET 11 async options validation. {exception.Message}");
        }
#endif

        return FromResult(result, name);
    }

#if NET11_0_OR_GREATER
    internal static async Task<ValidateOptionsResult> ValidateAsync<T>(
        IServiceProvider services,
        string? name,
        T options,
        CancellationToken cancellationToken)
        where T : class {
        ArgumentNullException.ThrowIfNull(options);

        using var scope = services.CreateScope();
        var validator = scope.ServiceProvider.GetService<IValidator<T>>();

        if(validator is null) {
            return ValidateOptionsResult.Fail(
                $"No IValidator<{typeof(T).Name}> is registered. " +
                "Register the validator before AddValidatedOptions<T>().");
        }

        var result = await validator.ValidateAsync(options, cancellationToken).ConfigureAwait(false);

        return FromResult(result, name);
    }
#endif

    private static ValidateOptionsResult FromResult(ValidationResult result, string? name) {
        var failures = result.Errors
                             .Where(static error => error.Severity == ValidationErrorSeverity.Error)
                             .Select(error => JeninnetOptionsValidationFormatter.Format(error, name))
                             .ToArray();

        return failures.Length is 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

internal static class JeninnetOptionsValidationFormatter {
    internal static string Format(ValidationError error, string? name) {
        var code = error.ErrorCode ?? error.MessageKey ?? "invalid";
        var message = error.Message ?? error.MessageKey ?? "The value is invalid.";
        var prefix = string.IsNullOrWhiteSpace(name) ? string.Empty : $"{name}: ";

        return $"{prefix}{error.MemberName} [{code}] {message}";
    }
}

#if NET11_0_OR_GREATER
/// <summary>
/// Runs the registered Jeninnet validator through the .NET 11 asynchronous options validation path.
/// </summary>
/// <typeparam name="T">The options type.</typeparam>
/// <param name="services">The service provider used to resolve validators.</param>
internal sealed class JeninnetAsyncOptionsValidator<T>(IServiceProvider services) : IAsyncValidateOptions<T>
    where T : class {
    public ValidateOptionsResult Validate(string? name, T options) =>
        JeninnetOptionsValidation.Validate(services, name, options);

    public Task<ValidateOptionsResult> ValidateAsync(
        string? name,
        T options,
        CancellationToken cancellationToken = default) =>
        JeninnetOptionsValidation.ValidateAsync(services, name, options, cancellationToken);
}
#endif
