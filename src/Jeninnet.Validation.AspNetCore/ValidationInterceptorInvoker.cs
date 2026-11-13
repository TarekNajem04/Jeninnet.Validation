//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Resolves and executes validation interceptors for both MVC and minimal APIs.
/// </summary>
internal static class ValidationInterceptorInvoker {
    internal static IReadOnlyList<IValidationInterceptor>? Resolve(IServiceProvider services) {
        if(services.GetService<IServiceProviderIsService>()?.IsService(typeof(IValidationInterceptor)) != true) {
            return null;
        }

        var interceptors = services.GetServices<IValidationInterceptor>().ToArray();
        return interceptors.Length is 0 ? null : interceptors;
    }

    internal static async ValueTask<ValidationResult> InvokeAsync(
        IReadOnlyList<IValidationInterceptor> interceptors,
        ValidationInterceptorContext context,
        Func<CancellationToken, ValueTask<ValidationResult>> validate,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(interceptors);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(validate);

        ValidationResult? shortCircuit = null;

        foreach(var interceptor in interceptors) {
            var current = interceptor;
            shortCircuit = await RunAsync(current, () => current.BeforeValidationAsync(context, cancellationToken))
                .ConfigureAwait(false);

            if(shortCircuit is not null) {
                break;
            }
        }

        var result = shortCircuit ?? await validate(cancellationToken).ConfigureAwait(false);

        foreach(var interceptor in interceptors) {
            var current = interceptor;
            result = await RunAsync(current, () => current.AfterValidationAsync(context, result, cancellationToken))
                .ConfigureAwait(false);
        }

        return result;
    }

    private static async ValueTask<T> RunAsync<T>(
        IValidationInterceptor interceptor,
        Func<ValueTask<T>> action) {
        try {
            return await action().ConfigureAwait(false);
        }
        catch(OperationCanceledException) {
            throw;
        }
        catch(Exception exception) {
            throw new ValidationException(
                $"Validation interceptor '{interceptor.GetType().FullName}' failed.",
                exception);
        }
    }
}
