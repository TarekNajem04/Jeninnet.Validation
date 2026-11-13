//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Validates a minimal API endpoint argument with the registered <see cref="IValidator{T}"/>.
/// </summary>
/// <typeparam name="T">The request model type.</typeparam>
/// <param name="services">The request service provider.</param>
public sealed class ValidationEndpointFilter<T>(IServiceProvider services) : IEndpointFilter where T : class {
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next) {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var endpoint = context.HttpContext.GetEndpoint();

        if(endpoint?.Metadata.OfType<SkipJeninnetValidationAttribute>().Any() == true ||
           endpoint?.Metadata.OfType<DisableValidationAttribute>().Any() == true) {
            return await next(context).ConfigureAwait(false);
        }

        if(ShouldSkipHandlerParameter(endpoint)) {
            return await next(context).ConfigureAwait(false);
        }

        var validator = services.GetService<IValidator<T>>() ?? ValidationConfiguration.CreateValidator<T>(services);

        if(validator is null) {
            return await next(context).ConfigureAwait(false);
        }

        var model = context.Arguments.OfType<T>().FirstOrDefault();

        if(model is null) {
            return await next(context).ConfigureAwait(false);
        }

        var ruleSets = endpoint?.Metadata.OfType<JeninnetValidationRuleSetsAttribute>().FirstOrDefault()?.RuleSets;
        var interceptors = ValidationInterceptorInvoker.Resolve(services);
        var interceptorContext = new ValidationInterceptorContext(
            model,
            typeof(T),
            validator.GetType().FullName,
            services,
            context.HttpContext,
            endpoint);
        var started = Stopwatch.GetTimestamp();
        var result = interceptors is null
            ? await ValidationInvoker
                .InvokeAsync(validator, model, ruleSets, services, context.HttpContext.RequestAborted)
                .ConfigureAwait(false)
            : await ValidationInterceptorInvoker.InvokeAsync(
                interceptors,
                interceptorContext,
                cancellationToken => ValidationInvoker.InvokeAsync(validator, model, ruleSets, services, cancellationToken),
                context.HttpContext.RequestAborted).ConfigureAwait(false);
        var elapsed = Stopwatch.GetElapsedTime(started);

        if(result.IsValid) {
            return await next(context).ConfigureAwait(false);
        }

        var options = services.GetService<ValidationProblemOptions>();
        var extensions = ValidationProblemExtensions.BuildProblemExtensions(
            context.HttpContext,
            options,
            typeof(T).FullName,
            elapsed);
        var errors = await result.Errors
            .ToProblemErrorsAsync(
                options,
                services.GetService<IMessageLocalizer>(),
                CultureInfo.CurrentUICulture,
                context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        return Results.ValidationProblem(
            errors,
            statusCode: StatusCodes.Status400BadRequest,
            title: options?.Title,
            extensions: extensions);
    }

    private static bool ShouldSkipHandlerParameter(Endpoint? endpoint) {
        if(endpoint?.Metadata.GetMetadata<MethodInfo>() is not { } method) {
            return false;
        }

        var parameters = method.GetParameters();
        var index = Array.FindIndex(
            parameters,
            static parameter => parameter.ParameterType == typeof(T));

        return index >= 0 &&
               parameters[index].GetCustomAttribute<SkipValidationAttribute>(inherit: true) is not null;
    }
}
