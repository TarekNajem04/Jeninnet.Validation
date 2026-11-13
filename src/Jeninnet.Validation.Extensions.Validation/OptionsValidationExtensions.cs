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
    public ValidateOptionsResult Validate(string? name, T options) {
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
        catch(AsyncValidatorInvokedSynchronouslyException exception) {
            return ValidateOptionsResult.Fail(
                $"The validator for options '{typeof(T).Name}' requires the asynchronous path; " +
                "synchronous options validation cannot run it. " +
                $"Use a synchronous validator or .NET 11 async options validation. {exception.Message}");
        }

        var failures = result.Errors
                             .Where(static error => error.Severity == ValidationErrorSeverity.Error)
                             .Select(error => Format(error, name))
                             .ToArray();

        return failures.Length is 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static string Format(ValidationError error, string? name) {
        var code = error.ErrorCode ?? error.MessageKey ?? "invalid";
        var message = error.Message ?? error.MessageKey ?? "The value is invalid.";
        var prefix = string.IsNullOrWhiteSpace(name) ? string.Empty : $"{name}: ";

        return $"{prefix}{error.MemberName} [{code}] {message}";
    }
}
